using System.Threading;
using CardCollectionImpl;
using cheatModule;
using UnityEngine;

namespace Game.Cheat
{
    public class AddCardsCheatModule : ICheatsModule
    {
        private const string CardsGroup = "Cards";
        
        private const string BaseTwoCardPackID = "Bronze_Pack";
        private const string BaseThreeCardPackID = "Emerald_Pack";
        private const string BaseFourCardPackID = "Lazurite_Pack";
        private const string BaseFiveCardPackID = "Sapphire_Pack";
        private const string BaseSixCardPackID = "Ruby_Pack";
        
        private readonly CancellationToken _ct;
        private readonly ICardCollectionSessionFacade _sessionFacade;
        
        public AddCardsCheatModule(ICardCollectionSessionFacade sessionFacade,  CancellationToken ct)
        {
            _ct = ct;
            _sessionFacade = sessionFacade;
        }
        
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Open Bronze_Pack", () =>
            {
                OpenNewCardWindow(BaseTwoCardPackID);
            }).WithGroup(CardsGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Open Emerald_Pack", () =>
            {
                OpenNewCardWindow(BaseThreeCardPackID);
            }).WithGroup(CardsGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Open Lazurite_Pack", () =>
            {
                OpenNewCardWindow(BaseFourCardPackID);
            }).WithGroup(CardsGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Open Sapphire_Pack", () =>
            {
                OpenNewCardWindow(BaseFiveCardPackID);
            }).WithGroup(CardsGroup));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Open Ruby_Pack", () =>
            {
                OpenNewCardWindow(BaseSixCardPackID);
            }).WithGroup(CardsGroup));
        }
        
        public void OpenNewCardWindow(string packId)
        {
            if (_sessionFacade.IsActive)
            {
                _sessionFacade.ShowNewCardWindow(packId, _ct);
            }
            else
            {
                Debug.LogWarning($"[CardCollectionRuntime] {GetType().Name}: OpenNewCardWindow called without active feature");
            }
        }
    }
}

