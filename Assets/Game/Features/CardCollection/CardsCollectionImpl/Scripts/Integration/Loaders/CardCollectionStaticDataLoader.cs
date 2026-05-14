using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionStaticDataLoader : ICardCollectionStaticDataLoader
    {
        private const string FallbackEventConfigAddress = "event_spring_collection_config";

        private readonly IEventConfigProvider _eventConfigProvider;

        public CardCollectionStaticDataLoader(IEventConfigProvider eventConfigProvider)
        {
            _eventConfigProvider = eventConfigProvider;
        }

        public async UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return new CardCollectionStaticData
            {
                EventConfig = await LoadEventConfigWithFallbackAsync(model.EventConfigAddress, ct),
            };
        }

        private async UniTask<EventConfig> LoadEventConfigWithFallbackAsync(string requestedAddress, CancellationToken ct)
        {
            var normalizedAddress = string.IsNullOrWhiteSpace(requestedAddress)
                ? null
                : requestedAddress;

            if (normalizedAddress == null)
            {
                Debug.LogError(
                    $"[CardCollectionStaticDataLoader] Event config address is empty. Falling back to '{FallbackEventConfigAddress}'.");
                return await LoadEventConfigAsync(FallbackEventConfigAddress, ct);
            }

            try
            {
                return await LoadEventConfigAsync(normalizedAddress, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (string.Equals(normalizedAddress, FallbackEventConfigAddress, StringComparison.Ordinal))
                {
                    Debug.LogError(
                        $"[CardCollectionStaticDataLoader] Failed to load card collection config '{normalizedAddress}'. " +
                        $"Fallback skipped because requested address already matches fallback. Error: {ex.Message}");
                    throw;
                }

                Debug.LogError(
                    $"[CardCollectionStaticDataLoader] Failed to load card collection config '{normalizedAddress}'. " +
                    $"Falling back to '{FallbackEventConfigAddress}'. Error: {ex.Message}");

                return await LoadEventConfigAsync(FallbackEventConfigAddress, ct);
            }
        }

        private async UniTask<EventConfig> LoadEventConfigAsync(string address, CancellationToken ct)
        {
            _eventConfigProvider.ClearCache();
            await _eventConfigProvider.LoadAsync(address, ct);
            return _eventConfigProvider.Data;
        }
    }
}
