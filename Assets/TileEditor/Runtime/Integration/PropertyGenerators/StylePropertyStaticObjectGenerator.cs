using UnityEngine;

namespace MergeMansion.TileEditor.PropertyGenerators
{
    public class StylePropertyStaticObjectGenerator : BaseStylePropertyGenerator
    {
        [SerializeField] protected GameObject[] _styleObjects;
        
        protected override void ChangeStyle(int styleId)
        {
            for (var i = 0; i < _styleObjects.Length; i++)
            {
                _styleObjects[i].SetActive(i == styleId);
            }
        }
    }
}