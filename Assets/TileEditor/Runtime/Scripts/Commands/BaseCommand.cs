using UnityEngine;

namespace TileEditor
{
    public abstract class BaseCommand
    {
        public bool IsApplied { get; private set; }

        public void Apply()
        {
            if (IsApplied)
            {
                Debug.LogWarning("Warning! Tried to re-apply command " + GetDescription());
                return;
            }

            IsApplied = true;
            DoApply();
        }

        public void Revert()
        {
            if (!IsApplied)
            {
                Debug.LogWarning("Warning! Tried to revert not applied command " + GetDescription());
                return;
            }

            IsApplied = false;
            DoRevert();
        }

        public abstract string GetDescription();

        protected abstract void DoApply();
        protected abstract void DoRevert();
    }
}