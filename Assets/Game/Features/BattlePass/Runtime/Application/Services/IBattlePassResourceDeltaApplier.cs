using System.Collections.Generic;

namespace BattlePass
{
    public interface IBattlePassResourceDeltaApplier
    {
        void Apply(IReadOnlyDictionary<string, int> resourceDeltas);
    }
}
