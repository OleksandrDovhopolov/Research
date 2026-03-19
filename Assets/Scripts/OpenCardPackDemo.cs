using CardCollection.Core;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class OpenCardPackDemo : MonoBehaviour
    {
        [SerializeField] private Button _twoCardButton;
        [SerializeField] private Button _threeCardButton;
        [SerializeField] private Button _fourCardButton;
        [SerializeField] private Button _fiveCardButton;
        [SerializeField] private Button _sixCardButton;
        
        private const string BaseTwoCardPackID = "Bronze_Pack";
        private const string BaseThreeCardPackID = "Emerald_Pack";
        private const string BaseFourCardPackID = "Lazurite_Pack";
        private const string BaseFiveCardPackID = "Sapphire_Pack";
        private const string BaseSixCardPackID = "Ruby_Pack";
        
        private void Start()
        {
            _sixCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseSixCardPackID));
            _twoCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseTwoCardPackID));
            _threeCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseThreeCardPackID));
            _fourCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseFourCardPackID));
            _fiveCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseFiveCardPackID));
        }

        //TODO restore when module integration is ready
        public void OpenNewCardWindow(string packId)
        {
            /*var cardCollectionModule = _cardCollectionEntryPoint.CardCollectionModule;
            var cardCollectionReader = _cardCollectionEntryPoint.CardCollectionReader;
            
            var pack = cardCollectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Pack not found: {packId}");
                return;
            }
            
            Debug.LogWarning($"Debug {pack.PackId}, {pack.CardCount}, {pack.PackName}");
            
            var compositionRoot = CardCollectionCompositionRegistry.Resolve();
            var windowPresenter = compositionRoot.CreateWindowPresenter();
            
            windowPresenter.OpenNewCardWindow(pack, cardCollectionModule, cardCollectionReader);*/
        }
        
        private void OnDestroy()
        {
            _twoCardButton.onClick.RemoveAllListeners();
            _threeCardButton.onClick.RemoveAllListeners();
            _fourCardButton.onClick.RemoveAllListeners();
            _fiveCardButton.onClick.RemoveAllListeners();
            _sixCardButton.onClick.RemoveAllListeners();
        }
    }
}