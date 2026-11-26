using System.Collections.Generic;
using UnityEngine;

/// <summary>让普通巡卫沿本图导航网循环走过配置路标，对话时停下，结束后接着走。</summary>
public sealed class MSceneNpcPatrol : MonoBehaviour
{
    /// <summary>普通巡卫的步行速度，单位为米每秒。</summary>
    private const float WalkSpeed = 1.45f;
    /// <summary>导航求得的闭合折线路径，首尾为同一点，单位米。</summary>
    private readonly List<Vector3> _path = new List<Vector3>();
    /// <summary>与模型、点击导航和名牌共用位置的巡卫实体。</summary>
    private NpcEntity _npc;
    /// <summary>播放 RO 待机和行走动作的模型动画器。</summary>
    private Animator _animator;
    /// <summary>当前行进目标在路线中的索引，零号位置是出发点。</summary>
    private int _next;
    /// <summary>本机玩家正在与该巡卫交谈。</summary>
    private bool _talking;

    /// <summary>把一圈路标展开为该地图导航网的可行路线，只在模型出生时求路。</summary>
    /// <param name="npc">本场景内已载入模型的巡逻实体。</param>
    /// <param name="waypoints">按行走顺序填写的世界路标，末点自动接回首点。</param>
    public void Begin(NpcEntity npc, Vector3[] waypoints)
    {
        _npc = npc;
        _animator = GetComponentInChildren<Animator>();
        // 各段共享临时路点；最终路线只保留相邻段共有的交点一次。
        List<Vector3> leg = new List<Vector3>();
        _path.Add(waypoints[0]);
        for (int i = 0; i < waypoints.Length; i++)
        {
            // 末段回到配置的第一个路标，沿导航面行走。
            Vector3 end = waypoints[(i + 1) % waypoints.Length];
            if (!MNavigationMgr.singleton.TryGetPath(waypoints[i], end, leg))
            {
                Debug.LogWarning("巡逻点无法连通：" + npc.Name + " " + waypoints[i] + " → " + end, this);
                enabled = false;
                return;
            }
            for (int j = 0; j < leg.Count; j++)
            {
                if ((leg[j] - _path[_path.Count - 1]).sqrMagnitude > 0.0001f)
                    _path.Add(leg[j]);
            }
        }
        _next = 1;
        if (_path.Count > 1) _animator.Play("Walk");
    }

    /// <summary>对白期间暂停本机表现，关闭后从原路点继续。</summary>
    /// <param name="talking">打开对白为真，退出对白为假。</param>
    public void SetTalking(bool talking)
    {
        if (_talking == talking) return;
        _talking = talking;
        if (talking) _animator.CrossFadeInFixedTime("Idle", 0.12f);
        else _animator.CrossFadeInFixedTime("Walk", 0.12f);
    }

    /// <summary>以真实帧间隔消耗本帧步长，跨过折点时继续使用剩余距离。</summary>
    private void Update()
    {
        if (_talking || _path.Count < 2) return;
        // 余下的本帧距离，单位米。
        float remaining = WalkSpeed * Time.deltaTime;
        while (remaining > 0f)
        {
            // 从当前脚底到下一导航折点的三维向量。
            Vector3 delta = _path[_next] - _npc.Position;
            // 当前路段尚余长度，单位米。
            float distance = delta.magnitude;
            // 转身只围绕竖轴，让双脚保持在道路上。
            Vector3 forward = new Vector3(delta.x, 0f, delta.z);
            if (forward.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.LookRotation(forward);
            if (remaining < distance)
            {
                _npc.Position += delta * (remaining / distance);
                return;
            }
            _npc.Position = _path[_next];
            remaining -= distance;
            _next++;
            if (_next == _path.Count) _next = 1;
        }
    }
}
