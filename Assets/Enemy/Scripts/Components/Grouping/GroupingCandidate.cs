using Unity.Entities;
using UnityEngine;

public struct GroupingCandidate : IComponentData
{
    public Entity PotentialTarget;
    public float Timer;
}
