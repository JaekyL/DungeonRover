using Components;
using EventComponents;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using Grid = Components.Grid;

namespace Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct InitializeGameSystem : ISystem
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
            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            //NewFloor Event
            Entity entity = ecb.CreateEntity();
            ecb.AddComponent<NewFloor>(entity);
            
            //SpawnPlayer Event
            Entity playerEntity = ecb.CreateEntity();
            ecb.AddComponent<SpawnPlayer>(playerEntity);

            //Initialize GridEntity
            Entity gridEntity = ecb.CreateEntity();
            ecb.AddComponent<Grid>(gridEntity);

            state.Enabled = false;
        }
    }
}