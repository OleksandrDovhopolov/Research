using CardCollection.Core;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public class CardCollectionImplInstallerBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            CardCollectionCompositionRegistry.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            CardCollectionCompositionRegistry.ResetForTests();
        }

        [Test]
        public void Installer_IsLifetimeScopeBasedAndNoLongerUsesStaticRuntimeHook()
        {
            Assert.AreEqual("LifetimeScope", typeof(CardCollectionImplInstaller).BaseType?.Name);

            var registerMethod = typeof(CardCollectionImplInstaller).GetMethod(
                "RegisterCompositionRoot",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNull(registerMethod, "Static runtime registration hook should be removed after VContainer migration.");
        }
    }
}
