using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial struct EnemyMeleeChasePlayerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        if (!SystemAPI.TryGetSingletonEntity<PlayerTag>(out Entity player)) return;

        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(player).Position;
        var playerHealth = SystemAPI.GetComponent<PlayerHealthComponent>(player);

        foreach (var (transform, velocity, speed, rotationSpeed, range, attack, cooldown, entity) in
                    SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>,
                    RefRO<EnemyMovementSpeedComponent>, RefRO<EnemyRotationSpeedComponent>,
                    RefRO<EnemyAttackRangeComponent>, RefRO<EnemyAttackPowerComponent>,
                    RefRW<EnemyAttackCooldownComponent>>()
                    .WithAll<EnemyMeleeTag>()
                    .WithEntityAccess())
        {
            // Пропускаем всех, кто в группе и НЕ является лидером (индекс != 0)
            if (SystemAPI.HasComponent<InGroup>(entity) && SystemAPI.GetComponent<InGroup>(entity).IndexInGroup != 0) 
                continue;

            float3 currentPos = transform.ValueRO.Position;

            // 1. ЖЕЛТАЯ ЛИНИЯ И ТОЧКА (Где лидер видит игрока)
            // Рисуем линию взгляда от врага к игроку
            Debug.DrawLine(currentPos, playerPos, Color.yellow);
            
            // Рисуем "большую точку" (крест) на игроке
            float pointSize = 0.2f; 
            Debug.DrawRay(playerPos, new float3(pointSize, pointSize, 0), Color.yellow);
            Debug.DrawRay(playerPos, new float3(-pointSize, -pointSize, 0), Color.yellow);
            Debug.DrawRay(playerPos, new float3(pointSize, -pointSize, 0), Color.yellow);
            Debug.DrawRay(playerPos, new float3(-pointSize, pointSize, 0), Color.yellow);

            // --- ТВОЯ ЛОГИКА РАСЧЕТА УГЛА ---
            float3 playerDirection = math.normalize(playerPos - currentPos);
            float targetAngle = math.atan2(playerDirection.y, playerDirection.x);

            float currentAngle = math.atan2(2f * transform.ValueRW.Rotation.value.z * transform.ValueRW.Rotation.value.w,
                1f - 2f * (transform.ValueRW.Rotation.value.z * transform.ValueRW.Rotation.value.z));

            float delta = math.atan2(math.sin(targetAngle - currentAngle), math.cos(targetAngle - currentAngle));
            float maxStep = math.radians(rotationSpeed.ValueRO.RotationSpeed) * dt;
            float step = math.clamp(delta, -maxStep, maxStep);
            float newAngle = currentAngle + step;

            // Вращение
            velocity.ValueRW.Angular = new float3(0, 0, delta);
            float maxRotSpeed = math.radians(rotationSpeed.ValueRO.RotationSpeed);
            velocity.ValueRW.Angular.z = math.clamp(delta, -maxRotSpeed, maxRotSpeed);

            // Движение
            float walkAngle = math.abs(math.clamp(delta * Mathf.Rad2Deg, -90f, 90f));
            double walkAngle2 = -0.000000358428f * math.pow(walkAngle, 3) + 0.000174160248 * math.pow(walkAngle, 2) - 0.024103955911 * math.pow(walkAngle, 1) + 1.045735731020;
            float f = (float)walkAngle2;
            float directionalSpeed = speed.ValueRO.Speed * f;

            float3 moveDir = math.normalize(new float3(math.cos(newAngle), math.sin(newAngle), 0f));
            velocity.ValueRW.Linear = moveDir * directionalSpeed;

            // Рисуем линию от врага в направлении его вектора скорости
            Debug.DrawRay(currentPos, velocity.ValueRO.Linear, Color.red);

            // --- ЛОГИКА АТАКИ ---
            cooldown.ValueRW.CurrentAttackCooldown -= dt;
            if (math.distance(playerPos, currentPos) < range.ValueRO.RangeAttack && cooldown.ValueRW.CurrentAttackCooldown < 0)
            {
                playerHealth.Health -= attack.ValueRO.AttackPower;
                SystemAPI.SetComponent(player, playerHealth);
                cooldown.ValueRW.CurrentAttackCooldown = cooldown.ValueRO.AttackCooldown;
            }
        }
    }
}