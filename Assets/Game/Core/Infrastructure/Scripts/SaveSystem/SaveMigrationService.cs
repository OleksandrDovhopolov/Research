using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Infrastructure.SaveSystem
{
    public sealed class SaveMigrationService
    {
        private readonly string _persistentRootPath;

        public SaveMigrationService()
        {
            _persistentRootPath = Application.persistentDataPath;
        }

        public async UniTask<GameSaveData> TryMigrateLegacyAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var migrated = GameSaveData.CreateDefault(schemaVersion, Guid.NewGuid().ToString("N"));
            var hasAnySource = false;

            hasAnySource |= await MigrateInventoryAsync(migrated, cancellationToken);
            hasAnySource |= await MigrateCardCollectionsAsync(migrated, cancellationToken);
            hasAnySource |= await MigrateResourcesAsync(migrated, cancellationToken);

            if (!hasAnySource)
            {
                return null;
            }

            return migrated;
        }

        public UniTask CleanupLegacyFilesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDeleteDirectory(Path.Combine(_persistentRootPath, "inventory"));
            TryDeleteDirectory(Path.Combine(_persistentRootPath, "event_cards"));
            TryDeleteDirectory(Path.Combine(_persistentRootPath, "resources_data"));
            return UniTask.CompletedTask;
        }

        private async UniTask<bool> MigrateInventoryAsync(GameSaveData migrated, CancellationToken cancellationToken)
        {
            var inventoryDir = Path.Combine(_persistentRootPath, "inventory");
            if (!Directory.Exists(inventoryDir))
            {
                return false;
            }

            var files = Directory.GetFiles(inventoryDir, "*.json", SearchOption.TopDirectoryOnly);
            var hadData = false;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                List<LegacyInventoryItemDto> items;
                try
                {
                    items = JsonConvert.DeserializeObject<List<LegacyInventoryItemDto>>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveMigrationService] Failed to parse inventory file {file}: {ex.Message}");
                    continue;
                }

                if (items == null || items.Count == 0)
                {
                    continue;
                }

                hadData = true;
                foreach (var byOwner in items.GroupBy(x => string.IsNullOrWhiteSpace(x.OwnerId) ? "player_1" : x.OwnerId))
                {
                    var ownerData = migrated.Inventory.Owners.FirstOrDefault(x => x.OwnerId == byOwner.Key);
                    if (ownerData == null)
                    {
                        ownerData = new InventoryOwnerSaveData { OwnerId = byOwner.Key };
                        migrated.Inventory.Owners.Add(ownerData);
                    }

                    ownerData.Items.AddRange(byOwner.Select(x => new InventoryItemSaveData
                    {
                        OwnerId = byOwner.Key,
                        ItemId = x.ItemId,
                        StackCount = Math.Max(0, x.StackCount),
                        CategoryId = x.CategoryId,
                    }));
                }
            }

            return hadData;
        }

        private async UniTask<bool> MigrateCardCollectionsAsync(GameSaveData migrated, CancellationToken cancellationToken)
        {
            var cardsDir = Path.Combine(_persistentRootPath, "event_cards");
            if (!Directory.Exists(cardsDir))
            {
                return false;
            }

            var files = Directory.GetFiles(cardsDir, "*.json", SearchOption.TopDirectoryOnly);
            var hadData = false;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                LegacyEventCardsDto dto;
                try
                {
                    dto = JsonConvert.DeserializeObject<LegacyEventCardsDto>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveMigrationService] Failed to parse event cards file {file}: {ex.Message}");
                    continue;
                }

                if (dto == null || string.IsNullOrWhiteSpace(dto.EventId))
                {
                    continue;
                }

                hadData = true;
                migrated.CardCollections.Add(new CardCollectionModuleSaveData
                {
                    EventId = dto.EventId,
                    Version = dto.Version,
                    Points = Math.Max(0, dto.Points),
                    Cards = dto.Cards?
                        .Where(x => !string.IsNullOrWhiteSpace(x.CardId))
                        .Select(x => new CardProgressSaveData
                        {
                            CardId = x.CardId,
                            IsUnlocked = x.IsUnlocked,
                            IsNew = x.IsNew,
                        })
                        .ToList() ?? new List<CardProgressSaveData>(),
                });
            }

            return hadData;
        }

        private async UniTask<bool> MigrateResourcesAsync(GameSaveData migrated, CancellationToken cancellationToken)
        {
            var resourcesFile = Path.Combine(_persistentRootPath, "resources_data", "resources.json");
            if (!File.Exists(resourcesFile))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(resourcesFile, cancellationToken);
            LegacyResourcesDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<LegacyResourcesDto>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveMigrationService] Failed to parse resources file: {ex.Message}");
                return false;
            }

            if (dto == null)
            {
                return false;
            }

            migrated.Resources.Version = dto.Version <= 0 ? 1 : dto.Version;
            migrated.Resources.Gold = Math.Max(0, dto.Gold);
            migrated.Resources.Energy = Math.Max(0, dto.Energy);
            migrated.Resources.Gems = Math.Max(0, dto.Gems);
            return true;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveMigrationService] Could not delete legacy path {path}: {ex.Message}");
            }
        }

        [Serializable]
        private sealed class LegacyInventoryItemDto
        {
            public string OwnerId;
            public string ItemId;
            public int StackCount;
            public string CategoryId;
        }

        [Serializable]
        private sealed class LegacyEventCardsDto
        {
            public string EventId;
            public int Version;
            public int Points;
            public List<LegacyCardProgressDto> Cards = new();
        }

        [Serializable]
        private sealed class LegacyCardProgressDto
        {
            public string CardId;
            public bool IsUnlocked;
            public bool IsNew;
        }

        [Serializable]
        private sealed class LegacyResourcesDto
        {
            public int Version;
            public int Gold;
            public int Energy;
            public int Gems;
        }
    }
}
