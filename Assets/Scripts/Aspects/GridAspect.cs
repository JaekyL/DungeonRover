using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Grid = Components.Grid;

namespace Aspects
{
    internal readonly partial struct GridAspect : IAspect
    {
        public readonly Entity Entity;

        private readonly RefRW<Grid> _grid;

        public void CreateGrid(NativeHashMap<int2,Vector3> grid)
        {
            _grid.ValueRW.CellMap = new NativeHashMap<int2, GridCell>(grid.Capacity, Allocator.Persistent);
            
            foreach (KVPair<int2,Vector3> cell in grid)
            {
                _grid.ValueRW.CellMap.Add(cell.Key, new GridCell(){Free = false, Index = cell.Key, Position = cell.Value});
            }
        }

        public void UpdateGrid(Vector3 position, bool free)
        {
            UpdateGrid(GetNearestGridPosition(position), free);
        }

        public void UpdateGrid(int2 index, bool free)
        {
            GridCell cell = _grid.ValueRW.CellMap[index];
            cell.Free = free;
        }
        
        public int2 GetNearestGridPosition(Vector3 pos)
        {
            pos = 5 * math.round(pos) / 5;
            return new int2((int) (pos.x * 10), (int) (pos.z * 10));
        }
    }
}