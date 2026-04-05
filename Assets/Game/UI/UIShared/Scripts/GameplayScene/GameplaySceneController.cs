using UISystem;
using VContainer;

namespace UIShared
{
    [Window("GameplaySceneController")]
    public class GameplaySceneController : WindowController<GameplaySceneView>
    {
        [Inject]
        public void Install()
        {
        }

        protected override void OnShowStart()
        {
        }

        protected override void OnShowComplete()
        {
        }

        protected override void OnHideStart(bool isClosed)
        {
        }
    }
}