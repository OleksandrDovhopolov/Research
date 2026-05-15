using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class InputFieldPopupModel : BasePopupModel
    {
        public string defaultValue;
        public string errorMessage;
        public Action<string> sendAction;
        public Func<string, bool> validateInputFunc;
        public InputField.ContentType contentType;
        public string buttonText;
        public float? inputFieldWidth;
        public TextAnchor? inputTextAnchor;
    }
}