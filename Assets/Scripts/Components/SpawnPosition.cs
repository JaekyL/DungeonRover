using Unity.Entities;
using UnityEngine;

namespace Components
{
    internal struct SpawnPosition : IComponentData
    {
        public Vector3 Value;
    }
}