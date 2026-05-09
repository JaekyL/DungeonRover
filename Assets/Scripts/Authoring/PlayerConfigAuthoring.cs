using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    internal class PlayerConfigAuthoring : MonoBehaviour
    {
        public GameObject playerPrefab;
    }

    internal class PlayerConfigBaking : Baker<PlayerConfigAuthoring>
    {
        public override void Bake(PlayerConfigAuthoring configAuthoring)
        {
            AddComponent(new PlayerConfig
            {
                PlayerPrefab = GetEntity(configAuthoring.playerPrefab)
            });
        }
    }
}