using Fabros.TileEditor;
using UnityEngine;

namespace MergeMansion.TileEditor.PropertyGenerators
{
    public abstract class BaseStylePropertyGenerator : MonoBehaviour
    {
        protected virtual void Awake()
        {
            var enumProperty = gameObject.AddComponent<EnumProperty>();
            enumProperty.SetPropertyName("Style");
            enumProperty.SetEnumValuesNames(new []{"Standart", "Artdeco", "Classic", "Scandi"});
            enumProperty.onValueChangeEvent.AddListener(ChangeStyle);

            enumProperty.SetValue(0);
        }

        protected abstract void ChangeStyle(int styleId);
    }
}