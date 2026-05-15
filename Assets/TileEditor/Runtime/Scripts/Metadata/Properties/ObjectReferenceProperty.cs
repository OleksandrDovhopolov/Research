using System;

namespace TileEditor
{
    public class ObjectReferenceProperty : GenericProperty<string>
    {
        private Predicate<LocationObject> _objectsFilter;
        public void SetReferencesFilter(Predicate<LocationObject> objectsFilter) => _objectsFilter = objectsFilter;
        public bool CanReferenceToObject(LocationObject lo) => _objectsFilter == null || _objectsFilter(lo);
    }
}