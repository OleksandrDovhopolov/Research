using UISystem;
using UnityEngine;

namespace UIShared
{
    public class InfoSlidesPageArgs : WindowArgs
    {
        public readonly SlidesType SlidesType;
        public readonly UIManager UIManager;
        
        public InfoSlidesPageArgs(SlidesType slidesType,  UIManager uiManager)
        {
            SlidesType = slidesType;
            UIManager = uiManager;
        }
    }
    
    [Window("InfoSlidesWindow")]
    public class InfoSlidesPageController : WindowController<InfoSlidesWindowView>
    {
        private InfoSlidesPageArgs Args => (InfoSlidesPageArgs)Arguments;

        protected override void OnShowComplete()
        {
            View.CloseClick += OnCloseClickHandler;
        }

        private void OnCloseClickHandler()
        {
            Args.UIManager.Hide<InfoSlidesPageController>();
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= OnCloseClickHandler;
        }

        public override void UpdateWindow()
        {
            View.UpdateWindow(Args.SlidesType, CreatePrefab);
        }

        private GameObject CreatePrefab(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}