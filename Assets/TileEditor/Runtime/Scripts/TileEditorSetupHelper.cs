using UnityEngine;

namespace TileEditor
{
    public class TileEditorSetupHelper : MonoBehaviour
    {
        [SerializeField] private TileEditor _tileEditor;
        [SerializeField] private MonoBehaviour _locationsSerializer;
        [SerializeField] private MonoBehaviour _locationObjectsFactory;
        [SerializeField] private MonoBehaviour _locationObjectsGetter;
        [SerializeField] private TileEditorSettings _tileEditorSettings;

        void Start()
        {
            if (_tileEditor == null)
            {
                Debug.LogWarning("TileEditorSetupHelper: TileEditor reference required!");
                return;
            }

            if (!(_locationsSerializer is ILocationsSerializer locationsSerializer))
            {
                Debug.LogWarning("TileEditorSetupHelper: ILocationsSerializer reference required!");
                return;
            }

            if (!(_locationObjectsFactory is ILocationObjectsFactory locationObjectsFactory))
            {
                Debug.LogWarning("TileEditorSetupHelper: ILocationObjectsFactory reference required!");
                return;
            }

            if (!(_locationObjectsGetter is ILocationObjectsGetter locationObjectsGetter))
            {
                Debug.LogWarning("TileEditorSetupHelper: ILocationObjectsGetter reference required!");
                return;
            }

            if (_tileEditorSettings == null)
            {
                Debug.LogWarning("TileEditorSetupHelper: TileEditorSettings reference required!");
                return;
            }

            _tileEditor.SetupEditor(locationsSerializer, locationObjectsFactory, locationObjectsGetter, _tileEditorSettings);
        }
    }
}