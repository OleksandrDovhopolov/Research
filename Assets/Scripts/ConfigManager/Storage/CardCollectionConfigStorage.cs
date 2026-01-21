using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace core
{
    public class CardCollectionConfigStorage : ConfigStorageBase<CardCollectionConfigStorage>
    {
        public string DefaultConfigFileName => "cardCollection";
        private string OverrideName => "cardCollection_override";

        public List<CardCollectionConfig> Data { get; } = new();
        
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
        
        public List<CardCollectionConfig> Get(string groupType)
        {
            var configs = Data.Where(config => groupType == config.GroupType).ToList();
            return configs;
        }
        
        public CardCollectionConfig GetById(string cardId)
        {
            var configs = Data.FirstOrDefault(config => cardId == config.Id);
            if (configs == null)
            {
                throw new InvalidOperationException("Failed to find config with id " + cardId);
            }
            
            return configs;
        }
        
        public List<CardCollectionConfig> GetByIds(List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return new List<CardCollectionConfig>();
            }

            var result = Data.Where(config => cardIds.Contains(config.Id)).ToList();
            return result;
        }
    }
}