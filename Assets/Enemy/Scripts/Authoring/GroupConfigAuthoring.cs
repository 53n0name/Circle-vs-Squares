using Unity.Entities;
using UnityEngine;

public class GroupConfigAuthoring : MonoBehaviour
{
    public int MinRequiredGroupSize = 4;
    public int MaxRequiredGroupSize = 5;
    public float FormationRadius = 1f;
    public float FormationTime = 5f;
    public float PointsDistance = 0.05f;

    class Baker : Baker<GroupConfigAuthoring>
    {
        public override void Bake(GroupConfigAuthoring authoring)
        {
            // TransformUsageFlags.None означает, что этой сущности не нужен компонент LocalTransform, 
            // так как это просто хранилище настроек, у которого нет позиции в мире.

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GroupFormationConfigComponent
            {
                MinRequiredGroupSize = authoring.MinRequiredGroupSize,
                MaxRequiredGroupSize = authoring.MaxRequiredGroupSize,
                FormationRadius = authoring.FormationRadius / 2,        // if not divide it's gonna search in diameter
                FormationTime = authoring.FormationTime,
                PointsDistance = authoring.PointsDistance
            });
        }
    }
}