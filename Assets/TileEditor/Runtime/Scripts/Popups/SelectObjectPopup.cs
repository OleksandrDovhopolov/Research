using UnityEngine;

namespace TileEditor
{
    public class SelectObjectPopup : BasePopup
    {
        [SerializeField] private Transform _objectViewsContainer;
        [SerializeField] private SelectObjectView _selectObjectViewPrefab;

        private SelectObjectPopupModel _selectObjectPopupModel;
        private bool _objectSelected;

        protected override void OnInit(BasePopupModel model)
        {
            var selectObjectModel = model as SelectObjectPopupModel;
            if (selectObjectModel == null)
            {
                Debug.LogWarning("Bad input model!");
                ClosePopup();
                return;
            }

            _selectObjectPopupModel = selectObjectModel;

            foreach (var availableObject in selectObjectModel.availableObjects)
            {
                var objectView = Instantiate(_selectObjectViewPrefab, _objectViewsContainer);
                objectView.Init(availableObject, OnObjectSelectedHandler);
            }
        }

        private void OnObjectSelectedHandler(LocationObject locationObject)
        {
            _selectObjectPopupModel.onObjectSelectedAction?.Invoke(locationObject);
            _objectSelected = true;
            ClosePopup();
        }

        protected override void OnClose()
        {
            base.OnClose();
            if (!_objectSelected) _selectObjectPopupModel.onCancelSelectionAction?.Invoke();
        }
    }
}