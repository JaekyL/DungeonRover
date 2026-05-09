using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_DeepColor")]
    struct DeepColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
