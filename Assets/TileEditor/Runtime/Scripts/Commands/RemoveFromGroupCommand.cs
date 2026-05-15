using System.Collections.Generic;
using System.Linq;

namespace TileEditor
{
    public sealed class RemoveFromGroupCommand : BaseCommand
    {
        private readonly List<LocationObject> _locationObjects;
        private readonly string _group;

        public RemoveFromGroupCommand(IEnumerable<LocationObject> objects, string group)
        {
            _locationObjects = objects.ToList();
            _group = group;
        }

        public override string GetDescription() => $"Remove {_locationObjects.Count} object(s) from group {_group}";

        protected override void DoApply()
        {
            _locationObjects.ForEach(lo => lo.RemoveFromGroup(_group));
        }

        protected override void DoRevert()
        {
            _locationObjects.ForEach(lo => lo.AddToGroup(_group));
        }
    }
}