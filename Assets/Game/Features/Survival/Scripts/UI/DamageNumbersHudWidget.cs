using System.Collections.Generic;
using TMPro;
using UIShared;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    // Pooled floating "-X" labels shown when damage is dealt. The bridge calls
    // Show(...) once per DamageEvent; labels float up and fade out, then return
    // to the pool. Uses Time.unscaledDeltaTime so they keep animating during
    // Time.timeScale = 0 pauses (Level Up / Game Over modals).
    //
    // If the project has a HudWidget base class, change the base type below
    // and the registry hookup follows the existing widgets in HudWidgetRegistry.
    public sealed class DamageNumbersHudWidget : MonoBehaviour, IHudWidget
    {
        [SerializeField] private Transform _container;
        [SerializeField] private TextMeshProUGUI _labelPrefab;
        [SerializeField] private float _lifetime = 0.6f;
        [SerializeField] private float _spawnHeightOffset = 2f;
        [SerializeField] private float _cameraDistanceOffset = 1f;
        [SerializeField] private float _floatHeight = 1.5f;
        [SerializeField] private Color _enemyDamageColor = Color.white;
        [SerializeField] private Color _playerDamageColor = Color.red;

        public Transform Container => _container != null ? _container : transform;

        private readonly Stack<TMP_Text> _pool = new();
        private readonly List<ActiveLabel> _active = new();

        private void Reset()
        {
            _container = transform;
        }

        private struct ActiveLabel
        {
            public TMP_Text Label;
            public RectTransform Rect;
            public Vector3 StartPos;
            public float Age;
        }

        public void Show(float3 worldPosition, float amount, bool toPlayer)
        {
            if (_labelPrefab == null)
                return;

            TMP_Text label = _pool.Count > 0 ? _pool.Pop() : Instantiate(_labelPrefab, Container);
            label.gameObject.SetActive(true);
            label.text = $"-{amount:0}";
            label.color = toPlayer ? _playerDamageColor : _enemyDamageColor;

            // World-space Canvas: rect.position is in WORLD units, not pixels.
            Vector3 spawnWorldPos = (Vector3)worldPosition + Vector3.up * _spawnHeightOffset;

            // Shift toward the camera so the label renders in front of the
            // player/enemy mesh (otherwise the 3D model occludes it).
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCamera = cam.transform.position - spawnWorldPos;
                float dist = toCamera.magnitude;
                if (dist > 0.001f)
                    spawnWorldPos += (toCamera / dist) * _cameraDistanceOffset;
            }

            RectTransform rect = label.rectTransform;
            rect.position = spawnWorldPos;

            _active.Add(new ActiveLabel
            {
                Label = label,
                Rect = rect,
                StartPos = spawnWorldPos,
                Age = 0f
            });
        }

        private void Update()
        {
            // unscaledDeltaTime: labels keep animating when the game is paused
            // via Time.timeScale = 0 (Level Up / Game Over).
            float dt = Time.unscaledDeltaTime;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveLabel a = _active[i];
                a.Age += dt;
                float t = a.Age / _lifetime;

                if (t >= 1f)
                {
                    a.Label.gameObject.SetActive(false);
                    _pool.Push(a.Label);
                    _active.RemoveAt(i);
                    continue;
                }

                a.Rect.position = a.StartPos + Vector3.up * (_floatHeight * t);
                Color c = a.Label.color;
                c.a = 1f - t;
                a.Label.color = c;
                _active[i] = a;
            }
        }
    }
}
