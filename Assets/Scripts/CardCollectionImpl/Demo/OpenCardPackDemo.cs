using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class OpenCardPackDemo : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private CardCollectionEntryPoint _cardCollectionEntryPoint;
        
        [SerializeField] private Button _twoCardButton;
        [SerializeField] private Button _threeCardButton;
        [SerializeField] private Button _fourCardButton;
        [SerializeField] private Button _fiveCardButton;
        [SerializeField] private Button _sixCardButton;
        
        private const string BaseTwoCardPackID = "starter_pack_2";
        private const string BaseThreeCardPackID = "basic_pack_3";
        private const string BaseFourCardPackID = "standard_pack_4";
        private const string BaseFiveCardPackID = "premium_pack_5";
        private const string BaseSixCardPackID = "mega_pack_6";
        
        private void Start()
        {
            _twoCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseTwoCardPackID));
            _threeCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseThreeCardPackID));
            _fourCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseFourCardPackID));
            _fiveCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseFiveCardPackID));
            _sixCardButton.onClick.AddListener(() => OpenNewCardWindow(BaseSixCardPackID));
        }

        public void OpenNewCardWindow(string packId)
        {
            var cardCollectionModule = _cardCollectionEntryPoint.CardCollectionModule;
            var pack = cardCollectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Pack not found: {packId}");
                return;
            }
            
            Debug.LogWarning($"Debug {pack.PackId}, {pack.CardCount}, {pack.PackName}");
            
            var args = new NewCardArgs(pack, _uiManager, cardCollectionModule);
            _uiManager.Show<NewCardController>(args);
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