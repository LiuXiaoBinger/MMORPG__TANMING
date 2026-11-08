using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;

/// <summary>本闭包Unreal、Anim、Real与Rect原服务端投射物，客户端轨迹不参与命中判定。</summary>
internal sealed class MoshentaSkillBullet
{
    /// <summary>原技能运行宿主。</summary>
    private readonly MoshentaSkillRuntime runtime;
    /// <summary>本弹丸所属冻结施法。</summary>
    private readonly MoshentaSkillCast cast;
    /// <summary>零基原结果号。</summary>
    private readonly int index;
    /// <summary>原LongAttackData节点。</summary>
    private readonly XElement data;
    /// <summary>原弹丸类型。</summary>
    private readonly string type;
    /// <summary>出生逻辑毫秒。</summary>
    private readonly long born;
    /// <summary>上一次更新毫秒。</summary>
    private long last;
    /// <summary>当前服务器位置。</summary>
    private Vector3 position;
    /// <summary>直线子弹固定方向。</summary>
    private readonly Vector3 direction;
    /// <summary>Unreal固定目的地。</summary>
    private readonly Vector3 destination;
    /// <summary>Rect原公转中心。</summary>
    private readonly Vector3 revolutionCenter;
    /// <summary>Rect原初始半径向量。</summary>
    private readonly Vector3 revolutionOffset;
    /// <summary>Rect初始朝向。</summary>
    private readonly float facing;
    /// <summary>实际使用的原速度曲线，Anim只使用InitTime。</summary>
    private readonly MoshentaActionCurve curve;
    /// <summary>各目标最近受击时间，原BoomInterval是逐目标间隔。</summary>
    private readonly Dictionary<ulong, long> hitTimes = new Dictionary<ulong, long>();
    /// <summary>全弹累计碰撞次数。</summary>
    private int hitCount;
    /// <summary>从原结果配置创建弹丸。</summary><param name="owner">真实技能组件。</param><param name="item">投射事件。</param>
    internal MoshentaSkillBullet(MoshentaSkillRuntime owner, MoshentaSkillEvent item)
    {
        runtime = owner; cast = item.Cast; index = item.Index; data = item.Data.Element("LongAttackData");
        type = MoshentaSkillCatalog.S(data, "BulletType"); born = item.At; last = born;
        Vector3 start = MoshentaSkillRuntime.Rotate(Vector("Start"), cast.Facing);
        Vector3 end = MoshentaSkillRuntime.Rotate(Vector("End"), cast.Facing);
        position = runtime.Actor.Position + start; destination = cast.Point + end;
        direction = Vector3.Normalize(end - start);
        facing = (float)(Math.Atan2(direction.X, direction.Z) * 180 / Math.PI);
        XElement center = data.Element("RevolutionCenter");
        revolutionCenter = runtime.Actor.Position + MoshentaSkillRuntime.Rotate(new Vector3(MoshentaSkillCatalog.F(center, "x"), 0, MoshentaSkillCatalog.F(center, "z")), cast.Facing);
        revolutionOffset = position - revolutionCenter;
        if (type != "ESkillBulletType_Anim" && MoshentaSkillCatalog.I(data, "UseCurve") != 0)
            curve = new MoshentaActionCurve(MoshentaSkillCatalog.S(data, "OffsetZCurve"));
    }
    /// <summary>读取原XYZ字段。</summary><param name="prefix">Start或End。</param>
    private Vector3 Vector(string prefix) { return new Vector3(MoshentaSkillCatalog.F(data, prefix + "X"), MoshentaSkillCatalog.F(data, prefix + "Y"), MoshentaSkillCatalog.F(data, prefix + "Z")); }
    /// <summary>推进真实弹丸并产生原Boom结果。</summary><param name="now">场景当前逻辑毫秒。</param>
    internal bool Update(long now)
    {
        if (now <= last) return true;
        if (type == "ESkillBulletType_Anim")
        {
            if (now - born < MoshentaSkillCatalog.F(data, "InitTime") * 1000) return true;
            Boom(runtime.Actor.Owner.FindRole(cast.TargetId), runtime.TargetPosition(cast.TargetId)); return false;
        }
        // Real/Rect在TotalTime边界最后更新一次；Unreal以抵达目的地为终点。
        bool unreal = type == "ESkillBulletType_Unreal";
        long finish = born + (long)(MoshentaSkillCatalog.F(data, "TotalTime") * 1000);
        long tick = now;
        if (!unreal) tick = Math.Min(now, finish);
        float seconds = (tick - born) / 1000f;
        float delta = (tick - last) / 1000f;
        float speed = MoshentaSkillCatalog.F(data, "MaxSpeed");
        if (speed <= 0 && type != "ESkillBulletType_Rect") speed = 40;
        if (curve != null) speed = MoshentaSkillCatalog.F(data, "StartSpeed") + (speed - MoshentaSkillCatalog.F(data, "StartSpeed")) * curve.Evaluate(seconds);
        Vector3 previous = position;
        if (unreal)
        {
            if (MoshentaSkillRuntime.Horizontal(position, destination) <= speed * delta)
            {
                Boom(null, destination); return false;
            }
            position += Vector3.Normalize(destination - position) * speed * delta;
            last = tick; return true;
        }
        float angle = 0;
        if (MoshentaSkillCatalog.I(data, "needRevolution") != 0)
        {
            angle = MoshentaSkillCatalog.F(data, "RevolutionAngularSpeed") * seconds;
            position = revolutionCenter + MoshentaSkillRuntime.Rotate(revolutionOffset, angle);
        }
        else position += direction * speed * delta;
        List<OnlineRole> players = runtime.Actor.Owner.Players;
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            OnlineRole player = players[playerIndex];
            if (player.mainRoleInfo.BaseInfo.CurrHp <= 0) continue;
            Vector3 point = player.mainRoleInfo.BaseInfo.Pos.ToVector3();
            bool collision;
            if (type == "ESkillBulletType_Rect")
            {
                // 原服务端RealRectBullet未使用center_offset；盒大小取scale半径。
                XElement scale = data.Element("RectBullet").Element("scale");
                Vector3 local = MoshentaSkillRuntime.Rotate(point - position, -facing - angle);
                collision = Math.Abs(local.X) < MoshentaSkillCatalog.F(scale, "x") / 2 && Math.Abs(local.Z) < MoshentaSkillCatalog.F(scale, "z") / 2;
            }
            else collision = SegmentDistance(previous, position, point) <= MoshentaSkillCatalog.F(data, "Radius");
            if (!collision) continue;
            long lastHit;
            if (hitTimes.TryGetValue(player.GetID(), out lastHit) && lastHit + MoshentaSkillCatalog.F(data, "BoomInterval") * 1000 >= tick) continue;
            hitTimes[player.GetID()] = tick; hitCount++;
            Boom(player, position);
            if (MoshentaSkillCatalog.I(data, "BoomCount") > 0 && hitCount >= MoshentaSkillCatalog.I(data, "BoomCount")) return false;
        }
        last = tick; return now < finish;
    }
    /// <summary>原BulletResult：碰撞目标且AttackCount1直接命中，其余按弹丸中心重查范围。</summary><param name="target">真实碰撞角色；Unreal为空。</param><param name="point">爆炸中心。</param>
    private void Boom(OnlineRole target, Vector3 point)
    {
        if (target != null && MoshentaSkillCatalog.Indexed(cast.Skill.Effect, "AttackCount", index) == 1)
            runtime.HitPlayer(cast, index, target, 1, 1);
        else runtime.HitRange(cast, index, cast.Skill.Results[index], point, cast.TargetId, false);
    }
    /// <summary>原RealBullet沿本次运动线段检查圆形半径，防止跨帧穿过角色。</summary><param name="start">前帧位置。</param><param name="end">当前点。</param><param name="point">角色位置。</param>
    private static float SegmentDistance(Vector3 start, Vector3 end, Vector3 point)
    {
        // 直接使用水平分量；本机.NET4运行器的Vector3局部参数SIMD路径实测发生访问冲突。
        float dx = end.X - start.X;
        float dz = end.Z - start.Z;
        float length = dx * dx + dz * dz;
        float factor = 0;
        if (length > 0) factor = Math.Max(0, Math.Min(1, ((point.X - start.X) * dx + (point.Z - start.Z) * dz) / length));
        float offsetX = start.X + dx * factor - point.X;
        float offsetZ = start.Z + dz * factor - point.Z;
        return (float)Math.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
    }
}
