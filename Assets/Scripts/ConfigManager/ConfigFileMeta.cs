using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace core
{
    /// <summary>
    /// Config file representation class
    /// </summary>
    public class ConfigFileMeta
    {
        public string FileName { get; }
        
        public ConfigFileLoader CurLoader { get; set; }
        
        public ConfigFileMeta OverrideFileMeta { get; }

        /// <summary>
        /// Текущая наибольшая из локальных версия
        /// </summary>
        public int CurrentLocalVersion { get; set; } = -1;
        
        private string _fileJsonData;
        private byte[] _binaryData;

        /// <summary>
        /// Создание конфиг мета данных
        /// </summary>
        /// <param name="fileName">имя json файла</param>
        /// <param name="overrideFirebaseParam">имя параметра из файрбейз для оверрайда а/б</param>
        public ConfigFileMeta(string fileName, string overrideFirebaseParam = "")
        {
            FileName = fileName;
            
            //Пытаемся инициализировать мета файрбейз оверрайда
            // if (!string.IsNullOrEmpty(overrideFirebaseParam) && AbConfigs.TryGetConfig(overrideFirebaseParam, out var configValue))
            // {
            //     OverrideFileMeta = new ConfigFileMeta(configValue.StringValue);
            // }
        }

        public List<T> GetParsedData<T>()
        {
            try
            {
                //Если корректно настроен оверрайд то он парсится в приоритете
                //if(OverrideFileMeta != null && OverrideFileMeta.CurLoader != null) return OverrideFileMeta.GetParsedData<T>();
                
                var jsonFile = Resources.Load<TextAsset>(FileName);
                return JsonConvert.DeserializeObject<List<T>>(jsonFile.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error while parsing file {FileName} from {CurLoader.GetType()} \n{e}");
                CurLoader.RemoveFile(FileName);
                throw;
            }
        }
        
        public async UniTask<List<T>> GetParsedBinaryData<T>()
        {
            try
            {
                //Если корректно настроен оверрайд то он парсится в приоритете
                if(OverrideFileMeta != null && OverrideFileMeta.CurLoader != null) 
                    return await OverrideFileMeta.GetParsedBinaryData<T>();

                return await CurLoader.LoadBinaryFile<T>(FileName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error while parsing file {FileName} from {CurLoader.GetType()} \n{e}");
                CurLoader.RemoveFile(FileName);
                throw;
            }
        }
    }
}