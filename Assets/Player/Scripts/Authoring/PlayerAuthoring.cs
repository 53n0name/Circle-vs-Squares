using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{

    public float _Speed;
    public float _Health;

    class  Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerMoveSpeedComponent
            {
                Speed = authoring._Speed
            });

            AddComponent(entity, new PlayerHealthComponent
            {
                Health = authoring._Health
            });

            AddComponent<PlayerTag>(entity);
        }
    }
}
