using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
internal partial struct InitialTilePositionJob : IJobEntity
{
    public int DungeonSize;
    private int _rowCounter;
    private int _columnCounter;
    
    void Execute([ChunkIndexInQuery] int chunkIndex, TileAspect tile)
    {
        
    }
}
