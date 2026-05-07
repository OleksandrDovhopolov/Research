using System.Collections;
using System.Collections.Generic;
using Fabros.TileEditor;
using UnityEngine;
using VContainer;

namespace Module.TileEditor
{
    public class TileEditorInitiator : MonoBehaviour
    {
        private LocationSerializer _locationSerializer;

        [SerializeField] private Fabros.TileEditor.TileEditor _tileEditor;
        [SerializeField] private StubObjectGetter _locationObjectsGetter;
        [SerializeField] private TileEditorSettings _tileEditorSettings;

        [Inject]
        public void Install(LocationSerializer locationSerializer)
        {
            _locationSerializer = locationSerializer;
        }

        private void Start()
        {
            _tileEditor.SetupEditor(
                _locationSerializer,
                new TileEditorObjectsFactory(),
                _locationObjectsGetter,
                _tileEditorSettings);
        }
    }
}