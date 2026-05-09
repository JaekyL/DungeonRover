using System.Collections;
using System.Collections.Generic;
using Config;
using Helper;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal class DungeonConfigAuthoring : MonoBehaviour
{
    public GameObject tilePrefab;
    public float startingPointFreeSpaceDistance;
    [SerializeField] public TileConfig[] tileConfigs;
}


internal class DungeonConfigBaker : Baker<DungeonConfigAuthoring>
{
    public override void Bake(DungeonConfigAuthoring authoring)
    {
        //Generating TileStats for use in the ecs system
        NativeArray<BlobAssetReference<TileStats>> tileStats = new NativeArray<BlobAssetReference<TileStats>>(authoring.tileConfigs.Length, Allocator.Persistent);

        for (int i = 0; i < tileStats.Length; i++)
        {
            tileStats[i] = authoring.tileConfigs[i].ToBlobAssetReference();
        }
        
        AddComponent(new DungeonConfig
        {
            TilePrefab = GetEntity(authoring.tilePrefab),
            StartingPointFreeSpaceDistance = authoring.startingPointFreeSpaceDistance,
            TileStats = tileStats,
        });
    }
}