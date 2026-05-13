using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIShared.Tests.Editor
{
    public sealed class HudWidgetRegistryAssetTests
    {
        private HudWidgetRegistryAsset _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<HudWidgetRegistryAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_registry != null)
                Object.DestroyImmediate(_registry);
        }

        [Test]
        public void GetDefinition_ReturnsDefinition_WhenWidgetTypeMatches()
        {
            var definition = new HudWidgetDefinition(
                typeof(RegistryTestWidget).FullName,
                "TestAddress",
                HudLayer.World,
                false);
            _registry.SetDefinitionsForTests(new[] { definition });

            var result = _registry.GetDefinition<RegistryTestWidget>();

            Assert.That(result, Is.SameAs(definition));
        }

        [Test]
        public void GetDefinition_Throws_WhenDefinitionIsMissing()
        {
            _registry.SetDefinitionsForTests(Array.Empty<HudWidgetDefinition>());

            Assert.Throws<InvalidOperationException>(() => _registry.GetDefinition<RegistryTestWidget>());
        }

        [Test]
        public void GetDefinition_Throws_WhenDefinitionIsDuplicated()
        {
            var definition = new HudWidgetDefinition(
                typeof(RegistryTestWidget).FullName,
                "TestAddress",
                HudLayer.World,
                false);
            _registry.SetDefinitionsForTests(new[] { definition, definition });

            Assert.Throws<InvalidOperationException>(() => _registry.GetDefinition<RegistryTestWidget>());
        }

        public sealed class RegistryTestWidget : MonoBehaviour, IHudWidget
        {
        }
    }
}
