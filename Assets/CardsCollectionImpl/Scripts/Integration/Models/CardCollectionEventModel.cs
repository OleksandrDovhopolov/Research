namespace EventOrchestration.Models
{
    public sealed class CardCollectionEventModel : BaseGameEventModel
    {
        public string CollectionId { get; set; }
        public string RewardsConfigAddress { get; set; }
    }
}
