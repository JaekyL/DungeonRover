
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace Components
{
    public struct NewFloorTiles : IComponentData
    {
        public NativeHashMap<int2, Vector3> Positions;
    }
}