namespace BattlePass
{
    public interface IBattlePassXpPresentationTracker
    {
        bool TryGetBaseline(string seasonId, out int level, out int xp);
        void InitializeBaseline(string seasonId, int level, int xp);
        void CommitPresented(string seasonId, int level, int xp);
        void Reset(string seasonId = null);
    }
}
