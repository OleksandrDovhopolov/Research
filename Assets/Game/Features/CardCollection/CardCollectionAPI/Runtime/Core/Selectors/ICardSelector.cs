using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardSelector
    {
        UniTask<List<string>> SelectCardsAsync(
            CardPack pack,
            List<CardDefinition> allCards,
            string eventId,
            CancellationToken ct = default);
    }
}
