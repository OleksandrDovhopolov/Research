using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using UIShared;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionHudPresenter 
    {
        private const string CardCollectionButtonId = "CardCollection";
        
        private readonly IHUDService _hudService;
        private readonly ICardCollectionReader _reader;
        private readonly ICardCollectionModule _module;
        private readonly IWindowPresenter _windowPresenter;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        
        private IEventButton _eventButton;
        
        public CardCollectionHudPresenter(
            IHUDService hudService,
            ICardCollectionReader reader,
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            IWindowPresenter windowPresenter,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _windowPresenter = windowPresenter ?? throw new ArgumentNullException(nameof(windowPresenter));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardDefinitionFactory = rewardDefinitionFactory ?? throw new ArgumentNullException(nameof(rewardDefinitionFactory));
        }

        public void Bind(ScheduleItem config, CancellationToken ct)
        {
            var entryButton = _hudService.SpawnEventButton(CardCollectionButtonId);

            if (entryButton == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] No button found for {CardCollectionButtonId}]");
                return;
            }
            
            _eventButton = entryButton;
            _eventButton.Setup(config, () => OnEventButtonClickHandler(ct), ct);
            _eventButton.SetVisible(true);
        }

        private void OnEventButtonClickHandler(CancellationToken ct)
        {
            _windowPresenter.OpenCardCollectionWindow( 
                _module,
                _reader,
                _exchangeOfferProvider,
                _rewardDefinitionFactory,
                _pointsAccount,
                ct).Forget();
        }
        
        public void Unbind()
        {
            _eventButton = null;
            _hudService.RemoveEventButton(CardCollectionButtonId);
        }
        
        public void Dispose()
        {
            
        }
    }
}