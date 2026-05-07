using UnityEngine;

namespace Fabros.TileEditor
{
    public class ActivateGameObjectPropertyHelper : EnumPropertyHelper
    {
        [SerializeField] private GameObject[] _gameObjects = default;

        protected override void OnValueChange(int id)
        {
            for(int i = 0; i < _gameObjects.Length; i++)
            {
                _gameObjects[i].SetActive(i == id);
            }
        }
    }
}
