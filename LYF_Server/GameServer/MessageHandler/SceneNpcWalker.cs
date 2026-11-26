using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Xml;

/// <summary>一条公共线路上一名巡卫的步行时钟和谈话者，共用 GS 的单调时间。</summary>
internal sealed class SceneNpcWalker
{
    /// <summary>对应 Luban 出生行，所有观察者使用相同编号。</summary>
    internal readonly int NpcId;
    /// <summary>闭环上已贴地的路点，首点不在末尾重复。</summary>
    private readonly Vector3[] points;
    /// <summary>每段终点到起点沿路累计的米数。</summary>
    private readonly double[] distances;
    /// <summary>该职业巡逻时的步速，单位米每秒。</summary>
    private readonly float speed;
    /// <summary>按配置步速走完整圈的秒数，不含停留。</summary>
    private readonly double lapSeconds;
    /// <summary>每圈返回岗位后的停留秒数，未配置的巡卫连续行走。</summary>
    private readonly double homeWait;
    /// <summary>正在与这名居民或巡卫对话的同线角色。</summary>
    private readonly HashSet<OnlineRole> speakers = new HashSet<OnlineRole>();
    /// <summary>上次起步的宿主时间，单位秒。</summary>
    private double started;
    /// <summary>前几次停步之前已累计的步行秒数。</summary>
    private double walked;
    /// <summary>停下与第一位玩家谈话时的水平朝向角。</summary>
    private float talkYaw;

    /// <summary>从策划路点建立一圈的累计弧长。</summary>
    /// <param name="route">地图配置中只属于本巡卫的 route。</param><param name="now">本次开线的单调秒数。</param>
    internal SceneNpcWalker(XmlElement route, double now)
    {
        NpcId = int.Parse(route.GetAttribute("npc"), CultureInfo.InvariantCulture);
        speed = float.Parse(route.GetAttribute("speed"), CultureInfo.InvariantCulture);
        if (route.HasAttribute("homeWait"))
            homeWait = double.Parse(route.GetAttribute("homeWait"), CultureInfo.InvariantCulture);
        started = now;
        // 配置中的折点已由该场景正式导航网求路、贴地。
        XmlNodeList nodes = route.SelectNodes("point");
        points = new Vector3[nodes.Count];
        distances = new double[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            // 同一世界坐标系下的三维脚底位置。
            XmlElement point = (XmlElement)nodes[i];
            points[i] = new Vector3(float.Parse(point.GetAttribute("x"), CultureInfo.InvariantCulture),
                float.Parse(point.GetAttribute("y"), CultureInfo.InvariantCulture), float.Parse(point.GetAttribute("z"), CultureInfo.InvariantCulture));
        }
        // 到每段终点的路径总长，最后一段接回配置首点。
        double length = 0;
        for (int i = 0; i < points.Length; i++)
        {
            length += Vector3.Distance(points[i], points[(i + 1) % points.Length]);
            distances[i] = length;
        }
        lapSeconds = length / speed;
    }

    /// <summary>按同一次 GS 采样计算脚底和下一折点，客户端最多走到这段的终点。</summary>
    /// <param name="now">宿主单调秒数。</param><param name="due">下一次应当发布的最迟时间，遇到转角可提前。</param>
    internal SceneNpcPatrolStep Sample(double now, ref double due)
    {
        // 停步时只读已累计的时间，继续巡逻后补上这一段运行时长。
        double seconds = walked;
        if (speakers.Count == 0) seconds += now - started;
        // 一圈先沿路行走，再在首点待机；交谈会暂停这同一只时钟。
        double phase = seconds % (lapSeconds + homeWait);
        // 本次是否已回到出生岗位。
        bool atHome = phase >= lapSeconds;
        // 当前沿路线的米数，待机时为首点的零米。
        double distance = 0;
        if (!atHome) distance = phase * speed;
        // 当前位置所在的折线段下标。
        int segment = 0;
        while (segment + 1 < distances.Length && distance >= distances[segment]) segment++;
        // 本段起点、终点和单位朝向。
        Vector3 from = points[segment];
        Vector3 target = points[(segment + 1) % points.Length];
        Vector3 direction = Vector3.Normalize(target - from);
        // 从目标逆推本次采样位置，单位米。
        double remaining = distances[segment] - distance;
        Vector3 position = target - direction * (float)remaining;
        // 对话时沿用面对说话者的朝向，并将步速保持为零。
        SceneNpcPatrolStep step = new SceneNpcPatrolStep { NpcId = NpcId, X = position.X, Y = position.Y, Z = position.Z,
            TargetX = target.X, TargetY = target.Y, TargetZ = target.Z, Yaw = talkYaw };
        if (speakers.Count == 0)
        {
            step.Yaw = (float)(Math.Atan2(direction.X, direction.Z) * 180.0 / Math.PI);
            if (atHome)
                due = Math.Min(due, now + lapSeconds + homeWait - phase);
            else
            {
                step.Speed = speed;
                due = Math.Min(due, now + remaining / speed);
            }
        }
        return step;
    }

    /// <summary>验证实际谈话距离，首位说话者暂停步行，最后一位离开后接着走。</summary>
    /// <param name="role">从网关会话取得的同线角色。</param><param name="talking">真为打开，假为关闭或离场。</param><param name="now">GS 单调秒数。</param>
    internal bool SetTalking(OnlineRole role, bool talking, double now)
    {
        if (!talking)
        {
            if (!speakers.Remove(role) || speakers.Count != 0) return false;
            started = now;
            return true;
        }
        // 由 GS 对照玩家位置与巡卫的当前路段，留一米移动同步余量。
        double due = now + 1;
        SceneNpcPatrolStep step = Sample(now, ref due);
        Vector3 offset = role.mainRoleInfo.BaseInfo.Pos.ToVector3() - new Vector3(step.X, step.Y, step.Z);
        if (offset.LengthSquared() > 121 || !speakers.Add(role)) return false;
        if (speakers.Count != 1) return false;
        walked += now - started;
        talkYaw = (float)(Math.Atan2(offset.X, offset.Z) * 180.0 / Math.PI);
        return true;
    }
}
