using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishingMinigameView : WindowView
    {
        private const float ResultDisplaySeconds = 1.1f;
        private const float PulseAmplitude = 0.08f;
        private const float PulseSpeed = 2.7f;

        [SerializeField] private Sprite _timingCircleOuter;
        [SerializeField] private Sprite _timingCircleShrinking;
        [SerializeField] private Sprite _timingCircleTarget;
        [SerializeField] private Sprite _circlePulse;

        private Button _screenTapButton;
        private RawImage _background;
        private RawImage _panel;
        private RectTransform _circleRoot;
        private Image _outerRing;
        private Image _pulseRing;
        private Image _targetRing;
        private Image _shrinkingRing;

        private FishingMinigameArgs _args;
        private float _elapsed;
        private float _currentRadius;
        private bool _isRunning;
        private bool _resolutionCommitted;

        public event Action<FishingMinigameResolution> ResolutionCommitted;

        protected override void Awake()
        {
            base.Awake();
            EnsureBuilt();
        }

        private void Update()
        {
            if (!_isRunning || _args == null)
                return;

            var config = _args.RuntimeConfig;
            _elapsed += Time.unscaledDeltaTime;

            var duration = Mathf.Max(0.05f, config.ShrinkDurationSeconds);
            var progress = Mathf.Clamp01(_elapsed / duration);
            _currentRadius = Mathf.Lerp(config.StartRadius, config.EndRadius, progress);

            ApplyShrinkingRadius(_currentRadius);
            ApplyPulse(progress);

            if (progress >= 1f)
                CommitResolution(isTap: false, isTimeout: true);
        }

        public void Initialize(FishingMinigameArgs args)
        {
            _args = args;
            _elapsed = 0f;
            _currentRadius = Mathf.Max(1f, args?.RuntimeConfig.StartRadius ?? 1f);
            _isRunning = false;
            _resolutionCommitted = false;

            EnsureBuilt();
            ApplyLayout();

            if (_screenTapButton != null)
                _screenTapButton.interactable = true;

            Debug.LogWarning($"[FishingMinigameView] Initialized. ZoneId='{_args.ZoneId}', AttemptId='{_args.AttemptId}', FishId='{_args.SelectedFish?.Id ?? string.Empty}', StartRadius={_args.RuntimeConfig.StartRadius:0.##}, TargetRadius={_args.RuntimeConfig.TargetRadius:0.##}, EndRadius={_args.RuntimeConfig.EndRadius:0.##}, Duration={_args.RuntimeConfig.ShrinkDurationSeconds:0.###}.");
        }

        public void BeginRunning()
        {
            _elapsed = 0f;
            _isRunning = true;
            _resolutionCommitted = false;
            Debug.LogWarning($"[FishingMinigameView] BeginRunning. AttemptId='{_args?.AttemptId.ToString() ?? string.Empty}'.");
        }

        public void ShowResolvingState()
        {
            _isRunning = false;
            
            if (_screenTapButton != null)
                _screenTapButton.interactable = false;

            Debug.LogWarning($"[FishingMinigameView] ShowResolvingState. AttemptId='{_args?.AttemptId.ToString() ?? string.Empty}'.");
        }

        public async UniTask ShowResultAsync(bool isSuccess, bool isPerfect, CancellationToken ct)
        {
            _isRunning = false;

            if (_screenTapButton != null)
                _screenTapButton.interactable = false;

            
            Debug.LogWarning($"[FishingMinigameView] ShowResult. AttemptId='{_args?.AttemptId.ToString() ?? string.Empty}', Success={isSuccess}, Perfect={isPerfect}.");

            await UniTask.Delay(TimeSpan.FromSeconds(ResultDisplaySeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
        }

        private void EnsureBuilt()
        {
            if (_screenTapButton != null)
                return;

            var rootRect = transform as RectTransform;
            if (rootRect == null)
                throw new InvalidOperationException("FishingMinigameView requires a RectTransform root.");

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            _background = CreateRawImage("FullscreenTapTarget", rootRect, new Color32(8, 15, 28, 196));
            Stretch(_background.rectTransform);
            _screenTapButton = _background.gameObject.AddComponent<Button>();
            _screenTapButton.targetGraphic = _background;
            _screenTapButton.onClick.AddListener(OnScreenTap);

            _panel = CreateRawImage("PopupPanel", rootRect, new Color32(248, 242, 224, 255));
            _panel.raycastTarget = false;
            var panelRect = _panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 820f);
            panelRect.anchoredPosition = Vector2.zero;
            
            _circleRoot = CreateRect("CircleRoot", panelRect);
            SetRect(_circleRoot, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 520f));

            _outerRing = CreateSpriteImage("OuterRing", _circleRoot, _timingCircleOuter, Color.white);
            _pulseRing = CreateSpriteImage("PulseRing", _circleRoot, _circlePulse, new Color(1f, 1f, 1f, 0.72f));
            _targetRing = CreateSpriteImage("TargetRing", _circleRoot, _timingCircleTarget, Color.white);
            _shrinkingRing = CreateSpriteImage("ShrinkingRing", _circleRoot, _timingCircleShrinking, Color.white);

            Debug.LogWarning("[FishingMinigameView] UI hierarchy was built.");
        }

        private void ApplyLayout()
        {
            if (_args == null)
                return;

            var config = _args.RuntimeConfig;
            var outerRadius = Mathf.Max(config.StartRadius, config.TargetRadius) + 24f;

            SetCircleDiameter(_outerRing.rectTransform, outerRadius * 2f);
            SetCircleDiameter(_pulseRing.rectTransform, (config.TargetRadius + config.SuccessRadiusThreshold) * 2f);
            SetCircleDiameter(_targetRing.rectTransform, config.TargetRadius * 2f);
            ApplyShrinkingRadius(config.StartRadius);

            ApplySprite(_outerRing, _timingCircleOuter, Color.white);
            ApplySprite(_pulseRing, _circlePulse, new Color(1f, 1f, 1f, 0.72f));
            ApplySprite(_targetRing, _timingCircleTarget, Color.white);
            ApplySprite(_shrinkingRing, _timingCircleShrinking, Color.white);
        }

        private void ApplyShrinkingRadius(float radius)
        {
            _currentRadius = Mathf.Max(1f, radius);
            SetCircleDiameter(_shrinkingRing.rectTransform, _currentRadius * 2f);
        }

        private void ApplyPulse(float progress)
        {
            var targetDiameter = (_args.RuntimeConfig.TargetRadius + _args.RuntimeConfig.SuccessRadiusThreshold) * 2f;
            var pulseFactor = 1f + Mathf.Sin((progress + Time.unscaledTime) * PulseSpeed) * PulseAmplitude;
            SetCircleDiameter(_pulseRing.rectTransform, targetDiameter * pulseFactor);
        }

        private void OnScreenTap()
        {
            if (!_isRunning)
                return;

            Debug.LogWarning($"[FishingMinigameView] Screen tap received. AttemptId='{_args?.AttemptId.ToString() ?? string.Empty}', Radius={_currentRadius:0.###}.");
            CommitResolution(isTap: true, isTimeout: false);
        }

        private void CommitResolution(bool isTap, bool isTimeout)
        {
            if (_resolutionCommitted || _args == null)
                return;

            _resolutionCommitted = true;
            _isRunning = false;

            var distance = Mathf.Abs(_currentRadius - _args.RuntimeConfig.TargetRadius);
            var isSuccess = isTap && distance <= _args.RuntimeConfig.SuccessRadiusThreshold;
            var isPerfect = isSuccess && distance <= _args.RuntimeConfig.PerfectRadiusThreshold;
            Debug.LogWarning($"[FishingMinigameView] CommitResolution. AttemptId='{_args.AttemptId}', IsTap={isTap}, IsTimeout={isTimeout}, Radius={_currentRadius:0.###}, TargetRadius={_args.RuntimeConfig.TargetRadius:0.###}, Distance={distance:0.###}, SuccessThreshold={_args.RuntimeConfig.SuccessRadiusThreshold:0.###}, PerfectThreshold={_args.RuntimeConfig.PerfectRadiusThreshold:0.###}, Success={isSuccess}, Perfect={isPerfect}.");
            ResolutionCommitted?.Invoke(new FishingMinigameResolution(isSuccess, isPerfect, isTimeout, _currentRadius));
        }

        private static void ApplySprite(Image image, Sprite sprite, Color color)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.color = color;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }

        private static RawImage CreateRawImage(string name, RectTransform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);

            var image = gameObject.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static Image CreateSpriteImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);

            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void SetCircleDiameter(RectTransform rectTransform, float diameter)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }
    }
}
