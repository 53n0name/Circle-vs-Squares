using Unity.Entities;
using UnityEngine;

public struct RandomGenerator : IComponentData
{
    public Unity.Mathematics.Random Value;
}