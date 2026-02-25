using System.Collections.Generic;

namespace core
{
    public class ResourceManager
    {
        private readonly Dictionary<ResourceType, int> _amountByType = new()
        {
            { ResourceType.Gold, 0 },
            { ResourceType.Energe, 0 },
            { ResourceType.Gems, 0 },
        };

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _amountByType[type] += amount;
        }

        public bool Remove(ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var currentAmount = _amountByType[type];
            if (currentAmount < amount)
            {
                return false;
            }

            _amountByType[type] = currentAmount - amount;
            return true;
        }

        public int Get(ResourceType type)
        {
            return _amountByType[type];
        }

        public int GetGold()
        {
            return Get(ResourceType.Gold);
        }

        public int GetEnergy()
        {
            return Get(ResourceType.Energe);
        }

        public int GetGems()
        {
            return Get(ResourceType.Gems);
        }
    }
}
