using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardGroupsConfigStorage : ConfigStorageBase<CardGroupsConfigStorage>
    {
        public string DefaultConfigFileName => "cardGroups"; 
        private string OverrideName => "cardGroups_override";

        private CardGroupsConfig _config;
        private readonly Dictionary<string, CardGroupsConfig> _data = new();
        
        public Dictionary<string, CardGroupsConfig> Data => _data; 

        public CardGroupsConfig Get(string id)
        {
            return _data.GetValueOrDefault(id);
        }
        
        public override void Configurate(ConfigManager configManager)
        {
            configManager.AddConfigFile(DefaultConfigFileName, new ConfigFileMeta(DefaultConfigFileName, OverrideName));
        }

        public override async UniTask LoadConfigData(ConfigManager configManager)
        {
            //var data = await configManager.GetParsedData<CardGroupsConfig>(DefaultConfigFileName);
            var data = configManager.GetParsedJsonData<CardGroupsConfig>(DefaultConfigFileName);

            foreach (var config in data)
            {
                _data.Add(config.Id, config);
                //Debug.Log($"Loading config {config == null}");
                Debug.LogWarning($"Loading config {config.Id}, {config.GroupType}, {config.GroupName}, {config.GroupIcon}");
            }
            
            Debug.LogWarning($"DATA _data {data.Count}");
        }
    }
}