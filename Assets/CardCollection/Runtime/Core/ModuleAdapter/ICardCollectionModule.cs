using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Facade that exposes a small, simple API for using the card collection module.
    /// Host projects should depend on this interface instead of individual services.
    /// </summary>
    public interface ICardCollectionModule
    {
        UniTask InitializeAsync();

        // Packs
        List<CardPack> GetAllPacks();
        CardPack GetPackById(string packId);

        // Gameplay flow
        UniTask<List<string>> OpenPackAndUnlockAsync(string packId);
        UniTask<List<string>> OpenPackAndUnlockAsync(CardPack cardPack);

        // Progress helpers
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds);
        UniTask ResetNewFlagAsync(string cardId);
    }
    
    public interface ICardCollectionUpdater
    {
        UniTask UnlockCard(string cardId);
        UniTask Save();
        UniTask<EventCardsSaveData> Load();
        UniTask Clear();
    }
}