using System;
using UnityEngine;

namespace Fabros.TileEditor
{
    public class SelectLocationButtons : MonoBehaviour
    {
        [SerializeField] private SimpleButton _selectLocationButton;
        [SerializeField] private SimpleButton _deleteLocationButton;

        public string LocationName { get; private set; }

        public void Init(string locationName, Action<string> onSelect, Action<string> onDelete)
        {
            LocationName = locationName;

            _selectLocationButton.Init(locationName, () => onSelect.Invoke(locationName));
            _deleteLocationButton.Init(() => onDelete.Invoke(locationName));
        }
    }
}