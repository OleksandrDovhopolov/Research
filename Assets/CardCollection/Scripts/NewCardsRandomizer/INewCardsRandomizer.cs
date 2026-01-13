using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface INewCardsRandomizer
    {
        UniTask<List<string>> GetRandomNewCardsAsync();
    }
}
