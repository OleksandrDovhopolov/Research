using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIShared
{
    public interface IHudController
    {
        UniTask CreateInitialWidgetsAsync(CancellationToken cancellationToken);

        UniTask<TWidget> GetHudWidgetAsync<TWidget>(CancellationToken cancellationToken)
            where TWidget : Component, IHudWidget;

        UniTask<TItem> CreateHudItemAsync<TItem>(
            string addressableKey,
            Transform parent,
            CancellationToken cancellationToken)
            where TItem : Component;

        TWidget GetHudWidget<TWidget>()
            where TWidget : Component, IHudWidget;

        bool TryGetHudWidget<TWidget>(out TWidget widget)
            where TWidget : Component, IHudWidget;

        void ReleaseHudWidget<TWidget>()
            where TWidget : Component, IHudWidget;

        void ReleaseHudItem(Component item);
    }
}
