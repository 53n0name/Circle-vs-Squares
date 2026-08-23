using Unity.Entities;
using UnityEngine;

public class GroupConfigAuthoring : MonoBehaviour
{
    public int RequiredGroupSize = 5;
    public float FormationRadius = 10f;
    public float FormationTime = 5f;

    class Baker : Baker<GroupConfigAuthoring>
    {
        public override void Bake(GroupConfigAuthoring authoring)
        {
            // TransformUsageFlags.None означает, что этой сущности не нужен компонент LocalTransform, 
            // так как это просто хранилище настроек, у которого нет позиции в мире.

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GroupFormationConfigComponent
            {
                RequiredGroupSize = authoring.RequiredGroupSize,
                FormationRadius = authoring.FormationRadius,
                FormationTime = authoring.FormationTime
            });
        }
    }
}