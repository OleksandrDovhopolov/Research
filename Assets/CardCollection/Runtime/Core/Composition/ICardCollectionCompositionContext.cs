namespace CardCollection.Core
{
    public interface ICardCollectionResourceContext
    {
    }

    public interface ICardCollectionExchangeConfigContext
    {
    }

    public sealed class CardCollectionResourceContext : ICardCollectionResourceContext
    {
        public CardCollectionResourceContext(object resourceManager)
        {
            ResourceManager = resourceManager;
        }

        public object ResourceManager { get; }
    }

    public sealed class CardCollectionExchangeConfigContext : ICardCollectionExchangeConfigContext
    {
        public CardCollectionExchangeConfigContext(object exchangePacksConfig)
        {
            ExchangePacksConfig = exchangePacksConfig;
        }

        public object ExchangePacksConfig { get; }
    }
}
