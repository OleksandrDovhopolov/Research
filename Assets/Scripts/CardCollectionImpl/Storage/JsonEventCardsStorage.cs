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

namespace core
{
    /// <summary>
    /// JSON-based storage implementation for event cards data.
    /// Stores event card progress data in JSON files on the local filesystem.
    /// </summary>
    public class JsonEventCardsStorage : IEventCardsStorage
    {
        private string _rootPath;
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Regex InvalidFileNameChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
        private bool _disposed;

        /// <summary>
        /// Initializes the storage by creating the root directory if it doesn't exist.
        /// </summary>
        public UniTask InitializeAsync()
        {
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

        /// <summary>
        /// Loads event cards data for the specified event ID.
        /// Returns a new instance with default values if the file doesn't exist or deserialization fails.
        /// </summary>
        /// <param name="eventId">The event identifier. Must not be null or empty.</param>
        /// <returns>The loaded event cards data, or a new instance if loading fails.</returns>
        public async UniTask<EventCardsSaveData> LoadAsync(string eventId)
        {
            ValidateEventId(eventId);

            var path = GetFilePath(eventId);

            if (!File.Exists(path))
            {
                return new EventCardsSaveData { EventId = eventId, Version = 1 };
            }

            await _fileSemaphore.WaitAsync();
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var data = JsonConvert.DeserializeObject<EventCardsSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning($"[JsonEventCardsStorage] Failed to deserialize data for event {eventId}");
                    return new EventCardsSaveData { EventId = eventId, Version = 1 };
                }

                // Ensure EventId matches (in case of file corruption or manual editing)
                if (string.IsNullOrEmpty(data.EventId))
                {
                    data.EventId = eventId;
                }

                return data;
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

        /// <summary>
        /// Saves event cards data to disk.
        /// </summary>
        /// <param name="data">The event cards data to save. Must not be null.</param>
        public async UniTask SaveAsync(EventCardsSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[JsonEventCardsStorage] SaveAsync called with null data");
                throw new ArgumentNullException(nameof(data));
            }

            ValidateEventId(data.EventId);

            var path = GetFilePath(data.EventId);

            await _fileSemaphore.WaitAsync();
            try
            {
                // Write to temporary file first, then rename (atomic operation)
                var tempPath = path + ".tmp";
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(tempPath, json);
                
                // Atomic replace
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
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

        /// <summary>
        /// Unlocks the specified cards for an event.
        /// </summary>
        /// <param name="eventId">The event identifier. Must not be null or empty.</param>
        /// <param name="cardIds">Collection of card IDs to unlock. Can be null or empty.</param>
        public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
                return;

            ValidateEventId(eventId);

            var data = await LoadAsync(eventId);

            var cardDict = data.Cards.ToDictionary(c => c.CardId, c => c);

            foreach (var cardId in cardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    Debug.LogWarning($"[JsonEventCardsStorage] Skipping null or empty cardId for event {eventId}");
                    continue;
                }

                if (cardDict.TryGetValue(cardId, out var existingCard))
                {
                    existingCard.IsUnlocked = true;
                    existingCard.IsNew = true;
                }
            }

            await SaveAsync(data);
        }

        /// <summary>
        /// Clears all event card data files from the storage directory.
        /// </summary>
        public async UniTask ClearCollectionAsync()
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

            await _fileSemaphore.WaitAsync();
            try
            {
                var files = Directory.GetFiles(_rootPath, "*.json");
                
                // Delete files asynchronously on thread pool
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
                    })
                );

                await UniTask.WhenAll(deleteTasks);
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

        /// <summary>
        /// Gets the file path for the specified event ID.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>The full file path for the event's JSON file.</returns>
        private string GetFilePath(string eventId)
        {
            // Sanitize eventId to prevent directory traversal attacks
            var sanitizedEventId = InvalidFileNameChars.Replace(eventId, "_");
            return Path.Combine(_rootPath, $"event_{sanitizedEventId}.json");
        }

        /// <summary>
        /// Validates that the event ID is not null or empty.
        /// </summary>
        /// <param name="eventId">The event identifier to validate.</param>
        /// <exception cref="ArgumentException">Thrown if eventId is null or empty.</exception>
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