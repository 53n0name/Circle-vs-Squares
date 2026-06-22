using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public partial struct PlayerMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        float2 input = float2.zero;

        if (Keyboard.current.aKey.isPressed) { input.x -= 1; }
        if (Keyboard.current.dKey.isPressed) { input.x += 1; }
        if (Keyboard.current.sKey.isPressed) { input.y -= 1; }
        if (Keyboard.current.wKey.isPressed) { input.y += 1; }

        if (math.lengthsq(input) > 0)
        {
            input = math.normalize(input);
        }

        foreach (var (transform, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerMoveSpeedComponent>>()
                 .WithAll<PlayerTag>())
        {
            float3 move = new float3(input.x, input.y, 0)
                          * speed.ValueRO.Speed
                          * dt;

            transform.ValueRW.Position += move;
        }
    }
}
