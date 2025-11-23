using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestConfigContainer", menuName = "Configs/TestConfigContainer")]
public class TestConfigContainerSO : ScriptableObject
{
    public List<TestConfigSOData> Configs = new List<TestConfigSOData>();
}

[System.Serializable]
public class TestConfigSOData
{
    public string Name;    // здесь можно задать любое имя вручную
    public string Id;
    public int Count;
    public List<int> Recipes = new List<int>();
}