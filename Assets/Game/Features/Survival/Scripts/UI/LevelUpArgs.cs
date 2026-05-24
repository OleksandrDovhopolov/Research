using System;
using System.Collections.Generic;
using UIShared;
using UISystem;

namespace Survival
{
    public class LevelUpArgs : WindowArgs
    {
        public readonly IReadOnlyList<UpgradeDefinition> Choices;
        public readonly Action<UpgradeDefinition> OnSelected;

        public LevelUpArgs(
            IReadOnlyList<UpgradeDefinition> choices,
            Action<UpgradeDefinition> onSelected)
        {
            Choices = choices;
            OnSelected = onSelected;
        }
    }
}
