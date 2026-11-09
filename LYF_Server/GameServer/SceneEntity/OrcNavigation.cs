using System;
using System.Collections.Generic;
using System.Numerics;
using ServerMonster = GameServer.SceneEntity.MonsterEntity;

/// <summary>在魔神塔公共导航网上分段移动兽人，同一实体坐标同时用于命中和客户端快照。</summary>
internal sealed class OrcNavigation
{
    /// <summary>与客户端相同的投影搜索半盒，单位米。</summary>
    private static readonly MNavigationPoint search = new MNavigationPoint(4, 8, 4);
    /// <summary>所属副本配置选定的导航查询器，地图间不共用查询状态。</summary>
    private readonly MNavigation nav;
    /// <summary>当前路线转角；坐标由正式导航网查询得到。</summary>
    private readonly List<MNavigationPoint> path = new List<MNavigationPoint>();
    /// <summary>站位可达性查询的复用列表，与正在移动的路线分开。</summary>
    private readonly List<MNavigationPoint> checkPath = new List<MNavigationPoint>();
    /// <summary>拥有真实 Transform 的实例怪物。</summary>
    private readonly ServerMonster monster;
    /// <summary>当前实体原EntityTable配置的移动米速。</summary>
    private readonly float speed;
    /// <summary>指向路线里下一个待发的转角。</summary>
    private int corner;
    /// <summary>当前移动段的起点。</summary>
    private Vector3 from;
    /// <summary>整条路线申请时的目标，用来识别心跳重新提交的相同目的地。</summary>
    private Vector3 goal;
    /// <summary>当前移动段开始时的实例逻辑毫秒。</summary>
    private long start;
    /// <summary>当前移动段结束时刻，无有效路线时不读取。</summary>
    internal long End { get; private set; }
    /// <summary>当前移动段的目标点，单位米。</summary>
    internal Vector3 To { get; private set; }
    /// <summary>是否正在推进一条已经成功生成的路线。</summary>
    internal bool Moving { get; private set; }
    /// <summary>读取怪物当前服务端位置，不缓存出生位置。</summary>
    internal Vector3 Position { get { return new Vector3(monster.Transform.X, monster.Transform.Y, monster.Transform.Z); } }

    /// <summary>绑定该副本的怪物实体。</summary>
    /// <param name="owner">服务端实体工厂创建的兽人。</param>
    /// <param name="moveSpeed">原实体移动米速；旧兽人入口仍使用其已核验值。</param>
    /// <param name="navigation">当前副本地图的导航查询器。</param>
    internal OrcNavigation(ServerMonster owner, float moveSpeed, MNavigation navigation) { monster = owner; speed = moveSpeed; nav = navigation; }
    /// <summary>保留旧兽人调用入口，原表速度在运行时读取。</summary><param name="owner">服务端兽人实体。</param>
    internal OrcNavigation(ServerMonster owner) : this(owner, MoshentaOrcDefinition.Speed,
        new DungeonBattleConfig(DungeonSceneRegistration.Load(owner.MapId).battleConfig).Navigation) { }

    /// <summary>转换为跨端导航查询器使用的米制坐标。</summary>
    /// <param name="point">服务端世界位置。</param>
    private static MNavigationPoint NavPoint(Vector3 point) { return new MNavigationPoint(point.X, point.Y, point.Z); }

    /// <summary>沿原Vector3.HorizontalRotate方向与整度量化旋转，保留Y分量。</summary><param name="direction">待旋转的三维方向。</param><param name="degrees">原顺时针度数。</param>
    internal static Vector3 RotateDirection(Vector3 direction, float degrees)
    {
        // 原先取负角再环绕至360度，并以floor(value+0.5)取整度查三角函数表。
        float wrapped = -degrees % 360;
        if (wrapped < 0) wrapped += 360;
        double radians = Math.Floor(wrapped + 0.5f) * Math.PI / 180;
        // C#三角函数边界保持原float运算精度，方向与原ROSin/ROCos约定一致。
        float sine = (float)Math.Sin(radians);
        float cosine = (float)Math.Cos(radians);
        return new Vector3(direction.X * cosine - direction.Z * sine, direction.Y, direction.X * sine + direction.Z * cosine);
    }

