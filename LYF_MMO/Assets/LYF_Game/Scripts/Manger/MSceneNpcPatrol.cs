using Google.Protobuf;
using UnityEngine;

/// <summary>显示所属 GS 已确认的巡逻路段，位置、点击碰撞和名牌始终跟随同一实体。</summary>
public sealed class MSceneNpcPatrol : MonoBehaviour
{
    /// <summary>当前模型绑定的场景 NPC，使用配置编号与服务端对应。</summary>
    private NpcEntity _npc;
    /// <summary>当前路段的 RO 人形模型动画器。</summary>
    private Animator _animator;
    /// <summary>最后一份服务端路段及说话朝向。</summary>
    private SceneNpcPatrolStep _step;
    /// <summary>真实收到该份采样的本机单调秒数，异步载入模型不重置时间。</summary>
    private double _received;
    /// <summary>该模型属于哪一份公共场景，用于对话停步。</summary>
    private long _sceneUid;

    /// <summary>在初次进场、转向或停步时，以宿主发布的位置和目标更新本机表现。</summary>
    /// <param name="npc">由本场景出生表创建的实体。</param><param name="uid">签发轨迹的实例。</param>
    /// <param name="step">GS 的当前位置、目标和步速。</param><param name="received">主线程收到采样的实时时刻。</param>
    public void Apply(NpcEntity npc, long uid, SceneNpcPatrolStep step, double received)
    {
        // 初次及动静转换时才切动作，普通校准不打断步行动画。
        bool changeMotion = _step == null || (_step.Speed > 0f) != (step.Speed > 0f);
        _npc = npc;
        _sceneUid = uid;
        _step = step;
        _received = received;
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (changeMotion)
        {
            if (step.Speed > 0f) _animator.CrossFadeInFixedTime("Walk", 0.12f);
            else _animator.CrossFadeInFixedTime("Idle", 0.12f);
        }
        transform.rotation = Quaternion.Euler(0f, step.Yaw, 0f);
        Update();
    }

    /// <summary>请求 GS 为同线所有玩家停步或继续巡逻。</summary><param name="talking">打开交谈为真，结束为假。</param>
    public void SetTalking(bool talking)
    {
        if (_npc == null) return;
        MNetClient.Instance.Send(SceneLineCode.NpcPatrol,
            new SceneNpcPatrolRequest { NpcId = _npc.NpcID, SceneUid = _sceneUid, Talking = talking }.ToByteString());
    }

    /// <summary>仅在已下发的路段内插值，下一路口、暂停和朝向都由宿主决定。</summary>
    private void Update()
    {
        if (_npc == null) return;
        // 从采样至本帧已走的米数，暂停时步速为零。
        float distance = (float)(MNetClient.MonotonicSeconds - _received) * _step.Speed;
        Vector3 from = new Vector3(_step.X, _step.Y, _step.Z);
        Vector3 to = new Vector3(_step.TargetX, _step.TargetY, _step.TargetZ);
        _npc.Position = Vector3.MoveTowards(from, to, distance);
    }

    /// <summary>模型离场后停止跟随上一份实体，等待下一次权威采样。</summary>
    private void OnDisable()
    {
        _npc = null;
        _step = null;
    }
}
