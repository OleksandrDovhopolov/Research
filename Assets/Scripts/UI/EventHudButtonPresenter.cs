using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Core;
using EventOrchestration.Models;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public sealed class EventHudButtonPresenter : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private bool _isBound;
        private EventOrchestrator _orchestrator;
        
        private ICardCollectionReader _reader;
        private ICardCollectionModule _module;
        private ICardCollectionPointsAccount _pointsAccount;
        private IWindowPresenter _windowPresenter;
        private IExchangeOfferProvider _exchangeOfferProvider;
        private IRewardDefinitionFactory _rewardDefinitionFactory;
        
        private CancellationToken _destroyCt;
        
        /*public void Construct(
            ICardCollectionReader reader,
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            IWindowPresenter windowPresenter,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _windowPresenter = windowPresenter ?? throw new ArgumentNullException(nameof(windowPresenter));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardDefinitionFactory = rewardDefinitionFactory ?? throw new ArgumentNullException(nameof(rewardDefinitionFactory));
        }*/
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }
        
        private void Start()
        {
            _windowPresenter = CardCollectionCompositionRegistry.Resolve().CreateWindowPresenter();
            
            _button.onClick.AddListener(() => OpenCardCollectionWindow().Forget());
        }

        private async UniTask OpenCardCollectionWindow()
        {
            Debug.LogWarning($"[Debug] OpenCardCollectionWindow");
            
            /*await _windowPresenter.OpenCardCollectionWindow( 
                _module,
                _reader,
                _exchangeOfferProvider,
                _rewardDefinitionFactory,
                _pointsAccount,
                _destroyCt);*/
        }
        
        public void Bind(EventOrchestrator orchestrator)
        {
            if (_isBound)
            {
                Unbind();
            }

            _orchestrator = orchestrator;
            if (_orchestrator == null)
            {
                SetVisible(false);
                return;
            }

            _orchestrator.OnEventStarted += OnAnyEventStarted;
            _orchestrator.OnEventCompleted += OnAnyEventCompleted;
            _isBound = true;
            SetVisible(false);
        }
        
        private void Unbind()
        {
            if (!_isBound || _orchestrator == null)
            {
                _isBound = false;
                return;
            }

            _orchestrator.OnEventStarted -= OnAnyEventStarted;
            _orchestrator.OnEventCompleted -= OnAnyEventCompleted;
            _isBound = false;
            _orchestrator = null;
        }

        private void OnAnyEventStarted(ScheduleItem item)
        {
            SetVisible(true);
        }

        private void OnAnyEventCompleted(ScheduleItem item)
        {
            SetVisible(false);
        }

        private void SetVisible(bool value)
        {
            _button.gameObject.SetActive(value);
        }
        
        private void OnDestroy()
        {
            //TODO _destroyCt should be nulled
            _button.onClick.RemoveAllListeners();
            Unbind();
        }
    }
}
