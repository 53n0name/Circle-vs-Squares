using Unity.Entities;


public struct GroupFormationConfigComponent : IComponentData
{
    public int RequiredGroupSize;   // Заданное количество врагов для группы
    public float FormationRadius;
    public float FormationTime;     // Заданное время для создания 
}