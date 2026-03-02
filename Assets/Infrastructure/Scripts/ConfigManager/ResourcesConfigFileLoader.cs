using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Infrastructure
{
    class ResourcesConfigFileLoader : ConfigFileLoader
    {
        private const string ConfigFolder = "Resources";
        
        public ResourcesConfigFileLoader(string pathRoot) : base(pathRoot)
        {
        }

        public override bool IsLoaderValid(string fileName)
        {
            return Resources.Load(GetPath(fileName, FileFormatEmpty), TypeOf<TextAsset>.Raw) != null;
        }

        public override async Task<string> LoadJsonFile(string file)
        {
            var loadOperation = Resources.Load<TextAsset>(GetPath(file, FileFormatEmpty));
            return loadOperation?.text ?? "";
        }

        public override async Task SaveJsonFile(string fileName, object serializeData)
        {
            var json = JsonConvert.SerializeObject(serializeData);
            
            var savePath = Path.Combine(Application.dataPath, ConfigFolder, string.Format(FileFormatJson, fileName));

            var directory = Path.GetDirectoryName(savePath);

            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(savePath, json);
        }

        public override async UniTask<List<T>> LoadBinaryFile<T>(string file)
        {
            var loadOperation = Resources.Load<TextAsset>(GetPath(file, FileFormatEmpty));
            return BinarySaverBytes.LoadData<List<T>>(loadOperation.bytes);
        }

        public override void RemoveFile(string fileName)
        {
            Debug.LogError("File stored in resources have error please check file, or ignore if file missed");
        }
    }
}
