using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public interface IHUDService
    {
        IEventButton SpawnEventButton(string eventId);
        void RemoveEventButton(string eventId);
    }
}