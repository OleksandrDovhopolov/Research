using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Crafting
{
    public interface ICraftingConfigContentSource
    {
        UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct);
    }

    public interface ICraftingConfigProvider
    {
        UniTask<CraftingStaticData> LoadAsync(CancellationToken ct);
        void ClearCache();
    }

    public sealed class StreamingAssetsCraftingConfigContentSource : ICraftingConfigContentSource
    {
        private readonly string _rootFolder;

        public StreamingAssetsCraftingConfigContentSource(string rootFolder = CraftingConfigPaths.RootFolder)
        {
            _rootFolder = string.IsNullOrWhiteSpace(rootFolder) ? CraftingConfigPaths.RootFolder : rootFolder;
        }

        public async UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Config relative path is empty.", nameof(relativePath));

            var fullPath = Path.Combine(Application.streamingAssetsPath, _rootFolder, relativePath);
            if (Application.platform == RuntimePlatform.Android)
            {
                using var request = UnityWebRequest.Get(fullPath);
                await request.SendWebRequest().WithCancellation(ct);

                if (request.result != UnityWebRequest.Result.Success)
                    throw new FileNotFoundException($"Crafting config file not found via WebRequest: {fullPath}. Error: {request.error}");

                return request.downloadHandler.text;
            }

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Crafting config file not found: {fullPath}");

            return await File.ReadAllTextAsync(fullPath, ct);
        }
    }

    public sealed class JsonCraftingConfigProvider : ICraftingConfigProvider
    {
        private readonly ICraftingConfigContentSource _contentSource;
        private readonly CraftingConfigValidator _validator;
        private CraftingStaticData _cachedData;

        public JsonCraftingConfigProvider(ICraftingConfigContentSource contentSource, CraftingConfigValidator validator)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async UniTask<CraftingStaticData> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_cachedData != null)
                return _cachedData;

            var json = await _contentSource.LoadJsonAsync(CraftingConfigPaths.Recipes, ct);
            var root = string.IsNullOrWhiteSpace(json)
                ? new CraftingRecipesRoot()
                : JsonConvert.DeserializeObject<CraftingRecipesRoot>(json) ?? new CraftingRecipesRoot();

            var data = new CraftingStaticData(root.CraftingRecipes);
            var errors = _validator.Validate(data);
            if (errors.Count > 0)
                throw new InvalidOperationException("Crafting config validation failed: " + string.Join("; ", errors));

            _cachedData = data;
            return _cachedData;
        }

        public void ClearCache()
        {
            _cachedData = null;
        }
    }

    public sealed class CraftingConfigValidator
    {
        public IReadOnlyList<string> Validate(CraftingStaticData data)
        {
            var errors = new List<string>();
            if (data == null)
            {
                errors.Add("Static data is null.");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recipe in data.Recipes)
            {
                if (recipe == null)
                    continue;

                if (string.IsNullOrWhiteSpace(recipe.Id))
                    errors.Add("Recipe has empty id.");
                else if (!ids.Add(recipe.Id))
                    errors.Add($"Recipe id '{recipe.Id}' is duplicated.");

                if (string.IsNullOrWhiteSpace(recipe.StationId))
                    errors.Add($"Recipe '{recipe.Id}' has empty station_id.");

                if (string.IsNullOrWhiteSpace(recipe.OutputItemId))
                    errors.Add($"Recipe '{recipe.Id}' has empty output_item_id.");

                if (recipe.OutputCount <= 0)
                    errors.Add($"Recipe '{recipe.Id}' output_count must be greater than 0.");

                if (recipe.CraftTimeSeconds < 0)
                    errors.Add($"Recipe '{recipe.Id}' craft_time_seconds must be non-negative.");

                if (recipe.Ingredients == null)
                    recipe.Ingredients = new List<CraftingIngredientConfig>();

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(ingredient.ItemId) || ingredient.Count <= 0)
                        errors.Add($"Recipe '{recipe.Id}' has invalid ingredient.");
                }
            }

            return errors;
        }
    }
}
