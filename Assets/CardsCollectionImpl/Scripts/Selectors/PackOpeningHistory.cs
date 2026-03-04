using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardCollectionImpl
{
    /// <summary>
    /// Tracks pack opening history to support missing card boost logic.
    /// </summary>
    public class PackOpeningHistory
    {
        private readonly Dictionary<string, PackHistoryEntry> _packHistory = new();

        /// <summary>
        /// Records a pack opening and whether it contained any missing cards.
        /// </summary>
        public void RecordPackOpening(string packId, bool hadMissingCard)
        {
            if (!_packHistory.TryGetValue(packId, out var entry))
            {
                entry = new PackHistoryEntry { PackId = packId };
                _packHistory[packId] = entry;
            }

            if (hadMissingCard)
            {
                // Reset consecutive count if we got a missing card
                entry.ConsecutivePacksWithoutMissingCard = 0;
            }
            else
            {
                // Increment consecutive count
                entry.ConsecutivePacksWithoutMissingCard++;
            }

            entry.TotalPacksOpened++;
        }

        /// <summary>
        /// Gets the number of consecutive packs opened without a missing card for the given pack ID.
        /// </summary>
        public int GetConsecutivePacksWithoutMissingCard(string packId)
        {
            if (_packHistory.TryGetValue(packId, out var entry))
            {
                return entry.ConsecutivePacksWithoutMissingCard;
            }
            return 0;
        }

        /// <summary>
        /// Gets the missing card boost percentage for the given pack based on consecutive packs without missing cards.
        /// </summary>
        public float GetMissingCardBoostPercentage(string packId, float[] boostPercentages)
        {
            if (boostPercentages == null || boostPercentages.Length == 0)
                return 0f;

            var consecutiveCount = GetConsecutivePacksWithoutMissingCard(packId);
            
            // Boost starts at the 3rd pack (index 0), 4th pack (index 1), 5th+ pack (index 2)
            if (consecutiveCount < 2)
                return 0f; // No boost for 1st and 2nd pack

            var boostIndex = Mathf.Min(consecutiveCount - 2, boostPercentages.Length - 1);
            return boostPercentages[boostIndex];
        }

        /// <summary>
        /// Clears history for a specific pack.
        /// </summary>
        public void ClearHistory(string packId)
        {
            _packHistory.Remove(packId);
        }

        /// <summary>
        /// Clears all pack history.
        /// </summary>
        public void ClearAllHistory()
        {
            _packHistory.Clear();
        }

        public PackOpeningHistorySaveData ToSaveData()
        {
            return new PackOpeningHistorySaveData
            {
                Entries = _packHistory.Values
                    .Select(entry => new PackOpeningHistoryEntrySaveData
                    {
                        PackId = entry.PackId,
                        ConsecutivePacksWithoutMissingCard = entry.ConsecutivePacksWithoutMissingCard,
                        TotalPacksOpened = entry.TotalPacksOpened
                    })
                    .ToList()
            };
        }

        public void LoadFromSaveData(PackOpeningHistorySaveData saveData)
        {
            _packHistory.Clear();
            if (saveData?.Entries == null)
            {
                return;
            }

            foreach (var entry in saveData.Entries)
            {
                if (string.IsNullOrEmpty(entry.PackId))
                {
                    continue;
                }

                _packHistory[entry.PackId] = new PackHistoryEntry
                {
                    PackId = entry.PackId,
                    ConsecutivePacksWithoutMissingCard = Mathf.Max(0, entry.ConsecutivePacksWithoutMissingCard),
                    TotalPacksOpened = Mathf.Max(0, entry.TotalPacksOpened)
                };
            }
        }

        private class PackHistoryEntry
        {
            public string PackId { get; set; }
            public int ConsecutivePacksWithoutMissingCard { get; set; }
            public int TotalPacksOpened { get; set; }
        }
    }
}
