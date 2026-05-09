using System.Collections;
using System.Collections.Generic;
using Config;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

readonly partial struct TileAspect : IAspect
{
    public readonly Entity Self;

    private readonly RefRW<TransformAuthoring> _transform;

    private readonly RefRW<Tile> _tile;

    public float3 Position
    {
        get => _transform.ValueRW.LocalPosition;
        set => _transform.ValueRW.LocalPosition = new float3(value.x, value.y, value.z);
    }
}
