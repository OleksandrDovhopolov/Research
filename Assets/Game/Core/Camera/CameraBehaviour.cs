using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using InputSystem;
using Lean.Common;
using Lean.Touch;
using UnityEngine;
using Utils;
using VContainer;

namespace CameraModule
{
    public class CameraBehaviour : MonoBehaviour
    {
        [SerializeField] Camera camera;
        
        private CameraSettings _settings;
        private BaseStateMachine _stateMachine;
        //private ICameraBorderController _borderController;
        
        private LeanFingerFilter _fingerFilter = new(true);
        private LeanScreenDepth _screenDepth = new(LeanScreenDepth.ConversionType.DepthIntercept);
        
        public Vector3 CameraPosition => transform.position;
        //public Corners FocusCorners => new(_focusBounds, _screenRect);
        public Camera Camera => camera;
        // Camera scale limits
        public float MaxScale => _settings.ZoomMin / ((float)Screen.width / Screen.height) * _settings.ZoomMultiplier;
        public float MinScale => _settings.ZoomMax / ((float)Screen.width / Screen.height) * _settings.ZoomMultiplier;
        public bool IsMoving => _stateMachine.CurrentState is CameraDragState;
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
            camera.orthographicSize = 13f;
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


        public void LateUpdate()
        {
            ((BaseCameraState)_stateMachine.CurrentState).LateUpdate();
        }
        
        public void DragStart()
        {
            ((BaseCameraState)_stateMachine.CurrentState).OnDragStart();
        }

        public void Drag()
        {
            ((BaseCameraState)_stateMachine.CurrentState).OnDrag();
        }
        
        public void DragEnd()
        {
            ((BaseCameraState)_stateMachine.CurrentState).OnDragEnd();
        }
        
        protected void OnDestroy()
        {
            transform.DOKill();
            _stateMachine.Clear();
        }
    }
}

