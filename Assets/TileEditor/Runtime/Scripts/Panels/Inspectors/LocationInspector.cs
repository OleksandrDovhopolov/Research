using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TileEditor
{
    //--------------------------------------------------------------------------------------------------------------------------

    public class LocationInspector : MonoBehaviour
    {
        [SerializeField] private SimpleButton _closeLocationButton;
        [SerializeField] private SimpleButton _updateLocationButton;
        [SerializeField] private ObjectInspectorMedium _objectInspectorPrefab;
        [SerializeField] private Transform _objectsInspectorsContainer;

        private TileEditor _tileEditor;
        private readonly List<ObjectInspectorMedium> _activeInspectors = new List<ObjectInspectorMedium>();

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();

            _closeLocationButton.Init(() => _tileEditor.DeselectCurrentTool());
            _updateLocationButton.Init(UpdateLocationData);
        }

        private void UpdateLocationData()
        {
            Clear();

            Vector3 cameraPosition = _tileEditor.GetCameraPosition();

            var locationObjects = _tileEditor
                .CurrentLocation
                .IterateObjects()
                .OrderBy(obj => Vector3.Distance(obj.transform.position, cameraPosition));

            foreach (var obj in locationObjects)
            {
                var objInspector = Instantiate(_objectInspectorPrefab, _objectsInspectorsContainer);
                objInspector.Init(
                    obj, 
                    () => _tileEditor.InspectObject(obj, true),
                    () => obj.Highlight(),
                    () => _tileEditor.MoveCamera(obj.transform.position),
                    () =>
                    {
                        _activeInspectors.Remove(objInspector);
                        Destroy(objInspector.gameObject);
                        obj.RemoveObject();
                    });

                _activeInspectors.Add(objInspector);
            }
        }

        public void Clear()
        {
            _activeInspectors.ForEach(insp => Destroy(insp.gameObject));
            _activeInspectors.Clear();
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
}
