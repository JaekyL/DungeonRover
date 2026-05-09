using Unity.Entities;

namespace Components
{
    internal struct PlayerConfig : IComponentData
    {
        public Entity PlayerPrefab;
    }
}