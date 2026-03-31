using System;
using System.Collections.Generic;
using System.Linq;
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

        private const string WinterCollectionEventId = "Winter_Collection";
        private const string WinterCollectionEventName = "Winter Collection";
        
        private const string SpringCollectionEventId = "Spring_Collection";
        private const string SpringCollectionEventName = "Spring Collection";
        
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
                var first = CreateDebugCardCollectionScheduleItemForNextMinute(WinterCollectionEventId, WinterCollectionEventName, 10, 120);
                //var first = CreateDebugCardCollectionScheduleItem(SpringCollectionEventId, SpringCollectionEventName, 30, 120);
                var second = CreateDebugCardCollectionScheduleItem(SpringCollectionEventId, SpringCollectionEventName, first.EndTimeUtc, TimeSpan.FromSeconds(120));
                
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(first);
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(second);
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete current event", () =>
            {
                _orchestratorRunner.CompleteCurrentEvent();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete all collection", () =>
            {
                CompleteAllCollectionAsync(_ct).Forget();
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Unlock all cards - 1", () =>
            {
                UnlockAllMinusOneCardAsync(_ct).Forget();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Complete group(Int)", groupIndex =>
            {
               UnlockGroupByInt(groupIndex, _ct).Forget();
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
        
        private ScheduleItem CreateDebugCardCollectionScheduleItemForNextMinute(string eventId, string eventName, int secondsDelay = 30, int secondsDuration = 30)
        {
            /*var now = DateTimeOffset.UtcNow;
            var startAt = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0,
                TimeSpan.Zero).AddSeconds(secondsDelay);*/
            var startAt = DateTimeOffset.UtcNow.AddSeconds(secondsDelay);
            var endAt = startAt.AddSeconds(secondsDuration);

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
                    ["rewardsConfigAddress"] = "winter_collection_rewards",
                    ["cardsCollectionAddress"] = "winter_collection_cards",
                    ["cardGroupsAddress"] = "winter_collection_groups",
                    ["cardPacksAddress"] = "shared_card_packs_config",
                    ["collectionName"] = eventName,
                },
            };
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItem(string eventId, string eventName, DateTimeOffset startAt, TimeSpan duration)
        {
            var endAt = startAt.Add(duration);

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
                    ["rewardsConfigAddress"] = "spring_collection_rewards",
                    ["cardsCollectionAddress"] = "spring_collection_cards",
                    ["cardGroupsAddress"] = "spring_collection_groups",
                    ["cardPacksAddress"] = "shared_card_packs_config",
                    ["collectionName"] = eventName,
                },
            };
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItem(string eventId, string eventName, int secondsDelay = 30, int secondsDuration = 30)
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
                    ["rewardsConfigAddress"] = "spring_collection_rewards",
                    ["cardsCollectionAddress"] = "spring_collection_cards",
                    ["cardGroupsAddress"] = "spring_collection_groups",
                    ["cardPacksAddress"] = "shared_card_packs_config",
                    ["collectionName"] = eventName,
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

            if (!_featureFacade.TryGetCollectionUpdater(out var updater))
            {
                Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                return;
            }

            var unlockIds = cardIds.Take(cardIds.Count - 1).ToList();
            await updater.UnlockCards(unlockIds, ct);
        }
        
        private async UniTask UnlockGroupByInt(int groupIndex, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (groupIndex < 0)
            {
                Debug.LogWarning("[Cheat] Group index must be >= 0.");
                return;
            }

            var cardIds = await GetAllCardIdsAsync(ct);
            if (cardIds.Count == 0)
            {
                Debug.LogWarning("[Cheat] Could not find card IDs to unlock group.");
                return;
            }

            const int groupSize = 10;
            var groupCardIds = cardIds
                .Skip(groupIndex * groupSize)
                .Take(groupSize)
                .ToList();

            if (groupCardIds.Count == 0)
            {
                Debug.LogWarning($"[Cheat] Group index {groupIndex} is out of range.");
                return;
            }

            if (!_featureFacade.TryGetCollectionUpdater(out var updater))
            {
                Debug.LogWarning("[Cheat] CardCollection updater is unavailable.");
                return;
            }

            foreach (var cardId in groupCardIds)
            {
                ct.ThrowIfCancellationRequested();
                await updater.UnlockCard(cardId, ct);
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
