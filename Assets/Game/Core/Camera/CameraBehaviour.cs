using System;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using Fabros.TileEditor;
using Game.Features.Locations;
using InputSystem;
using Lean.Common;
using Lean.Touch;
using UnityEngine;
using Utils;
using VContainer;

namespace CameraModule
{
    public class CameraBehaviour : MonoBehaviour, ICameraFocusService
    {
        private const float DefaultOrthographicSize = 30f;
        private const float DefaultFocusSize = 15f;
        private const int FocusBoundsSearchIterations = 12;
        private const float BoundsEpsilon = 0.001f;

        [SerializeField] Camera camera;
        
        private IObjectResolver _resolver;
        private CameraSettings _settings;
        private BaseStateMachine _stateMachine;
        private MainLocationBootstrap _mainLocationBootstrap;
        private bool _locationBootstrapResolutionAttempted;
        private CameraBoundsDefinition _cameraBoundsDefinition;
        private bool _cameraBoundsDefinitionResolutionAttempted;
        private Rect _locationBounds;
        private bool _hasLocationBounds;
        private Location _locationBoundsSource;
        private CancellationTokenSource _focusCancellationTokenSource;
        private Tween _focusTween;
        
        private LeanFingerFilter _fingerFilter = new(true);
        private LeanScreenDepth _screenDepth = new(LeanScreenDepth.ConversionType.DepthIntercept);
        
        public Vector3 CameraPosition => transform.position;
        public Camera Camera => camera;
        public float MaxScale => _settings.ZoomMin * _settings.ZoomMultiplier;
        public float MinScale => _settings.ZoomMax * _settings.ZoomMultiplier;
        public bool IsMoving => _stateMachine?.CurrentState is CameraDragState;
        public bool IsFocusActive => _focusTween != null && _focusTween.IsActive();
        public LeanFingerFilter FingerFilter => _fingerFilter;
        public LeanScreenDepth ScreenDepth => _screenDepth;
        
        public Vector3 MoveOffset { get; set; }
        public Vector3 MoveDelta { get; set; }
        public Vector3 ShadowPosition { get; set; }
        public float ScaleDelta { get; set; }
        
        public event Action<float> ActionOrthographicSize;
        public float OrthographicSize
        {
            get => Camera.orthographicSize;
            set
            {
                Camera.orthographicSize = value;
                ActionOrthographicSize?.Invoke(value);
            }
        }
        
        [Inject]
        private void Construct(IObjectResolver diContainer, CameraSettings cameraSettings, ScreenPointConverter screenPointConverter)
        {
            _resolver = diContainer;
            _settings = cameraSettings;
            screenPointConverter.Configurate(this);
            
            Configure();
            
            _stateMachine = new BaseStateMachine(diContainer);
            _stateMachine.AddState(new CameraIdleState(this, _settings));
            _stateMachine.AddState(new CameraDragState(this, _settings));
            _stateMachine.AddState(new CameraPinchZoomState(this, _settings));
            _stateMachine.AddState(new CameraOneHandZoomState(this, _settings));
            _stateMachine.ChangeState<CameraIdleState>();
        }
        
        private void Configure()
        {
            _screenDepth = GetScreenDepth();
            _fingerFilter.UpdateRequiredSelectable(gameObject);
            SetOrthographicSize(Mathf.Clamp(DefaultOrthographicSize, MaxScale, MinScale));
            ApplyConstraints();
        }
        
        private LeanScreenDepth GetScreenDepth()
        {
            var screenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.PlaneIntercept);
            var leanPlane = new GameObject("LeanPlane").AddComponent<LeanPlane>();
            leanPlane.transform.eulerAngles = new Vector3(90, 0, 0);
            leanPlane.MinY = leanPlane.MinX = -1000;
            leanPlane.MaxY = leanPlane.MaxX = 1000;
            screenDepth.Object = leanPlane;
            return screenDepth;
        }
        
        public void ResetMoveData()
        {
            MoveOffset = MoveDelta = Vector3.zero;
            ShadowPosition = transform.position;
        }

