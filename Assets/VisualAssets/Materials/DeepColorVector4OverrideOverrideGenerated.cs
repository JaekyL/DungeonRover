using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace VisualAssets.Materials
{
    [MaterialProperty("_DeepColor")]
    struct DeepColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
