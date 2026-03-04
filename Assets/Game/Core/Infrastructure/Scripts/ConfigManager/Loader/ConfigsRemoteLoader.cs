using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Infrastructure
{
    public static class ConfigsRemoteLoader
    {
        private static ConfigManager _managerToUpdate;
        private const float TimeoutStartDownload = 8f;
        private const float TimeoutFinishDownload = 15f;
        
        public static async UniTask TryApplyRemoteConfigs()
        {
            if (_managerToUpdate == null) return;

            Debug.Log($"[Remote Leader] Apply New Configs");
            
            await _managerToUpdate.ApplyParsedConfigs();
            
            _managerToUpdate = null;
        }
    }
}

