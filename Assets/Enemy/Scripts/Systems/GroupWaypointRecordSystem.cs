using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct GroupWaypointRecordSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        // 1. Получаем конфиг. Если его нет на сцене, система просто пропустит кадр
        if (!SystemAPI.TryGetSingleton<GroupFormationConfigComponent>(out var groupConfig))
            return;

        // Получаем доступ к буферам крошек с правом на запись
        var bufferLookup = SystemAPI.GetBufferLookup<WaypointElement>(false);

        // Ищем только командиров
        foreach (var (transform, memberInfo) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<GroupMemberComponent>>()
                     .WithAll<EnemyMeleeTag, CommanderTag>())
        {
            Entity groupEntity = memberInfo.ValueRO.GroupEntity;

            // Если у группы почему-то нет буфера — пропускаем
            if (!bufferLookup.HasBuffer(groupEntity)) continue;

            var waypoints = bufferLookup[groupEntity];

            // Если буфер пустой (только создали группу), сразу кладем первую точку
            if (waypoints.Length == 0)
            {
                waypoints.Add(new WaypointElement { Position = transform.ValueRO.Position });
                continue;
            }

            // Берем самую свежую крошку (она всегда под нулевым индексом)
            float3 newestCrumb = waypoints[0].Position;
            float distance = math.distance(transform.ValueRO.Position.xy, newestCrumb.xy);

            // Если командир отошел на 0.3 юнита, оставляем новую крошку
            if (distance >= groupConfig.PointsDistance)
            {
                // Ограничиваем длину змейки, чтобы не съесть всю оперативную память.
                // 50 точек хватит на длинную змейку. Старые точки удаляем.
                if (waypoints.Length >= 50)
                {
                    waypoints.RemoveAt(waypoints.Length - 1);
                }

                // Вставляем новую позицию командира в самое начало списка
                waypoints.Insert(0, new WaypointElement { Position = transform.ValueRO.Position });
            }

        }
    }

}