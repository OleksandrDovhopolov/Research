using System.Collections.Generic;
using Fabros.TileEditor;
using UnityEngine;

namespace Module.TileEditor
{
    public class StubObjectGetter : MonoBehaviour, ILocationObjectsGetter
    {
        public List<LocationObject> GetAllLocationObjects()
        {
            return new List<LocationObject>();
        }
    }
}