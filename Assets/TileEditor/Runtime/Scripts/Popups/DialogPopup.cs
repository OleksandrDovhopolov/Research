using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class DialogPopup : BasePopup
    {
        [SerializeField] private Button button1;
        [SerializeField] private Text button1Text;
        [SerializeField] private Button button2;
        [SerializeField] private Text button2Text;
        [SerializeField] private Button button3;
        [SerializeField] private Text button3Text;

        protected override void OnInit(BasePopupModel model)
        {
            var dialogModel = model as DialogPopupModel;
            if (dialogModel == null)
            {
                Debug.LogWarning("Bad input model!");
                ClosePopup();
                return;
            }

            InitButton(dialogModel.button1, button1, button1Text);
            InitButton(dialogModel.button2, button2, button2Text);
            InitButton(dialogModel.button3, button3, button3Text);
        }

        private void InitButton(PopupButtonModel butonModel, Button button, Text buttonText)
        {
            if (butonModel == null) button.gameObject.SetActive(false);
            else
            {
                button.onClick.AddListener(() =>
                {
                    butonModel.buttonAction?.Invoke();
                    ClosePopup();
                });
                buttonText.text = butonModel.buttonText;
            }
        }
    }
}