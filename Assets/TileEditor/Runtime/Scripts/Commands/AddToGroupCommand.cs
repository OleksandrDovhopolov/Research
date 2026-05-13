using System.Collections.Generic;
using System.Linq;

namespace Fabros.TileEditor
{
    public sealed class AddToGroupCommand : BaseCommand
    {
        private readonly List<LocationObject> _locationObjects;
        private readonly string _group;

        public AddToGroupCommand(IEnumerable<LocationObject> objects, string group)
        {
            _locationObjects = objects.ToList();
            _group = group;
        }

        public override string GetDescription() => $"Add {_locationObjects.Count} object(s) to group {_group}";

        protected override void DoApply()
        {
            _locationObjects.ForEach(lo => lo.AddToGroup(_group));
        }

        protected override void DoRevert()
        {
            _locationObjects.ForEach(lo => lo.RemoveFromGroup(_group));
        }
    }
}