using System;
using System.Linq;
using System.Threading;
using cheatModule;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using UISystem;
using UnityEngine;

namespace Game.Cheat
{
    public sealed class FishingCheatModule : ICheatsModule
    {
        private const string FishingGroup = "Fishing";

        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishCatchResolver _fishCatchResolver;
        private readonly ICaughtFishService _caughtFishService;
        private readonly IFishBookService _fishBookService;
        private readonly UIManager _uiManager;
        private readonly CancellationToken _ct;

        public FishingCheatModule(
            IFishingConfigProvider configProvider,
            IFishCatchResolver fishCatchResolver,
            ICaughtFishService caughtFishService,
            IFishBookService fishBookService,
            UIManager uiManager,
            CancellationToken ct)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishCatchResolver = fishCatchResolver ?? throw new ArgumentNullException(nameof(fishCatchResolver));
            _caughtFishService = caughtFishService ?? throw new ArgumentNullException(nameof(caughtFishService));
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _ct = ct;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            InitializeAsync(cheatsContainer).Forget();
        }

        private async UniTaskVoid InitializeAsync(ICheatsContainer cheatsContainer)
        {
            try
            {
                var data = await _configProvider.LoadAsync(_ct);
                var fishEntries = data?.Fish?
                    .Where(fish => fish != null &&
                                   !fish.EventOnly &&
                                   !string.IsNullOrWhiteSpace(fish.Id))
                    .GroupBy(fish => fish.Id, StringComparer.Ordinal)
                    .Select(group => group.Last())
                    .OrderBy(fish => string.IsNullOrWhiteSpace(fish.DisplayName) ? fish.Id : fish.DisplayName, StringComparer.Ordinal)
                    .ToArray();

                if (fishEntries == null || fishEntries.Length == 0)
                {
                    Debug.LogWarning("[FishingCheatModule] Fishing config contains no non-event fish entries.");
                    return;
                }

                foreach (var fish in fishEntries)
                {
                    var fishId = fish.Id;
                    var label = string.IsNullOrWhiteSpace(fish.DisplayName) ? fishId : fish.DisplayName;
                    cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick($"Catch {label}", () =>
                    {
                        CatchFishAsync(fishId).Forget();
                    }).WithGroup(FishingGroup));
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingCheatModule] Failed to initialize fish cheat buttons. {exception}");
            }
        }

        private async UniTask CatchFishAsync(string fishId)
        {
            try
            {
                var result = await _fishCatchResolver.ResolveCatchAsync(fishId, _ct);
                if (result == null || !result.Success)
                {
                    Debug.LogError($"[FishingCheatModule] Failed to catch fish '{fishId}'. Error={(result?.Error ?? FishingError.ConfigInvalid)}.");
                    return;
                }

                var handledResult = await _caughtFishService.HandleCatchAsync(result, _ct);
                if (handledResult == null || !handledResult.Success || string.IsNullOrWhiteSpace(handledResult.FishId))
                {
                    return;
                }

                try
                {
                    var progress = await _fishBookService.GetProgressAsync(handledResult.FishId, _ct);
                    var args = NewFishArgs.FromCatchResult(handledResult, progress);
                    _uiManager.Show<NewFishController>(args);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[FishingCheatModule] Failed to open new fish window for '{handledResult.FishId}'. {exception}");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingCheatModule] Catch flow crashed for fish '{fishId}'. {exception}");
            }
        }
    }
}
