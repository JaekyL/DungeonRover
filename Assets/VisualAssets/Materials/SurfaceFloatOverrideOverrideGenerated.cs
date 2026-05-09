using Unity.Entities;
using Unity.Rendering;

namespace VisualAssets.Materials
{
    [MaterialProperty("_Surface")]
    struct SurfaceFloatOverride : IComponentData
    {
        public float Value;
    }
}
