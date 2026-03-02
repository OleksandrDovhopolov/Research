using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public abstract class ConfigStorageBase<T> : IConfigStorage where T : class, new()
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }

        public abstract void Configurate(ConfigManager configManager);

        public abstract UniTask LoadConfigData(ConfigManager configManager);

        public virtual void RunPostProcess(){}

        protected static void ApplyPostProcess<TC>(List<TC> configsList) where TC : ConfigBase
        {
            foreach (var item in configsList)
            {
                item.PostProcess();
            }
        }

        protected string GetOverrideName(string configName)
        {
            return configName + "_override";
        }

        public void Clear()
        {
            _instance = null;
        }
    }
}
