using System.Collections.Generic;
using CameraModule;
using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class LocationRaycaster
    {
        private const int MaxHits = 32;

        private readonly CameraBehaviour _cameraBehaviour;
        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        private readonly List<HitEntry> _hitEntries = new(MaxHits);
        private readonly List<object> _result = new(MaxHits);

        public LocationRaycaster(CameraBehaviour cameraBehaviour)
        {
            _cameraBehaviour = cameraBehaviour;
        }

        public List<object> Raycast(Vector2 screenPosition)
        {
            _result.Clear();
            _hitEntries.Clear();

            var camera = _cameraBehaviour != null ? _cameraBehaviour.Camera : Camera.main;
            if (camera == null)
                return _result;

            var ray = camera.ScreenPointToRay(screenPosition);
            var hitCount = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
            if (hitCount == 0)
                return _result;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                if (hit.transform == null)
                    continue;

                var interactable = hit.transform.GetComponentInParent<ILocationInteractable>();
                if (interactable == null || !interactable.IsInteractionEnabled || Contains(interactable))
                    continue;

                _hitEntries.Add(new HitEntry(interactable, hit.distance));
            }

            _hitEntries.Sort(CompareHits);

            for (var i = 0; i < _hitEntries.Count; i++)
                _result.Add(_hitEntries[i].Interactable);

            return _result;
        }

        private bool Contains(ILocationInteractable interactable)
        {
            for (var i = 0; i < _hitEntries.Count; i++)
            {
                if (ReferenceEquals(_hitEntries[i].Interactable, interactable))
                    return true;
            }

            return false;
        }

        private static int CompareHits(HitEntry left, HitEntry right)
        {
            var priorityComparison = right.Interactable.Priority.CompareTo(left.Interactable.Priority);
            return priorityComparison != 0
                ? priorityComparison
                : left.Distance.CompareTo(right.Distance);
        }

        private readonly struct HitEntry
        {
            public HitEntry(ILocationInteractable interactable, float distance)
            {
                Interactable = interactable;
                Distance = distance;
            }

            public ILocationInteractable Interactable { get; }
            public float Distance { get; }
        }
    }
}
