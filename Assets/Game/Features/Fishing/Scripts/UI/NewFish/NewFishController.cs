using System.Collections;
using System.Collections.Generic;
using UnityEngine;using UISystem;


namespace Game.Fishing
{
    public sealed class NewFishArgs : WindowArgs
    {
        public NewFishArgs(string fishId, bool isNew, float bestCaughtWeight)
        {
            IsNew = isNew;
            FishId = fishId;
            BestCaughtWeight = bestCaughtWeight;
        }

        public string FishId { get; }
        public float BestCaughtWeight { get; }
        public bool IsNew { get; }
    }
    
    [Window("NewFishWindow")]
    public class NewFishController : WindowController<NewFishView>
    {
        private NewFishArgs Args => (NewFishArgs)Arguments;

        protected override void OnShowStart()
        {
            if (Args == null)
            {
                Debug.LogError("NewFishController: Args is null");
                CloseWindow();
                return;
            }
            
            View.Render(Args.FishId, Args.BestCaughtWeight, Args.IsNew);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        private void CloseWindow()
        {
            UIManager.Hide<NewFishController>();
        }
    }
}
