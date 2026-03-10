namespace Inventory.API
{
    //TODO in next iteration remove this from API. API doesnt know about cardPacks
    public readonly struct CardPackMetadata
    {
        public CardPackMetadata(string packName, int cardsInside)
        {
            PackName = packName;
            CardsInside = cardsInside;
        }

        public string PackName { get; }
        public int CardsInside { get; }
    }
}
