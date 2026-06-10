using UnityEngine;

namespace Survival
{
    [CreateAssetMenu(menuName = "Survival/Upgrade Definition", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;
        public UpgradeType Type;
        public float Value;
    }
}
