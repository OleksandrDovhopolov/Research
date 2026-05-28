using TMPro;
using UIShared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Survival
{
    // Reflects the player's Health / MaxHealth on a Slider + TMP_Text each
    // frame. Lives in the regular scene (not SubScene). Wire references in
    // the inspector: Slider drives the bar fill, Text shows "85 / 100".
    //
    // Polls ECS each LateUpdate — no event subscription needed. Uses
    // Time.unscaledDeltaTime so the bar keeps animating during Level-Up modal
    // pauses (timeScale = 0).
    public sealed class PlayerHealthHudWidget : MonoBehaviour//, IHudWidget
    {
        [SerializeField] private Slider _bar;
        [SerializeField] private TextMeshProUGUI _text;
        [Tooltip("How fast the bar lerps toward the target value. ~12 feels snappy, " +
                 "~6 feels weighty. Higher = bar follows damage instantly.")]
        [SerializeField] private float _lerpSpeed = 12f;

        public Transform Container => transform;

        private EntityQuery _query;
        private bool _queryReady;
        private float _displayed;

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!_queryReady)
            {
                _query = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlayerTag>(),
                    ComponentType.ReadOnly<Health>(),
                    ComponentType.ReadOnly<MaxHealth>());
                _queryReady = true;
            }

            // Player entity not baked yet (or already dead) — leave bar as-is.
            if (_query.CalculateEntityCount() != 1)
                return;

            using var entities = _query.ToEntityArray(Allocator.Temp);
            var em = world.EntityManager;
            Entity player = entities[0];

            float current = math.max(0f, em.GetComponentData<Health>(player).Value);
            float max = math.max(0.001f, em.GetComponentData<MaxHealth>(player).Value);

            float t = math.saturate(_lerpSpeed * Time.unscaledDeltaTime);
            _displayed = math.lerp(_displayed, current, t);

            if (_bar != null)
                _bar.value = _displayed / max;
            if (_text != null)
                _text.text = $"{current:0} / {max:0}";
        }
    }
}
