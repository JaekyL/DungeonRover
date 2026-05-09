
#include "Assets/Scripts/Raymarching/ShaderGraph/Primitives.hlsl"
#include "Assets/Scripts/Raymarching/ShaderGraph/Math.hlsl"
#include "Assets/Scripts/Raymarching/ShaderGraph/Utils.hlsl"


float3 preparePosition(float3 p, float repeat, float time)
{
    float3 preparedPosition = p % repeat - p/abs(p) * repeat / 2; ///If only on octant is used (for performance)
          
    preparedPosition += float3(
                     0,
                     0, //tan(3.412 * time),
                     0);

    return preparedPosition;
}
					
float sphereDistance(float3 p, float radius, float repeat, float time)
{
        return min(
        length(preparePosition(p, repeat, time)) - radius,
        length(preparePosition(p + p, repeat, time)) - radius
        );							 
}

float3 adjustedStartPoint(float3 pos, float3 direction, float radius, float repeat, float time)
{
    float3 s = preparePosition(pos, repeat, time);

    float3 c = float3(	repeat / 2 * -(pos.x*1/abs(pos.x)),
                        repeat / 2 * -(pos.y*1/abs(pos.y)), 
                        repeat / 2 * -(pos.z*1/abs(pos.z)));

    float3 p = s - c;
    float rSquared = radius * radius;
    float p_d = dot(p, direction);

    // Flatten p into the plane passing through c perpendicular to the ray.
    // This gives the closest approach of the ray to the center.
    float3 a = p - p_d * direction;
    float aSquared = dot(a, a);

    // Calculate distance from plane where ray enters/exits the sphere.    
    float h = sqrt(rSquared - aSquared);

    // Calculate intersection point relative to sphere center.
    float3 i = a - h * direction;

    float3 intersection = c + i;

    return length(intersection);				
}

bool InsideSphere(float3 position, float radius, float repeat, float time)
{
    float3 pos = preparePosition(position, repeat, time);
    /*
    pos -= float3(	-pos.x*1/abs(pos.x) * _Repeat /2,
    -pos.y*1/abs(pos.y) * _Repeat /2,
    pos.z*1/abs(pos.z) * _Repeat /2);
    */
    float cLength = length(pos);
	
    return cLength * cLength <= radius * radius;
    //(x - center_x)^2 + (y - center_y)^2 <= radius^2
}

float HexTileDistanceFunction(float3 pos, float radius, float time)
{
    // combine even hex tiles and odd hex tiles
    float space = 0.1;
    float wave = 0.1;
    float3 objectScale = GetScale() * float3(100,1,100);
    float height = objectScale.y * 0.5 - wave;
    float3 scale = objectScale * 0.5;

    float pitch = radius * 2 + space;
    float3 offset = float3(pitch * 0.5, 0.0, pitch * 0.866);
    float3 loop = float3(offset.x * 2, 1.0, offset.z * 2);
	
    float3 p1 = pos;
    float3 p2 = pos + offset;

    // calculate indices
    float2 pi1 = floor(p1 / loop).xz;
    float2 pi2 = floor(p2 / loop).xz;
    pi1.y = pi1.y * 2 + 1;
    pi2.y = pi2.y * 2;

    p1 = Repeat(p1, loop);
    p2 = Repeat(p2, loop);

    // draw hexagonal prisms with random heights
    float dy1 = wave * sin(10 * Rand(pi1) + 5 * 3.412 * time);
    float dy2 = wave * sin(10 * Rand(pi2) + 5 * 3.412 * time);
    float d1 = HexagonalPrismY(float3(p1.x, pos.y + dy1, p1.z), float2(radius, height));
    float d2 = HexagonalPrismY(float3(p2.x, pos.y + dy2, p2.z), float2(radius, height));

    // maximum indices
    loop.z *= 0.5;
    float2 mpi1 = floor((scale.xz + float2(space * 0.5,    radius)) / loop.xz);
    float2 mpi2 = floor((scale.xz + float2(radius + space, radius)) / loop.xz);

    // remove partial hexagonal prisms
    if (pi1.x >= mpi1.x || pi1.x <  -mpi1.x) d1 = max(d1, space);
    if (pi1.y >= mpi1.y || pi1.y <= -mpi1.y) d1 = max(d1, space);
    float o1 = any(
        step(mpi1.x, pi1.x) +
        step(pi1.x + 1, -mpi1.x) +
        step(mpi1.y, abs(pi1.y)));
    d1 = o1 * max(d1, 0.1) + (1 - o1) * d1;

    if (!all(max(mpi2 - abs(pi2), 0.0))) d2 = max(d2, space);
    float o2 = any(step(mpi2, abs(pi2)));
    d2 = o2 * max(d2, 0.1) + (1 - o2) * d2;

    // combine
    return min(d1, d2);
}

float GetDist(float3 pos)
{
    float4 s = float4(0,1,6,1);

    float sphereDistance = length(pos-s.xyz) - s.w;
    float planeDistance = pos.y;

    return min(sphereDistance, planeDistance);    
}