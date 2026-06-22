using Unity.Entities;
using UnityEngine;

public partial struct EnemyAttackCooldownComponent : IComponentData
{
    public float CurrentAttackCooldown;
    public float AttackCooldown;
}
