using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public sealed class CardCollectionEventModel : BaseGameEventModel
    {
        public string EventConfigAddress { get; set; }
    }
}
