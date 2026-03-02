using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Infrastructure
{
    public class LocalConfigFileLoader : ConfigFileLoader
    {
        public LocalConfigFileLoader(string pathRoot) : base(pathRoot)
        {
        }

        public override Task<string> LoadJsonFile(string file)
        {
            var path = GetPath(file, FileFormatJson);
            return File.ReadAllTextAsync(path);
        }
        
        public override UniTask<List<T>> LoadBinaryFile<T>(string file)
        {
            var path = GetPath(file, FileFormatBinary);

            return UniTask.RunOnThreadPool(async () =>
            {
                var bytes = await File.ReadAllBytesAsync(path);
                return await BinarySaverBytes.LoadDataAsync<List<T>>(bytes);
            });
        }
        
        public override void RemoveFile(string fileName)
        {
            Debug.LogWarning($"File will be removed and updated from server");
            var path = GetPath(fileName, FileFormatBinary);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            path = GetPath(fileName, FileFormatJson);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
