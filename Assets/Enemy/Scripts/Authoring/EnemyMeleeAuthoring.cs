using Unity.Entities;
using UnityEngine;

public class EnemyMeleeAuthoring : MonoBehaviour
{
    public float _MovementSpeed;
    public float _RotationSpeed;
    public float _AttackPower;
    public float _AttackCooldown;
    public float _AttackRange;

    class Baker : Baker<EnemyMeleeAuthoring>
    {
        public override void Bake(EnemyMeleeAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(entity, new EnemyAttackCooldownComponent
            {
                AttackCooldown = authoring._AttackCooldown,
                CurrentAttackCooldown = authoring._AttackCooldown
            });

            AddComponent(entity, new EnemyAttackRangeComponent
            {
                RangeAttack = authoring._AttackRange
            });

            AddComponent(entity, new EnemyAttackPowerComponent
            {
                AttackPower = authoring._AttackPower
            });

            AddComponent(entity, new EnemyMovementSpeedComponent
            {
                Speed = authoring._MovementSpeed
            });

            AddComponent(entity, new EnemyRotationSpeedComponent
            {
                RotationSpeed = authoring._RotationSpeed
            });

            AddComponent<EnemyMeleeTag>(entity);
        }
    }
}