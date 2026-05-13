using System.Collections.Generic;

namespace Fabros.TileEditor
{
    public interface ILocationsSerializer : ILocationsLoader
    {
        List<string> GetAllLocations();
        void SaveLocation(string locationName, string locationData);
        void DeleteLocation(string locationName);
    }
}