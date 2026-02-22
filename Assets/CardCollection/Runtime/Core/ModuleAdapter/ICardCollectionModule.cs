using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Facade that exposes a small, simple API for using the card collection module.
    /// Host projects should depend on this interface instead of individual services.
    /// </summary>
    public interface ICardCollectionModule
    {
        UniTask InitializeAsync(CancellationToken ct = default);

        // Packs
        List<CardPack> GetAllPacks();
        CardPack GetPackById(string packId);

        // Gameplay flow
        UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default);
        UniTask<List<string>> OpenPackAndUnlockAsync(CardPack cardPack, CancellationToken ct = default);

        // Progress helpers
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default);
        UniTask ResetNewFlagAsync(string cardId, CancellationToken ct = default);
    }

    public interface ICardCollectionReader
    {
        UniTask<EventCardsSaveData> Load(CancellationToken ct = default);
        UniTask<HashSet<string>> GetMissingCardIdsAsync(List<CardDefinition> allCards, CancellationToken ct = default);
        int GetCollectionPoints();
    }

    public interface ICardCollectionUpdater
    {
        UniTask UnlockCard(string cardId, CancellationToken ct = default);
        UniTask Save(CancellationToken ct = default);
        UniTask Clear(CancellationToken ct = default);
    }
}
