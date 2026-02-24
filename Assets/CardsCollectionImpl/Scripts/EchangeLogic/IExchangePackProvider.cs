using System.Collections.Generic;
using UnityEngine;

namespace core
{
    public interface IExchangePackProvider
    {
        IReadOnlyCollection<ExchangePackEntry> GetAllPacks();
        Sprite GetPackSprite(string packId);
        int GetPackPrice(string packId);
    }
}