        public void ApplyConstraints()
        {
            if (camera == null || _settings == null)
                return;

            TryRefreshLocationBounds();

            var clampedSize = ClampOrthographicSizeToBounds(camera.orthographicSize);
            if (!Mathf.Approximately(camera.orthographicSize, clampedSize))
                SetOrthographicSize(clampedSize);

            ClampPositionToBounds();
        }

        public async Task FocusAsync(
            ILocationInteractable interactable,
            Vector3 fallbackWorldPosition,
            float targetSize = DefaultFocusSize,
            CancellationToken cancellationToken = default)
        {
            var worldPoint = ResolveFocusPoint(interactable, fallbackWorldPosition);
            await FocusAsync(worldPoint, targetSize, cancellationToken);
        }

        public async Task FocusAsync(
            Vector3 worldPoint,
            float targetSize = DefaultFocusSize,
            CancellationToken cancellationToken = default)
        {
            CancelActiveFocus();

            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _focusCancellationTokenSource = linkedCancellation;

            IDisposable inputBlock = null;
            CancellationTokenRegistration cancellationRegistration = default;
            Tween focusTween = null;

            try
            {
                linkedCancellation.Token.ThrowIfCancellationRequested();

                inputBlock = AcquireInputBlock();
                PrepareForFocus();
                TryRefreshLocationBounds();

                var targetPosition = new Vector3(worldPoint.x, transform.position.y, worldPoint.z);
                var targetOrthographicSize = ClampOrthographicSizeToBounds(targetSize);
                var duration = Mathf.Max(0f, _settings.CameraFocusDuration);

                if (duration <= 0f)
                {
                    transform.position = targetPosition;
                    SetOrthographicSize(targetOrthographicSize);
                    ApplyConstraints();
                    linkedCancellation.Token.ThrowIfCancellationRequested();
                    return;
                }

                cancellationRegistration = linkedCancellation.Token.Register(CancelActiveFocus);
                focusTween = CreateFocusTween(targetPosition, targetOrthographicSize, duration);
                _focusTween = focusTween;

                await focusTween.AsyncWaitForCompletion();
                linkedCancellation.Token.ThrowIfCancellationRequested();
                ApplyConstraints();
            }
            finally
            {
                cancellationRegistration.Dispose();
                inputBlock?.Dispose();

                if (ReferenceEquals(_focusTween, focusTween))
                    _focusTween = null;

                if (ReferenceEquals(_focusCancellationTokenSource, linkedCancellation))
                    _focusCancellationTokenSource = null;

                linkedCancellation.Dispose();
                ApplyConstraints();
            }
        }

        public void LateUpdate()
        {
            if (_stateMachine?.CurrentState == null)
                return;

            TryRefreshLocationBounds();
            ((BaseCameraState)_stateMachine.CurrentState).LateUpdate();
            ApplyConstraints();
        }
        
        public void DragStart()
        {
            if (IsFocusActive || _stateMachine?.CurrentState == null)
                return;

            ((BaseCameraState)_stateMachine.CurrentState).OnDragStart();
        }

        public void Drag()
        {
            if (IsFocusActive || _stateMachine?.CurrentState == null)
                return;

            ((BaseCameraState)_stateMachine.CurrentState).OnDrag();
        }
        
        public void DragEnd()
        {
            if (IsFocusActive || _stateMachine?.CurrentState == null)
                return;

            ((BaseCameraState)_stateMachine.CurrentState).OnDragEnd();
        }
        
        protected void OnDestroy()
        {
            CancelActiveFocus();
            transform.DOKill();
            _stateMachine?.Clear();
        }

        private void PrepareForFocus()
        {
            transform.DOKill();
            _stateMachine?.ChangeState<CameraIdleState>();
            ScaleDelta = 0f;
            ResetMoveData();
            ApplyConstraints();
        }

