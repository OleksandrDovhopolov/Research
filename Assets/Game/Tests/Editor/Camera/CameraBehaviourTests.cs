using System.Collections.Generic;
using System.Reflection;
using CameraModule;
using InputSystem;
using Lean.Touch;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace Game.Tests.Editor.Camera
{
    public sealed class CameraBehaviourTests
    {
        private readonly List<Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _objectsToCleanup.Count - 1; i >= 0; i--)
            {
                if (_objectsToCleanup[i] != null)
                    Object.DestroyImmediate(_objectsToCleanup[i]);
            }

            _objectsToCleanup.Clear();
        }

        [Test]
        public void Configure_SetsDefaultOrthographicSizeTo40()
        {
            var context = CreateContext();

            InvokePrivateMethod(context.CameraBehaviour, "Configure");

            Assert.That(context.Camera.orthographicSize, Is.EqualTo(40f));
        }

        [Test]
        public void CameraSettings_DefaultRange_Includes40()
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            _objectsToCleanup.Add(settings);

            Assert.That(settings.ZoomMin * settings.ZoomMultiplier, Is.LessThanOrEqualTo(40f));
            Assert.That(settings.ZoomMax * settings.ZoomMultiplier, Is.GreaterThanOrEqualTo(40f));
        }

        [Test]
        public void DragStart_WithOneManualFinger_DoesNotEnterMovingState()
        {
            var context = CreateInitializedContext();
            ConfigureManualFingers(context.CameraBehaviour, CreateFinger(0));

            context.CameraBehaviour.DragStart();

            Assert.That(context.CameraBehaviour.IsMoving, Is.False);
            Assert.That(GetCurrentStateType(context.CameraBehaviour), Is.EqualTo(typeof(CameraIdleState)));
        }

        [Test]
        public void DragStart_WithTwoManualFingers_DoesNotEnterZoomState()
        {
            var context = CreateInitializedContext();
            ConfigureManualFingers(context.CameraBehaviour, CreateFinger(0), CreateFinger(1));

            context.CameraBehaviour.DragStart();

            Assert.That(context.CameraBehaviour.IsMoving, Is.False);
            Assert.That(GetCurrentStateType(context.CameraBehaviour), Is.EqualTo(typeof(CameraIdleState)));
        }

        private CameraTestContext CreateInitializedContext()
        {
            var context = CreateContext();
            var screenPointConverter = new ScreenPointConverter();
            InvokePrivateMethod(context.CameraBehaviour, "Construct", context.Resolver, context.Settings, screenPointConverter);
            return context;
        }

        private CameraTestContext CreateContext()
        {
            var go = new GameObject("CameraBehaviourTest");
            var unityCamera = go.AddComponent<UnityEngine.Camera>();
            unityCamera.orthographic = true;
            var cameraBehaviour = go.AddComponent<CameraBehaviour>();

            _objectsToCleanup.Add(go);

            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            settings.ZoomMin = 3.8f;
            settings.ZoomMax = 40f;
            settings.ZoomMultiplier = 1f;
            _objectsToCleanup.Add(settings);

            var builder = new ContainerBuilder();
            var resolver = builder.Build();

            SetInstanceField(cameraBehaviour, "camera", unityCamera);

            return new CameraTestContext(cameraBehaviour, unityCamera, settings, resolver);
        }

        private static void ConfigureManualFingers(CameraBehaviour cameraBehaviour, params LeanFinger[] fingers)
        {
            var fingerFilter = cameraBehaviour.FingerFilter;
            fingerFilter.Filter = LeanFingerFilter.FilterType.ManuallyAddedFingers;
            fingerFilter.RemoveAllFingers();

            for (var i = 0; i < fingers.Length; i++)
                fingerFilter.AddFinger(fingers[i]);
        }

        private static LeanFinger CreateFinger(int index)
        {
            return new LeanFinger
            {
                Index = index,
                Set = true,
                LastSet = true,
                Age = 1f,
                ScreenPosition = new Vector2(100 + index * 10, 100),
                LastScreenPosition = new Vector2(95 + index * 10, 100),
                StartScreenPosition = new Vector2(90 + index * 10, 100)
            };
        }

        private static System.Type GetCurrentStateType(CameraBehaviour cameraBehaviour)
        {
            var stateMachine = GetPrivateField<object>(cameraBehaviour, "_stateMachine");
            var currentStateProperty = stateMachine.GetType().GetProperty("CurrentState", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(currentStateProperty, Is.Not.Null);

            var currentState = currentStateProperty.GetValue(stateMachine);
            return currentState?.GetType();
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Instance field '{fieldName}' was not found.");
            return (T)field.GetValue(target);
        }

        private static void SetInstanceField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Instance field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Instance method '{methodName}' was not found.");
            return method.Invoke(target, args);
        }

        private sealed class CameraTestContext
        {
            public CameraTestContext(CameraBehaviour cameraBehaviour, UnityEngine.Camera camera, CameraSettings settings, IObjectResolver resolver)
            {
                CameraBehaviour = cameraBehaviour;
                Camera = camera;
                Settings = settings;
                Resolver = resolver;
            }

            public CameraBehaviour CameraBehaviour { get; }
            public UnityEngine.Camera Camera { get; }
            public CameraSettings Settings { get; }
            public IObjectResolver Resolver { get; }
        }
    }
}
