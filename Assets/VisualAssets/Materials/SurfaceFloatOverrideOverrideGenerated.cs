using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_Surface")]
    struct SurfaceFloatOverride : IComponentData
    {
        public float Value;
    }
}
