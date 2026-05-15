using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TileEditor
{
    public sealed class MoveObjectsCommand : BaseCommand
    {
        private readonly List<LocationObject> _locationObjects;
        private readonly Vector2Int _direction;

        public MoveObjectsCommand(IEnumerable<LocationObject> objects, Vector2Int direction)
        {
            _locationObjects = objects.ToList();
            _direction = direction;
        }

        public override string GetDescription() => $"Move {_locationObjects.Count} object(s) in direction {_direction}";

        protected override void DoApply()
        {
            MoveObjectsInDirection(_direction);
        }

        protected override void DoRevert()
        {
            MoveObjectsInDirection(_direction * -1);
        }

        private void MoveObjectsInDirection(Vector2Int direction)
        {
            var objectsCells = new Dictionary<LocationObject, LocationCell>();

            // remove all objects from cells first - to avoid overlays
            _locationObjects.ForEach(io =>
            {
                objectsCells[io] = io.Cell;
                io.Cell.RemoveObject(io, false);
            });

            // add objects to new cells
            foreach (var keyValuePair in objectsCells)
                keyValuePair.Value.GetCellInDirection(direction).AddObject(keyValuePair.Key);
        }
    }
}