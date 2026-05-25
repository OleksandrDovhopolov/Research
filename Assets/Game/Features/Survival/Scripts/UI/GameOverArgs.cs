using System;
using UIShared;
using UISystem;

namespace Survival
{
    public class GameOverArgs : WindowArgs
    {
        public readonly Action OnRestart;

        public GameOverArgs(Action onRestart)
        {
            OnRestart = onRestart;
        }
    }
}
