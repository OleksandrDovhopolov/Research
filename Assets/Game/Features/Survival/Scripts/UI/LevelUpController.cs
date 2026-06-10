using UISystem;

namespace Survival
{
    [Window("LevelUpWindow")]
    public class LevelUpController : WindowController<LevelUpView>
    {
        private LevelUpArgs Args => (LevelUpArgs)Arguments;

        protected override void OnShowStart()
        {
            View.Bind(Args.Choices);
        }

        protected override void OnShowComplete()
        {
            View.OnCardClicked += HandleCardClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.OnCardClicked -= HandleCardClicked;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            View.DisableAll();
        }

        private void HandleCardClicked(int index)
        {
            if (index < 0 || index >= Args.Choices.Count) return;
            var chosen = Args.Choices[index];
            Args.OnSelected?.Invoke(chosen);
            UIManager.Hide<LevelUpController>();
        }
    }
}
