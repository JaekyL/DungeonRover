using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Components
{
    [MaterialProperty("_DeepColor")]
    public struct TileColor : IComponentData
    {
        public float4 Value;
    }
}