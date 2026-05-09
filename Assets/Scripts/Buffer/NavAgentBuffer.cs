using Unity.Entities;
using Unity.Mathematics;

namespace Buffer
{
    public struct NavAgentBuffer : IBufferElementData
    {
        public float3 Waypoints;
    }
}