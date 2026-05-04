using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CardCollectionImpl;
using cheatModule;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Cheat
{
    public class CardCollectionCheatModule : ICheatsModule
    {
        private const string CardCollectionGroup = "CardCollection";
        private const string CardCollectionPointGroup = "CardCollectionPoints";
        private const string ScheduleFileName = "liveops_schedule.json";
        
        private readonly CancellationToken _ct;
        private readonly OrchestratorRunner _orchestratorRunner;
        private readonly ICardCollectionSessionFacade _sessionFacade;

        private const string WinterCollectionEventId = "Winter_Collection";
        private const string WinterCollectionEventName = "Winter Collection";
        private const string WinterConfigEventName = "event_winter_collection_config";
        
        private const string SpringCollectionEventId = "Spring_Collection";
        private const string SpringCollectionEventName = "Spring Collection";
        private const string SpringConfigEventName = "event_spring_collection_config";
        
        public CardCollectionCheatModule(ICardCollectionSessionFacade sessionFacade, OrchestratorRunner orchestratorRunner, CancellationToken ct)
        {
            _ct = ct;
            _sessionFacade = sessionFacade;
            _orchestratorRunner = orchestratorRunner;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Create test events", () =>
            {
                //var first = CreateDebugCardCollectionScheduleItemForNextMinute(WinterCollectionEventId, WinterCollectionEventName, WinterConfigEventName, 5, 10000);
                //var first = CreateDebugCardCollectionScheduleItemForNextMinute(SpringCollectionEventId, SpringCollectionEventName,SpringConfigEventName, 5, 1000000);
                var first = CreateDebugCardCollectionScheduleItemForNextMinute(WinterCollectionEventId, WinterCollectionEventName,WinterConfigEventName, 5, 1000000);
                //var second = CreateDebugCardCollectionScheduleItem(SpringCollectionEventId, SpringCollectionEventName, SpringConfigEventName, first.EndTimeUtc, TimeSpan.FromSeconds(500));
                var second = CreateDebugCardCollectionScheduleItem(SpringCollectionEventId, SpringCollectionEventName, SpringConfigEventName,first.EndTimeUtc, TimeSpan.FromSeconds(500));
                
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(first);
                _orchestratorRunner.AddDebugCardCollectionEventNextMinute(second);
                RewriteScheduleFile(first, second);
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete current event", () =>
            {
                _orchestratorRunner.CompleteCurrentEvent();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Force next event", () =>
            {
                _orchestratorRunner.ForceNextEvent();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Complete all collection", () =>
            {
                _sessionFacade.TryCompleteAllCollection(_ct).Forget();
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Unlock all cards - 1", () =>
            {
                _sessionFacade.TryUnlockAllMinusOneCard(_ct).Forget();
            }).WithGroup(CardCollectionGroup));

            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Unlock first 9 cards in each group", () =>
            {
                _sessionFacade.TryUnlockFirstNineCardsInEachGroup(_ct).Forget();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Complete group(Int)", groupIndex =>
            {
               _sessionFacade.TryUnlockGroupByIndex(groupIndex, _ct).Forget();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<string>("Open card ID(str)", cardId =>
            {
                _sessionFacade.TryUnlockCards(new[] { cardId }, _ct).Forget();
            }).WithGroup(CardCollectionGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add points(int)", points =>
            {
                _sessionFacade.TryAddPoints(points, _ct).Forget();
            }).WithGroup(CardCollectionPointGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove points(int)", points =>
            {
                _sessionFacade.TryRemovePoints(points, _ct).Forget();
            }).WithGroup(CardCollectionPointGroup));
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItemForNextMinute(string eventId, string eventName, string eventConfig, int secondsDelay = 30, int secondsDuration = 30)
        {
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
                    ["collectionName"] = eventName,
                    //["eventConfigAddress"] = "event_winter_collection_config",
                    ["eventConfigAddress"] = eventConfig,
                },
            };
        }
        
        private ScheduleItem CreateDebugCardCollectionScheduleItem(string eventId, string eventName, string eventConfig, DateTimeOffset startAt, TimeSpan duration)
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
                    ["collectionName"] = eventName,
                    //["eventConfigAddress"] = "event_spring_collection_config",
                    ["eventConfigAddress"] = eventConfig,
                },
            };
        }

        private static void RewriteScheduleFile(ScheduleItem first, ScheduleItem second)
        {
            try
            {
                var schedulePath = Path.Combine(Application.streamingAssetsPath, ScheduleFileName);
                var scheduleItems = new[] { first, second };

                // Explicitly clear the file before writing fresh schedule entries.
                File.WriteAllText(schedulePath, "[]");
                File.WriteAllText(schedulePath, JsonConvert.SerializeObject(scheduleItems, Formatting.Indented));

                Debug.Log($"[CardCollectionCheatModule] Rewrote schedule file: {schedulePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionCheatModule] Failed to rewrite schedule file: {e}");
            }
        }
        
    }
}
