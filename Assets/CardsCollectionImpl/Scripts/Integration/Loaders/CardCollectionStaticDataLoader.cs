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
        private readonly ICardPackProvider _cardPackProvider;
        private readonly ICardsConfigProvider _cardsConfigProvider;
        private readonly ICardGroupsConfigProvider _cardGroupsConfigProvider;

        public CardCollectionStaticDataLoader(
            ICardPackProvider cardPackProvider,
            ICardsConfigProvider cardsConfigProvider,
            ICardGroupsConfigProvider cardGroupsConfigProvider)
        {
            _cardPackProvider = cardPackProvider;
            _cardsConfigProvider = cardsConfigProvider;
            _cardGroupsConfigProvider = cardGroupsConfigProvider;
        }

        public async UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            ValidateResources(model.CardPacksFileName, model.CardCollectionFileName, model.GroupsFileName);

            _cardsConfigProvider.ClearCache();
            _cardGroupsConfigProvider.ClearCache();
            _cardPackProvider.ClearCache();

            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(model.CardPacksFileName, ct),
                _cardsConfigProvider.LoadAsync(model.CardCollectionFileName, ct),
                _cardGroupsConfigProvider.LoadAsync(model.GroupsFileName, ct));

            return new CardCollectionStaticData
            {
                Packs = _cardPackProvider.Data,
                Cards = _cardsConfigProvider.Data,
                Groups = _cardGroupsConfigProvider.Data
            };
        }

        private static void ValidateResources(params string[] paths)
        {
            foreach (var path in paths)
            {
                var asset = Resources.Load<TextAsset>(path);
                if (asset == null)
                {
                    throw new System.IO.FileNotFoundException(
                        $"[ConfigError] File not found in Resources: {path}. " +
                        "Ensure the file exists and extension (.json) is omitted.");
                }

                Resources.UnloadAsset(asset);
            }
        }
    }
}
