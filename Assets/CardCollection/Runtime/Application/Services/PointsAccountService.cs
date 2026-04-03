using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class PointsAccountService : IPointsAccountService
    {
        private readonly CardProgressService _cardProgressService;

        public PointsAccountService(CardProgressService cardProgressService)
        {
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
        }

        public async UniTask<bool> TryAddAsync(string eventId, int pointsToAdd, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _cardProgressService.AddPointsAsync(eventId, pointsToAdd, ct);
            return true;
        }

        public UniTask<bool> TrySpendAsync(string eventId, int pointsToSpend, CancellationToken ct = default)
        {
            return _cardProgressService.TrySpendPointsAsync(eventId, pointsToSpend, ct);
        }

        public UniTask<int> GetBalanceAsync(string eventId, CancellationToken ct = default)
        {
            return _cardProgressService.GetPoints(eventId, ct);
        }
    }
}
