using Unity.Entities;
using UnityEngine;

public struct GroupingConfig : IComponentData
{
    public float MaxDistance;
    public float TimeToJoin;
}