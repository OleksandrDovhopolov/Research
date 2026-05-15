using System;
using System.Collections.Generic;

namespace TileEditor
{
    public partial class TileEditor
    {
        public event Action OnCommandApplied;
        public event Action OnCommandReverted;

        private const int _UNDO_LIMIT = 20;

        private readonly LinkedList<BaseCommand> _lastCommands = new LinkedList<BaseCommand>();
        private readonly LinkedList<BaseCommand> _revertedCommands = new LinkedList<BaseCommand>();

        public void ExecuteCommand(BaseCommand command)
        {
            command.Apply();
            _revertedCommands.Clear();
            _lastCommands.AddLast(command);
            if (_lastCommands.Count > _UNDO_LIMIT) _lastCommands.RemoveFirst();

            OnCommandApplied?.Invoke();
        }

        public void TryUndo()
        {
            var lastCommand = _lastCommands.Last?.Value;
            if (lastCommand == null) return;

            _lastCommands.RemoveLast();
            _revertedCommands.AddLast(lastCommand);

            lastCommand.Revert();
            OnCommandReverted?.Invoke();
        }

        public void TryRedo()
        {
            var lastRevertedCommand = _revertedCommands.Last?.Value;
            if (lastRevertedCommand == null) return;

            _revertedCommands.RemoveLast();
            _lastCommands.AddLast(lastRevertedCommand);

            lastRevertedCommand.Apply();
            OnCommandApplied?.Invoke();
        }

        private void ClearCommandsLists()
        {
            _lastCommands.Clear();
            _revertedCommands.Clear();
        }

        public string GetLastCommandDescription()
        {
            return _lastCommands.Last?.Value.GetDescription();
        }

        public string GetLastUndoCommandDescription()
        {
            return _revertedCommands.Last?.Value.GetDescription();
        }
    }
}