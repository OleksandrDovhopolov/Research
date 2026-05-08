using UnityEngine;

namespace UIShared
{
    public interface ILocationZoneInfoHudBootstrap
    {
        //TODO MonoBehaviour should be MainLocationBootstrap. fix it 
        void Initialize(MonoBehaviour locationBootstrap);
    }
}
