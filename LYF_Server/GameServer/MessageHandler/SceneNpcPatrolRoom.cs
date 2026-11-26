using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using Google.Protobuf;

/// <summary>保存一个公共实例内的巡卫和已进入该场景的观察者。</summary>
internal sealed class SceneNpcPatrolRoom
{
    /// <summary>宿主分配的公共实例编号；独立 GS 的本地公共地图使用负数。</summary>
    internal readonly long Uid;
    /// <summary>按本线同一只巡逻时钟取人物脚底坐标，固定居民直接取出生位置。</summary>
    /// <param name="npc">已核对地图的 NPC 出生行。</param><param name="now">GS 当前的单调秒数。</param>
    internal Vector3 GetNpcPosition(NpcParseInfo npc, double now)
    {
        for (int index = 0; index < walkers.Length; index++)
        {
            if (walkers[index].NpcId != npc.ID) continue;
            // 单次读取不更改本线路下一次统一发布巡逻的位置或时间。
            double next = double.MaxValue;
            // 同线玩家收到的同一套巡逻采样。
            SceneNpcPatrolStep step = walkers[index].Sample(now, ref next);
            return new Vector3(step.X, step.Y, step.Z);
        }
        return npc.Position;
    }

    /// <summary>出生表中用于匹配客户端场景的地图编号。</summary>
    private readonly int map;
    /// <summary>本实例中各自独立暂停、行走的巡卫。</summary>
    private readonly SceneNpcWalker[] walkers;
    /// <summary>已完成公共场景同步请求、应收取后续转向的角色。</summary>
    internal readonly List<OnlineRole> Members = new List<OnlineRole>();
    /// <summary>下一拐点或一秒校准到期的宿主时刻。</summary>
    private double due;

    /// <summary>根据本地图的路点建立每条线独立的巡逻状态。</summary>
    /// <param name="uid">公共实例号。</param><param name="mapId">地图编号。</param><param name="routes">本地图的巡逻配置行。</param><param name="now">宿主单调秒数。</param>
    internal SceneNpcPatrolRoom(long uid, int mapId, XmlNodeList routes, double now)
    {
        Uid = uid;
        map = mapId;
        walkers = new SceneNpcWalker[routes.Count];
        for (int i = 0; i < routes.Count; i++) walkers[i] = new SceneNpcWalker((XmlElement)routes[i], now);
    }

    /// <summary>只向请求者补发，或在转角、停步和定期校准时向本实例广播同一份数据。</summary>
    /// <param name="now">一次广播共用的时刻。</param><param name="recipient">补发的单个角色；空表示全线。</param>
    internal void Send(double now, OnlineRole recipient)
    {
        // 最长一秒校准一次，路线拐角会提前触发。
        double next = now + 1;
        SceneNpcPatrolState state = new SceneNpcPatrolState { SceneUid = Uid, MapId = map, SampledMs = (long)(now * 1000) };
        for (int i = 0; i < walkers.Length; i++) state.Steps.Add(walkers[i].Sample(now, ref next));
        // 同线玩家复用序列化后的采样。
        ByteString payload = state.ToByteString();
        if (recipient != null)
        {
            recipient.SendToClient(SceneLineCode.NpcPatrol, payload);
            return;
        }
        due = next;
        for (int i = 0; i < Members.Count; i++) Members[i].SendToClient(SceneLineCode.NpcPatrol, payload);
    }

    /// <summary>退出副本前后的旧观察者不参与本地图广播，空场景由宿主回收。</summary>
    /// <param name="now">本次逻辑帧的秒数。</param>
    internal void Update(double now)
    {
        if (now < due) return;
        for (int i = Members.Count - 1; i >= 0; i--)
        {
            // 进副本的角色已脱离公共地图视野。
            OnlineRole member = Members[i];
            if (member.mainRoleInfo.BaseInfo.InstanceId != 0) Leave(member, now);
        }
        if (Members.Count > 0) Send(now, null);
    }

    /// <summary>玩家离开视野时同时释放其正在进行的谈话。</summary>
    /// <param name="role">离场或断线的真实角色。</param><param name="now">结束谈话的宿主时刻。</param>
    internal void Leave(OnlineRole role, double now)
    {
        if (!Members.Remove(role)) return;
        for (int i = 0; i < walkers.Length; i++)
            if (walkers[i].SetTalking(role, false, now)) due = now;
    }

    /// <summary>把真实说话者的停步或继续结果立即广播给同线观察者。</summary>
    /// <param name="role">正在对话的角色。</param><param name="request">目标 NPC 与开关。</param><param name="now">宿主时刻。</param>
    internal void Talk(OnlineRole role, SceneNpcPatrolRequest request, double now)
    {
        for (int i = 0; i < walkers.Length; i++)
        {
            if (walkers[i].NpcId != request.NpcId) continue;
            if (walkers[i].SetTalking(role, request.Talking, now)) Send(now, null);
            return;
        }
    }
}
