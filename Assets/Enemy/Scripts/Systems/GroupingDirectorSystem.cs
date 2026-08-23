using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct GroupingDirectorSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GroupFormationConfigComponent>(out var config))
            return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // ВАЖНО: Исключаем тех, кто уже в процессе группировки (.WithNone<FormingGroupTag>())
        var query = SystemAPI.QueryBuilder()
            .WithAll<EnemyMeleeTag, IndependentTag, LocalTransform>()
            .WithNone<FormingGroupTag>()
            .Build();

        var entities = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var alreadyGrouped = new NativeArray<bool>(entities.Length, Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (alreadyGrouped[i]) continue;

            var potentialGroup = new NativeList<int>(Allocator.Temp);
            potentialGroup.Add(i);

            for (int j = i + 1; j < entities.Length; j++)
            {
                if (alreadyGrouped[j]) continue;

                // 2D математика
                float dist = math.distance(transforms[i].Position.xy, transforms[j].Position.xy);

                if (dist <= config.FormationRadius)
                {
                    potentialGroup.Add(j);
                }

                if (potentialGroup.Length >= config.RequiredGroupSize)
                    break;
            }

            // Если нашли нужное количество - ЗАПУСКАЕМ ПРОЦЕСС
            if (potentialGroup.Length >= config.RequiredGroupSize)
            {
                Entity trackerEntity = ecb.CreateEntity();

                // Создаем динамический буфер на трекере для хранения будущих участников
                var buffer = ecb.AddBuffer<FormingGroupElement>(trackerEntity);

                ecb.AddComponent(trackerEntity, new GroupingTimer
                {
                    TimeRemaining = config.FormationTime,
                    PotentialCommander = entities[potentialGroup[0]] // Центр круга
                });

                for (int k = 0; k < potentialGroup.Length; k++)
                {
                    int index = potentialGroup[k];
                    Entity enemy = entities[index];
                    alreadyGrouped[index] = true;

                    // Вешаем защитный тег. (IndependentTag НЕ снимаем, они продолжают бежать!)
                    ecb.AddComponent<FormingGroupTag>(enemy);

                    // Записываем врага в буфер трекера
                    buffer.Add(new FormingGroupElement { EnemyEntity = enemy });
                }
            }
            potentialGroup.Dispose();
        }

        entities.Dispose();
        transforms.Dispose();
        alreadyGrouped.Dispose();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}