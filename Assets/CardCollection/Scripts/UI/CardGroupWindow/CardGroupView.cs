using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardGroupView : WindowView
    {
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        
        [SerializeField] private Image _icon;
        
        private Material _iconMaterial;
        
        private float _brightness = 1f;
        private float _targetBrightness = 1.5f;
        private float _changeDuration = 1f;
        private float _timer = 0f;
        
        protected override void Awake()
        {
            base.Awake();
            
            _iconMaterial = new Material(_icon.materialForRendering);
            _icon.material = _iconMaterial;
        }
        
        private void Update()
        {
            /*_timer += Time.deltaTime;

            if (_timer >= _changeDuration)
            {
                // Меняем цель яркости (чередование между 0.5 и 1)
                _targetBrightness = (_targetBrightness == 1f) ? 0.5f : 1f;
                _timer = 0f;
            }

            // Плавно интерполируем яркость между текущей и целью
            _brightness = Mathf.Lerp(_brightness, _targetBrightness, Time.deltaTime / _changeDuration);

            _iconMaterial.SetFloat(Brightness, _brightness);*/
        }
    }
}