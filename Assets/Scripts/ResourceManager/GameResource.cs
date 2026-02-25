namespace core
{
    public class GameResource
    {
        public ResourceType Type;
        public int Amount;

        public GameResource(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }
}
