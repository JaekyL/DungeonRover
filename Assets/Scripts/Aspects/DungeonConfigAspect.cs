using System;
using Config;
using Helper;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Aspects
{
    internal readonly partial struct DungeonConfigAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<RandomNumbers> _random;
        private readonly RefRO<DungeonConfig> _dungeonConfig;
        
        public TileStats GetRandomTile()
        {
            InitializeRandomTileGeneration();

            int checkValue = _random.ValueRW.Value.NextInt(0, _random.ValueRO.MaxTileWeight);
            int index = -1;
            for (int i = 0; i < _random.ValueRO.TileWeights.Length; i++)
            {
                if (checkValue >= _random.ValueRO.TileWeights[i]) continue;
                
                index = i;
                break;
            }
            
            return _dungeonConfig.ValueRO.TileStats[index].Value;
        }

        private void InitializeRandomTileGeneration()
        {
            if (_random.ValueRO.HasMaxTileWeight) return;
            
            _random.ValueRW.MaxTileWeight = 0;
            _random.ValueRW.TileWeights = new NativeArray<int>(_dungeonConfig.ValueRO.TileStats.Length, Allocator.Persistent);

            for (int i = 0; i < _dungeonConfig.ValueRO.TileStats.Length; i++)
            {
                _random.ValueRW.MaxTileWeight += _dungeonConfig.ValueRO.TileStats[i].Value.Weight;
                _random.ValueRW.TileWeights[i] = _random.ValueRW.MaxTileWeight;
            }

            _random.ValueRW.TileStatsList = new NativeList<TileStats>(_random.ValueRO.MaxTileWeight, Allocator.Persistent);
            _random.ValueRW.HasMaxTileWeight = true;
        }

        public TileStats GetRandomTileControlled()
        {
            InitializeRandomTileGeneration();
            
            if (_random.ValueRO.TileStatsList.Length == 0)
            {
                for (int i = 0; i < _random.ValueRO.TileWeights.Length; i++)
                {
                    for (int j = 0; j < _random.ValueRO.TileWeights[i]; j++)
                    {
                        _random.ValueRW.TileStatsList.Add(_dungeonConfig.ValueRO.TileStats[i].Value);   
                    }
                }
            }

            int index = _random.ValueRW.Value.NextInt(0, _random.ValueRO.TileStatsList.Length);
            TileStats tile = _random.ValueRW.TileStatsList[index];
            _random.ValueRW.TileStatsList.RemoveAt(index);
            
            return tile;
        }
    }
}