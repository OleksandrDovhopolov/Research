namespace TileEditor
{
    public class RemoveObjectCommand : BaseCommand
    {
        private readonly LocationObjectModel _objectModel;
        private readonly Location _currentLocation;

        private LocationObject _removedObject;

        public RemoveObjectCommand(Location currentLocation, LocationObject locationObject)
        {
            _currentLocation = currentLocation;
            _removedObject = locationObject;
            _objectModel = _removedObject.SaveObject();
        }

        public override string GetDescription() => $"Remove {_objectModel.objectId} from ({_objectModel.cellX}, {_objectModel.cellY})";

        protected override void DoApply()
        {
            _removedObject.DestroyObject();
            _removedObject = null;
        }

        protected override void DoRevert()
        {
            _removedObject = _currentLocation.AddObject(_objectModel).Result;
        }
    }
}