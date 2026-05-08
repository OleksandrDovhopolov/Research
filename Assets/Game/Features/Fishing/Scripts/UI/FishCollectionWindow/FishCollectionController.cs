using UISystem;

namespace Game.Fishing
{
    [Window("FishCollectionWindow", WindowType.Popup)]
    public sealed class FishCollectionController : WindowController<FishCollectionView>
    {
        private FishCollectionArgs Args => (FishCollectionArgs)Arguments;

        protected override void OnShowStart()
        {
            View.Render(Args?.Entries);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.Dispose();
        }

        private void CloseWindow()
        {
            UIManager.Hide<FishCollectionController>();
        }
    }
}
