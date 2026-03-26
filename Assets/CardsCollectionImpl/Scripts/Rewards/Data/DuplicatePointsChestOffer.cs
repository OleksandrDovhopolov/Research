using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using CoreResources;
using Rewards;

namespace CardCollectionImpl
{
    public class DuplicatePointsChestOffer : CollectionRewardDefinition
    {
        public const string Regular = "regular";
        public List<CardPack> CardPack { get; set; } = new();
        public List<GameResource> Resources { get; set; } = new();
        
        public override IEnumerable<RewardGrantRequest> ToRequests()
        {
            foreach (var r in Resources.Where(r => r.Amount > 0))
                yield return new RewardGrantRequest(r.Type.ToString(), r.Amount, Regular);

            foreach (var p in CardPack.Where(p => !string.IsNullOrEmpty(p.PackId)))
                yield return new RewardGrantRequest(p.PackId, 1,  CardsConfig.CardPack);
        }
    }
}