using Unity.Entities;


public struct GroupFormationConfigComponent : IComponentData
{
    public int MinRequiredGroupSize;   // Заданное количество врагов для группы
    public int MaxRequiredGroupSize;
    public float FormationRadius;
    public float FormationTime;     // Заданное время для создания 
}