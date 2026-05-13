using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIShared.Tests.Editor
{
    public sealed class HudRootTests
    {
        [Test]
        public void GetLayerRoot_ReturnsConfiguredLayer()
        {
            var rootObject = new GameObject("HudRoot");
            var configuredLayer = new GameObject("ConfiguredScreenLayer").transform;
            configuredLayer.SetParent(rootObject.transform, false);
            var root = rootObject.AddComponent<HudRoot>();

            var serializedObject = new SerializedObject(root);
            serializedObject.FindProperty("_screenLayer").objectReferenceValue = configuredLayer;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var result = root.GetLayerRoot(HudLayer.Screen);

            Assert.That(result, Is.SameAs(configuredLayer));
            Object.DestroyImmediate(rootObject);
        }

        [Test]
        public void GetLayerRoot_CreatesFallbackLayer_WhenLayerIsMissing()
        {
            var rootObject = new GameObject("HudRoot", typeof(RectTransform));
            var root = rootObject.AddComponent<HudRoot>();

            var result = root.GetLayerRoot(HudLayer.World);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.name, Is.EqualTo("WorldHudLayer"));
            Assert.That(result.parent, Is.SameAs(root.transform));
            Assert.That(root.GetLayerRoot(HudLayer.World), Is.SameAs(result));
            Object.DestroyImmediate(rootObject);
        }
    }
}
