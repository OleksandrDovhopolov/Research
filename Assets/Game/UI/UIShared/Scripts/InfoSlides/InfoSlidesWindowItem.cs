using TMPro;
using UnityEngine;

namespace UIShared
{
    public class InfoSlidesWindowItem : MonoBehaviour
    {
        [field: SerializeField] public Transform Placeholder { get; private set; }
        [field: SerializeField] public TextMeshProUGUI Text { get; private set; }
    }
}