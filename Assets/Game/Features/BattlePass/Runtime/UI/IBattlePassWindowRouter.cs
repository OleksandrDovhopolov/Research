using System.Collections.Generic;

namespace BattlePass
{
    public interface IBattlePassWindowRouter
    {
        void ShowInfo(string message);
        void ShowBattlePassWindow();
        void ShowPremiumPurchase(BattlePassIAPWindowArgs args);
        void ShowGrantedRewards(IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards);
        void HideBattlePassWindow();
        void HideBattlePassPurchaseWindow();
    }
}
