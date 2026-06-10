using System;
using System.Collections.Generic;
using Unity.Entities;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;
using Random = Unity.Mathematics.Random;

namespace Survival
{
    // Bridges the ECS LevelUpRequest signal to the project's UI system, then
    // writes the user's choice back into ECS as a PendingUpgrade component.
    public sealed class LevelUpBridge : MonoBehaviour
    {
        [SerializeField] private UpgradeCatalog _catalog;
        [SerializeField] private int _choicesPerLevelUp = 3;
        [SerializeField] private uint _rngSeed = 7777;

        private UIManager _uiManager;
        private EntityQuery _requestQuery;
        private bool _modalOpen;
        private Random _rng;
        private readonly List<UpgradeDefinition> _picked = new();

        private bool _loggedNoCatalog;
        private bool _loggedNoUiManager;

        [Inject]
        private void Construct(UIManager uiManager)
        {
            _uiManager = uiManager;
            Debug.Log($"[LevelUpBridge] Construct — uiManager={(uiManager == null ? "NULL" : "ok")}");
        }

        private void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[LevelUpBridge] Start — DefaultGameObjectInjectionWorld is NULL");
                return;
            }

            _requestQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<LevelUpRequest>(),
                ComponentType.ReadOnly<PlayerTag>());

            _rng = Random.CreateFromIndex(_rngSeed);

            int catalogCount = _catalog == null || _catalog.Upgrades == null ? 0 : _catalog.Upgrades.Count;
            Debug.Log($"[LevelUpBridge] Start — catalog={catalogCount} upgrades, uiManager={(_uiManager == null ? "NULL" : "ok")}, world=ok");
        }

        private void Update()
        {
            if (_modalOpen) return;

            if (_catalog == null || _catalog.Upgrades == null || _catalog.Upgrades.Count == 0)
            {
                if (!_loggedNoCatalog)
                {
                    Debug.LogWarning("[LevelUpBridge] catalog is not assigned or empty — modal will never open");
                    _loggedNoCatalog = true;
                }
                return;
            }

            if (_uiManager == null)
            {
                if (!_loggedNoUiManager)
                {
                    Debug.LogError("[LevelUpBridge] UIManager is NULL — VContainer Construct() did not run; check Auto Inject GameObject on the scene scope");
                    _loggedNoUiManager = true;
                }
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            if (_requestQuery.IsEmpty) return;

            Debug.Log("[LevelUpBridge] LevelUpRequest detected — opening modal");

            _modalOpen = true;
            Time.timeScale = 0f;

            PickRandom(_catalog.Upgrades, _choicesPerLevelUp, _picked);
            Debug.Log($"[LevelUpBridge] Picked {_picked.Count} choices: " +
                      string.Join(", ", _picked.ConvertAll(u => u == null ? "<null>" : u.DisplayName)));

            var args = new LevelUpArgs(_picked, OnChoiceSelected);

            try
            {
                _uiManager.Show<LevelUpController>(args);
                Debug.Log("[LevelUpBridge] UIManager.Show<LevelUpController> called");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelUpBridge] UIManager.Show threw: {e}");
                _modalOpen = false;
                Time.timeScale = 1f;
            }
        }

        private void OnChoiceSelected(UpgradeDefinition definition)
        {
            Debug.Log($"[LevelUpBridge] OnChoiceSelected: {(definition == null ? "<null>" : definition.DisplayName)} (Type={definition?.Type}, Value={definition?.Value})");

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var em = world.EntityManager;
            if (_requestQuery.IsEmpty)
            {
                Debug.LogWarning("[LevelUpBridge] OnChoiceSelected — no LevelUpRequest entity to consume");
                goto resume;
            }

            Entity player = _requestQuery.GetSingletonEntity();

            em.AddComponentData(player, new PendingUpgrade
            {
                Type = definition.Type,
                Value = definition.Value
            });
            em.RemoveComponent<LevelUpRequest>(player);

        resume:
            Time.timeScale = 1f;
            _modalOpen = false;
        }

        // Pick up to `count` distinct upgrades from `source` into `result`.
        private void PickRandom(
            List<UpgradeDefinition> source, int count, List<UpgradeDefinition> result)
        {
            result.Clear();
            if (source.Count == 0) return;

            // Reservoir-style for small counts: shuffle a working set up to `count`.
            int take = Mathf.Min(count, source.Count);
            // Copy non-null entries.
            var pool = new List<UpgradeDefinition>(source.Count);
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null) pool.Add(source[i]);

            for (int i = 0; i < take && pool.Count > 0; i++)
            {
                int index = _rng.NextInt(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }
    }
}
