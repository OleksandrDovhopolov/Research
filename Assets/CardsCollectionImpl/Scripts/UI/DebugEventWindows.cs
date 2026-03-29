using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class DebugEventWindows : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Button _btn1;
        [SerializeField] private Button _btn2;

        private void Start()
        {
            _btn1.onClick.AddListener(OpenStartWindow);
            _btn2.onClick.AddListener(OpenEndWindow);
        }

        private void OpenStartWindow()
        {
            var args = new CollectionCompletedArgs("test", "Spring Collection");
            _uiManager.Show<CollectionCompletedController>(args);
        }

        private void OpenEndWindow()
        {
            var args = new CollectionStartedArgs("test", "Spring Collection");
            _uiManager.Show<CollectionStartedController>(args);
        }

        private void OnDestroy()
        {
            _btn1.onClick.RemoveAllListeners();
            _btn2.onClick.RemoveAllListeners();
        }
    }
}

