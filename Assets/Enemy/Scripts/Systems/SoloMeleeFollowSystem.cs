using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics; // Не забудьте подключить пространство имен физики

[BurstCompile]
public partial struct SoloMeleeFollowSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out Entity playerEntity)) return;

        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        // Запрашиваем PhysicsVelocity для изменения (RefRW) и LocalTransform (RefRW для поворота)
        foreach (var (velocity, transform, speed, attackRange) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRW<LocalTransform>, RefRO<EnemyMovementSpeedComponent>, RefRO<EnemyAttackRangeComponent>>()
                     .WithAll<EnemyMeleeTag, IndependentTag>())
        {
            float distance = math.distance(transform.ValueRO.Position.xy, playerPos.xy);
            float distanceToEdge = distance - attackRange.ValueRO.RangeAttack;

            float2 direction2D = math.normalize(playerPos.xy - transform.ValueRO.Position.xy);
            float3 direction = new float3(direction2D.x, direction2D.y, 0f);

            if (distanceToEdge > 0f)
            {
                // Задаем вектор линейной скорости. 
                // Умножать на deltaTime НЕ НУЖНО, физический движок делает это сам под капотом.
                velocity.ValueRW.Linear = direction * speed.ValueRO.Speed;
            }
            else
            {
                // Принудительно гасим скорость при достижении цели, чтобы избежать скольжения
                velocity.ValueRW.Linear = float3.zero;
            }

            // Вычисление поворота в 2D (оставляем через LocalTransform)
            float angle = math.atan2(direction.y, direction.x);
            transform.ValueRW.Rotation = quaternion.RotateZ(angle);
        }
    }
}