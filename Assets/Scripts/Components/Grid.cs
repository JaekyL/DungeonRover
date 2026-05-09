using System.Numerics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    internal struct Grid : IComponentData
    {
        public NativeHashMap<int2, GridCell> CellMap;
    }
}