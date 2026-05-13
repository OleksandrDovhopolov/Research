using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishingMinigameView : WindowView
    {
        [Serializable]
        private sealed class FishingMinigamePreStartSettings
        {
            [Min(0f)] public float StartDelayMinSeconds = 2f;
            [Min(0f)] public float StartDelayMaxSeconds = 4f;
            [Min(0f)] public float StartWarningDurationSeconds = 1f;
            [Min(0f)] public float StartWarningFadeDurationSeconds = 0.5f;
            [TextArea] public string DefaultInstructionText = "Wait for the signal, then tap when the circles overlap.";
        }

        private enum FishingMinigamePhase
        {
            Preparing = 0,
            Running = 1,
            Resolved = 2
        }

        private const float ResultDisplaySeconds = 1.1f;
        // private const string TapNowInstructionText = "TAP!";

        // private const float PulseAmplitude = 0.08f;
        // private const float PulseTweenDuration = 0.42f;

        private const float ResultFlashScale = 1.45f;
        private const float ResultPerfectFlashScaleBonus = 0.18f;
        private const float ResultFlashDuration = 0.42f;
        private const float ResultFlashFadeOutDuration = 0.22f;

        private const float HitPunchScale = 0.08f;
        private const float PerfectHitPunchScaleBonus = 0.04f;
        private const float TapNowPunchScale = 0.1f;
        private const float TapNowPunchDuration = 0.18f;

        private static readonly Color PulseRingGuideColor = new(1f, 1f, 1f, 0.24f);
        private static readonly Color TargetRingIdleColor = new(1f, 1f, 1f, 0.86f);
        private static readonly Color TargetRingTapNowColor = new(1f, 0.97f, 0.72f, 1f);
        private static readonly Color ShrinkingRingIdleColor = new(1f, 1f, 1f, 0.94f);
        private static readonly Color ShrinkingRingTapNowColor = new(1f, 0.92f, 0.58f, 1f);

        [SerializeField] private Sprite _timingCircleShrinking;
        [SerializeField] private Sprite _timingCircleTarget;
        [SerializeField] private Sprite _circleSuccessFlash;
        [SerializeField] private Sprite _circleFailFlash;
        [SerializeField] private Sprite _circlePulse;
        [SerializeField] private RectTransform _root;
        [SerializeField] private Button _screenTapButton;
        [SerializeField] private FishingMinigamePreStartSettings _preStartSettings = new();
        [SerializeField] private TextMeshProUGUI _instructionText;
        [SerializeField] private GameObject _startWarningObject;

        //private Button _screenTapButton;
        //private RawImage _background;
        //private RawImage _panel;
        //private RectTransform _circleRoot;
        private Image _pulseRing;
        private Image _targetRing;
        private Image _shrinkingRing;
        private Image _successFlash;
        private Image _failFlash;

        private FishingMinigameArgs _args;
        private float _elapsed;
        private float _currentRadius;
        private float _resolutionRadius;
        private bool _resolutionCommitted;
        private bool _isInsideSuccessWindow;
        private FishingMinigamePhase _phase = FishingMinigamePhase.Resolved;

        private Sequence _resultSequence;
        // private Tween _pulseTween;
        private Tween _startWarningTween;
        private CancellationTokenSource _startSequenceCts;
        private CanvasGroup _startWarningCanvasGroup;

        public event Action<FishingMinigameResolution> ResolutionCommitted;

        protected override void Awake()
        {
            base.Awake();
            EnsureBuilt();
            _screenTapButton.onClick.AddListener(OnScreenTap);
        }

        private void Update()
        {
            if (_phase != FishingMinigamePhase.Running || _args == null)
                return;

            var config = _args.RuntimeConfig;
            _elapsed += Time.unscaledDeltaTime;

            var duration = Mathf.Max(0.05f, config.ShrinkDurationSeconds);
            var progress = Mathf.Clamp01(_elapsed / duration);

            _currentRadius = Mathf.Lerp(config.StartRadius, config.EndRadius, progress);
            ApplyShrinkingRadius(_currentRadius);
            UpdateTapNowState();

            if (progress >= 1f)
                CommitTimeoutResolution();
        }

        public void Initialize(FishingMinigameArgs args)
        {
            _args = args;
            _elapsed = 0f;
            _currentRadius = Mathf.Max(1f, args?.RuntimeConfig.StartRadius ?? 1f);
            _resolutionRadius = _currentRadius;
            _resolutionCommitted = false;
            _phase = FishingMinigamePhase.Preparing;

            CancelStartSequence();
            KillAnimations();

            EnsureBuilt();
            ApplyLayout();
            ApplyInstructionText(_preStartSettings?.DefaultInstructionText);
            SetStartWarningVisibleImmediate(false);

            ResetGraphicState(_pulseRing);
            ResetGraphicState(_targetRing);
            ResetGraphicState(_shrinkingRing);
            ResetGraphicState(_successFlash);
            ResetGraphicState(_failFlash);
            ApplyIdleRingVisualState();

            SetGameplayRingsVisible(false);

            SetGraphicVisible(_successFlash, false);
            SetGraphicVisible(_failFlash, false);

            if (_screenTapButton != null)
                _screenTapButton.interactable = true;
        }

        public void BeginRunning()
        {
            CancelStartSequence();
            _elapsed = 0f;
            _resolutionCommitted = false;
            _phase = FishingMinigamePhase.Preparing;

            RunStartSequenceAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void ShowResolvingState()
        {
            CancelStartSequence();
            _phase = FishingMinigamePhase.Resolved;
            SetStartWarningVisibleImmediate(false);

            // _pulseTween?.Kill();
            // _pulseTween = null;

            if (_screenTapButton != null)
                _screenTapButton.interactable = false;
        }

        public async UniTask ShowResultAsync(bool isSuccess, bool isPerfect, CancellationToken ct)
        {
            CancelStartSequence();
            _phase = FishingMinigamePhase.Resolved;
            SetStartWarningVisibleImmediate(false);

            if (_screenTapButton != null)
                _screenTapButton.interactable = false;

            PlayResultAnimation(isSuccess, isPerfect);

            await UniTask.Delay(
                TimeSpan.FromSeconds(ResultDisplaySeconds),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                ct);
        }

        private bool _isBuilt;
        private void EnsureBuilt()
        {
            /*if (_screenTapButton != null)
                return;*/
            if (_isBuilt)
                return;
            
            _isBuilt = true;
            
            var rootRect = transform as RectTransform;
            if (rootRect == null)
                throw new InvalidOperationException("FishingMinigameView requires a RectTransform root.");

            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            /*_background = CreateRawImage("FullscreenTapTarget", rootRect, new Color32(8, 15, 28, 196));
            Stretch(_background.rectTransform);*/

            /*_screenTapButton = _background.gameObject.AddComponent<Button>();
            _screenTapButton.targetGraphic = _background;
            _screenTapButton.onClick.AddListener(OnScreenTap);*/

            /*_panel = CreateRawImage("PopupPanel", rootRect, new Color32(248, 242, 224, 255));
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
                new Vector2(520f, 520f));*/

            _pulseRing = CreateSpriteImage("PulseRing", _root, _circlePulse, PulseRingGuideColor);
            _targetRing = CreateSpriteImage("TargetRing", _root, _timingCircleTarget, Color.white);
            _shrinkingRing = CreateSpriteImage("ShrinkingRing", _root, _timingCircleShrinking, Color.white);
            _successFlash = CreateSpriteImage("SuccessFlash", _root, _circleSuccessFlash, new Color(1f, 1f, 1f, 0.96f));
            _failFlash = CreateSpriteImage("FailFlash", _root, _circleFailFlash, new Color(1f, 1f, 1f, 0.96f));

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

            ApplySprite(_pulseRing, _circlePulse, PulseRingGuideColor);
            ApplySprite(_targetRing, _timingCircleTarget, TargetRingIdleColor);
            ApplySprite(_shrinkingRing, _timingCircleShrinking, ShrinkingRingIdleColor);
            ApplySprite(_successFlash, _circleSuccessFlash, new Color(1f, 1f, 1f, 0.96f));
            ApplySprite(_failFlash, _circleFailFlash, new Color(1f, 1f, 1f, 0.96f));

            SetGameplayRingsVisible(false);
            ApplyIdleRingVisualState();
            
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
            // if (_args == null || _pulseRing == null)
            //     return;
            //
            // _pulseTween?.Kill();
            //
            // ResetGraphicState(_pulseRing);
            //
            // var targetDiameter = (_args.RuntimeConfig.TargetRadius + _args.RuntimeConfig.SuccessRadiusThreshold) * 2f;
            // SetCircleDiameter(_pulseRing.rectTransform, targetDiameter);
            //
            // _pulseTween = _pulseRing.rectTransform
            //     .DOScale(1f + PulseAmplitude, PulseTweenDuration)
            //     .SetEase(Ease.InOutSine)
            //     .SetLoops(-1, LoopType.Yoyo)
            //     .SetUpdate(true);
        }

        private async UniTaskVoid RunStartSequenceAsync(CancellationToken destroyToken)
        {
            var startSequenceCts = CancellationTokenSource.CreateLinkedTokenSource(destroyToken);
            _startSequenceCts = startSequenceCts;

            try
            {
                var minDelaySeconds = Mathf.Max(0f, _preStartSettings?.StartDelayMinSeconds ?? 0f);
                var maxDelaySeconds = Mathf.Max(minDelaySeconds, _preStartSettings?.StartDelayMaxSeconds ?? minDelaySeconds);
                var startDelaySeconds = maxDelaySeconds > minDelaySeconds
                    ? UnityEngine.Random.Range(minDelaySeconds, maxDelaySeconds)
                    : minDelaySeconds;
                var warningDurationSeconds = Mathf.Max(0f, _preStartSettings?.StartWarningDurationSeconds ?? 0f);

                if (startDelaySeconds > 0f)
                {
                    var warningLeadSeconds = Mathf.Min(warningDurationSeconds, startDelaySeconds);
                    var delayBeforeWarningSeconds = Mathf.Max(0f, startDelaySeconds - warningLeadSeconds);

                    if (delayBeforeWarningSeconds > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(delayBeforeWarningSeconds),
                            DelayType.UnscaledDeltaTime,
                            PlayerLoopTiming.Update,
                            startSequenceCts.Token);
                    }

                    if (warningLeadSeconds > 0f)
                    {
                        SetStartWarningVisibleAnimated(true);
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(warningLeadSeconds),
                            DelayType.UnscaledDeltaTime,
                            PlayerLoopTiming.Update,
                            startSequenceCts.Token);
                    }
                }

                if (startSequenceCts.IsCancellationRequested || _resolutionCommitted || _phase != FishingMinigamePhase.Preparing)
                    return;

                SetStartWarningVisibleAnimated(false);
                StartActivePhase();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_phase != FishingMinigamePhase.Running)
                    SetStartWarningVisibleImmediate(false);

                if (ReferenceEquals(_startSequenceCts, startSequenceCts))
                    _startSequenceCts = null;

                startSequenceCts.Dispose();
            }
        }

        private void StartActivePhase()
        {
            _elapsed = 0f;
            _phase = FishingMinigamePhase.Running;
            SetGameplayRingsVisible(true);
            ResetGraphicState(_pulseRing);
            ResetGraphicState(_targetRing);
            ResetGraphicState(_shrinkingRing);
            ApplyIdleRingVisualState();
            ApplyShrinkingRadius(_args?.RuntimeConfig.StartRadius ?? _currentRadius);
            // StartPulseTween();
        }

        private void OnScreenTap()
        {
            switch (_phase)
            {
                case FishingMinigamePhase.Preparing:
                    CommitEarlyTapResolution();
                    break;
                case FishingMinigamePhase.Running:
                    CommitTapResolution();
                    break;
            }
        }

        private void CommitTapResolution()
        {
            if (_resolutionCommitted || _args == null)
                return;

            _resolutionCommitted = true;
            _phase = FishingMinigamePhase.Resolved;
            SetStartWarningVisibleImmediate(false);

            // _pulseTween?.Kill();
            // _pulseTween = null;

            // Фиксируем радиус именно в момент клика или таймаута.
            // Потом success/fail flash стартует с этого же диаметра.
            _resolutionRadius = _currentRadius;

            var distance = Mathf.Abs(_currentRadius - _args.RuntimeConfig.TargetRadius);
            var isSuccess = distance <= _args.RuntimeConfig.SuccessRadiusThreshold;
            var isPerfect = isSuccess && distance <= _args.RuntimeConfig.PerfectRadiusThreshold;
            var endReason = isSuccess
                ? FishingMinigameEndReason.SuccessfulTap
                : FishingMinigameEndReason.MissedTap;

            LogResolution(isSuccess, endReason, _currentRadius);

            ResolutionCommitted?.Invoke(new FishingMinigameResolution(
                isSuccess,
                isPerfect,
                false,
                _currentRadius,
                endReason));
        }

        private void CommitEarlyTapResolution()
        {
            if (_resolutionCommitted || _args == null)
                return;

            _resolutionCommitted = true;
            _phase = FishingMinigamePhase.Resolved;
            CancelStartSequence();
            KillAnimations();
            SetStartWarningVisibleImmediate(false);
            _resolutionRadius = _currentRadius;

            LogResolution(false, FishingMinigameEndReason.EarlyTap, _currentRadius);

            ResolutionCommitted?.Invoke(new FishingMinigameResolution(
                false,
                false,
                false,
                _currentRadius,
                FishingMinigameEndReason.EarlyTap));
        }

        private void CommitTimeoutResolution()
        {
            if (_resolutionCommitted || _args == null)
                return;

            _resolutionCommitted = true;
            _phase = FishingMinigamePhase.Resolved;
            SetStartWarningVisibleImmediate(false);

            // _pulseTween?.Kill();
            // _pulseTween = null;
            _resolutionRadius = _currentRadius;

            LogResolution(false, FishingMinigameEndReason.Timeout, _currentRadius);

            ResolutionCommitted?.Invoke(new FishingMinigameResolution(
                false,
                false,
                true,
                _currentRadius,
                FishingMinigameEndReason.Timeout));
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

            // _pulseTween?.Kill();
            // _pulseTween = null;
            _startWarningTween?.Kill();
            _startWarningTween = null;

            _pulseRing?.rectTransform.DOKill();
            _targetRing?.rectTransform.DOKill();
            _shrinkingRing?.rectTransform.DOKill();
            _successFlash?.rectTransform.DOKill();
            _failFlash?.rectTransform.DOKill();

            // _pulseRing?.DOKill();
            _targetRing?.DOKill();
            _shrinkingRing?.DOKill();
            _successFlash?.DOKill();
            _failFlash?.DOKill();
        }

        private void CancelStartSequence()
        {
            if (_startSequenceCts == null)
                return;

            _startSequenceCts.Cancel();
            _startSequenceCts.Dispose();
            _startSequenceCts = null;
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

        private void SetStartWarningVisible(bool isVisible)
        {
            SetStartWarningVisibleImmediate(isVisible);
        }

        private void SetStartWarningVisibleImmediate(bool isVisible)
        {
            _startWarningTween?.Kill();
            _startWarningTween = null;

            if (_startWarningObject == null)
                return;

            var canvasGroup = GetOrCreateStartWarningCanvasGroup();
            if (canvasGroup != null)
                canvasGroup.alpha = isVisible ? 1f : 0f;

            _startWarningObject.SetActive(isVisible);
        }

        private void SetStartWarningVisibleAnimated(bool isVisible)
        {
            if (_startWarningObject == null)
                return;

            _startWarningTween?.Kill();
            _startWarningTween = null;

            var canvasGroup = GetOrCreateStartWarningCanvasGroup();
            if (canvasGroup == null)
            {
                _startWarningObject.SetActive(isVisible);
                return;
            }

            var fadeDuration = Mathf.Max(0f, _preStartSettings?.StartWarningFadeDurationSeconds ?? 0f);
            if (fadeDuration <= 0f)
            {
                canvasGroup.alpha = isVisible ? 1f : 0f;
                _startWarningObject.SetActive(isVisible);
                return;
            }

            if (isVisible)
            {
                _startWarningObject.SetActive(true);
                canvasGroup.alpha = 0f;
                _startWarningTween = DOTween
                    .To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1f, fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
                return;
            }

            canvasGroup.alpha = Mathf.Clamp01(canvasGroup.alpha);
            _startWarningTween = DOTween
                .To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _startWarningTween = null;
                    if (_startWarningObject != null)
                        _startWarningObject.SetActive(false);
                });
        }

        private CanvasGroup GetOrCreateStartWarningCanvasGroup()
        {
            if (_startWarningObject == null)
                return null;

            if (_startWarningCanvasGroup == null)
                _startWarningCanvasGroup = _startWarningObject.GetComponent<CanvasGroup>() ?? _startWarningObject.AddComponent<CanvasGroup>();

            return _startWarningCanvasGroup;
        }

        private void SetGameplayRingsVisible(bool isVisible)
        {
            SetGraphicVisible(_pulseRing, isVisible);
            SetGraphicVisible(_targetRing, isVisible);
            SetGraphicVisible(_shrinkingRing, isVisible);
        }

        private void ApplyInstructionText(string value)
        {
            if (_instructionText == null)
                return;

            _instructionText.text = value ?? string.Empty;
        }

        private void UpdateTapNowState()
        {
            if (_phase != FishingMinigamePhase.Running || _args == null)
                return;

            var distance = Mathf.Abs(_currentRadius - _args.RuntimeConfig.TargetRadius);
            var isInsideSuccessWindow = distance <= _args.RuntimeConfig.SuccessRadiusThreshold;
            if (_isInsideSuccessWindow == isInsideSuccessWindow)
                return;

            _isInsideSuccessWindow = isInsideSuccessWindow;

            if (isInsideSuccessWindow)
            {
                ApplyTapNowVisualState();
                return;
            }

            ApplyIdleRingVisualState();
        }

        private void ApplyIdleRingVisualState()
        {
            _isInsideSuccessWindow = false;
            ApplyInstructionText(_preStartSettings?.DefaultInstructionText);
            ApplyGraphicColor(_pulseRing, PulseRingGuideColor);
            ApplyGraphicColor(_targetRing, TargetRingIdleColor);
            ApplyGraphicColor(_shrinkingRing, ShrinkingRingIdleColor);

            if (_targetRing != null)
            {
                _targetRing.rectTransform.DOKill();
                _targetRing.rectTransform.localScale = Vector3.one;
            }

            if (_shrinkingRing != null)
                _shrinkingRing.rectTransform.localScale = Vector3.one;
        }

        private void ApplyTapNowVisualState()
        {
            // ApplyInstructionText(TapNowInstructionText);
            ApplyGraphicColor(_pulseRing, PulseRingGuideColor);
            ApplyGraphicColor(_targetRing, TargetRingTapNowColor);
            ApplyGraphicColor(_shrinkingRing, ShrinkingRingTapNowColor);

            if (_targetRing == null)
                return;

            _targetRing.rectTransform.DOKill();
            _targetRing.rectTransform.localScale = Vector3.one;
            _targetRing.rectTransform
                .DOPunchScale(Vector3.one * TapNowPunchScale, TapNowPunchDuration, 1, 0f)
                .SetUpdate(true);
        }

        private void LogResolution(bool isSuccess, FishingMinigameEndReason endReason, float currentRadius)
        {
            if (_args == null)
                return;

            var config = _args.RuntimeConfig;
            var targetRadius = config.TargetRadius;
            var distance = Mathf.Abs(currentRadius - targetRadius);

            Debug.LogWarning(
                $"[FishingMinigameView] Result={(isSuccess ? "Success" : "Fail")}, " +
                $"EndReason={endReason}, " +
                $"CurrentRadius={currentRadius:0.###}, " +
                $"TargetRadius={targetRadius:0.###}, " +
                $"Distance={distance:0.###}, " +
                $"SuccessThreshold={config.SuccessRadiusThreshold:0.###}, " +
                $"PerfectThreshold={config.PerfectRadiusThreshold:0.###}.");
        }

        private static void ApplyGraphicColor(Graphic graphic, Color color)
        {
            if (graphic == null)
                return;

            graphic.color = color;
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
            CancelStartSequence();
            KillAnimations();

            if (_screenTapButton != null)
                _screenTapButton.onClick.RemoveListener(OnScreenTap);
        }
    }
}
