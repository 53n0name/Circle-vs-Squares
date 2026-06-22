using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
public partial struct EnemyGroupingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Инициализация конфига, если его нет (для теста)
        state.EntityManager.CreateSingleton(new GroupingConfig
        {
            MaxDistance = 5f,
            TimeToJoin = 2f
        });

        // Инициализация рандома
        state.EntityManager.CreateSingleton(new RandomGenerator
        {
            Value = new Random(123)
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var config = SystemAPI.GetSingleton<GroupingConfig>();
        var randomGen = SystemAPI.GetSingletonRW<RandomGenerator>();

        // 1. Ищем одиночек
        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<EnemyMeleeTag>()
                     .WithNone<InGroup>()
                     .WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            Entity foundTarget = Entity.Null;

            // Ищем ближайшего другого врага
            foreach (var (targetTransform, targetEntity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<EnemyMeleeTag>()
                         .WithEntityAccess())
            {
                if (entity == targetEntity) continue;

                if (math.distance(pos, targetTransform.ValueRO.Position) <= config.MaxDistance)
                {
                    foundTarget = targetEntity;
                    break;
                }
            }

            // Логика таймера кандидата
            if (foundTarget != Entity.Null)
            {
                if (!SystemAPI.HasComponent<GroupingCandidate>(entity))
                {
                    ecb.AddComponent(entity, new GroupingCandidate { PotentialTarget = foundTarget, Timer = config.TimeToJoin });
                }
                else
                {
                    var candidate = SystemAPI.GetComponentRW<GroupingCandidate>(entity);
                    candidate.ValueRW.Timer -= dt;

                    if (candidate.ValueRO.Timer <= 0)
                    {
                        JoinOrCreateGroup(entity, foundTarget, ref state, ecb, ref randomGen.ValueRW.Value);
                        ecb.RemoveComponent<GroupingCandidate>(entity);
                    }
                }
            }
            else if (SystemAPI.HasComponent<GroupingCandidate>(entity))
            {
                ecb.RemoveComponent<GroupingCandidate>(entity);
            }
        }
        ecb.Playback(state.EntityManager);
    }

    private void JoinOrCreateGroup(Entity self, Entity target, ref SystemState state, EntityCommandBuffer ecb, ref Random random)
    {
        if (SystemAPI.HasComponent<InGroup>(target))
        {
            var groupInfo = SystemAPI.GetComponent<InGroup>(target);
            ecb.AddComponent(self, new InGroup { GroupEntity = groupInfo.GroupEntity, IndexInGroup = 99 });
        }
        else
        {
            Entity group = ecb.CreateEntity();
            ecb.AddComponent(group, new EnemyGroupTag());
            bool selfIsLeader = random.NextBool();
            Entity leader = selfIsLeader ? self : target;
            Entity follower = selfIsLeader ? target : self;

            ecb.AddComponent(group, new EnemyGroupData { Leader = leader });
            ecb.AddComponent(leader, new InGroup { GroupEntity = group, IndexInGroup = 0 });
            ecb.AddComponent(follower, new InGroup { GroupEntity = group, IndexInGroup = 1 });
        }
    }
}