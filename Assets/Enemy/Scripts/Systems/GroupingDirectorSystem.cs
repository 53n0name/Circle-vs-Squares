using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct GroupingDirectorSystem : ISystem
{

    //private struct DistanceDebugRecord
    //{
    //    public int CommanderIndex;
    //    public int CheckedEnemyIndex;
    //    public float Distance;
    //}

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

            //var debugRecords = new NativeList<DistanceDebugRecord>(Allocator.Temp);

            for (int j = 0; j < entities.Length; j++)
            {
                if (alreadyGrouped[j]) continue;

                if (entities[i] == entities[j]) continue;

                // 2D математика
                float dist = math.distance(transforms[i].Position.xy, transforms[j].Position.xy);

                if (dist <= config.FormationRadius)
                {
                    potentialGroup.Add(j);

                    //// 2. Сохраняем данные текущей проверки в список
                    //debugRecords.Add(new DistanceDebugRecord
                    //{
                    //    CommanderIndex = entities[i].Index,
                    //    CheckedEnemyIndex = entities[j].Index,
                    //    Distance = dist
                    //});
                }


                // 3. Проверяем условие break
                if (potentialGroup.Length >= config.MaxRequiredGroupSize)
                {
                    //UnityEngine.Debug.Log(config.FormationRadius);

                    //// 4. Выводим весь накопленный список перед выходом
                    //UnityEngine.Debug.Log($"--- Группа собрана! Командир ID: {entities[i].Index}. Лог проверок: ---");

                    //for (int k = 0; k < debugRecords.Length; k++)
                    //{
                    //    var record = debugRecords[k];
                    //    UnityEngine.Debug.Log($"Враг 1: {record.CommanderIndex} | Враг 2: {record.CheckedEnemyIndex} | Расстояние: {record.Distance}");
                    //}
                    break; // Выходим из внутреннего цикла
                }
            }

            // Если нашли нужное количество - ЗАПУСКАЕМ ПРОЦЕСС
            if (potentialGroup.Length >= config.MinRequiredGroupSize)
            {
                Entity trackerEntity = ecb.CreateEntity();

                // Создаем динамический буфер на трекере для хранения будущих участников
                var buffer = ecb.AddBuffer<FormingGroupElement>(trackerEntity);

                ecb.AddComponent(trackerEntity, new GroupingTimerComponent
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