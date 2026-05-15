using System.Collections.Generic;
using UnityEngine;

namespace TileEditor
{
    public partial class TileEditor
    {
        [SerializeField] private EditorTileCell _tileCellPrefab;
        [SerializeField] private Transform _tileCellContainer;

        private readonly List<EditorTileCell> _tileCells = new List<EditorTileCell>();

        private void GenerateGrid()
        {
            var gridCellSizeX = Settings.gridSizeX;
            var gridCellSizeY = Settings.gridSizeY;

            for (var x = -80; x < 80; x++)
            {
                for (var y = -80; y < 80; y++)
                {
                    var gridCell = Instantiate(_tileCellPrefab, _tileCellContainer);
                    gridCell.Init(x, y, gridCellSizeX, gridCellSizeY, CellLeftClickHandler, CellMiddleClickHandler, CellHoverHandler);
                    gridCell.transform.localPosition = new Vector3(gridCellSizeX * x, 0, gridCellSizeY * y);
                    _tileCells.Add(gridCell);
                }
            }
        }

        private EditorTileCell GetGridCell(int x, int y) => _tileCells.Find(cell => cell.X == x && cell.Y == y);

        public void SetGridCellRendererEnabled(bool isEnabled) => _tileCells.ForEach(c => c.SetGridRendererEnabled(isEnabled));
        public void SetGridCellCoordsEnabled(bool isEnabled) => _tileCells.ForEach(c => c.SetGridCoordsEnabled(isEnabled));
    }
}
