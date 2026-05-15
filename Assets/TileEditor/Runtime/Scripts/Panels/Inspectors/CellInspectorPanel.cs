using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class CellInspectorPanel : MonoBehaviour
    {
        [SerializeField] private Text _cellCoordsText;
        [SerializeField] private Text _totalItemsText;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private ObjectInspectorLite objectInspectorLitePrefab;

        private readonly List<ObjectInspectorLite> _objectInspectors = new List<ObjectInspectorLite>();

        private TileEditor _tileEditor;

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();
        }

        public void Inspect(LocationCell cell)
        {
            foreach (var inspector in _objectInspectors) Destroy(inspector.gameObject);
            _objectInspectors.Clear();

            if (cell == null)
            {
                _cellCoordsText.text = "(select cell)";
                _totalItemsText.text = "";
                return;
            }

            _cellCoordsText.text = $"({cell.X}, {cell.Y})";
            _totalItemsText.text = $"Total items: {cell.GetObjectsCount()}";

            foreach (var locationObject in cell.IterateObjects())
            {
                var cellObjectInspector = Instantiate(objectInspectorLitePrefab, _itemsContainer);
                cellObjectInspector.Init(locationObject, 
                    () =>
                        {
                            cell.RemoveObject(locationObject);
                            _totalItemsText.text = $"Total items: {cell.GetObjectsCount()}";
                            _objectInspectors.Remove(cellObjectInspector);
                            Destroy(cellObjectInspector.gameObject);
                        }, 
                    () =>
                        {
                            _tileEditor.InspectObject(locationObject, true);
                        });
                _objectInspectors.Add(cellObjectInspector);
            }
        }
    }
}