using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    public class JsonResourcesStorage : IDisposable
    {
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
        private string _rootPath;
        private bool _disposed;

        public UniTask InitializeAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _rootPath = Path.Combine(Application.persistentDataPath, "resources_data");
            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Directory.CreateDirectory(_rootPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonResourcesStorage] Failed to initialize directory: {e}");
                throw;
            }

            return UniTask.CompletedTask;
        }

        public async UniTask<ResourcesSaveData> LoadAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                return new ResourcesSaveData();
            }

            await _fileSemaphore.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(path, ct);
                var data = JsonConvert.DeserializeObject<ResourcesSaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[JsonResourcesStorage] Failed to deserialize data. Falling back to defaults.");
                    return new ResourcesSaveData();
                }

                return data;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonResourcesStorage] Load failed: {e}");
                return new ResourcesSaveData();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async UniTask SaveAsync(ResourcesSaveData data, CancellationToken ct)
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
                Debug.LogError($"[JsonResourcesStorage] Save failed: {e}");
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

            return Path.Combine(_rootPath, "resources.json");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JsonResourcesStorage));
            }
        }
    }
}
