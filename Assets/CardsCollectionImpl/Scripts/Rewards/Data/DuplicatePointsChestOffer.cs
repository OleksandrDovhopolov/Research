using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using Resources.Core;
using Rewards;

namespace CardCollectionImpl
{
    public class DuplicatePointsChestOffer : CollectionRewardDefinition
    {
        public List<CardPack> CardPack { get; set; } = new();
        public List<GameResource> Resources { get; set; } = new();
        
        public override IEnumerable<RewardGrantRequest> ToRequests()
        {
            foreach (var r in Resources.Where(r => r.Amount > 0))
                yield return new RewardGrantRequest(r.Type.ToString(), r.Amount);

            foreach (var p in CardPack.Where(p => !string.IsNullOrEmpty(p.PackId)))
                yield return new RewardGrantRequest(p.PackId, 1);
        }
    }
}