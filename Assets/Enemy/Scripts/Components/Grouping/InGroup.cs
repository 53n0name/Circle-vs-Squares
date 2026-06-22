using Unity.Entities;
using UnityEngine;

public struct InGroup : IComponentData
{
    public Entity GroupEntity;
    public int IndexInGroup;            // 0 - лидер, 1+ - последователи
}