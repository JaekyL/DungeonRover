using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Authoring
{
    internal class FloorConfigAuthoring : MonoBehaviour
    {
        public GameObject floorPrefab;
        public uint randomSeed;
        public int maxFloorTiles;
        public int minFloorTileSize;
        public int maxFloorTileSize;
        public int minStairs;
        public int maxStairs;
        public int stairMargin;
    }
    
    internal class FloorBaker : Baker<FloorConfigAuthoring>
    {
        public override void Bake(FloorConfigAuthoring configAuthoring)
        {
            AddComponent(new FloorConfig()
            {
                FloorPrefab = GetEntity(configAuthoring.floorPrefab),
                MaxFloorTiles = configAuthoring.maxFloorTiles,
                MinFloorTileSize = configAuthoring.minFloorTileSize,
                MaxFloorTileSize = configAuthoring.maxFloorTileSize,
                MinStairsAmount = configAuthoring.minStairs,
                MaxStairsAmount = configAuthoring.maxStairs,
                StairMargin = configAuthoring.stairMargin,
            });
            AddComponent(new RandomNumbers()
            {
                Value = Random.CreateFromIndex(configAuthoring.randomSeed)
            });
        }
    }
}