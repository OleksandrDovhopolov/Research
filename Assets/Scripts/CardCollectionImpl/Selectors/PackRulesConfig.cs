using System.Collections.Generic;

namespace core
{
    /// <summary>
    /// Configuration helper for pack-specific rules.
    /// </summary>
    public static class PackRulesConfig
    {
        /// <summary>
        /// Creates default pack rules configuration.
        /// </summary>
        public static Dictionary<string, PackRule> CreateDefaultRules()
        {
            var rules = new Dictionary<string, PackRule>();

            // Sapphire_Pack rule
            rules["Sapphire_Pack"] = new PackRule
            {
                PackId = "Sapphire_Pack",
                MinCardsWith3PlusStars = 1, // At least one card must be 3+ stars
                HasMissingCardBoost = true,
                MissingCardBoostPercentages = new float[] { 33f, 66f, 100f } // 3rd, 4th, 5th+ pack
            };

            return rules;
        }
    }
}
