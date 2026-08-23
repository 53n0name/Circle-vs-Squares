using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile]
public partial struct GroupMeleeFollowSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out Entity playerEntity)) return;
        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        // Добавили RefRW<PhysicsVelocity>
        foreach (var (velocity, transform, speed, memberInfo) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRW<LocalTransform>, RefRO<EnemyMovementSpeedComponent>, RefRO<GroupMemberComponent>>()
                     .WithAll<EnemyMeleeTag, InGroupTag>()
                     .WithNone<CommanderTag>())
        {
            // Целевая позиция в строю (2D)
            float xOffset = (memberInfo.ValueRO.MemberIndex % 2 == 0 ? -1f : 1f) * (memberInfo.ValueRO.MemberIndex * 1.5f);
            float3 targetFormationPos = playerPos + new float3(xOffset, -2f, 0f);

            float distance = math.distance(transform.ValueRO.Position.xy, targetFormationPos.xy);

            if (distance > 0.5f) // Буфер для остановки
            {
                // Вычисляем направление к точке в строю
                float2 direction2D = math.normalize(targetFormationPos.xy - transform.ValueRO.Position.xy);
                float3 direction = new float3(direction2D.x, direction2D.y, 0f);

                // Задаем физическую скорость
                velocity.ValueRW.Linear = direction * speed.ValueRO.Speed;

                // Поворачиваемся по ходу движения к своему месту
                float angle = math.atan2(direction.y, direction.x);
                transform.ValueRW.Rotation = quaternion.RotateZ(angle);
            }
            else
            {
                // ЖЕСТКАЯ ОСТАНОВКА: гасим инерцию, чтобы они не толкались на месте
                velocity.ValueRW.Linear = float3.zero;

                // Необязательная, но приятная деталь: когда юнит стоит на своем месте в строю,
                // заставляем его смотреть на игрока, а не в ту сторону, откуда он прибежал.
                float2 lookDir2D = math.normalize(playerPos.xy - transform.ValueRO.Position.xy);
                float angle = math.atan2(lookDir2D.y, lookDir2D.x);
                transform.ValueRW.Rotation = quaternion.RotateZ(angle);
            }
        }
    }
}