using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace Survival
{
    public struct Health : IComponentData
    {
        public float Value;
    }
}