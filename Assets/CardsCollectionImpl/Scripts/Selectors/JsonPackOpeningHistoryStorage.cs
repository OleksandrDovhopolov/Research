using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public class JsonPackOpeningHistoryStorage : IDisposable
    {
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
        private readonly string _directoryName;
        private readonly string _fileName;
        private string _rootPath;
        private bool _disposed;
        
        public JsonPackOpeningHistoryStorage(
            string directoryName = "pack_opening_history",
            string fileName = "pack_opening_history.json")
        {
            _directoryName = string.IsNullOrWhiteSpace(directoryName) ? "pack_opening_history" : directoryName;
            _fileName = string.IsNullOrWhiteSpace(fileName) ? "pack_opening_history.json" : fileName;
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _rootPath = Path.Combine(Application.persistentDataPath, _directoryName);
            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Directory.CreateDirectory(_rootPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonPackOpeningHistoryStorage] Failed to initialize directory: {e}");
                throw;
            }

            return UniTask.CompletedTask;
        }

        public async UniTask<PackOpeningHistorySaveData> LoadAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                return new PackOpeningHistorySaveData();
            }

            await _fileSemaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(path, ct);
                var data = JsonConvert.DeserializeObject<PackOpeningHistorySaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[JsonPackOpeningHistoryStorage] Failed to deserialize data. Falling back to defaults.");
                    return new PackOpeningHistorySaveData();
                }

                return data;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonPackOpeningHistoryStorage] Load failed: {e}");
                return new PackOpeningHistorySaveData();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async UniTask SaveAsync(PackOpeningHistorySaveData data, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var path = GetFilePath();
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
                Debug.LogError($"[JsonPackOpeningHistoryStorage] Save failed: {e}");
                throw;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _fileSemaphore.Dispose();
        }

        private string GetFilePath()
        {
            if (string.IsNullOrEmpty(_rootPath))
            {
                throw new InvalidOperationException("Storage is not initialized. Call InitializeAsync first.");
            }

            return Path.Combine(_rootPath, _fileName);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JsonPackOpeningHistoryStorage));
            }
        }
    }
}
