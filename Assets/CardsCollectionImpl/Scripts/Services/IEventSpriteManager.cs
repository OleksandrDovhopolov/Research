using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public interface IEventSpriteManager
    {
        UniTask<Sprite> BindSpriteAsync(string eventId, string address, Image image, CancellationToken ct);
        UniTask<Sprite> BindSpriteFromAtlasAsync(string eventId, string atlasAddress, string spriteName, Image image, CancellationToken ct);
        void ReleaseEvent(string eventId);
        void ReleaseAll();
    }
}
