using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CardCollectionImpl.Tests
{
    public sealed class CardCollectionStaticDataLoaderTests
    {
        [Test]
        public void LoadAsync_WithValidRequestedAddress_LoadsWithoutFallback()
        {
            var expected = CreateEventConfig("primary_pack");
            var provider = new StubEventConfigProvider();
            provider.SetResult("custom_config", expected);

            var loader = new CardCollectionStaticDataLoader(provider);
            var model = new CardCollectionEventModel { EventConfigAddress = "custom_config" };

            var result = loader.LoadAsync(model, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(provider.LoadedAddresses, Is.EqualTo(new[] { "custom_config" }));
            Assert.That(result.EventConfig, Is.SameAs(expected));
        }

        [Test]
        public void LoadAsync_WithEmptyRequestedAddress_UsesSpringFallbackAndLogsError()
        {
            var expected = CreateEventConfig("spring_pack");
            var provider = new StubEventConfigProvider();
            provider.SetResult("event_spring_collection_config", expected);

            var loader = new CardCollectionStaticDataLoader(provider);
            var model = new CardCollectionEventModel { EventConfigAddress = "   " };

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[CardCollectionStaticDataLoader\] Event config address is empty\..*event_spring_collection_config"));

            var result = loader.LoadAsync(model, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(provider.LoadedAddresses, Is.EqualTo(new[] { "event_spring_collection_config" }));
            Assert.That(result.EventConfig, Is.SameAs(expected));
        }

        [Test]
        public void LoadAsync_WhenPrimaryLoadFails_UsesSpringFallbackAndLogsError()
        {
            var expected = CreateEventConfig("spring_pack");
            var provider = new StubEventConfigProvider();
            provider.SetException("broken_config", new InvalidOperationException("primary failed"));
            provider.SetResult("event_spring_collection_config", expected);

            var loader = new CardCollectionStaticDataLoader(provider);
            var model = new CardCollectionEventModel { EventConfigAddress = "broken_config" };

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[CardCollectionStaticDataLoader\] Failed to load card collection config 'broken_config'\..*event_spring_collection_config.*primary failed"));

            var result = loader.LoadAsync(model, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(provider.LoadedAddresses, Is.EqualTo(new[] { "broken_config", "event_spring_collection_config" }));
            Assert.That(result.EventConfig, Is.SameAs(expected));
        }

        [Test]
        public void LoadAsync_WhenRequestedAddressAlreadyMatchesFallback_DoesNotRetrySameAddress()
        {
            var provider = new StubEventConfigProvider();
            provider.SetException("event_spring_collection_config", new InvalidOperationException("spring failed"));

            var loader = new CardCollectionStaticDataLoader(provider);
            var model = new CardCollectionEventModel { EventConfigAddress = "event_spring_collection_config" };

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[CardCollectionStaticDataLoader\] Failed to load card collection config 'event_spring_collection_config'\. Fallback skipped.*spring failed"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => loader.LoadAsync(model, CancellationToken.None).GetAwaiter().GetResult());

            Assert.That(ex!.Message, Is.EqualTo("spring failed"));
            Assert.That(provider.LoadedAddresses, Is.EqualTo(new[] { "event_spring_collection_config" }));
        }

        [Test]
        public void LoadAsync_WhenPrimaryAndFallbackFail_ThrowsFallbackError()
        {
            var provider = new StubEventConfigProvider();
            provider.SetException("broken_config", new InvalidOperationException("primary failed"));
            provider.SetException("event_spring_collection_config", new InvalidOperationException("fallback failed"));

            var loader = new CardCollectionStaticDataLoader(provider);
            var model = new CardCollectionEventModel { EventConfigAddress = "broken_config" };

            LogAssert.Expect(
                LogType.Error,
                new Regex(@"\[CardCollectionStaticDataLoader\] Failed to load card collection config 'broken_config'\..*event_spring_collection_config.*primary failed"));

            var ex = Assert.Throws<InvalidOperationException>(
                () => loader.LoadAsync(model, CancellationToken.None).GetAwaiter().GetResult());

            Assert.That(ex!.Message, Is.EqualTo("fallback failed"));
            Assert.That(provider.LoadedAddresses, Is.EqualTo(new[] { "broken_config", "event_spring_collection_config" }));
        }

        private static EventConfig CreateEventConfig(string packId)
        {
            return new EventConfig
            {
                packs = new List<CardPackConfig>
                {
                    new() { packId = packId, cardCount = 1, packName = packId }
                }
            };
        }

        private sealed class StubEventConfigProvider : IEventConfigProvider
        {
            private readonly Dictionary<string, EventConfig> _configsByAddress = new(StringComparer.Ordinal);
            private readonly Dictionary<string, Exception> _exceptionsByAddress = new(StringComparer.Ordinal);

            public List<string> LoadedAddresses { get; } = new();

            public EventConfig Data { get; private set; }

            public void SetResult(string address, EventConfig config)
            {
                _configsByAddress[address] = config;
            }

            public void SetException(string address, Exception exception)
            {
                _exceptionsByAddress[address] = exception;
            }

            public UniTask<EventConfig> LoadAsync(string fileName, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                LoadedAddresses.Add(fileName);

                if (_exceptionsByAddress.TryGetValue(fileName, out var exception))
                {
                    throw exception;
                }

                if (!_configsByAddress.TryGetValue(fileName, out var config))
                {
                    throw new InvalidOperationException($"Missing config for '{fileName}'.");
                }

                Data = config;
                return UniTask.FromResult(config);
            }

            public void ClearCache()
            {
                Data = null;
            }
        }
    }
}
