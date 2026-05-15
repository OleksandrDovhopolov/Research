namespace TileEditor
{
    public sealed class AddObjectCommand : BaseCommand
    {
        private readonly LocationObjectModel _objectModel;
        private readonly Location _currentLocation;

        private LocationObject _createdObject;

        public AddObjectCommand(Location currentLocation, LocationObjectModel objectModel)
        {
            _currentLocation = currentLocation;
            _objectModel = objectModel;
        }

        public override string GetDescription() => $"Add {_objectModel.objectId} to ({_objectModel.cellX}, {_objectModel.cellY})";

        protected override void DoApply()
        {
            _createdObject = _currentLocation.AddObject(_objectModel).Result;
        }

        protected override void DoRevert()
        {
            _createdObject.DestroyObject();
            _createdObject = null;
        }
    }
}