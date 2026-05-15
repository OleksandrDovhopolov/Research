using System;
using UnityEngine;

namespace TileEditor
{
    public class TopInfoPopup : BasePopup
    {
        private Action _onCloseAction;

        protected override void OnInit(BasePopupModel model)
        {
            var topInfoPopupModel = model as TopInfoPopupModel;
            if (topInfoPopupModel == null)
            {
                Debug.LogWarning("Bad input model!");
                ClosePopup();
                return;
            }

            _onCloseAction = topInfoPopupModel.onCloseAction;
        }

        protected override void OnClose()
        {
            base.OnClose();
            _onCloseAction?.Invoke();
        }
    }
}