using System;
using System.Collections.Generic;
using System.Threading;
using CardCollectionImpl;
using cheatModule;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using EventOrchestration.Models;
using UnityEngine;

namespace Game.Cheat
{
    public class CardCollectionCheatModule : ICheatsModule
    {
        private const string CardCollectionGroup = "CardCollection";
        private const string CardCollectionPointGroup = "CardCollectionPoints";
        
        private readonly CancellationToken _ct;
        private readonly OrchestratorRunner _orchestratorRunner;
        private readonly ICardCollectionFeatureFacade _featureFacade;

        private int _eventCounter = 0;
        
        public CardCollectionCheatModule(ICardCollectionFeatureFacade featureFacade, OrchestratorRunner orchestratorRunner, CancellationToken ct)
        {
            _ct = ct;
            _featureFacade = featureFacade;
            _orchestratorRunner = orchestratorRunner;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Create test events", () =>
            {
                var first = CreateDebugCardCollectionScheduleItemForNextMinute(30, 30);
                var second = CreateDebugCardCollectionScheduleItem(first.EndTimeUtc, TimeSpan.FromMinutes(60));
                
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(first);
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(second);
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete all collection", () =>
            {
                CompleteAllCollectionAsync(_ct).Forget();
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Unlock all cards - 1", () =>
            {
                UnlockAllMinusOneCardAsync(_ct).Forget();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<string>("Open card ID(str)", cardId =>
            {
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    updater.UnlockCard(cardId, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                }
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add points(int)", points =>
            {
                if (_featureFacade.TryGetCollectionPointsAccount(out var pointsAccount))
                {
                    pointsAccount.TryAddPointsAsync(points, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection points account is unavailable.");
                }
            }).WithGroup(CardCollectionPointGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove points(int)", points =>
            {
                if (_featureFacade.TryGetCollectionPointsAccount(out var pointsAccount))
                {
                    pointsAccount.TrySpendPointsAsync(points, _ct).Forget();
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection points account is unavailable.");
                }
            }).WithGroup(CardCollectionPointGroup));
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItemForNextMinute(int secondsDelay = 30, int secondsDuration = 30)
        {
            var now = DateTimeOffset.UtcNow;
            var startAt = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0,
                TimeSpan.Zero).AddSeconds(secondsDelay);
            var endAt = startAt.AddSeconds(secondsDuration);
            var eventId = $"season_cards_debug_{_eventCounter++}";

            return new ScheduleItem
            {
                Id = eventId,
                EventType = "CardCollection",
                StartTimeUtc = startAt,
                EndTimeUtc = endAt,
                Priority = 10,
                StreamId = "card_collection_seasons",
                CustomParams = new Dictionary<string, string>
                {
                    ["eventId"] = eventId,
                    ["rewardsConfigAddress"] = "season_rewards_001",
                    ["cardsCollectionAddress"] = "season_cards_001",
                    ["cardGroupsAddress"] = "season_groups_001",
                    ["cardPacksAddress"] = "shared_card_packs_config",
                },
            };
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItem(DateTimeOffset startAt, TimeSpan duration)
        {
            var endAt = startAt.Add(duration);
            var eventId = $"season_cards_debug_{_eventCounter++}";

            return new ScheduleItem
            {
                Id = eventId,
                EventType = "CardCollection",
                StartTimeUtc = startAt,
                EndTimeUtc = endAt,
                Priority = 10,
                StreamId = "card_collection_seasons",
                CustomParams = new Dictionary<string, string>
                {
                    ["eventId"] = eventId,
                    ["rewardsConfigAddress"] = "season_rewards_001",
                    ["cardsCollectionAddress"] = "season_cards_001",
                    ["cardGroupsAddress"] = "season_groups_001",
                    ["cardPacksAddress"] = "shared_card_packs_config",
                },
            };
        }
        
        private async UniTask CompleteAllCollectionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var cardIds = await GetAllCardIdsAsync(ct);
            if (cardIds.Count == 0)
            {
                Debug.LogWarning("[Cheat] Could not find card IDs to complete collection.");
                return;
            }

            foreach (var cardId in cardIds)
            {
                ct.ThrowIfCancellationRequested();
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    await updater.UnlockCard(cardId, ct);
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                    return;
                }
            }
        }

        private async UniTask UnlockAllMinusOneCardAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var cardIds = await GetAllCardIdsAsync(ct);
            if (cardIds.Count <= 1)
            {
                Debug.LogWarning("[Cheat] Not enough cards to unlock all minus one.");
                return;
            }

            var unlockCount = cardIds.Count - 1;
            for (var i = 0; i < unlockCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (_featureFacade.TryGetCollectionUpdater(out var updater))
                {
                    await updater.UnlockCard(cardIds[i], ct);
                }
                else
                {
                    Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                    return;
                }
            }
        }

        private async UniTask<List<string>> GetAllCardIdsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (!_featureFacade.TryGetCollectionReader(out var reader))
            {
                Debug.LogWarning("[Cheat] CardCollection reader is unavailable.");
                return new List<string>();
            }

            var data = await reader.Load(ct);

            var result = new List<string>();
            var seen = new HashSet<string>();
            foreach (var card in data.Cards)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(card?.CardId) && seen.Add(card.CardId))
                    result.Add(card.CardId);
            }

            return result;
        }
    }
}
