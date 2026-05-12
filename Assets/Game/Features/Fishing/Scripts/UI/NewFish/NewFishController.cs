using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UISystem;
using VContainer;


namespace Game.Fishing
{
    public sealed class NewFishArgs : WindowArgs
    {
        private const string FishItemType = "fish";

        public NewFishArgs(
            string fishId,
            bool isNew,
            float caughtWeight,
            float bestCaughtWeight,
            bool isDiscovered,
            IReadOnlyList<string> unlockedWeightStates)
        {
            FishId = fishId ?? string.Empty;
            IsNew = isNew;
            CaughtWeight = Mathf.Max(0f, caughtWeight);
            BestCaughtWeight = bestCaughtWeight;
            IsDiscovered = isDiscovered;
            UnlockedWeightStates = CloneUnlockedWeightStates(unlockedWeightStates);
        }

        public string FishId { get; }
        public bool IsNew { get; }
        public float CaughtWeight { get; }
        public float BestCaughtWeight { get; }
        public bool IsDiscovered { get; }
        public IReadOnlyList<string> UnlockedWeightStates { get; }

        public static NewFishArgs FromCatchResult(FishingCatchResult result, FishBookProgress progress)
        {
            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.FishId))
                throw new ArgumentException("A successful catch result is required to create NewFishArgs.", nameof(result));

            var fallbackStateId = ToStateId(result.State);
            IReadOnlyList<string> unlockedWeightStates = progress?.UnlockedWeightStates;
            if (unlockedWeightStates == null || unlockedWeightStates.Count == 0)
                unlockedWeightStates = new[] { fallbackStateId };

            var bestCaughtWeight = progress != null && progress.BestWeight > 0f
                ? progress.BestWeight
                : result.Weight;

            return new NewFishArgs(
                result.FishId,
                progress?.IsNew ?? true,
                result.Weight,
                bestCaughtWeight,
                progress?.IsDiscovered ?? true,
                unlockedWeightStates);
        }

        internal FishCollectionEntryViewData CreateCollectionEntryViewData()
        {
            return new FishCollectionEntryViewData(
                FishId,
                FishId,
                string.Empty,
                string.Empty,
                string.Empty,
                FishItemType,
                0f,
                0f,
                BestCaughtWeight,
                IsDiscovered,
                Array.Empty<FishCollectionLureViewData>(),
                CreateProgressSnapshot());
        }

        private FishBookProgress CreateProgressSnapshot()
        {
            return new FishBookProgress
            {
                FishId = FishId,
                IsDiscovered = IsDiscovered,
                IsNew = IsNew,
                BestWeight = BestCaughtWeight,
                UnlockedWeightStates = new List<string>(UnlockedWeightStates)
            };
        }

        private static IReadOnlyList<string> CloneUnlockedWeightStates(IReadOnlyList<string> unlockedWeightStates)
        {
            if (unlockedWeightStates == null || unlockedWeightStates.Count == 0)
                return Array.Empty<string>();

            var result = new List<string>(unlockedWeightStates.Count);
            for (var i = 0; i < unlockedWeightStates.Count; i++)
            {
                var stateId = unlockedWeightStates[i];
                if (string.IsNullOrWhiteSpace(stateId))
                    continue;

                var isDuplicate = false;
                for (var j = 0; j < result.Count; j++)
                {
                    if (string.Equals(result[j], stateId, StringComparison.Ordinal))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                    result.Add(stateId);
            }

            return result.Count == 0 ? Array.Empty<string>() : result;
        }

        private static string ToStateId(FishWeightState state)
        {
            return state.ToString().ToLowerInvariant();
        }
    }

    [Window("NewFishWindow")]
    public class NewFishController : WindowController<NewFishView>
    {
        private IFishBookService _fishBookService;

        private NewFishArgs Args => (NewFishArgs)Arguments;
        private bool _markAsViewedRequested;

        [Inject]
        private void Construct(IFishBookService fishBookService)
        {
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
        }

        protected override void OnShowStart()
        {
            _markAsViewedRequested = false;

            if (Args == null)
            {
                Debug.LogError("[NewFishController] Args is null.");
                CloseWindow();
                return;
            }

            View.Render(Args);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            TryMarkAsViewed();
        }

        private void CloseWindow()
        {
            UIManager.Hide<NewFishController>();
        }

        private void TryMarkAsViewed()
        {
            if (_markAsViewedRequested || Args == null || !Args.IsNew || string.IsNullOrWhiteSpace(Args.FishId))
                return;

            _markAsViewedRequested = true;
            MarkAsViewedAsync(Args.FishId).Forget();
        }

        private async UniTaskVoid MarkAsViewedAsync(string fishId)
        {
            try
            {
                await _fishBookService.MarkAsViewedAsync(fishId);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NewFishController] Failed to mark fish '{fishId}' as viewed. {exception}");
            }
        }
    }
}
