namespace EventOrchestration.Models
{
    public sealed class CardCollectionEventModel : BaseGameEventModel
    {
        public string RewardsConfigAddress { get; set; }
        public string CardPacksFileName { get; set; }
        
        public string EventConfigAddress { get; set; }
    }
}
