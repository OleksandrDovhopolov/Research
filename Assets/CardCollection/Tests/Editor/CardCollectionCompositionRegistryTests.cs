using System;
using System.Collections.Generic;
using CardCollection.Core;
using NUnit.Framework;

namespace CardCollection.Tests
{
    public class CardCollectionCompositionRegistryTests
    {
        [TearDown]
        public void TearDown()
        {
            CardCollectionCompositionRegistry.ResetForTests();
        }

        [Test]
        public void Resolve_WhenNotRegistered_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => CardCollectionCompositionRegistry.Resolve());
        }

        [Test]
        public void Register_WhenSameInstanceIsRegisteredTwice_IsIdempotent()
        {
            var compositionRoot = new FakeCompositionRoot();
            CardCollectionCompositionRegistry.Register(compositionRoot);

            CardCollectionCompositionRegistry.Register(compositionRoot);

            Assert.AreSame(compositionRoot, CardCollectionCompositionRegistry.Resolve());
        }

        [Test]
        public void Register_WhenDifferentInstanceIsAlreadyRegistered_ThrowsInvalidOperationException()
        {
            CardCollectionCompositionRegistry.Register(new FakeCompositionRoot());

            Assert.Throws<InvalidOperationException>(() =>
                CardCollectionCompositionRegistry.Register(new FakeCompositionRoot()));
        }

        private sealed class FakeCompositionRoot : ICardCollectionCompositionRoot
        {
            public IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs)
            {
                throw new NotImplementedException();
            }

            public IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler)
            {
                throw new NotImplementedException();
            }

            public CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
