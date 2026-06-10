using System.Collections.Generic;
using UnityEngine;

namespace Survival
{
    [CreateAssetMenu(menuName = "Survival/Upgrade Catalog", fileName = "UpgradeCatalog")]
    public class UpgradeCatalog : ScriptableObject
    {
        public List<UpgradeDefinition> Upgrades = new();
    }
}
