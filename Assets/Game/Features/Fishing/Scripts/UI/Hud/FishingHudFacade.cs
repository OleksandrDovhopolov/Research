using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using Game.Crafting;
using Infrastructure;
using UIShared;
using UnityEngine;
using VContainer.Unity;
using UISystem;

namespace Game.Fishing
{
    public interface IFishingHudFacade
    {
        UniTask<bool> TryShowAsync(CancellationToken ct = default);
    }

    public interface IFishingHudActions
    {
        UniTask<CraftStartResult> StartCraftAsync(string craftRecipeId, CancellationToken ct = default);
        UniTask<CraftTask> GetActiveCraftAsync(CancellationToken ct = default);
        UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default);
        UniTask<CraftCollectResult> CompleteAndCollectAsync(CraftTaskId taskId, CancellationToken ct = default);
        UniTask<IReadOnlyList<FishingHudLureRenderData>> GetLureRenderDataAsync(CancellationToken ct = default);
        DateTimeOffset GetCurrentTimeUtc();
        void HideHud();
        void ShowInfo(string message);
    }

    public sealed class FishingHudFacade : IFishingHudFacade, IFishingHudActions, IStartable, IDisposable
    {
        private const string LoadingMessage = "Lure production is loading.";

        private enum PrewarmState
        {
            Idle = 0,
            Loading = 1,
            Ready = 2,
            Failed = 3
        }

        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishingHudLureDataBuilder _lureDataBuilder;
        private readonly IHudController _hudController;
        private readonly ICraftingService _craftingService;
        private readonly IClock _clock;
        private readonly UIManager _uiManager;
        private readonly Dictionary<string, Sprite> _spritesByAddress = new(StringComparer.Ordinal);
        private readonly List<string> _warmedAddresses = new();
        private readonly SemaphoreSlim _prewarmSemaphore = new(1, 1);

        private CancellationTokenSource _lifetimeCts;
        private PrewarmState _state = PrewarmState.Idle;
        private bool _disposed;

        public FishingHudFacade(
            IFishingConfigProvider configProvider,
            IFishingHudLureDataBuilder lureDataBuilder,
            IHudController hudController,
            ICraftingService craftingService,
            IClock clock,
            UIManager uiManager)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _lureDataBuilder = lureDataBuilder ?? throw new ArgumentNullException(nameof(lureDataBuilder));
            _hudController = hudController ?? throw new ArgumentNullException(nameof(hudController));
            _craftingService = craftingService ?? throw new ArgumentNullException(nameof(craftingService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _uiManager = uiManager;
        }

        public void Start()
        {
            if (_disposed)
                return;

            _lifetimeCts ??= new CancellationTokenSource();
            EnsurePrewarmStarted();
        }

        public async UniTask<bool> TryShowAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_state != PrewarmState.Ready)
            {
                EnsurePrewarmStarted();
                ShowInfo(LoadingMessage);
                return false;
            }

            var renderData = await GetLureRenderDataAsync(ct);
            var widget = await _hudController.GetHudWidgetAsync<FishingHudWidget>(ct);
            await widget.RenderAsync(renderData, ct);
            return true;
        }

        public UniTask<CraftStartResult> StartCraftAsync(string craftRecipeId, CancellationToken ct = default)
        {
            return _craftingService.StartCraftAsync(craftRecipeId, ct);
        }

        public UniTask<CraftTask> GetActiveCraftAsync(CancellationToken ct = default)
        {
            return _craftingService.GetFirstActiveTaskAsync(CraftingStationIds.LureCrafting, ct);
        }

        public UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default)
        {
            return _craftingService.CollectAsync(taskId, ct);
        }

        public UniTask<CraftCollectResult> CompleteAndCollectAsync(CraftTaskId taskId, CancellationToken ct = default)
        {
            return _craftingService.CompleteAndCollectAsync(taskId, ct);
        }

        public async UniTask<IReadOnlyList<FishingHudLureRenderData>> GetLureRenderDataAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var lures = await _lureDataBuilder.BuildAsync(ct);
            return BuildRenderData(lures);
        }

        public DateTimeOffset GetCurrentTimeUtc()
        {
            return _clock.UtcNow;
        }

        public void HideHud()
        {
            _hudController.HideHudWidget<FishingHudWidget>();
        }

        public void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_uiManager == null)
            {
                Debug.LogWarning($"[FishingHudFacade] UIManager is not assigned. Info='{message}'.");
                return;
            }

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;

            ProdAddressablesWrapper.ReleaseGroup(_warmedAddresses);
            _warmedAddresses.Clear();
            _spritesByAddress.Clear();

            _prewarmSemaphore.Dispose();
        }

        private FishingHudLureRenderData[] BuildRenderData(IReadOnlyList<FishingHudLureViewData> lures)
        {
            if (lures == null || lures.Count == 0)
                return Array.Empty<FishingHudLureRenderData>();

            return lures
                .Where(lure => lure != null)
                .Select(lure =>
                {
                    _spritesByAddress.TryGetValue(lure.SpriteAddress ?? string.Empty, out var sprite);
                    return new FishingHudLureRenderData(lure, sprite);
                })
                .ToArray();
        }

        private void EnsurePrewarmStarted()
        {
            if (_disposed)
                return;

            _lifetimeCts ??= new CancellationTokenSource();
            if (_lifetimeCts.IsCancellationRequested || _state == PrewarmState.Loading || _state == PrewarmState.Ready)
                return;

            RunPrewarmAsync(_lifetimeCts.Token).Forget();
        }

        private async UniTaskVoid RunPrewarmAsync(CancellationToken ct)
        {
            try
            {
                await _prewarmSemaphore.WaitAsync(ct);
                if (_state == PrewarmState.Ready)
                    return;

                _state = PrewarmState.Loading;

                var data = await _configProvider.LoadAsync(ct);
                var addresses = data?.Lures?
                    .Where(lure => lure != null && !string.IsNullOrWhiteSpace(lure.Id))
                    .Select(lure => lure.Id)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray() ?? Array.Empty<string>();

                if (addresses.Length == 0)
                {
                    ProdAddressablesWrapper.ReleaseGroup(_warmedAddresses);
                    _warmedAddresses.Clear();
                    _spritesByAddress.Clear();
                    _state = PrewarmState.Ready;
                    return;
                }

                var loadTasks = new Task<KeyValuePair<string, Sprite>>[addresses.Length];
                for (var i = 0; i < addresses.Length; i++)
                {
                    loadTasks[i] = LoadSpritePairAsync(addresses[i], ct);
                }

                KeyValuePair<string, Sprite>[] loadedSprites;
                try
                {
                    loadedSprites = await Task.WhenAll(loadTasks);
                }
                catch
                {
                    foreach (var task in loadTasks)
                    {
                        if (task.Status == TaskStatus.RanToCompletion)
                            ProdAddressablesWrapper.Release(task.Result.Key);
                    }

                    throw;
                }

                ProdAddressablesWrapper.ReleaseGroup(_warmedAddresses);
                _warmedAddresses.Clear();
                _spritesByAddress.Clear();

                foreach (var pair in loadedSprites)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                        continue;

                    _warmedAddresses.Add(pair.Key);
                    _spritesByAddress[pair.Key] = pair.Value;
                }

                _state = PrewarmState.Ready;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _state = PrewarmState.Failed;
                Debug.LogWarning($"[FishingHudFacade] HUD prewarm failed. {exception}");
            }
            finally
            {
                if (_prewarmSemaphore.CurrentCount == 0)
                    _prewarmSemaphore.Release();
            }
        }

        private static async Task<KeyValuePair<string, Sprite>> LoadSpritePairAsync(string address, CancellationToken ct)
        {
            var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(address, ct);
            return new KeyValuePair<string, Sprite>(address, sprite);
        }
    }
}
