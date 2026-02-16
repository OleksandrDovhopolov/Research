using System;

namespace core
{
    /// <summary>
    /// Defines pack-specific rules for card selection.
    /// </summary>
    [Serializable]
    public class PackRule
    {
        /// <summary>
        /// Pack ID this rule applies to.
        /// </summary>
        public string PackId { get; set; }

        /// <summary>
        /// Minimum number of cards that must be 3+ stars.
        /// </summary>
        public int MinCardsWith3PlusStars { get; set; }

        /// <summary>
        /// Whether this pack supports missing card boost.
        /// </summary>
        public bool HasMissingCardBoost { get; set; }

        /// <summary>
        /// Missing card boost percentages for consecutive packs without missing cards.
        /// Index 0 = 3rd pack (33%), Index 1 = 4th pack (66%), Index 2 = 5th+ pack (100%).
        /// </summary>
        public float[] MissingCardBoostPercentages { get; set; } = { 33f, 66f, 100f };
    }
}
