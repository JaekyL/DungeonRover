using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

namespace Components
{
    internal struct NavAgent : IComponentData
    {
        public float3 StartPosition;
        public float TargetLocation;
        public NavMeshLocation NmlStartPosition;
        public NavMeshLocation NmlTargetPosition;
        public bool Routed;
    }
}