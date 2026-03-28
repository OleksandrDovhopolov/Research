namespace EventOrchestration.Models
{
    public sealed class CardCollectionEventModel : BaseGameEventModel
    {
        public string CollectionId { get; set; }
        public string RewardsConfigAddress { get; set; }
        public string CardCollectionFileName { get; set; }
        public string GroupsFileName { get; set; }
        public string CardPacksFileName { get; set; }
    }
}
