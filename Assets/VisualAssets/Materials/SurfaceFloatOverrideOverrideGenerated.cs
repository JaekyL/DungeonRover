using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_Surface")]
    struct SurfaceFloatOverride : IComponentData
    {
        public float Value;
    }
}
