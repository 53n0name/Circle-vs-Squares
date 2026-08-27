using Unity.Entities;
using UnityEngine;

// Данные для таймера (будет висеть на невидимой сущности-трекере)
public struct GroupingTimerComponent : IComponentData
{
    public float TimeRemaining;
    public Entity PotentialCommander;   // Тот, от кого строился круг
}