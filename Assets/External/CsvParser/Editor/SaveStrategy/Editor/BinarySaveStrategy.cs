using System.Collections.Generic;
using System.IO;
using Infrastructure;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "BinarySaveStrategy", menuName = "CsvLoader/Save Strategies/Binary")]
    public class BinarySaveStrategy : BaseCsvSaveStrategy
    {
        public override void SaveToFile(string saveFolder, string fileName, string typeName, IList<IList<string>> data)
        {
            var saveData = BinarySaverBytes.SaveData(data);
            var savePath = Path.Combine(Application.dataPath, saveFolder);
            
            savePath = Path.Combine(savePath, $"{fileName}.bytes");
            
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            File.WriteAllBytes(savePath, saveData);
        }
    }
}