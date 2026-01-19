using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardPackProvider
    {
        UniTask<List<CardPackConfig>> GetCardPacksAsync();

        UniTask<CardPackConfig> GetCardPackByIdAsync(string packId);
    }
}