        private Tween CreateFocusTween(Vector3 targetPosition, float targetOrthographicSize, float duration)
        {
            return DOTween.Sequence()
                .SetTarget(this)
                .Join(DOTween.To(() => OrthographicSize, SetOrthographicSize, targetOrthographicSize, duration))
                .Join(transform.DOMoveX(targetPosition.x, duration))
                .Join(transform.DOMoveZ(targetPosition.z, duration))
                .OnUpdate(ApplyConstraints)
                .OnComplete(ApplyConstraints);
        }

        private void CancelActiveFocus()
        {
            if (_focusCancellationTokenSource != null && !_focusCancellationTokenSource.IsCancellationRequested)
                _focusCancellationTokenSource.Cancel();

            if (_focusTween != null && _focusTween.IsActive())
                _focusTween.Kill(false);
        }

        private IDisposable AcquireInputBlock()
        {
            return TryResolveInput(out var input) ? input.Block() : null;
        }

        private bool TryResolveInput(out IInput input)
        {
            input = null;
            if (_resolver == null || !_resolver.TryResolve(typeof(IInput), out var resolved) || resolved is not IInput typedInput)
                return false;

            input = typedInput;
            return true;
        }

        private bool TryRefreshLocationBounds()
        {
            var cameraBoundsDefinition = ResolveCameraBoundsDefinition();
            if (cameraBoundsDefinition != null && cameraBoundsDefinition.TryGetBounds(out var manualBounds))
            {
                _locationBounds = manualBounds;
                _locationBoundsSource = null;
                _hasLocationBounds = true;
                return true;
            }

            var bootstrap = ResolveLocationBootstrap();
            var location = bootstrap?.CurrentLocation;
            if (location == null)
                return _hasLocationBounds;

            if (_hasLocationBounds && ReferenceEquals(_locationBoundsSource, location))
                return true;

            if (!TryBuildLocationBounds(location, bootstrap.GridSize, out var locationBounds))
                return false;

            _locationBounds = locationBounds;
            _locationBoundsSource = location;
            _hasLocationBounds = true;
            return true;
        }

        private CameraBoundsDefinition ResolveCameraBoundsDefinition()
        {
            if (_cameraBoundsDefinition != null)
                return _cameraBoundsDefinition;

            if (_cameraBoundsDefinitionResolutionAttempted)
                return null;

            _cameraBoundsDefinitionResolutionAttempted = true;
            _cameraBoundsDefinition = FindObjectOfType<CameraBoundsDefinition>();
            return _cameraBoundsDefinition;
        }

        private MainLocationBootstrap ResolveLocationBootstrap()
        {
            if (_mainLocationBootstrap != null)
                return _mainLocationBootstrap;

            if (_locationBootstrapResolutionAttempted || _resolver == null)
                return null;

            _locationBootstrapResolutionAttempted = true;
            if (_resolver.TryResolve(typeof(MainLocationBootstrap), out var resolved))
                _mainLocationBootstrap = resolved as MainLocationBootstrap;

            return _mainLocationBootstrap;
        }

        private static bool TryBuildLocationBounds(Location location, Vector2 gridSize, out Rect locationBounds)
        {
            var hasPoint = false;
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;

            foreach (var locationObject in location.IterateObjects())
            {
                if (locationObject == null)
                    continue;

                var worldPoint = locationObject.Cell != null
                    ? location.transform.TransformPoint(new Vector3(locationObject.Cell.X * gridSize.x, 0f, locationObject.Cell.Y * gridSize.y))
                    : locationObject.transform.position;

                hasPoint = true;
                minX = Mathf.Min(minX, worldPoint.x);
                maxX = Mathf.Max(maxX, worldPoint.x);
                minZ = Mathf.Min(minZ, worldPoint.z);
                maxZ = Mathf.Max(maxZ, worldPoint.z);
            }

            if (!hasPoint)
            {
                locationBounds = default;
                return false;
            }

            var halfCellX = gridSize.x * 0.5f;
            var halfCellZ = gridSize.y * 0.5f;
            locationBounds = Rect.MinMaxRect(minX - halfCellX, minZ - halfCellZ, maxX + halfCellX, maxZ + halfCellZ);
            return true;
        }

