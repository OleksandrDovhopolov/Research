using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "JsonSaveStrategy", menuName = "CsvLoader/Save Strategies/Json")]
    public class JsonSaveStrategy : BaseCsvSaveStrategy
    {
        public override void SaveToFile(string saveFolder, string fileName, string typeName, IList<IList<string>> data)
        {
            var parsedSheet = ParseSheetByTypeName(data, typeName);
            Save(saveFolder, fileName, parsedSheet);
        }
        
        private void Save(string saveFolder, string fileName, IList data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            
            var savePath = Path.Combine(Application.dataPath, saveFolder);
            
            savePath = Path.Combine(savePath, $"{fileName}.json");
            
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            
            File.WriteAllText(savePath, json);
        }
    }
}

