using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [BurstCompile]
    internal partial struct PlayerSpawnSystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
            builder.WithAll<SpawnPlayer, SpawnPosition>();
            _query = state.GetEntityQuery(builder);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Checking on Event Entity
            if(_query.ToComponentDataArray<SpawnPlayer>(Allocator.Temp).Length == 0) return;

            NativeArray<SpawnPosition> playerPosition = _query.ToComponentDataArray<SpawnPosition>(Allocator.Temp);
            
            //Spawning Player
            PlayerConfig config = SystemAPI.GetSingleton<PlayerConfig>();
            
            BeginSimulationEntityCommandBufferSystem.Singleton beginECBSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer spawnECB = beginECBSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            Entity playerEntity = spawnECB.Instantiate(config.PlayerPrefab);
            spawnECB.SetComponent(playerEntity, new LocalTransform(){Position = playerPosition[0].Value, Rotation = quaternion.identity, Scale = 1});
            
            //Destroying the Event Entity
            EndSimulationEntityCommandBufferSystem.Singleton endECBSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = endECBSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            NativeArray<Entity> eventEntities = _query.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in eventEntities)
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}