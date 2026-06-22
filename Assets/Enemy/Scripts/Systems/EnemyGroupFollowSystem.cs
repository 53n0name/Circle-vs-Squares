using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(EnemyGroupingSystem))]
public partial struct EnemyGroupFollowSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // ECB нужен для безопасного удаления компонентов, если лидер исчез
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        foreach (var (groupData, groupEntity) in SystemAPI.Query<RefRO<EnemyGroupData>>().WithEntityAccess())
        {
            if (!SystemAPI.Exists(groupData.ValueRO.Leader))
            {
                ecb.DestroyEntity(groupEntity);
                continue;
            }

            // Берем данные лидера
            var leaderTransform = SystemAPI.GetComponent<LocalTransform>(groupData.ValueRO.Leader);
            float3 leaderPos = leaderTransform.Position;

            float3 leaderForward = math.mul(leaderTransform.Rotation, new float3(0, 1, 0)); // Направление "вверх/вперед"
            
            float3 leaderBack = -leaderForward;

            // Управляем участниками
            foreach (var (transform, velocity, inGroup, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRO<InGroup>>()
                     .WithEntityAccess())
            {
                if (inGroup.ValueRO.GroupEntity != groupEntity || inGroup.ValueRO.IndexInGroup == 0) continue;

                // 1. Рассчитываем целевую точку строго за спиной
                float spacing = 2.0f;
                float3 targetPos = leaderPos + (leaderBack * inGroup.ValueRO.IndexInGroup * spacing);
                //Debug.Log(inGroup.ValueRO.IndexInGroup);                                                // <------------------  error

                // Принудительно держим Z на одном уровне (например, 0), чтобы не улетали "вглубь"
                targetPos.z = 0;

                Debug.DrawLine(transform.ValueRO.Position, targetPos, Color.green);
                float crossSize = 0.3f;
                Debug.DrawRay(leaderPos, new float3(crossSize, 0, 0), Color.red);
                Debug.DrawRay(leaderPos, new float3(-crossSize, 0, 0), Color.red);
                Debug.DrawRay(leaderPos, new float3(0, crossSize, 0), Color.red);
                Debug.DrawRay(leaderPos, new float3(0, -crossSize, 0), Color.red);
                Debug.Log(leaderPos);

                float3 currentPos = transform.ValueRO.Position;
                float dist = math.distance(currentPos, targetPos);

                if (dist > 0.3f)
                {
                    float3 dir = math.normalize(targetPos - currentPos);

                    // Плавное замедление (Spring-эффект)
                    float speedScale = math.clamp(dist * 2.0f, 0f, 6f);
                    velocity.ValueRW.Linear = dir * speedScale;

                    // Поворот последователя: пусть он тоже смотрит на свою цель или за лидером
                    // Это предотвратит вращение вокруг своей оси при движении
                    velocity.ValueRW.Angular = float3.zero;
                }
                else
                {
                    velocity.ValueRW.Linear = float3.zero;
                    velocity.ValueRW.Angular = float3.zero;
                }
            }
        }
        ecb.Playback(state.EntityManager);
    }
}