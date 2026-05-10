using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreResources;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UISystem;
using UIShared;
using UnityEngine;
using VContainer.Unity;

namespace Game.Fishing
{
    public sealed class FishingLureSelectionHudFacade : IFishingLureSelectionHudFacade, IFishingLureSelectionHudActions, IStartable, IDisposable
    {
        private const string LoadingMessage = "Fishing tackle is loading.";

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
        private readonly UIManager _uiManager;
        private readonly IFishingLureSelectionStartService _startService;
        private readonly Dictionary<string, Sprite> _spritesByAddress = new(StringComparer.Ordinal);
        private readonly List<string> _warmedAddresses = new();
        private readonly SemaphoreSlim _prewarmSemaphore = new(1, 1);

        private CancellationTokenSource _lifetimeCts;
        private PrewarmState _state = PrewarmState.Idle;
        private bool _disposed;

        public FishingLureSelectionHudFacade(
            IFishingConfigProvider configProvider,
            IFishingHudLureDataBuilder lureDataBuilder,
            IHudController hudController,
            UIManager uiManager,
            IFishingLureSelectionStartService startService)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _lureDataBuilder = lureDataBuilder ?? throw new ArgumentNullException(nameof(lureDataBuilder));
            _hudController = hudController ?? throw new ArgumentNullException(nameof(hudController));
            _uiManager = uiManager;
            _startService = startService ?? throw new ArgumentNullException(nameof(startService));
        }

        public void Start()
        {
            if (_disposed)
                return;

            _lifetimeCts ??= new CancellationTokenSource();
            EnsurePrewarmStarted();
        }

        public async UniTask<bool> TryShowAsync(string zoneId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_state != PrewarmState.Ready)
            {
                EnsurePrewarmStarted();
                ShowInfo(LoadingMessage);
                return false;
            }

            var data = await _configProvider.LoadAsync(ct);
            if (!data.ZonesById.TryGetValue(zoneId ?? string.Empty, out var zone))
            {
                ShowInfo("Fishing zone was not found.");
                return false;
            }

            if (zone.IsUnlockFeatureEnabled && !zone.IsUnlockedByDefault)
            {
                ShowInfo("This fishing zone is locked.");
                return false;
            }

            var renderData = await BuildRenderDataAsync(ct);
            var allowedLureIds = zone.AllowedLureIds?
                .Where(lureId => !string.IsNullOrWhiteSpace(lureId))
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();

            var widget = await _hudController.GetHudWidgetAsync<FishingLureSelectionHudWidget>(ct);
            await widget.RenderAsync(new FishingLureSelectionRenderArgs(zone.Id, renderData, allowedLureIds), ct);
            return true;
        }

        public UniTask<FishingLureSelectionStartResult> TryStartFishingAsync(string zoneId, string lureId, CancellationToken ct = default)
        {
            return _startService.TryStartAsync(zoneId, lureId, ct);
        }

        public void HideHud()
        {
            _hudController.HideHudWidget<FishingLureSelectionHudWidget>();
        }

        public void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_uiManager == null)
            {
                Debug.LogWarning($"[FishingLureSelectionHudFacade] UIManager is not assigned. Info='{message}'.");
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

        private async UniTask<IReadOnlyList<FishingHudLureRenderData>> BuildRenderDataAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var lures = await _lureDataBuilder.BuildAsync(ct);
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
                Debug.LogWarning($"[FishingLureSelectionHudFacade] HUD prewarm failed. {exception}");
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
