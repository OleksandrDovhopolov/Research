using System;

namespace BattlePass
{
    public enum BattlePassPremiumOwnedBehavior
    {
        ShowAlreadyActiveInfo = 0,
        OpenBattlePassWindow = 1
    }

    public enum BattlePassPremiumEntryAction
    {
        None = 0,
        ShowInfo = 1,
        OpenBattlePassWindow = 2,
        OpenPurchaseWindow = 3
    }

    public readonly struct BattlePassPremiumEntryDecision
    {
        public BattlePassPremiumEntryDecision(
            BattlePassPremiumEntryAction action,
            string infoMessage,
            string seasonId,
            string productId)
        {
            Action = action;
            InfoMessage = infoMessage ?? string.Empty;
            SeasonId = seasonId ?? string.Empty;
            ProductId = productId ?? string.Empty;
        }

        public BattlePassPremiumEntryAction Action { get; }
        public string InfoMessage { get; }
        public string SeasonId { get; }
        public string ProductId { get; }
    }

    public static class BattlePassPremiumEntryFlow
    {
        public const string MissingDataMessage = "Battle Pass premium purchase is unavailable. Missing seasonId or productId.";
        public const string AlreadyActiveMessage = "Battle Pass premium is already active.";

        public static BattlePassPremiumEntryDecision Resolve(
            BattlePassSnapshot snapshot,
            BattlePassPremiumOwnedBehavior ownedBehavior)
        {
            var seasonId = snapshot?.Season?.Id;
            var productId = snapshot?.Products?.PremiumProductId;
            if (string.IsNullOrWhiteSpace(seasonId) || string.IsNullOrWhiteSpace(productId))
            {
                return new BattlePassPremiumEntryDecision(
                    BattlePassPremiumEntryAction.ShowInfo,
                    MissingDataMessage,
                    string.Empty,
                    string.Empty);
            }

            if (HasPremiumAccess(snapshot?.UserState?.PassType ?? BattlePassPassType.Unknown))
            {
                if (ownedBehavior == BattlePassPremiumOwnedBehavior.OpenBattlePassWindow)
                {
                    return new BattlePassPremiumEntryDecision(
                        BattlePassPremiumEntryAction.OpenBattlePassWindow,
                        string.Empty,
                        string.Empty,
                        string.Empty);
                }

                return new BattlePassPremiumEntryDecision(
                    BattlePassPremiumEntryAction.ShowInfo,
                    AlreadyActiveMessage,
                    string.Empty,
                    string.Empty);
            }

            return new BattlePassPremiumEntryDecision(
                BattlePassPremiumEntryAction.OpenPurchaseWindow,
                string.Empty,
                seasonId,
                productId);
        }

        private static bool HasPremiumAccess(BattlePassPassType passType)
        {
            return passType is BattlePassPassType.Premium or BattlePassPassType.Platinum;
        }
    }
}
