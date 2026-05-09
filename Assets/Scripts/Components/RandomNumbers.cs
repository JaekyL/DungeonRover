using System.Collections;
using System.Collections.Generic;
using Config;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

internal struct RandomNumbers : IComponentData
{
    //TileGeneration Values
    public bool HasMaxTileWeight;
    public int MaxTileWeight;
    public NativeArray<int> TileWeights;
    public bool HasTileStatsList;
    public NativeList<TileStats> TileStatsList;

    //FloorGeneration Values
    public bool SetMaxFloorWeight;
    public int MaxFloorWeight;
    public NativeArray<int> FloorWeights;
    
    public Random Value;
}
