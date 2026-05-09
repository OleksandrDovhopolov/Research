using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Infrastructure
{
    public sealed class LocalDiskSaveDebugMirror : ISaveDebugMirror
    {
        private const string DefaultFileName = "global_save.debug.json";

        private readonly string _filePath;
        private readonly string _backupPath;
        private readonly SemaphoreSlim _ioSemaphore = new(1, 1);

        public LocalDiskSaveDebugMirror(string fileName = DefaultFileName)
        {
            var rootPath = Application.persistentDataPath;
            _filePath = Path.Combine(rootPath, fileName);
            _backupPath = _filePath + ".bak";
            Debug.Log($"[LocalDiskSaveDebugMirror] Enabled. Path={_filePath}");
        }

        public async UniTask WriteAsync(string json, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = _filePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json ?? string.Empty, Encoding.UTF8, cancellationToken);

                if (File.Exists(_filePath))
                {
                    File.Replace(tempPath, _filePath, _backupPath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(tempPath, _filePath);
                }
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                if (File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }
    }
}
