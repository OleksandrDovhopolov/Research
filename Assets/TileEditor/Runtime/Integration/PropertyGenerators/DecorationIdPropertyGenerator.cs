using UnityEngine;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    public class DecorationIdPropertyGenerator : MonoBehaviour
    {
        void Awake()
        {
            var stringProperty = gameObject.AddComponent<IntegerProperty>();
            stringProperty.SetPropertyName("DecorationId");

            stringProperty.SetValue(-1);
        }
    }
}