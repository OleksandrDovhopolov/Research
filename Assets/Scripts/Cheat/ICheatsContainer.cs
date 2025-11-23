using System;
using cheatModule;

namespace core
{
    public interface ICheatsContainer
    {
        void AddItem<T>(Action<T> initializer) where T : CheatItem;
    }
}