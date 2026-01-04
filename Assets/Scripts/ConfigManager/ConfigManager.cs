using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    public class ConfigManager
    {
        public const string RootConfigPath = "Configs";

        //public static readonly ConfigFileLoader ResourcesLoader = new ResourcesConfigFileLoader(RootConfigPath);
        public static readonly ConfigFileLoader ResourcesLoader = new ResourcesConfigFileLoader("");
        public static readonly ConfigFileLoader LocalLoader =
            new LocalConfigFileLoader(Path.Combine(Application.persistentDataPath, RootConfigPath));

        private readonly List<IConfigStorage> _configStorages = new();

        private readonly Dictionary<string, ConfigFileMeta> _configFiles = new();
        private readonly Dictionary<string, ConfigFileMeta> _configFilesWithOverrides = new();

        public IEnumerable<ConfigFileMeta> GetConfigMetas(bool includeOverrides = false) =>
            includeOverrides ? _configFilesWithOverrides.Values : _configFiles.Values;

        public void AddConfigFile(string id, ConfigFileMeta configFileMeta)
        {
            _configFiles[id] = configFileMeta;

            //Также добавляем конфиги в расширенный словарь для поиска еще и по оверрайдам
            _configFilesWithOverrides[id] = configFileMeta;

            if (configFileMeta.OverrideFileMeta != null)
            {
                _configFilesWithOverrides[configFileMeta.OverrideFileMeta.FileName] = configFileMeta.OverrideFileMeta;
            }
        }

        [CanBeNull]
        public ConfigFileMeta GetConfigFile(string id, bool includeOverrides = false)
        {
            Debug.LogWarning($"_configFiles {_configFiles.Count}");
            Debug.LogWarning($"_configFilesWithOverrides {_configFilesWithOverrides.Count}");
            var targetDict = includeOverrides ? _configFilesWithOverrides : _configFiles;
            return targetDict.GetValueOrDefault(id);
        }

        public async UniTask<List<T>> GetParsedData<T>(string id)
        {
            var configFile = GetConfigFile(id);
            if (configFile == null)
            {
                throw new InvalidOperationException($"Failed to find config file with ID {id}");
            }
            
            return await configFile.GetParsedBinaryData<T>();
            //return await configFile.GetParsedData<T>();
        }

        public List<T> GetParsedJsonData<T>(string id)
        {
            var configFile = GetConfigFile(id);
            if (configFile == null)
            {
                throw new InvalidOperationException($"Failed to find config file with ID {id}");
            }
            
            return configFile.GetParsedData<T>();
        }
        
        public async UniTask ApplyParsedConfigs(List<IConfigStorage> storages = null)
        {
            storages ??= GetAllConfigStorages();

            var loadConfigsTasks = new List<UniTask>();

            foreach (var configStorage in storages)
            {
                loadConfigsTasks.Add(UniTask.Create(async () =>
                {
                    await configStorage.LoadConfigData(this);
                    configStorage.RunPostProcess();
                }));
            }

            await UniTask.WhenAll(loadConfigsTasks);
        }

        public List<IConfigStorage> GetAllConfigStorages()
        {
            if (_configStorages.Count > 0) return _configStorages;

            _configStorages.Add(CardGroupsConfigStorage.Instance);

            return _configStorages;
        }

        public static void SaveConfig(string configName, object fileContent, string fileFormat)
        {
            var savePath = LocalLoader.GetPath(configName, fileFormat);

            var directory = Path.GetDirectoryName(savePath);

            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            switch (fileContent)
            {
                case string json:
                    File.WriteAllText(savePath, json);
                    break;
                case byte[] bytes:
                    File.WriteAllBytes(savePath, bytes);
                    break;
            }
        }
    }
}