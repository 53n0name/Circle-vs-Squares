using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile]
public partial struct GroupMeleeSnakeFollowSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        // Нам нужно только чтение буфера
        var bufferLookup = SystemAPI.GetBufferLookup<WaypointElement>(true);

        foreach (var (velocity, transform, speed, memberInfo) in SystemAPI.Query<RefRW<PhysicsVelocity>, 
            RefRW<LocalTransform>, 
            RefRO<EnemyMovementSpeedComponent>, 
            RefRO<GroupMemberComponent>>()
                .WithAll<EnemyMeleeTag, InGroupTag>()
                .WithNone<CommanderTag>())
        {
            Entity groupEntity = memberInfo.ValueRO.GroupEntity;

            if (!bufferLookup.HasBuffer(groupEntity)) continue;

            var waypoints = bufferLookup[groupEntity];

            // Магия змейки: каждый следующий участник идет к более старой крошке.
            // Умножитель (например, 5) определяет дистанцию между юнитами.
            int delayMultiplier = 5;
            int targetIndex = (memberInfo.ValueRO.MemberIndex - 1) * delayMultiplier;

            // 2. ВАШЕ УЛУЧШЕНИЕ: Если точек для этого врага еще не накопилось
            if (waypoints.Length <= targetIndex)
            {
                velocity.ValueRW.Linear = float3.zero;
                continue;
            }

            float3 targetPos = waypoints[targetIndex].Position;

            float distance = math.distance(transform.ValueRO.Position.xy, targetPos.xy);

            float2 direction2D = math.normalize(targetPos.xy - transform.ValueRO.Position.xy);

            velocity.ValueRW.Linear = new float3(direction2D.x, direction2D.y, 0f) * speed.ValueRO.Speed;

            float angle = math.atan2(direction2D.y, direction2D.x);
            transform.ValueRW.Rotation = quaternion.RotateZ(angle);

        }
    }
}