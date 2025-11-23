using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestConfigSO", menuName = "Configs/TestConfigSO")]
public class TestConfigSO : ScriptableObject
{
    public string Id;
    public int Count;
    public List<int> Recipes = new List<int>();
}