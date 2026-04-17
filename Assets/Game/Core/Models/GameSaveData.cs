using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Models
{
    [Serializable]
    public sealed class GameSaveData
    {
        public MetaData Meta = new();
        public InventoryModuleSaveData Inventory = new();
        public List<CardCollectionModuleSaveData> CardCollections = new();
        public List<EventStateSaveData> EventStates = new();
        public ResourcesModuleSaveData Resources = new();
        public FortuneWheelModuleSaveData FortuneWheel = new();
        public Dictionary<string, string> CustomModulesJson = new();

        public static GameSaveData CreateDefault(int schemaVersion, string saveId)
        {
            return new GameSaveData
            {
                Meta = new MetaData
                {
                    SchemaVersion = schemaVersion,
                    LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    SaveId = saveId,
                    Revision = 0,
                    Hash = string.Empty,
                },
            };
        }
    }

    [Serializable]
    public sealed class MetaData
    {
        public int SchemaVersion;
        public long LastSaveTimestamp;
        public string Hash;
        public string SaveId;
        public int Revision;
    }

    [Serializable]
    public sealed class InventoryModuleSaveData
    {
        public List<InventoryOwnerSaveData> Owners = new();
    }

    [Serializable]
    public sealed class InventoryOwnerSaveData
    {
        public string OwnerId;
        public List<InventoryItemSaveData> Items = new();
    }

    [Serializable]
    public sealed class InventoryItemSaveData
    {
        public string OwnerId;
        public string ItemId;
        public int StackCount;
        public string CategoryId;
    }

    [Serializable]
    public sealed class CardCollectionModuleSaveData
    {
        public string EventId;
        public int Version;
        public int Points;
        public List<CardProgressSaveData> Cards = new();
    }

    [Serializable]
    public sealed class CardProgressSaveData
    {
        public string CardId;
        public bool IsUnlocked;
        public bool IsNew;
    }

    [Serializable]
    public sealed class EventStateSaveData
    {
        public string ScheduleItemId;
        public int State;
        public int Version;
        public long UpdatedAtUnixSeconds;
        public string LastError;
        public bool StartInvoked;
        public bool EndInvoked;
        public bool SettlementInvoked;
    }

    [Serializable]
    public sealed class ResourcesModuleSaveData
    {
        public int Version = 1;
        public int Gold;
        public int Energy;
        public int Gems;
    }

    [Serializable]
    public sealed class FortuneWheelModuleSaveData
    {
        public int AvailableSpins { get; set; }
        public long UpdatedAt { get; set; }
        public long NextUpdateAt { get; set; }

        [JsonProperty("LastResetTimestamp")]
        private long LegacyLastResetTimestamp
        {
            set
            {
                if (UpdatedAt <= 0)
                {
                    UpdatedAt = Math.Max(0, value);
                }
            }
        }
    }
}
