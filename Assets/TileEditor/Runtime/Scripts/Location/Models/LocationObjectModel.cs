using System.Collections;
using System.Collections.Generic;

namespace TileEditor
{
    public sealed class LocationObjectModel
    {
        public string objectId;
        public string instanceId;
        public int cellX;
        public int cellY;
        public List<string> groups;
        public bool needPreload;
        public Hashtable metadata;
    }
}