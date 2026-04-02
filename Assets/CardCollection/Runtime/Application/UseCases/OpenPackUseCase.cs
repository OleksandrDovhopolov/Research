using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class OpenPackUseCase : IOpenPackUseCase
    {
        private readonly CardPackService _cardPackService;
        private readonly PackBasedCardsRandomizer _cardRandomizer;
        private readonly CardProgressService _cardProgressService;
        private readonly IDuplicateCardPointsCalculator _duplicateCardPointsCalculator;

        public OpenPackUseCase(
            CardPackService cardPackService,
            PackBasedCardsRandomizer cardRandomizer,
            CardProgressService cardProgressService,
            IDuplicateCardPointsCalculator duplicateCardPointsCalculator)
        {
            _cardPackService = cardPackService;
            _cardRandomizer = cardRandomizer;
            _cardProgressService = cardProgressService;
            _duplicateCardPointsCalculator = duplicateCardPointsCalculator;
        }

        public async UniTask<OpenPackResult> ExecuteAsync(string eventId, string packId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var pack = _cardPackService.GetPackById(packId);
            if (pack == null)
            {
                return OpenPackResult.Empty;
            }

            var openedCardIds = await _cardRandomizer.GetRandomNewCardsAsync(pack, ct);
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return OpenPackResult.Empty;
            }

            var openedCardsProgress = await _cardProgressService.GetCardsByIdsAsync(eventId, openedCardIds, ct);
            var duplicatePoints = _duplicateCardPointsCalculator.Calculate(openedCardIds, openedCardsProgress);
            if (duplicatePoints.HasPoints)
            {
                await _cardProgressService.AddPointsAsync(eventId, duplicatePoints.TotalPoints, ct);
            }

            await _cardProgressService.UnlockCardsAsync(eventId, openedCardIds, ct);

            return new OpenPackResult(openedCardIds, duplicatePoints.TotalPoints);
        }
    }
}
