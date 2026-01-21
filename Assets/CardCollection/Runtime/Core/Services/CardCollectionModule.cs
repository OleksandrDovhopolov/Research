using System;
using System.Collections.Generic;
using core;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Configuration object used to wire external implementations into the card collection module.
    /// A host project provides concrete providers/selectors/storage here.
    /// </summary>
    public sealed class CardCollectionModuleConfig 
    {
        public ICardPackProvider PackProvider { get; }
        public ICardSelector CardSelector { get; }
        public IEventCardsStorage EventCardsStorage { get; }
        public string DefaultEventId { get; }

        public CardCollectionModuleConfig (
            ICardPackProvider packProvider,
            IEventCardsStorage eventCardsStorage,
            ICardSelector cardSelector = null,
            string defaultEventId = "default")
        {
            PackProvider = packProvider ?? throw new ArgumentNullException(nameof(packProvider));
            EventCardsStorage = eventCardsStorage ?? throw new ArgumentNullException(nameof(eventCardsStorage));
            CardSelector = cardSelector ?? new RandomCardSelector();
            DefaultEventId = defaultEventId;
        }
    }

    /// <summary>
    /// Internal context that constructs and owns all core services.
    /// </summary>
    internal sealed class CardCollectionContext : IDisposable
    {
        public CardCollectionService Packs { get; }
        public PackBasedCardsRandomizer Randomizer { get; }
        public EventCardsService Events { get; }

        public string DefaultEventId { get; }

        public CardCollectionContext(CardCollectionModuleConfig  config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.PackProvider == null) throw new ArgumentNullException(nameof(config.PackProvider));
            if (config.EventCardsStorage == null) throw new ArgumentNullException(nameof(config.EventCardsStorage));

            DefaultEventId = config.DefaultEventId;

            Packs = new CardCollectionService(config.PackProvider);
            Randomizer = new PackBasedCardsRandomizer(config.CardSelector ?? new RandomCardSelector());
            Events = new EventCardsService(config.EventCardsStorage);
        }

        public async UniTask InitializeAsync()
        {
            await Packs.InitializeAsync();
            await Events.InitializeAsync();
        }

        public void Dispose()
        {
            Packs.Dispose();
        }
    }

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

        // Progress helpers
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds);
        UniTask ResetNewFlagAsync(string cardId);
    }

    public sealed class CardCollectionModule : ICardCollectionModule, IDisposable
    {
        private readonly CardCollectionContext _context;

        public CardCollectionModule(CardCollectionModuleConfig  config)
        {
            _context = new CardCollectionContext(config);
        }

        public UniTask InitializeAsync() => _context.InitializeAsync();

        public List<CardPack> GetAllPacks() => _context.Packs.GetAllPacks();

        public CardPack GetPackById(string packId) => _context.Packs.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId)
        {
            var pack = _context.Packs.GetPackById(packId);
            if (pack == null)
            {
                return new List<string>();
            }

            var cardIds = await _context.Randomizer.GetRandomNewCardsAsync(pack);
            if (cardIds.Count > 0)
            {
                await _context.Events.UnlockCardsAsync(_context.DefaultEventId, cardIds);
            }

            return cardIds;
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds)
        {
            return _context.Events.GetCardsByIdsAsync(_context.DefaultEventId, cardIds);
        }

        public UniTask ResetNewFlagAsync(string cardId)
        {
            return _context.Events.ResetNewFlagAsync(_context.DefaultEventId, cardId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

