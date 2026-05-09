using System;
using Helper;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Aspects
{
    internal readonly partial struct FloorConfigAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<RandomNumbers> _random;
        private readonly RefRO<FloorConfig> _floorConfig;

        public NativeArray<int> GetRandomFloorTiles()
        {
            NativeArray<int> tiles = new NativeArray<int>(_floorConfig.ValueRO.MaxFloorTiles, Allocator.Temp);

            for (int i = 0; i < _floorConfig.ValueRO.MaxFloorTiles; i++)
            {
                tiles[i] = _random.ValueRW.Value.NextInt(_floorConfig.ValueRO.MinFloorTileSize, _floorConfig.ValueRO.MaxFloorTileSize) * 2;
            }
            
            return tiles;
        }

        public NativeArray<Vector3> GenerateFloorGrid(NativeArray<int> floorTiles, out NativeHashMap<int2, Vector3> grid)
        {
            NativeArray<Vector3> tilePositions = new NativeArray<Vector3>(floorTiles.Length, Allocator.Temp);
            NativeArray<Bounds> bounds = new NativeArray<Bounds>(floorTiles.Length, Allocator.Temp);
            float3 checkVector = Vector3.one;
            Bounds checkBounds;

            int safetyCounter = 0;
            int walkBack = 0;
            int walkBackCounter = 0;
            int walkBackCounterMax = 5;
            int gridSize = 0;

            foreach (int tile in floorTiles)
            {
                gridSize += tile * tile;
            }
            
            grid = new NativeHashMap<int2, Vector3>(gridSize, Allocator.Persistent);
            NativeHashMap<int2, float3> checkList = new NativeHashMap<int2, float3>(floorTiles[0] * floorTiles[0], Allocator.Temp);;
            
            tilePositions[0] = Vector3.zero;
            bounds[0] = new Bounds(Vector3.zero, new Vector3(floorTiles[0], 1, floorTiles[0]));
            
            for (int x = 0; x < floorTiles[0]; x++)
            {
                for (int z = 0; z < floorTiles[0]; z++)
                {
                    float3 pos = new float3(-floorTiles[0] / 2f + 0.5f + x, 0, -floorTiles[0] / 2f + 0.5f + z);
                    checkList.Add(new int2((int)(pos.x * 10), (int)(pos.z * 10)) , pos);
                }
            }
            
            foreach (KVPair<int2, float3> index in checkList)
            {
                grid.Add(index.Key, index.Value);
            }
            
            for (int i = 1; i < floorTiles.Length; i++)
            {
                bool freeSpace = false;
                
                do
                {
                    int previousIndex = i - 1 - walkBack;
                    if(i > 1) previousIndex = _random.ValueRW.Value.NextInt(0, i - walkBack);
                    int currentIndex = i;
                    float distance = (floorTiles[previousIndex] + floorTiles[currentIndex]) / 2f;
                    float halfSize = floorTiles[currentIndex] / 2f;

                    checkList = new NativeHashMap<int2, float3>(floorTiles[currentIndex] * floorTiles[currentIndex], Allocator.Temp);
                    
                    int rngDirection = _random.ValueRW.Value.NextInt(0, 4);
                    
                    switch (rngDirection)
                    {
                        case 0: checkVector = tilePositions[previousIndex] + new Vector3(distance, 0, 0); break;
                        case 1: checkVector = tilePositions[previousIndex] - new Vector3(distance, 0, 0); break;
                        case 2: checkVector = tilePositions[previousIndex] + new Vector3(0, 0, distance); break;
                        case 3: checkVector = tilePositions[previousIndex] - new Vector3(0, 0, distance); break;
                    }
                    
                    checkList.Clear();

                    checkBounds = new Bounds(checkVector, new Vector3(floorTiles[currentIndex] - 1, 1, floorTiles[currentIndex] - 1)); //0.5f because otherwise boundEdges would be on top of each other
                    
                    for (int x = 0; x < floorTiles[currentIndex]; x++)
                    {
                        for (int z = 0; z < floorTiles[currentIndex]; z++)
                        {
                            float3 pos = new float3(checkVector.x - halfSize + 0.5f + x, 0, checkVector.z - halfSize + 0.5f + z);
                            checkList.Add(new int2((int)(pos.x * 10), (int)(pos.z * 10)) , pos);
                        }
                    }

                    freeSpace = true;
                    
                    foreach (Bounds bound in bounds)
                    {
                        if (bound.Intersects(checkBounds))
                        {
                            freeSpace = false;
                            walkBackCounter++;
                            
                            if(walkBackCounter >= walkBackCounterMax)
                            {
                                walkBackCounter = 0;
                                walkBack = math.clamp(walkBack+1, 0, i-1);
                            }
                            break;
                        }
                    }

                    safetyCounter++;
                    
                    if(safetyCounter >= 100) Debug.Log("Had To safetyBreak with walkBack at " + walkBack);

                } while (!freeSpace && safetyCounter < 100);

                safetyCounter = 0;
                walkBackCounter = 0;
                walkBack = 0;
                bounds[i] = checkBounds;
                
                foreach (KVPair<int2, float3> index in checkList)
                {
                    grid.Add(index.Key, index.Value);
                    //Debug.Log(index.Key + " | " + index.Value);
                }
                
                tilePositions[i] = checkVector;
            }
                        
            return tilePositions;
        }
    }
}