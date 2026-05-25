using UISystem;

namespace Survival
{
    [Window("GameOverWindow")]
    public class GameOverController : WindowController<GameOverView>
    {
        private GameOverArgs Args => (GameOverArgs)Arguments;

        protected override void OnShowComplete()
        {
            View.OnRestartClicked += HandleRestartClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.OnRestartClicked -= HandleRestartClicked;
        }

        private void HandleRestartClicked()
        {
            Args.OnRestart?.Invoke();
            UIManager.Hide<GameOverController>();
        }
    }
}
