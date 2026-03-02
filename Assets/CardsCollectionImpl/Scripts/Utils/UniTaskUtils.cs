using System;
using Cysharp.Threading.Tasks;

namespace core
{
    public static class UniTaskUtils
    {
        public static async UniTaskVoid DelayCallbackAsync(Action callback, float delaySeconds)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
            callback?.Invoke();
        }
    }
}