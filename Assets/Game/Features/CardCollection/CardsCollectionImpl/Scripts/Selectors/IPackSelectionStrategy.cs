using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IPackSelectionStrategy
    {
        UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardDefinition> allCards, CancellationToken ct = default);
    }
}
