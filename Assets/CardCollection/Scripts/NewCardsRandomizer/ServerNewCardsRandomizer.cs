using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public class ServerNewCardsRandomizer : INewCardsRandomizer
    {
        public async UniTask<List<string>> GetRandomNewCardsAsync()
        {
            // TODO: Implement server-based card retrieval
            await UniTask.CompletedTask;
            
            return new List<string>();
        }
    }
}
