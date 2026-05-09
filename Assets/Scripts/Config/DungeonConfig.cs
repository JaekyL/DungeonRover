using System.Collections;
using System.Collections.Generic;
using Config;
using Helper;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

internal struct DungeonConfig : IComponentData
{
    public Entity TilePrefab;
    public float StartingPointFreeSpaceDistance;
    public NativeArray<BlobAssetReference<TileStats>> TileStats;
}
