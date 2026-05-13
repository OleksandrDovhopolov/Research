using System;
using System.Collections.Generic;

namespace Fabros.TileEditor
{
    public class DropdownPopupModel : BasePopupModel
    {
        public List<string> contentList;
        public Action<string> sendAction;
        public string buttonText;
    }
}