using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TileEditor
{
    //--------------------------------------------------------------------------------------------------------------------------

    public class GroupsInspector : MonoBehaviour
    {
        [SerializeField] private SimpleButton _closeLocationButton;
        [SerializeField] private SimpleButton _addGroupButton;

        [SerializeField] private GroupView _groupViewPrefab;
        [SerializeField] private Transform _groupViewsContainer;

        private TileEditor _tileEditor;
        private readonly List<GroupView> _currentGroups = new List<GroupView>();

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();

            _closeLocationButton.Init(() => _tileEditor.DeselectCurrentTool());
            _addGroupButton.Init(HandleAddGroupButtonClick);
            UpdateGroups();
        }

        void OnEnable()
        {
            if (_tileEditor != null) UpdateGroups();
        }

        private void UpdateGroups()
        {
            _currentGroups.ForEach(gr =>
            {
                if (gr != null) Destroy(gr.gameObject);
            });

            _currentGroups.Clear();

            foreach (string group in _tileEditor.CurrentLocation.IterateGroups().OrderBy(gr => gr))
            {
                var groupView = Instantiate(_groupViewPrefab, _groupViewsContainer);
                groupView.Init(group, _tileEditor.CurrentLocation, 
                    () =>
                    {
                        _tileEditor.InspectObjectsList(_tileEditor.CurrentLocation.GetObjectsFromGroup(group).ToList());
                    });

                _currentGroups.Add(groupView);
            }
        }

        private void HandleAddGroupButtonClick()
        {
            var inputFieldModel = new InputFieldPopupModel();
            inputFieldModel.buttonText = "Add group";
            inputFieldModel.defaultValue = $"New Group {_currentGroups.Count + 1}";
            inputFieldModel.errorMessage = "Group name must be unique and non-empty!";
            inputFieldModel.validateInputFunc = groupName =>
                !string.IsNullOrEmpty(groupName) && !_tileEditor.CurrentLocation.IsGroupExists(groupName);
            inputFieldModel.sendAction = groupName =>
            {
                _tileEditor.CurrentLocation.AddGroup(groupName);
                UpdateGroups();
            };

            _tileEditor.OpenPopup(inputFieldModel);
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
}