    /// <summary>检查攻击站位的地面与连通性，失败时由追近子树尝试备用点。</summary>
    /// <param name="point">计算出的候选位置。</param>
    internal bool Reachable(Vector3 point) { return nav.PathFind(NavPoint(Position), NavPoint(point), checkPath, search); }
    /// <summary>技能逐步冲锋只取既有导航首段，不沿完整绕行路线瞬移。</summary><param name="desired">本时点原曲线期望位置。</param>
    internal Vector3 ClampSkillStep(Vector3 desired)
    {
        if (!nav.PathFind(NavPoint(Position), NavPoint(desired), checkPath, search)) return Position;
        // 本次原曲线步长上限，首段较长时仍只前进该距离。
        float step = Vector3.Distance(Position, desired);
        for (int index = 0; index < checkPath.Count; index++)
        {
            MNavigationPoint point = checkPath[index];
            Vector3 cornerPoint = new Vector3(point.X, point.Y, point.Z);
            Vector3 direction = cornerPoint - Position;
            float length = direction.Length();
            if (length < 0.001f) continue;
            if (length > step) return Position + direction / length * step;
            return cornerPoint;
        }
        return Position;
    }

    /// <summary>重复心跳沿用当前追击路线，目标移动后才重算。</summary>
    /// <param name="point">待比较的最终目的地。</param>
    internal bool GoingTo(Vector3 point) { return Moving && Vector3.DistanceSquared(goal, point) < 0.01f; }

    /// <summary>从权威当前位置开始走向一个新目的地。</summary>
    /// <param name="point">原场景提供的巡逻点或树算出的追击点。</param>
    /// <param name="now">当前实例逻辑毫秒。</param>
    internal bool Begin(Vector3 point, long now)
    {
        Moving = false;
        if (!nav.PathFind(NavPoint(Position), NavPoint(point), path, search)) return false;
        goal = point;
        corner = 0;
        return Next(now);
    }

    /// <summary>取下一段有长度的转角，计算速度、朝向与到达时刻。</summary>
    /// <param name="now">当前转角开始的实例逻辑毫秒。</param>
    internal bool Next(long now)
    {
        from = Position;
        while (corner < path.Count)
        {
            // 导航网返回的下一个三维转角。
            MNavigationPoint point = path[corner++];
            To = new Vector3(point.X, point.Y, point.Z);
            // 当前段的三维米数；导航查询的首点通常等于当前位置，直接跳过。
            float length = Vector3.Distance(from, To);
            if (length < 0.001f) continue;
            Face(To);
            start = now;
            if (speed <= 0) return false;
            End = start + (int)Math.Ceiling(length / speed * 1000);
            Moving = true;
            return true;
        }
        Moving = false;
        return false;
    }

    /// <summary>按已流逝的段时间插值，瞬时中止时位置也与下行描述一致。</summary>
    /// <param name="now">到本段结束为止的当前逻辑毫秒。</param>
    internal void Advance(long now)
    {
        // 本段内的真实位置；在最后一毫秒精确写入转角。
        Vector3 position = Vector3.Lerp(from, To, (float)(now - start) / (End - start));
        monster.Transform.X = position.X;
        monster.Transform.Y = position.Y;
        monster.Transform.Z = position.Z;
    }

    /// <summary>当前点停止，不再使用之前的路径。</summary>
    internal void Stop() { Moving = false; }

    /// <summary>面向水平目标，与 Unity 正 Z 朝前的角度约定一致。</summary>
    /// <param name="point">朝向目标，世界米制坐标。</param>
    internal void Face(Vector3 point)
    {
        // 水平偏移决定绕 Y 轴的朝向角。
        Vector3 offset = point - Position;
        if (offset.X != 0 || offset.Z != 0)
            monster.Transform.Yaw = (float)((Math.Atan2(offset.X, offset.Z) * 180 / Math.PI + 360) % 360);
    }
}
