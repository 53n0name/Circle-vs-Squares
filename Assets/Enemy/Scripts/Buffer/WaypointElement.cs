using Unity.Entities;
using Unity.Mathematics;

public struct WaypointElement : IBufferElementData
{
    public float3 Position; // позиция точки, на которой был командир
}