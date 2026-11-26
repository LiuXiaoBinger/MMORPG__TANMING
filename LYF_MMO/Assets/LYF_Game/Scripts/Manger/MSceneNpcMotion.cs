using UnityEngine;

/// <summary>公共场景人物的职业待机与上身受击表现，根节点位置由出生和巡逻系统持有。</summary>
public sealed class MSceneNpcMotion : MonoBehaviour
{
    /// <summary>本图人物的出生编号，用于其他客户端找到同一个 NPC。</summary>
    internal int NpcId { get; private set; }

    /// <summary>基础层默认循环，名称来自各职业已有的动画控制器。</summary>
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    /// <summary>独立上身层的一次性受击状态，播完后由控制器过渡回空闲层。</summary>
    private static readonly int BeHit = Animator.StringToHash("Contact.BeHit");
    /// <summary>本人物实际驱动骨骼的模型动画器。</summary>
    private Animator _animator;
    /// <summary>该模型上身受击层的下标，未配置此层时为负数。</summary>
    private int _contactLayer;

    /// <summary>人物进场后从自己的待机相位开始，避免同街居民同时做出完全相同的动作。</summary>
    /// <param name="animator">已绑定预制体控制器的骨骼动画器。</param>
    /// <param name="npcId">出生表编号，决定零到一之间的循环起点。</param>
    internal void Bind(Animator animator, int npcId)
    {
        NpcId = npcId;
        _animator = animator;
        _contactLayer = animator.GetLayerIndex("Contact");
        // 黄金分割的小数部分把相邻出生编号分散到循环的不同位置。
        float phase = Mathf.Repeat(npcId * 0.618034f, 1f);
        if (animator.HasState(0, Idle)) animator.Play(Idle, 0, phase);
        // CharacterController 对触发器使用缩小的形状；用人物自己的身体尺寸标记实际接触面。
        if (!TryGetComponent(out CharacterController body)) return;
        // 接触面只与玩家的贴身探测器交叠，角色的实体阻挡仍由原来的控制器负责。
        CapsuleCollider surface = gameObject.AddComponent<CapsuleCollider>();
        surface.isTrigger = true;
        surface.excludeLayers = ~LayerMask.GetMask("Ignore Raycast");
        surface.center = body.center;
        surface.radius = body.radius;
        surface.height = body.height;
    }

    /// <summary>一次新的玩家接触从头播放受击，只叠加上半身，腿部继续待机或走路。</summary>
    internal void PlayContact()
    {
        if (_contactLayer >= 0) _animator.Play(BeHit, _contactLayer, 0f);
    }
}
