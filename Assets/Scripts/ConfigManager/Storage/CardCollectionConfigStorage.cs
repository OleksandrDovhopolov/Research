using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionConfigStorage : ConfigStorageBase<CardCollectionConfigStorage>
    {
        public string DefaultConfigFileName => "cardCollection";
        private string OverrideName => "cardCollection_override";

        public List<CardCollectionConfig> Data { get; } = new();

        public List<CardCollectionConfig> Get(string groupType)
        {
            var configs = Data.Where(config => groupType == config.GroupType).ToList();
            return configs;
        }
        
        public override void Configurate(ConfigManager configManager)
        {
            configManager.AddConfigFile(DefaultConfigFileName, new ConfigFileMeta(DefaultConfigFileName, OverrideName));
        }

        public override UniTask LoadConfigData(ConfigManager configManager)
        {
            var data = configManager.GetParsedJsonData<CardCollectionConfig>(DefaultConfigFileName);

            foreach (var config in data)
            {
                Data.Add(config);
            }

            return UniTask.CompletedTask;
        }
    }
}