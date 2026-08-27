using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct GroupingTimerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GroupFormationConfigComponent>(out var config))
            return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Перебираем все Трекеры
        foreach (var (timer, buffer, trackerEntity) in SystemAPI.Query<RefRW<GroupingTimerComponent>, DynamicBuffer<FormingGroupElement>>().WithEntityAccess())
        {
            bool isGroupValid = true;

            // Проверяем, существует ли еще командир (вдруг его убил игрок)
            if (!SystemAPI.Exists(timer.ValueRO.PotentialCommander))
            {
                isGroupValid = false;
            }
            else
            {
                float3 commanderPos = SystemAPI.GetComponent<LocalTransform>(timer.ValueRO.PotentialCommander).Position;

                // Проверяем дистанцию всех участников до командира
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity enemy = buffer[i].EnemyEntity;

                    // Если враг уничтожен
                    if (!SystemAPI.Exists(enemy))
                    {
                        isGroupValid = false;
                        break;
                    }

                    float3 enemyPos = SystemAPI.GetComponent<LocalTransform>(enemy).Position;
                    float dist = math.distance(commanderPos.xy, enemyPos.xy);

                    // Вариант А: Если хоть один вышел из радиуса - отмена!
                    if (dist > config.FormationRadius)
                    {
                        isGroupValid = false;
                        break;
                    }
                }
            }

            // ЕСЛИ ГРУППА РАСПАЛАСЬ
            if (!isGroupValid)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity enemy = buffer[i].EnemyEntity;
                    if (SystemAPI.Exists(enemy))
                    {
                        // Снимаем защитный тег. Теперь Дирижер снова может забрать их в другие группы.
                        ecb.RemoveComponent<FormingGroupTag>(enemy);
                    }
                }

                // Уничтожаем трекер
                ecb.DestroyEntity(trackerEntity);
                continue;
            }

            // ЕСЛИ ВСЕ ХОРОШО - Убавляем таймер
            timer.ValueRW.TimeRemaining -= deltaTime;

            // ЕСЛИ ВРЕМЯ ВЫШЛО - ФИНАЛИЗИРУЕМ ГРУППУ
            if (timer.ValueRO.TimeRemaining <= 0f)
            {
                Entity finalGroupEntity = ecb.CreateEntity();

                // Заранее сохраняем лидера (он всегда нулевой в нашем буфере)
                Entity commanderEntity = buffer[0].EnemyEntity;

                // Вешаем на саму группу данные о её лидере
                ecb.AddComponent(finalGroupEntity, new GroupStateComponent
                {
                    CommanderEntity = commanderEntity,
                    TotalMembers = buffer.Length
                });

                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity enemy = buffer[i].EnemyEntity;

                    // Снимаем старые теги (включая защитный)
                    ecb.RemoveComponent<IndependentTag>(enemy);
                    ecb.RemoveComponent<FormingGroupTag>(enemy);

                    // Выдаем финальные групповые теги
                    ecb.AddComponent<InGroupTag>(enemy);

                    ecb.AddComponent(enemy, new GroupMemberComponent
                    {
                        GroupEntity = finalGroupEntity,
                        MemberIndex = i + 1
                    });

                    // Первый элемент буфера - это наш PotentialCommander
                    if (i == 0)
                    {
                        ecb.AddComponent<CommanderTag>(enemy);
                    }
                }

                // Таймер отработал, трекер больше не нужен
                ecb.DestroyEntity(trackerEntity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}