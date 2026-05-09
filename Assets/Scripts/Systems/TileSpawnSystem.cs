using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aspects;
using Components;
using Config;
using Helper;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Aspects;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Analytics;
using Random = Unity.Mathematics.Random;
using Grid = Components.Grid;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
internal partial struct TileSpawnSystem : ISystem
{
    private EntityQuery _query;
    private EntityQuery _playerQuery;
    private int _deepestFloor;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DungeonConfig>();
        
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<NewFloorTiles>();
        _query = state.GetEntityQuery(builder);

        builder.Reset();
        builder.WithAll<SpawnPlayer>();
        _playerQuery = state.GetEntityQuery(builder);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Checking on Event Entity
        if(_query.ToComponentDataArray<NewFloorTiles>(Allocator.Temp).Length == 0) return;
        
        DungeonConfig config = SystemAPI.GetSingleton<DungeonConfig>();
        Entity configEntity = SystemAPI.GetSingletonEntity<DungeonConfig>();
        DungeonConfigAspect dungeonConfigAspect = SystemAPI.GetAspect<DungeonConfigAspect>(configEntity);
        
        NativeArray<NewFloorTiles> newFloorTiles = _query.ToComponentDataArray<NewFloorTiles>(Allocator.Temp);
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        //Declaring startPosition to stop spawning for the starting area
        Vector3 startPosition = ArrayHelper.GetAveragePosition(newFloorTiles[0].Positions.GetValueArray(Allocator.Temp).ToArray());

        NativeArray<Entity> playerEntity = _playerQuery.ToEntityArray(Allocator.Temp);

        if (playerEntity.Length > 0)
        {
            ecb.AddComponent(playerEntity[0],new SpawnPosition
            {
                Value = startPosition
            });
        }
        
        //Need GridAspect to update grid accordingly
        Entity gridEntity = SystemAPI.GetSingletonEntity<Grid>();
        GridAspect gridAspect = SystemAPI.GetAspect<GridAspect>(gridEntity);

        NativeArray<int> tileTypes = new NativeArray<int>(Enum.GetValues(typeof(TileType)).Length, Allocator.Temp);

        //Creating "Breakable" DungeonParts
        foreach (NewFloorTiles newFloor in newFloorTiles)
        {
            foreach (KVPair<int2,Vector3> position in newFloor.Positions)
            {
                TileStats stats = dungeonConfigAspect.GetRandomTile();
                
                //Counting TileTypes for randomness Debugging
                tileTypes[(int) stats.Type]++;
                
                if (stats.Type == TileType.Empty || Vector3.Distance(startPosition, position.Value) < config.StartingPointFreeSpaceDistance)
                {
                    gridAspect.UpdateGrid(position.Key, true);
                    continue;
                }

                Entity entity = ecb.Instantiate(config.TilePrefab);
                ecb.SetComponent(entity, new LocalTransform(){Position = position.Value + Vector3.up * 0.55f, Rotation = quaternion.identity, Scale = 1f});
                
                ecb.AddComponent<TileColor>(entity);
                ecb.SetComponent(entity, new TileColor()
                {
                    Value = new float4(stats.Color.r, stats.Color.g, stats.Color.b, 1)
                });

                ecb.AddComponent(entity, new Health(){Value = stats.Health});
                
                ecb.AddComponent(entity, new Hardness(){Value = stats.Hardness});
            }
        }

        //Debugging tileTypes can be removed, when its satisfying
        for (int i = 0; i < tileTypes.Length; i++)
        {
            Debug.Log("Tile " + Enum.GetName(typeof(TileType), (TileType)i) + ": " + tileTypes[i]);
        }
        
        //Destroying the Event Entity
        EndSimulationEntityCommandBufferSystem.Singleton endECBSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer endECB = endECBSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        NativeArray<Entity> eventEntities = _query.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in eventEntities)
        {
            endECB.DestroyEntity(entity);
        }
    }
}
