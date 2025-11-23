using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using core; // твой namespace с TestConfig

public class JsonToSO
{
    [MenuItem("Tools/Create TestConfig Container SO from JSON")]
    public static void CreateSOContainer()
    {
        string jsonPath = "Assets/Resources/test.json";

        if (!File.Exists(jsonPath))
        {
            Debug.LogError("JSON file not found at: " + jsonPath);
            return;
        }

        string jsonText = File.ReadAllText(jsonPath);

        // Десериализация JSON массива в список TestConfig
        var configs = JsonConvert.DeserializeObject<List<TestConfig>>(jsonText);

        // Создаем контейнер
        TestConfigContainerSO container = ScriptableObject.CreateInstance<TestConfigContainerSO>();

        foreach (var config in configs)
        {
            TestConfigSOData data = new TestConfigSOData
            {
                Id = config.Id,
                Count = config.Count,
                Recipes = config.Recipes
            };
            container.Configs.Add(data);
        }

        // Сохраняем в Assets
        string assetPath = "Assets/Resources/TestConfigContainerSO.asset";
        AssetDatabase.CreateAsset(container, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"TestConfigContainerSO created with {configs.Count} configs at: {assetPath}");
    }
}