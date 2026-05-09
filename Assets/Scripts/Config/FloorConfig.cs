using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal struct FloorConfig : IComponentData
{
    public Entity FloorPrefab;
    public int MaxFloorTiles;
    public int MinFloorTileSize;
    public int MaxFloorTileSize;
    public int MinStairsAmount;
    public int MaxStairsAmount;
    public int StairMargin;
}
