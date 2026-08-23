using Unity.Entities;
using UnityEngine;

public partial struct GroupMemberComponent : IComponentData {
    public Entity GroupEntity;
    public int MemberIndex;
}
