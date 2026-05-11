using Game.Bootstrap;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Module.TileEditor
{
    public class TileEditorBindings : MonoInstaller
    {
        [SerializeField] private EditorLocationContainer _editorLocationContainer;

        public override void InstallBindings(IContainerBuilder builder)
        {
            builder.Register<LocationSerializer>(Lifetime.Singleton);
            builder.RegisterInstance(_editorLocationContainer);
            builder.RegisterComponentInHierarchy<TileEditorInitiator>();
        }
    }
}