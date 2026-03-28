using CardCollectionImpl;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace core
{
    public class OpenCardPackDemo : MonoBehaviour
    {
        private ICardCollectionFeatureFacade _feature;
        
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
        
        [Inject]
        private void Construct(ICardCollectionFeatureFacade feature)
        {
            
            Debug.LogWarning($"[VContainer] Construct {GetType().Name}");
            _feature = feature;
        }
        
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
            if (_feature.IsActive)
            {
                _feature.ShowNewCardWindow(packId, destroyCancellationToken);
            }
            else
            {
                Debug.LogWarning($"[CardCollectionRuntime] {GetType().Name}: OpenNewCardWindow called without active feature");
            }
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