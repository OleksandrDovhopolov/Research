using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Crafting
{
    public static class CraftingConfigPaths
    {
        public const string RootFolder = "Fishing";
        public const string Recipes = "crafting_recipes.json";
    }

    [Serializable]
    public sealed class CraftingRecipesRoot
    {
        [JsonProperty("crafting_recipes")] public List<CraftingRecipeConfig> CraftingRecipes = new();
    }

    [Serializable]
    public sealed class CraftingRecipeConfig
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("station_id")] public string StationId;
        [JsonProperty("output_item_id")] public string OutputItemId;
        [JsonProperty("output_count")] public int OutputCount;
        [JsonProperty("craft_time_seconds")] public int CraftTimeSeconds;
        [JsonProperty("ingredients")] public List<CraftingIngredientConfig> Ingredients = new();
        [JsonProperty("requirements")] public List<CraftingRequirementConfig> Requirements = new();
        [JsonProperty("is_enabled")] public bool IsEnabled;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class CraftingIngredientConfig
    {
        [JsonProperty("item_id")] public string ItemId;
        [JsonProperty("count")] public int Count;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class CraftingRequirementConfig
    {
        [JsonProperty("type")] public string Type;
        [JsonProperty("value")] public JToken Value;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    public sealed class CraftingStaticData
    {
        public CraftingStaticData(IReadOnlyList<CraftingRecipeConfig> recipes)
        {
            Recipes = recipes ?? Array.Empty<CraftingRecipeConfig>();
            var byId = new Dictionary<string, CraftingRecipeConfig>(StringComparer.Ordinal);
            for (var i = 0; i < Recipes.Count; i++)
            {
                var recipe = Recipes[i];
                if (recipe != null && !string.IsNullOrWhiteSpace(recipe.Id))
                    byId[recipe.Id] = recipe;
            }

            RecipesById = byId;
        }

        public IReadOnlyList<CraftingRecipeConfig> Recipes { get; }
        public IReadOnlyDictionary<string, CraftingRecipeConfig> RecipesById { get; }
    }
}
