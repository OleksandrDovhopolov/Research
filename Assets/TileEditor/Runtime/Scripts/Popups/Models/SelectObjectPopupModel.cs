using System;
using System.Collections.Generic;

namespace TileEditor
{
    public class SelectObjectPopupModel : BasePopupModel
    {
        public List<LocationObject> availableObjects;
        public Action<LocationObject> onObjectSelectedAction;
        public Action onCancelSelectionAction;
    }
}