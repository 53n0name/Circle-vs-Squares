using Unity.Entities;
using UnityEngine;

public class EnemyBowAuthoring : MonoBehaviour
{
    public float _MovementSpeed;
    public float _AttackCooldown;

    class Baker : Baker<EnemyBowAuthoring>
    {
        public override void Bake(EnemyBowAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyAttackCooldownComponent
            {
                AttackCooldown = authoring._AttackCooldown
            });

            AddComponent(entity, new EnemyMovementSpeedComponent
            {
                Speed = authoring._MovementSpeed
            });

            AddComponent<EnemyBowTag>(entity);
        }
    }
}