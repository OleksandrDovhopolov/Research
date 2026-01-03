using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace core
{
    [Serializable]
    public abstract class ConfigBase
    {
        [CanBeNull] private List<object> _jsonModels;
        [NonSerialized] private CancellationTokenSource _jsonPreloadCancellationToken;
        private List<string> _jsonModelsBuffer;

        private Dictionary<string, ConfigParam> _params;

        public virtual void PostProcess()
        {
        }

        protected virtual string GetConfigInfo()
        {
            return "";
        }

        #region Nested Jsons Logic

        protected void AddJsonModel(string jsonModel)
        {
            if (string.IsNullOrEmpty(jsonModel)) return;

            _jsonModels ??= new List<object>();
            _jsonModelsBuffer ??= new List<string>();

            _jsonModelsBuffer.Add(jsonModel);
        }

        private void TryParseJsonModels()
        {
            if (_jsonModelsBuffer == null || _jsonModelsBuffer.Count == 0 || _jsonModels == null) return;

            _jsonPreloadCancellationToken?.Cancel();

            _jsonModels.Clear();

            for (int i = 0; i < _jsonModelsBuffer.Count; i++)
            {
                try
                {
                    var nestedJsonParsed = ConfigSerializer.DeserializeObject(_jsonModelsBuffer[i]);

                    if (nestedJsonParsed == null)
                    {
                        Debug.LogError($"Cant parse nested json for {this.GetType()}");
                        continue;
                    }

                    _jsonModels.Add(nestedJsonParsed);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{GetConfigInfo()}\n{e}");
                }
            }

            _jsonModelsBuffer.Clear();
        }

        public async void PreloadJsonModel()
        {
            if (_jsonModelsBuffer == null || _jsonModelsBuffer.Count == 0 || _jsonModels == null) return;

            _jsonPreloadCancellationToken = new CancellationTokenSource();

            for (var i = 0; i < _jsonModelsBuffer.Count; i++)
            {
                try
                {
                    var nestedJsonParsed = await ConfigSerializer.DeserializeObjectAsync(_jsonModelsBuffer[i],
                        _jsonPreloadCancellationToken.Token);

                    if (_jsonPreloadCancellationToken.IsCancellationRequested)
                    {
                        _jsonPreloadCancellationToken = null;
                        return;
                    }

                    if (nestedJsonParsed == null)
                    {
                        Debug.LogError($"Cant parse nested json for {this.GetType()}");
                        continue;
                    }

                    _jsonModels.Add(nestedJsonParsed);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{ToString()}\n{e}");
                }
            }

            _jsonModelsBuffer.Clear();
            _jsonPreloadCancellationToken = null;
        }

        public IEnumerable<object> GetAllNestedObjects()
        {
            TryParseJsonModels();
            return _jsonModels?.ToList() ?? new List<object>();
        }

        [CanBeNull]
        public T GetNestedData<T>() where T : class
        {
            if (_jsonModels == null) return null;

            TryParseJsonModels();

            foreach (object nestedJson in _jsonModels)
            {
                if (nestedJson is T json)
                {
                    return json;
                }
            }

            return null;
        }

        public bool TryGetJsonModel<T>(out T jsonModel) where T : class
        {
            jsonModel = GetNestedData<T>();
            return jsonModel != null;
        }

        #endregion

        #region Params

        protected void AddParam(string paramName, string paramValue)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            _params ??= new Dictionary<string, ConfigParam>();
            _params[paramName] = new ConfigParam(paramName, paramValue);
        }

        public bool TryGetParam(string key, out ConfigParam param)
        {
            param = GetParam(key);
            return !(param is null);
        }

        public ConfigParam GetParam(string key) => _params?.GetSafe(key);

        public bool HaveParam(string param) => _params != null && _params.ContainsKey(param);

        #endregion
    }
}