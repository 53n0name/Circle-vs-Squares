using Unity.Entities;

public struct GroupStateComponent : IComponentData
{
    public Entity CommanderEntity;      // Прямая ссылка на сущность лидера
    public int TotalMembers;            // Сколько всего участников
}