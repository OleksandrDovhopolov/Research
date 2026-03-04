using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public class JsonEventCardsStorage : IEventCardsStorage
    {
        private string _rootPath;
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Regex InvalidFileNameChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
        private bool _disposed;

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _rootPath = Path.Combine(Application.persistentDataPath, "event_cards");

            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Directory.CreateDirectory(_rootPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonEventCardsStorage] Failed to initialize directory: {e}");
                throw;
            }

            return UniTask.CompletedTask;
        }

        public async UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
        {
            ValidateEventId(eventId);

            var path = GetFilePath(eventId);

            if (!File.Exists(path))
            {
                return new EventCardsSaveData { EventId = eventId, Version = 1 };
            }

            await _fileSemaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(path, ct);
                var data = JsonConvert.DeserializeObject<EventCardsSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning($"[JsonEventCardsStorage] Failed to deserialize data for event {eventId}");
                    return new EventCardsSaveData { EventId = eventId, Version = 1 };
                }

                if (string.IsNullOrEmpty(data.EventId))
                {
                    data.EventId = eventId;
                }

                return data;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonEventCardsStorage] Load failed for event {eventId}: {e}");
                return new EventCardsSaveData { EventId = eventId, Version = 1 };
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
        {
            if (data == null)
            {
                Debug.LogError("[JsonEventCardsStorage] SaveAsync called with null data");
                throw new ArgumentNullException(nameof(data));
            }

            ValidateEventId(data.EventId);

            var path = GetFilePath(data.EventId);

            await _fileSemaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                var tempPath = path + ".tmp";
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(tempPath, json, ct);
                
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonEventCardsStorage] Save failed for event {data.EventId}: {e}");
                throw;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            if (cardIds == null || cardIds.Count == 0)
                return;

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            ValidateEventId(data.EventId);

            var cardDict = data.Cards.ToDictionary(c => c.CardId, c => c);

            foreach (var cardId in cardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    Debug.LogWarning($"[JsonEventCardsStorage] Skipping null or empty cardId for event {data.EventId}");
                    continue;
                }

                if (cardDict.TryGetValue(cardId, out var existingCard))
                {
                    existingCard.IsUnlocked = true;
                    existingCard.IsNew = true;
                }
            }

            await SaveAsync(data, ct);
        }
        
        public async UniTask ClearCollectionAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_rootPath))
            {
                Debug.LogWarning("[JsonEventCardsStorage] ClearCollectionAsync called before InitializeAsync");
                return;
            }

            if (!Directory.Exists(_rootPath))
            {
                return;
            }

            await _fileSemaphore.WaitAsync(ct);
            try
            {
                var files = Directory.GetFiles(_rootPath, "*.json");
                
                var deleteTasks = files.Select(file => 
                    UniTask.RunOnThreadPool(() => 
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[JsonEventCardsStorage] Failed to delete file {file}: {e}");
                        }
                    }, cancellationToken: ct)
                );

                await UniTask.WhenAll(deleteTasks);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonEventCardsStorage] ClearCollectionAsync failed: {e}");
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private string GetFilePath(string eventId)
        {
            var sanitizedEventId = InvalidFileNameChars.Replace(eventId, "_");
            return Path.Combine(_rootPath, $"event_{sanitizedEventId}.json");
        }

        private static void ValidateEventId(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _fileSemaphore.Dispose();
        }
    }
}
