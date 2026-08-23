using Unity.Entities;
using UnityEngine;

// Элемент массива для хранения участников внутри трекера
public struct FormingGroupElement : IBufferElementData
{
    public Entity EnemyEntity;
}
