using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public abstract class BasePropertyEditor : MonoBehaviour
    {
        [SerializeField] protected Text _fieldName;
        public abstract object GetValue();
    }
}
