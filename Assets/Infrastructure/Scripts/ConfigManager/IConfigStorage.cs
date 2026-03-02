using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public interface IConfigStorage
    {
        void Configurate(ConfigManager configManager);
        UniTask LoadConfigData(ConfigManager configManager);

        public void RunPostProcess()
        {
        }

        public void Clear()
        {
        }

        public static string GetOverrideName(string configName)
        {
            return configName + "_override";
        }

        public void SetInstance(IConfigStorage instance)
        {
        }

        public static void ApplyPostProcess<TC>(IEnumerable<TC> configs) where TC : ConfigBase
        {
            foreach (var config in configs)
            {
                config.PostProcess();
            }
        }
    }
}
