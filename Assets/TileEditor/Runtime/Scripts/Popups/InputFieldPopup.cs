using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class InputFieldPopup : BasePopup
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button button;
        [SerializeField] private Text buttonText;
        [SerializeField] private Text errorText;

        protected override void OnInit(BasePopupModel model)
        {
            var inputFieldModel = model as InputFieldPopupModel;

            if (inputFieldModel == null)
            {
                Debug.LogWarning("Bad input model!");
                ClosePopup();
                return;
            }

            inputField.SetTextWithoutNotify(inputFieldModel.defaultValue);
            inputField.contentType = inputFieldModel.contentType;

            if (inputFieldModel.inputTextAnchor.HasValue)
                inputField.textComponent.alignment = inputFieldModel.inputTextAnchor.Value;

            if (inputFieldModel.inputFieldWidth.HasValue)
            {
                var inputRect = inputField.GetComponent<RectTransform>();
                inputRect.sizeDelta = new Vector2(inputFieldModel.inputFieldWidth.Value, inputRect.sizeDelta.y);
            }

            buttonText.text = inputFieldModel.buttonText;
            errorText.text = inputFieldModel.errorMessage;
            errorText.gameObject.SetActive(false);

            button.onClick.AddListener(() =>
            {
                if (!inputFieldModel.validateInputFunc(inputField.text))
                {
                    errorText.gameObject.SetActive(true);
                    return;
                }

                inputFieldModel.sendAction.Invoke(inputField.text);
                ClosePopup();
            });
        }
    }
}