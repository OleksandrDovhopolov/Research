using System;
using System.Collections.Generic;

namespace Fabros.TileEditor
{
    public class SelectObjectPopupModel : BasePopupModel
    {
        public List<LocationObject> availableObjects;
        public Action<LocationObject> onObjectSelectedAction;
        public Action onCancelSelectionAction;
    }
}