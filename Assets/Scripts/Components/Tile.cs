using System.Collections;
using System.Collections.Generic;
using Config;
using Unity.Entities;
using UnityEngine;

internal struct Tile : IComponentData
{
    public BlobAssetReference<TileStats> TileStats;
}
