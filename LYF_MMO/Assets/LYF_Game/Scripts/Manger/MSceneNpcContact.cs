using UnityEngine;
using Google.Protobuf;

/// <summary>跟随公共地图角色的触发胶囊，仅在接触居民的那一刻通知其播放受击。</summary>
public sealed class MSceneNpcContact : MonoBehaviour
{
    /// <summary>为新角色或跨图沿用的角色建立一个贴身探测器。</summary>
    /// <param name="role">本机操作角色的实际模型根节点。</param>
    internal static void Attach(GameObject role)
    {
        if (role.GetComponentInChildren<MSceneNpcContact>() != null) return;
        // 两种职业复用各自角色控制器的高度、半径和脚底位置。
        if (!role.TryGetComponent(out CharacterController body)) return;
        // 探测器在独立子物体上，保持角色自身的实体碰撞和点击形状。
        GameObject sensor = new GameObject("居民接触");
        sensor.layer = LayerMask.NameToLayer("Ignore Raycast");
        sensor.transform.SetParent(role.transform, false);
        // 运动学刚体让走动的角色和巡逻靠近站定角色都能产生触发事件。
        Rigidbody rigid = sensor.AddComponent<Rigidbody>();
        rigid.isKinematic = true;
        rigid.useGravity = false;
        // 仅探测 NPC 层，补入控制器皮肤和物理接触偏移，使两具胶囊顶住时也能进入触发器。
        CapsuleCollider contact = sensor.AddComponent<CapsuleCollider>();
        contact.isTrigger = true;
        contact.excludeLayers = ~LayerMask.GetMask("NPC");
        contact.center = body.center;
        contact.radius = body.radius + body.skinWidth + Physics.defaultContactOffset;
        contact.height = body.height;
        sensor.AddComponent<MSceneNpcContact>();
    }

    /// <summary>同一对碰撞体只在相交时触发一次，分开后下次接触可再次播放。</summary>
    /// <param name="other">本次进入探测器的 NPC 身体碰撞体。</param>
    private void OnTriggerEnter(Collider other)
    {
        // 只有出生为人物的模型根节点持有动作组件，路牌和马车等仍沿用物件交互。
        if (!other.isTrigger || !other.TryGetComponent(out MSceneNpcMotion motion)) return;
        motion.PlayContact();
        // 真实进入时即发一次独立事件，短碰不必等下一份角色位置采样。
        MNetClient.Instance.Send(SceneLineCode.NpcContact, new SceneNpcContact { NpcId = motion.NpcId }.ToByteString());
    }
}
