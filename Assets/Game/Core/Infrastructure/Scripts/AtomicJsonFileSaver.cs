using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Infrastructure
{
    public sealed class AtomicJsonFileSaver
    {
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

        public UniTask InitializeAsync(
            string rootPath,
            CancellationToken cancellationToken,
            string operationContext = "AtomicJsonFileSaver")
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or empty.", nameof(rootPath));
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                return UniTask.CompletedTask;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{operationContext}] Failed to initialize directory at path: {rootPath}. Exception: {e}");
                throw;
            }
        }

        public async UniTask<T> LoadAsync<T>(
            string path,
            CancellationToken cancellationToken,
            string operationContext = "AtomicJsonFileSaver")
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await _fileSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(path))
                {
                    return default;
                }

                var json = await File.ReadAllTextAsync(path, cancellationToken);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{operationContext}] Failed to load JSON file at path: {path}. Exception: {e}");
                return default;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async UniTask SaveAsync<T>(
            string path,
            T data,
            CancellationToken cancellationToken,
            string operationContext = "AtomicJsonFileSaver",
            Formatting formatting = Formatting.Indented)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();
            await _fileSemaphore.WaitAsync(cancellationToken);

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = path + ".tmp";
                var json = JsonConvert.SerializeObject(data, formatting);
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);

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
                var tempPath = path + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                Debug.LogError($"[{operationContext}] Failed to save JSON file at path: {path}. Exception: {e}");
                throw;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }
    }
}
