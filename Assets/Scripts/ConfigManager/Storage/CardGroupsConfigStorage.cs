using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public class CardGroupsConfigStorage : ConfigStorageBase<CardGroupsConfigStorage>
    {
        public string DefaultConfigFileName => "cardGroups"; 
        private string OverrideName => "cardGroups_override";

        private CardGroupsConfig _config;

        //public Dictionary<string, CardGroupsConfig> Data { get; } = new();
        public List<CardGroupsConfig> Data { get; } = new();

        public CardGroupsConfig Get(string id)
        {
            return Data.Find(config => id == config.Id);
        }
        
        public override void Configurate(ConfigManager configManager)
        {
            configManager.AddConfigFile(DefaultConfigFileName, new ConfigFileMeta(DefaultConfigFileName, OverrideName));
        }

        public override UniTask LoadConfigData(ConfigManager configManager)
        {
            var data = configManager.GetParsedJsonData<CardGroupsConfig>(DefaultConfigFileName);
            
            foreach (var config in data)
            {
                Data.Add(config);
            }

            return UniTask.CompletedTask;
        }
    }
}