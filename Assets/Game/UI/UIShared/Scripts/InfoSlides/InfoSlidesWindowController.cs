using UISystem;
using UnityEngine;

namespace UIShared
{
    public class InfoSlidesPageArgs : WindowArgs
    {
        public readonly SlidesType SlidesType;
        
        public InfoSlidesPageArgs(SlidesType slidesType)
        {
            SlidesType = slidesType;
        }
    }
    
    [Window("InfoSlidesWindow")]
    public class InfoSlidesPageController : WindowController<InfoSlidesWindowView>
    {
        protected override void OnInit()
        {
            base.OnInit();
            //View.Init(_localizationManager);
        }

        public override void UpdateWindow()
        {
            var windowArg = (InfoSlidesPageArgs)Arguments;
            View.UpdateWindow(windowArg.SlidesType, CreatePrefab);
        }

        private GameObject CreatePrefab(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent);
        }
    }
}