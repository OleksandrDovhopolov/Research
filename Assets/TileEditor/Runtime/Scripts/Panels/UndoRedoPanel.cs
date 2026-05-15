using UnityEngine;

namespace TileEditor
{
    public class UndoRedoPanel : MonoBehaviour
    {
        [SerializeField] private SimpleButton _undoButton;
        [SerializeField] private SimpleButton _redoButton;

        private TileEditor _tileEditor;

        void Start()
        {
            _tileEditor = GetComponentInParent<TileEditor>();

            _tileEditor.OnLocationChanged += UpdatePanelState;
            _tileEditor.OnCommandApplied += UpdateButtonsState;
            _tileEditor.OnCommandReverted += UpdateButtonsState;

            _undoButton.Init("Undo <color=red>(Z)</color>", _tileEditor.TryUndo);
            _redoButton.Init("Redo <color=red>(Y)</color>", _tileEditor.TryRedo);

            UpdatePanelState();
        }

        private void UpdatePanelState()
        {
            if (_tileEditor.CurrentLocation == null) gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
                _undoButton.SetInteractable(false);
                _redoButton.SetInteractable(false);
            }
        }

        private void UpdateButtonsState()
        {
            var lastCommand = _tileEditor.GetLastCommandDescription();
            if (lastCommand == null)
            {
                _undoButton.SetHintText("Nothing to undo");
                _undoButton.SetInteractable(false);
            }
            else
            {
                _undoButton.SetHintText("UNDO: " + lastCommand);
                _undoButton.SetInteractable(true);

            }

            var lastUndoCommand = _tileEditor.GetLastUndoCommandDescription();
            if (lastUndoCommand == null)
            {
                _redoButton.SetHintText("Nothing to redo");
                _redoButton.SetInteractable(false);
            }
            else
            {
                _redoButton.SetHintText("REDO: " + lastUndoCommand);
                _redoButton.SetInteractable(true);

            }
        }
    }
}