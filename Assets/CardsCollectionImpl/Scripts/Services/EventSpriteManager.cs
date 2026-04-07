using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Core;
using EventOrchestration.Models;
using Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace CardCollectionImpl
{
    public sealed class EventSpriteManager : IEventSpriteManager, IDisposable
    {
        private const int ReleaseDelay = 1000;
        
        private sealed class SpriteBinding
        {
            public string Address;
            public WeakReference<Image> ImageRef;
        }

        private sealed class EventState
        {
            public readonly List<SpriteBinding> Bindings = new();
            public readonly Dictionary<string, Sprite> SpriteByAddress = new(StringComparer.Ordinal);
            public readonly Dictionary<string, UniTask<Sprite>> InFlightByAddress = new(StringComparer.Ordinal);
            public readonly Dictionary<string, SpriteAtlas> AtlasByAddress = new(StringComparer.Ordinal);
            public readonly Dictionary<string, UniTask<SpriteAtlas>> InFlightAtlasByAddress = new(StringComparer.Ordinal);
        }

        private readonly object _gate = new();
        private readonly Dictionary<string, EventState> _stateByEventId = new(StringComparer.Ordinal);
        private readonly EventOrchestrator _eventOrchestrator;
        
        private readonly CancellationTokenSource _disposeCts = new();
        
        public EventSpriteManager(EventOrchestrator eventOrchestrator)
        {
            _eventOrchestrator = eventOrchestrator ?? throw new ArgumentNullException(nameof(eventOrchestrator));
            _eventOrchestrator.OnEventCompleted += HandleEventCompleted;
        }

        public async UniTask<Sprite> BindSpriteAsync(string eventId, string address, Image image, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("EventId is null or empty.", nameof(eventId));
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is null or empty.", nameof(address));
            if (image == null) throw new ArgumentNullException(nameof(image));

            UniTask<Sprite> loadTask;
            Sprite cachedSprite = null;
            var shouldLoad = false;

            lock (_gate)
            {
                var state = GetOrCreateEventState(eventId);
                state.Bindings.Add(new SpriteBinding
                {
                    Address = address,
                    ImageRef = new WeakReference<Image>(image),
                });

                if (state.SpriteByAddress.TryGetValue(address, out cachedSprite) && cachedSprite != null)
                {
                    loadTask = default;
                }
                else if (state.InFlightByAddress.TryGetValue(address, out loadTask))
                {
                }
                else
                {
                    loadTask = ProdAddressablesWrapper.LoadAsync<Sprite>(address, ct).AsUniTask();
                    state.InFlightByAddress[address] = loadTask;
                    shouldLoad = true;
                }
            }

            if (cachedSprite != null)
            {
                if (image != null)
                {
                    image.sprite = cachedSprite;
                }

                return cachedSprite;
            }

            Sprite loadedSprite;
            try
            {
                loadedSprite = await loadTask;
                ct.ThrowIfCancellationRequested();
            }
            catch
            {
                if (shouldLoad)
                {
                    lock (_gate)
                    {
                        if (_stateByEventId.TryGetValue(eventId, out var state))
                        {
                            state.InFlightByAddress.Remove(address);
                        }
                    }
                }

                throw;
            }

            var shouldAssign = false;
            lock (_gate)
            {
                if (_stateByEventId.TryGetValue(eventId, out var state))
                {
                    if (shouldLoad)
                    {
                        state.InFlightByAddress.Remove(address);
                        state.SpriteByAddress.TryAdd(address, loadedSprite);
                    }

                    shouldAssign = true;
                }
            }

            if (!shouldAssign)
            {
                // Event ended while load was in-flight; release immediately.
                ProdAddressablesWrapper.Release(address);
                return loadedSprite;
            }

            if (image != null)
            {
                image.sprite = loadedSprite;
            }

            return loadedSprite;
        }

        public async UniTask<Sprite> BindSpriteFromAtlasAsync(
            string eventId,
            string atlasAddress,
            string spriteName,
            Image image,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("EventId is null or empty.", nameof(eventId));
            if (string.IsNullOrWhiteSpace(atlasAddress)) throw new ArgumentException("Atlas address is null or empty.", nameof(atlasAddress));
            if (string.IsNullOrWhiteSpace(spriteName)) throw new ArgumentException("Sprite name is null or empty.", nameof(spriteName));
            if (image == null) throw new ArgumentNullException(nameof(image));

            UniTask<SpriteAtlas> loadTask;
            SpriteAtlas cachedAtlas = null;
            var shouldLoadAtlas = false;
            var bindingAddress = $"{atlasAddress}[{spriteName}]";

            lock (_gate)
            {
                var state = GetOrCreateEventState(eventId);
                state.Bindings.Add(new SpriteBinding
                {
                    Address = bindingAddress,
                    ImageRef = new WeakReference<Image>(image),
                });

                if (state.AtlasByAddress.TryGetValue(atlasAddress, out cachedAtlas) && cachedAtlas != null)
                {
                    loadTask = default;
                }
                else if (state.InFlightAtlasByAddress.TryGetValue(atlasAddress, out loadTask))
                {
                }
                else
                {
                    loadTask = ProdAddressablesWrapper.LoadAsync<SpriteAtlas>(atlasAddress, ct).AsUniTask();
                    state.InFlightAtlasByAddress[atlasAddress] = loadTask;
                    shouldLoadAtlas = true;
                }
            }

            SpriteAtlas loadedAtlas = cachedAtlas;
            if (loadedAtlas == null)
            {
                try
                {
                    loadedAtlas = await loadTask;
                    ct.ThrowIfCancellationRequested();
                }
                catch
                {
                    if (shouldLoadAtlas)
                    {
                        lock (_gate)
                        {
                            if (_stateByEventId.TryGetValue(eventId, out var state))
                            {
                                state.InFlightAtlasByAddress.Remove(atlasAddress);
                            }
                        }
                    }

                    throw;
                }
            }

            var shouldAssign = false;
            lock (_gate)
            {
                if (_stateByEventId.TryGetValue(eventId, out var state))
                {
                    if (shouldLoadAtlas)
                    {
                        state.InFlightAtlasByAddress.Remove(atlasAddress);
                        state.AtlasByAddress.TryAdd(atlasAddress, loadedAtlas);
                    }

                    shouldAssign = true;
                }
            }

            var sprite = loadedAtlas.GetSprite(spriteName);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve sprite '{spriteName}' from atlas '{atlasAddress}'.");
            }

            if (!shouldAssign)
            {
                // Event ended while this call owned an in-flight atlas load; release immediately.
                if (shouldLoadAtlas)
                {
                    ProdAddressablesWrapper.Release(atlasAddress);
                }
                return sprite;
            }

            if (image != null)
            {
                image.sprite = sprite;
            }

            return sprite;
        }

        private async UniTaskVoid ReleaseEventAsync(string eventId, CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(ReleaseDelay, cancellationToken: ct);
                ReleaseEvent(eventId);
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        public void ReleaseEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return;

            EventState state = null;
            lock (_gate)
            {
                _stateByEventId.Remove(eventId, out state);
            }

            if (state == null)
                return;

            foreach (var binding in state.Bindings)
            {
                if (binding.ImageRef != null && binding.ImageRef.TryGetTarget(out var image) && image != null)
                {
                    image.sprite = null;
                }
            }

            foreach (var address in state.SpriteByAddress.Keys
                         .Concat(state.AtlasByAddress.Keys)
                         .Distinct(StringComparer.Ordinal))
            {
                ProdAddressablesWrapper.Release(address);
            }
        }

        public void ReleaseAll()
        {
            string[] eventIds;
            lock (_gate)
            {
                eventIds = _stateByEventId.Keys.ToArray();
            }

            foreach (var eventId in eventIds)
            {
                ReleaseEvent(eventId);
            }
        }

        public void Dispose()
        {
            _eventOrchestrator.OnEventCompleted -= HandleEventCompleted;
            _disposeCts.Cancel();
            _disposeCts.Dispose();
            ReleaseAll();
        }

        private void HandleEventCompleted(ScheduleItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
                return;
            
            ReleaseEventAsync(item.Id, _disposeCts.Token).Forget();
        }

        private EventState GetOrCreateEventState(string eventId)
        {
            if (!_stateByEventId.TryGetValue(eventId, out var state))
            {
                state = new EventState();
                _stateByEventId[eventId] = state;
            }

            return state;
        }
    }
}
