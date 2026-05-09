#ifndef RAYMARCH_INCLUDED
#define RAYMARCH_INCLUDED

#include "Assets/Scripts/Raymarching/ShaderGraph/RaymarchDistances.hlsl"

void raymarch_sphere_float
(
    float3 rayOrigin,
    float3 rayDirection,
    float radius,
    float repeat,
    float4 color,
    float4 backgroundColor,
    int steps,
    float minDistance,
    float maxDistance,
    float time,
    out float4 result
    )
{           
    float distance = sphereDistance(rayOrigin, radius, repeat, time);
    float completeDistance = distance;
    
    for (int i = 0; i < steps; i++)
    {	        				
        if (distance < minDistance || completeDistance > maxDistance)
        {
            //result = min(completeDistance / i, i / (float)steps) + color; // differentShading
            result = i / (float)steps + color;
            return;
        }

        rayOrigin += distance * rayDirection;		
					
        distance = sphereDistance(rayOrigin, radius, repeat, time);
        completeDistance += distance;
    }

    result = backgroundColor;
}

void raymarch_hexTiles_float
(
    float3 rayOrigin,
    float3 rayDirection,
    float radius,
    float repeat,
    int steps,
    float minDistance,
    float maxDistance,
    float4 color,
    float4 backgroundColor,
    float time,
    out float4 result
    )
{
    float distance = HexTileDistanceFunction(rayOrigin, radius, time);
    float completeDistance = distance;
    result = color;

    for (int i = 0; i < steps; i++)
    {	        				
        if (distance < minDistance || completeDistance > maxDistance)
        {
            //result = min(completeDistance / i, i / (float)steps) + color; // differentShading
            result += lerp(i / (float)steps, color, backgroundColor);
            return;
        }

        rayOrigin += distance * rayDirection;		
					
        distance = HexTileDistanceFunction(rayOrigin, radius, time);
        completeDistance += distance;
    }

    result = backgroundColor;
}

void raymarch_test_float(
    float3 rayOrigin,
    float3 rayDirection,
    float radius,
    float repeat,
    int steps,
    float minDistance,
    float maxDistance,
    float4 color,
    out float4 result
    )
{
    float distance = 0.;
    result = float4(0,0,0,0);
    int lastStep = steps;
    
    for (int i = 0; i < steps; i++)
    {
        float3 p = rayOrigin + rayDirection * distance;

        float lastDistance = GetDist(p);
        distance += lastDistance;

        if (lastDistance < minDistance || distance > maxDistance)
        {
            lastStep = i;
            break;
        }        
    }

    result = distance / lastStep + color;
    result.a = 1;
}

#endif // RAYMARCH_INCLUDED