using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
internal partial struct TileGravitationJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;
    public float Depth;

    void Execute([ChunkIndexInQuery] int chunkIndex, TileAspect tile)
    {
        float3 gravity = new float3(0, -9.82f, 0);

        if(tile.Position.y <= -Depth) return;

        tile.Position += gravity * DeltaTime;
    }
}

[BurstCompile]
internal partial struct TileGravitationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        return;
        
        EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        TileGravitationJob tileJob = new TileGravitationJob()
        {
            ECB = ecb.AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            
            
        };

        tileJob.ScheduleParallel();
    }
}