        private float ClampOrthographicSizeToBounds(float requestedSize)
        {
            var clampedBySettings = Mathf.Clamp(requestedSize, MaxScale, MinScale);
            if (!_hasLocationBounds)
                return clampedBySettings;

            if (DoesViewportFit(clampedBySettings))
                return clampedBySettings;

            if (!DoesViewportFit(MaxScale))
                return MaxScale;

            var min = MaxScale;
            var max = clampedBySettings;
            for (var i = 0; i < FocusBoundsSearchIterations; i++)
            {
                var mid = (min + max) * 0.5f;
                if (DoesViewportFit(mid))
                    min = mid;
                else
                    max = mid;
            }

            return min;
        }

        private bool DoesViewportFit(float orthographicSize)
        {
            if (!_hasLocationBounds || camera == null)
                return true;

            var originalSize = camera.orthographicSize;
            camera.orthographicSize = orthographicSize;
            var viewportRect = GetViewportRectOnGround();
            camera.orthographicSize = originalSize;

            return viewportRect.width <= _locationBounds.width + BoundsEpsilon
                && viewportRect.height <= _locationBounds.height + BoundsEpsilon;
        }

        private void ClampPositionToBounds()
        {
            if (!_hasLocationBounds)
                return;

            var viewportRect = GetViewportRectOnGround();
            var offset = CalculateBoundsOffset(viewportRect);
            if (offset.sqrMagnitude <= BoundsEpsilon * BoundsEpsilon)
                return;

            var position = transform.position;
            transform.position = new Vector3(position.x + offset.x, position.y, position.z + offset.y);
        }

        private Vector2 CalculateBoundsOffset(Rect viewportRect)
        {
            var offsetX = CalculateAxisOffset(viewportRect.xMin, viewportRect.xMax, viewportRect.center.x, _locationBounds.xMin, _locationBounds.xMax, _locationBounds.center.x);
            var offsetZ = CalculateAxisOffset(viewportRect.yMin, viewportRect.yMax, viewportRect.center.y, _locationBounds.yMin, _locationBounds.yMax, _locationBounds.center.y);
            return new Vector2(offsetX, offsetZ);
        }

        private static float CalculateAxisOffset(float viewMin, float viewMax, float viewCenter, float boundsMin, float boundsMax, float boundsCenter)
        {
            if (viewMax - viewMin >= boundsMax - boundsMin - BoundsEpsilon)
                return boundsCenter - viewCenter;

            if (viewMin < boundsMin)
                return boundsMin - viewMin;

            if (viewMax > boundsMax)
                return boundsMax - viewMax;

            return 0f;
        }

        private Rect GetViewportRectOnGround()
        {
            var bottomLeft = ViewportToGroundPoint(new Vector2(0f, 0f));
            var topLeft = ViewportToGroundPoint(new Vector2(0f, 1f));
            var topRight = ViewportToGroundPoint(new Vector2(1f, 1f));
            var bottomRight = ViewportToGroundPoint(new Vector2(1f, 0f));

            var minX = Mathf.Min(bottomLeft.x, topLeft.x, topRight.x, bottomRight.x);
            var maxX = Mathf.Max(bottomLeft.x, topLeft.x, topRight.x, bottomRight.x);
            var minZ = Mathf.Min(bottomLeft.z, topLeft.z, topRight.z, bottomRight.z);
            var maxZ = Mathf.Max(bottomLeft.z, topLeft.z, topRight.z, bottomRight.z);

            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private Vector3 ViewportToGroundPoint(Vector2 viewportPoint)
        {
            var ray = camera.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0f));
            return ScreenPointConverter.XZPlane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : ray.origin;
        }

        private Vector3 ResolveFocusPoint(ILocationInteractable interactable, Vector3 fallbackWorldPosition)
        {
            /*if (interactable?.HudAnchor != null)
                return interactable.HudAnchor.position;*/

            if (interactable is Component component)
                return component.transform.position;

            return fallbackWorldPosition;
        }

        private void SetOrthographicSize(float value)
        {
            camera.orthographicSize = value;
            ActionOrthographicSize?.Invoke(value);
        }
    }
}

