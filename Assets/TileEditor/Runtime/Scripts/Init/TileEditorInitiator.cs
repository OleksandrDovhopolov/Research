using Fabros.TileEditor;
using UnityEngine;
using VContainer;

namespace Module.TileEditor
{
    public class TileEditorInitiator : MonoBehaviour
    {
        private LocationSerializer _locationSerializer;

        [SerializeField] private Fabros.TileEditor.TileEditor _tileEditor;
        [SerializeField] private CampLocationObjectGetter _locationObjectsGetter;
        [SerializeField] private TileEditorSettings _tileEditorSettings;

        [Inject]
        public void Install(LocationSerializer locationSerializer)
        {
            _locationSerializer = locationSerializer;
        }

        private void Start()
        {
            if (_locationObjectsGetter is not ILocationObjectsGetter locationObjectsGetter)
            {
                Debug.LogError("TileEditorInitiator: ILocationObjectsGetter reference is required.");
                return;
            }

            _tileEditor.SetupEditor(
                _locationSerializer,
                new TileEditorObjectsFactory(),
                locationObjectsGetter,
                _tileEditorSettings);
        }
    }
}
