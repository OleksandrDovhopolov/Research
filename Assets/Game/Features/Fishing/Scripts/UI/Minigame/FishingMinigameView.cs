using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishingMinigameView : WindowView
    {
        private const float ResultDisplaySeconds = 1.1f;

        private const float PulseAmplitude = 0.08f;
        private const float PulseTweenDuration = 0.42f;

        private const float ResultFlashScale = 1.45f;
        private const float ResultPerfectFlashScaleBonus = 0.18f;
        private const float ResultFlashDuration = 0.42f;
        private const float ResultFlashFadeOutDuration = 0.22f;

        private const float HitPunchScale = 0.08f;
        private const float PerfectHitPunchScaleBonus = 0.04f;

        [SerializeField] private Sprite _timingCircleShrinking;
        [SerializeField] private Sprite _timingCircleTarget;
        [SerializeField] private Sprite _circleSuccessFlash;
        [SerializeField] private Sprite _circleFailFlash;
        [SerializeField] private Sprite _circlePulse;

        private Button _screenTapButton;
        private RawImage _background;
        private RawImage _panel;
        private RectTransform _circleRoot;
        private Image _pulseRing;
        private Image _targetRing;
        private Image _shrinkingRing;
        private Image _successFlash;
        private Image _failFlash;

        private FishingMinigameArgs _args;
        private float _elapsed;
        private float _currentRadius;
        private float _resolutionRadius;
        private bool _isRunning;
        private bool _resolutionCommitted;

        private Sequence _resultSequence;
        private Tween _pulseTween;

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

            if (progress >= 1f)
                CommitResolution(isTap: false, isTimeout: true);
        }

        public void Initialize(FishingMinigameArgs args)
        {
            _args = args;
            _elapsed = 0f;
            _currentRadius = Mathf.Max(1f, args?.RuntimeConfig.StartRadius ?? 1f);
            _resolutionRadius = _currentRadius;
            _isRunning = false;
            _resolutionCommitted = false;

            KillAnimations();

            EnsureBuilt();
            ApplyLayout();

            ResetGraphicState(_pulseRing);
            ResetGraphicState(_targetRing);
            ResetGraphicState(_shrinkingRing);
            ResetGraphicState(_successFlash);
            ResetGraphicState(_failFlash);

            SetGraphicVisible(_pulseRing, true);
            SetGraphicVisible(_targetRing, true);
            SetGraphicVisible(_shrinkingRing, true);

            SetGraphicVisible(_successFlash, false);
            SetGraphicVisible(_failFlash, false);

            if (_screenTapButton != null)
                _screenTapButton.interactable = true;
        }

        public void BeginRunning()
        {
            _elapsed = 0f;
            _isRunning = true;
            _resolutionCommitted = false;

            StartPulseTween();
        }

        public void ShowResolvingState()
        {
            _isRunning = false;

            _pulseTween?.Kill();
            _pulseTween = null;

            if (_screenTapButton != null)
                _screenTapButton.interactable = false;
        }

        public async UniTask ShowResultAsync(bool isSuccess, bool isPerfect, CancellationToken ct)
        {
            _isRunning = false;

            if (_screenTapButton != null)
                _screenTapButton.interactable = false;

            PlayResultAnimation(isSuccess, isPerfect);

            await UniTask.Delay(
                TimeSpan.FromSeconds(ResultDisplaySeconds),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                ct);
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
            SetRect(
                _circleRoot,
                new Vector2(0.5f, 0.48f),
                new Vector2(0.5f, 0.48f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(520f, 520f));

            _pulseRing = CreateSpriteImage("PulseRing", _circleRoot, _circlePulse, new Color(1f, 1f, 1f, 0.72f));
            _targetRing = CreateSpriteImage("TargetRing", _circleRoot, _timingCircleTarget, Color.white);
            _shrinkingRing = CreateSpriteImage("ShrinkingRing", _circleRoot, _timingCircleShrinking, Color.white);
            _successFlash = CreateSpriteImage("SuccessFlash", _circleRoot, _circleSuccessFlash, new Color(1f, 1f, 1f, 0.96f));
            _failFlash = CreateSpriteImage("FailFlash", _circleRoot, _circleFailFlash, new Color(1f, 1f, 1f, 0.96f));

            SetGraphicVisible(_successFlash, false);
            SetGraphicVisible(_failFlash, false);
        }

        private void ApplyLayout()
        {
            if (_args == null)
                return;

            var config = _args.RuntimeConfig;
            var outerRadius = Mathf.Max(config.StartRadius, config.TargetRadius) + 24f;

            SetCircleDiameter(_pulseRing.rectTransform, (config.TargetRadius + config.SuccessRadiusThreshold) * 2f);
            SetCircleDiameter(_targetRing.rectTransform, config.TargetRadius * 2f);

            // Это дефолтный размер до результата. В момент результата flash будет переустановлен
            // в точный размер _shrinkingRing через _resolutionRadius.
            SetCircleDiameter(_successFlash.rectTransform, outerRadius * 2.08f);
            SetCircleDiameter(_failFlash.rectTransform, outerRadius * 2.08f);

            ApplyShrinkingRadius(config.StartRadius);

            ApplySprite(_pulseRing, _circlePulse, new Color(1f, 1f, 1f, 0.72f));
            ApplySprite(_targetRing, _timingCircleTarget, Color.white);
            ApplySprite(_shrinkingRing, _timingCircleShrinking, Color.white);
            ApplySprite(_successFlash, _circleSuccessFlash, new Color(1f, 1f, 1f, 0.96f));
            ApplySprite(_failFlash, _circleFailFlash, new Color(1f, 1f, 1f, 0.96f));

            SetGraphicVisible(_pulseRing, true);
            SetGraphicVisible(_targetRing, true);
            SetGraphicVisible(_shrinkingRing, true);
            
            SetGraphicVisible(_successFlash, false);
            SetGraphicVisible(_failFlash, false);
        }

        private void ApplyShrinkingRadius(float radius)
        {
            _currentRadius = Mathf.Max(1f, radius);
            SetCircleDiameter(_shrinkingRing.rectTransform, _currentRadius * 2f);
        }

        private void StartPulseTween()
        {
            if (_args == null || _pulseRing == null)
                return;

            _pulseTween?.Kill();

            ResetGraphicState(_pulseRing);

            var targetDiameter = (_args.RuntimeConfig.TargetRadius + _args.RuntimeConfig.SuccessRadiusThreshold) * 2f;
            SetCircleDiameter(_pulseRing.rectTransform, targetDiameter);

            _pulseTween = _pulseRing.rectTransform
                .DOScale(1f + PulseAmplitude, PulseTweenDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void OnScreenTap()
        {
            if (!_isRunning)
                return;

            CommitResolution(isTap: true, isTimeout: false);
        }

        private void CommitResolution(bool isTap, bool isTimeout)
        {
            if (_resolutionCommitted || _args == null)
                return;

            _resolutionCommitted = true;
            _isRunning = false;

            _pulseTween?.Kill();
            _pulseTween = null;

            // Фиксируем радиус именно в момент клика или таймаута.
            // Потом success/fail flash стартует с этого же диаметра.
            _resolutionRadius = _currentRadius;

            var distance = Mathf.Abs(_currentRadius - _args.RuntimeConfig.TargetRadius);
            var isSuccess = isTap && distance <= _args.RuntimeConfig.SuccessRadiusThreshold;
            var isPerfect = isSuccess && distance <= _args.RuntimeConfig.PerfectRadiusThreshold;

            ResolutionCommitted?.Invoke(new FishingMinigameResolution(
                isSuccess,
                isPerfect,
                isTimeout,
                _currentRadius));
        }

        private void PlayResultAnimation(bool isSuccess, bool isPerfect)
        {
            KillAnimations();

            var flash = isSuccess ? _successFlash : _failFlash;
            var otherFlash = isSuccess ? _failFlash : _successFlash;

            if (flash == null) return;

            SetGraphicVisible(otherFlash, false);

            // На результате скрываем игровые кольца, чтобы был виден только success/fail flash.
            SetGraphicVisible(_pulseRing, false);
            SetGraphicVisible(_targetRing, false);
            SetGraphicVisible(_shrinkingRing, false);

            var resultDiameter = Mathf.Max(1f, _resolutionRadius * 2f);

            // Flash стартует ровно с размера shrinking ring в момент клика/таймаута.
            SetCircleDiameter(flash.rectTransform, resultDiameter);
            SetGraphicVisible(flash, true);

            ResetGraphicState(flash);

            var flashColor = flash.color;
            flashColor.a = isPerfect ? 1f : 0.92f;
            flash.color = flashColor;

            var flashTargetScale = ResultFlashScale + (isPerfect ? ResultPerfectFlashScaleBonus : 0f);

            _resultSequence = DOTween.Sequence().SetUpdate(true);

            _resultSequence.AppendCallback(() => { flash.rectTransform.localScale = Vector3.one; });

            _resultSequence.Append(flash.rectTransform.DOScale(flashTargetScale, ResultFlashDuration)
                .SetEase(isSuccess ? Ease.OutBack : Ease.OutQuad)
                .SetUpdate(true));

            _resultSequence.AppendInterval(0.08f);

            _resultSequence.Append(DOTween.To(() => flash.color.a, alpha =>
                {
                    var color = flash.color;
                    color.a = alpha;
                    flash.color = color;
                }, 0f, ResultFlashFadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true));

            _resultSequence.Join(flash.rectTransform.DOScale(flashTargetScale + 0.16f, ResultFlashFadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true));
        }

        private void KillAnimations()
        {
            _resultSequence?.Kill();
            _resultSequence = null;

            _pulseTween?.Kill();
            _pulseTween = null;

            _pulseRing?.rectTransform.DOKill();
            _targetRing?.rectTransform.DOKill();
            _shrinkingRing?.rectTransform.DOKill();
            _successFlash?.rectTransform.DOKill();
            _failFlash?.rectTransform.DOKill();

            _pulseRing?.DOKill();
            _targetRing?.DOKill();
            _shrinkingRing?.DOKill();
            _successFlash?.DOKill();
            _failFlash?.DOKill();
        }

        private static void ResetGraphicState(Graphic graphic)
        {
            if (graphic == null)
                return;

            graphic.rectTransform.localScale = Vector3.one;

            var color = graphic.color;
            color.a = 1f;
            graphic.color = color;
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

        private static void SetGraphicVisible(Graphic graphic, bool isVisible)
        {
            if (graphic != null)
                graphic.gameObject.SetActive(isVisible);
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

        protected override void OnDestroy()
        {
            KillAnimations();

            if (_screenTapButton != null)
                _screenTapButton.onClick.RemoveListener(OnScreenTap);
        }
    }
}