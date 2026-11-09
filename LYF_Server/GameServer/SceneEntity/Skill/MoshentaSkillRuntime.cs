using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using MMoRpgCommon.SceneEntity;

/// <summary>按RO技能起手、吟唱、施放、结果和结束顺序推进的魔神塔技能组件。</summary>
internal sealed class MoshentaSkillRuntime
{
    /// <summary>真实怪物宿主。</summary>
    internal readonly MoshentaActor Actor;
    /// <summary>原Buff容器，与本实体共同清理。</summary>
    internal readonly MoshentaBuffRuntime Buffs;
    /// <summary>该宿主原Lua公式环境，共用场景随机序列。</summary>
    internal readonly MoshentaDamageCalculator Calculator;
    /// <summary>同实例玩家的原Buff容器，场景Clear时精确释放。</summary>
    private static readonly Dictionary<SceneWaveSpawner, Dictionary<ulong, MoshentaBuffRuntime>> playerBuffs = new Dictionary<SceneWaveSpawner, Dictionary<ulong, MoshentaBuffRuntime>>();
    /// <summary>每技能下次允许真正施法的逻辑毫秒。</summary>
    private readonly Dictionary<int, long> cooldowns = new Dictionary<int, long>();
    /// <summary>所有尚未发生的原动作事件。</summary>
    private readonly List<MoshentaSkillEvent> events = new List<MoshentaSkillEvent>();
    /// <summary>实际飞行中的原投射物。</summary>
    private readonly List<MoshentaSkillBullet> bullets = new List<MoshentaSkillBullet>();
    /// <summary>当前吟唱或动作上下文。</summary>
    private MoshentaSkillCast current;
    /// <summary>是否处于固定/浮动吟唱阶段。</summary>
    private bool singing;
    /// <summary>当前阶段开始时点。</summary>
    private long startTime;
    /// <summary>当前阶段截止时点。</summary>
    private long endTime;
    /// <summary>原Charge位移起点，未启用时截止值为0。</summary>
    private Vector3 chargeFrom;
    /// <summary>原Charge按施法方向计算的完整位移。</summary>
    private Vector3 chargeOffset;
    /// <summary>原Charge实际开始毫秒。</summary>
    private long chargeStart;
    /// <summary>原Charge结束毫秒；0表示没有位移。</summary>
    private long chargeEnd;
    /// <summary>是否正在吟唱或播放原技能动作。</summary>
    internal bool IsCasting { get { return current != null; } }
    /// <summary>当前技能编号，空闲为0。</summary>
    internal int CurrentSkillId { get { if (current != null) return current.Skill.Id; return 0; } }
    /// <summary>当前技能等级，空闲为0。</summary>
    internal int CurrentLevel { get { if (current != null) return current.Level; return 0; } }
    /// <summary>冻结施法位置。</summary>
    internal Vector3 CastPoint { get { if (current != null) return current.Point; return Actor.Position; } }
    /// <summary>冻结目标UID。</summary>
    internal ulong CastTargetId { get { if (current != null) return current.TargetId; return 0; } }
    /// <summary>当前阶段开始毫秒。</summary>
    internal long StartTime { get { return startTime; } }
    /// <summary>当前阶段截止毫秒。</summary>
    internal long EndTime { get { return endTime; } }
    /// <summary>最早尚未处理的事件时点。</summary>
    internal long NextEventTime
    {
        get
        {
            // 无待办使用最大值，与场景时序合并。
            long next = long.MaxValue;
            if (current != null) next = endTime;
            for (int index = 0; index < events.Count; index++) next = Math.Min(next, events[index].At);
            next = Math.Min(next, Buffs.NextTime);
            if (chargeEnd > 0) next = Math.Min(next, chargeEnd);
            Dictionary<ulong, MoshentaBuffRuntime> container;
            if (playerBuffs.TryGetValue(Actor.Owner, out container))
            {
                List<MoshentaBuffRuntime> values = new List<MoshentaBuffRuntime>(container.Values);
                for (int index = 0; index < values.Count; index++) next = Math.Min(next, values[index].NextTime);
            }
            return next;
        }
    }
    /// <summary>绑定真实实体；出生Buff由Actor.Born在场景登记后处理。</summary><param name="actor">已创建的原实体。</param>
    internal MoshentaSkillRuntime(MoshentaActor actor)
    {
        Actor = actor; Buffs = new MoshentaBuffRuntime(actor.Owner, actor, null);
        Calculator = new MoshentaDamageCalculator(actor.Owner.Random, actor.Owner.Battle.Catalog);
    }
    /// <summary>检查原CD、吟唱/技能状态和目标射程。</summary><param name="skillId">原技能编号。</param><param name="level">实际等级。</param><param name="targetId">同实例目标UID。</param><param name="castPoint">本次施法位置。</param>
    internal bool CanCast(int skillId, int level, ulong targetId, Vector3 castPoint)
    { return CanCastCore(skillId, level, targetId, castPoint, true); }
    /// <summary>原AIFilterSkill use_attack_field=false仅略过射程，不跳过CD和实体状态。</summary><param name="skillId">技能编号。</param><param name="level">等级。</param><param name="targetId">目标。</param><param name="castPoint">施法点。</param>
    internal bool CanCastIgnoringRange(int skillId, int level, ulong targetId, Vector3 castPoint)
    { return CanCastCore(skillId, level, targetId, castPoint, false); }
    /// <summary>两类原筛选节点共用真实状态与CD判断。</summary><param name="skillId">技能。</param><param name="level">等级。</param><param name="targetId">目标。</param><param name="castPoint">位置。</param><param name="checkRange">原节点是否检查攻击范围。</param>
    private bool CanCastCore(int skillId, int level, ulong targetId, Vector3 castPoint, bool checkRange)
    {
        if (Actor.IsDead || IsCasting || level != 1) return false;
        // 真实Gene状态已经被玩家技能和本副本Buff共用。
        GeneState state = Actor.Monster.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime.GetStates();
        if ((state & (GeneState.Stun | GeneState.Silence)) != 0) return false;
        long next;
        if (cooldowns.TryGetValue(skillId, out next) && Actor.Owner.Now < next) return false;
        // 原需要目标技能必须使用同实例有效实体，位置类技能允许无目标。
        MoshentaSkillDefinition skill = Actor.Owner.Battle.Catalog.Skill(skillId);
        if (skill.NeedTarget)
        {
            OnlineRole role = Actor.Owner.FindRole(targetId);
            MoshentaActor monster = Actor.Owner.FindActor(targetId);
            if (role == null && monster == null) return false;
            if (role != null && role.mainRoleInfo.BaseInfo.CurrHp <= 0) return false;
            if (monster != null && monster.IsDead) return false;
            if (!checkRange) return true;
            // CastPoint已由原AI节点从真实目标位置取得。
            float distance = Horizontal(Actor.Position, castPoint);
            float far = MoshentaSkillCatalog.F(skill.Effect, "FarRange");
            if (distance < MoshentaSkillCatalog.F(skill.Effect, "LowRange")) return false;
            if (far > 0 && distance > far) return false;
        }
        return true;
    }
    /// <summary>保留原组内过滤顺序，不在过滤时抽概率。</summary><param name="group">原UnitSkillID组。</param><param name="targetId">实际目标。</param>
    internal List<int> FilterGroup(int group, ulong targetId)
    {
        // 原绑定为skill=level=group=percent。
        string[] bindings = MoshentaSkillCatalog.S(Actor.Owner.Battle.Catalog.Entity(Actor.EntityId), "UnitSkillID").Split('|');
        List<int> result = new List<int>();
        for (int index = 0; index < bindings.Length; index++)
        {
            string[] fields = bindings[index].Split('=');
            if (fields.Length != 4 || int.Parse(fields[2]) != group) continue;
            int id = int.Parse(fields[0]);
            if (CanCast(id, int.Parse(fields[1]), targetId, TargetPosition(targetId))) result.Add(id);
        }
        return result;
    }
    /// <summary>原RandomSkill固定万分比抽签；未覆盖概率段返回失败，交行为树继续普攻分支。</summary><param name="skillIds">已过滤的原技能序列。</param><param name="targetId">实际目标。</param><param name="castPoint">本次目标位置。</param>
    internal bool CastFromList(List<int> skillIds, ulong targetId, Vector3 castPoint)
    {
        // 原randInt(0,MAX_EASY_SKILL_LIMIT+1)包含0到10000。
        int value = Actor.Owner.Random.Next(0, 10001);
        string[] bindings = MoshentaSkillCatalog.S(Actor.Owner.Battle.Catalog.Entity(Actor.EntityId), "UnitSkillID").Split('|');
        for (int index = 0; index < skillIds.Count; index++)
        {
            for (int binding = 0; binding < bindings.Length; binding++)
            {
                string[] fields = bindings[binding].Split('=');
                if (fields.Length < 4 || int.Parse(fields[0]) != skillIds[index]) continue;
                value -= int.Parse(fields[3]);
                if (value <= 0) return Cast(skillIds[index], int.Parse(fields[1]), targetId, castPoint);
                break;
            }
        }
        return false;
    }
    /// <summary>创建真实吟唱状态；无吟唱立即进入Fire。</summary><param name="skillId">原技能编号。</param><param name="level">原等级。</param><param name="targetId">真实目标UID。</param><param name="castPoint">冻结施法点。</param>
    internal bool Cast(int skillId, int level, ulong targetId, Vector3 castPoint)
    {
        if (!CanCast(skillId, level, targetId, castPoint)) return false;
        // 攻击属性冻结在起手，命中目标属性不冻结。
        MoshentaSkillDefinition skill = Actor.Owner.Battle.Catalog.Skill(skillId);
        current = new MoshentaSkillCast { Skill = skill, Level = level, TargetId = targetId, Point = castPoint,
            Facing = Actor.Facing, Snapshot = MoshentaDamageSnapshot.Capture(Actor), TimeScale = TimeScale(skill) };
        // 有目标时技能方向按真实施法点，空目标保留原宿主朝向。
        Vector3 offset = castPoint - Actor.Position;
        if (offset.X * offset.X + offset.Z * offset.Z > 0.000001f) current.Facing = (float)(Math.Atan2(offset.X, offset.Z) * 180 / Math.PI);
        // 原Direction/Self中心跟随自身，只有Direction朝向目标。
        int rangeType = MoshentaSkillCatalog.I(skill.Table, "SkillTargetRangeType");
        if (rangeType == 1 || rangeType == 3) current.Point = Actor.Position;
        if (rangeType == 3) current.Facing = Actor.Facing;
        Actor.Monster.Transform.Yaw = current.Facing;
        startTime = Actor.Owner.Now; endTime = startTime + (long)(skill.Singing * 1000);
        singing = skill.Singing > 0;
        if (singing) Actor.Owner.PublishSkillAction(Actor, MonsterActionPhase.Singing, skill.Id, level, targetId, current.Point, startTime, endTime);
        else Fire(startTime);
        return true;
    }
    /// <summary>原ASPD仅影响声明受其影响的动作和CD。</summary><param name="skill">原技能。</param>
    private float TimeScale(MoshentaSkillDefinition skill)
    {
        if (MoshentaSkillCatalog.I(skill.Table, "IsAffectedASPD") == 0) return 1;
        // 原怪物最终ASPD，基础字段本副本均低于200。
        float speed = (int)(50.0 / (200 - MoshentaSkillCatalog.F(Actor.Owner.Battle.Catalog.Entity(Actor.EntityId), "ASPD")) * 10000) / 10000f;
        if (MoshentaSkillCatalog.I(skill.Table, "SkillDamType") != 0)
        {
            if (speed <= 1) return 1;
            speed = 1 + (speed - 1) * MoshentaSkillCatalog.F(skill.Table, "ASPDPercent");
        }
        return 1 / speed;
    }
    /// <summary>吟唱结束后开启动作、CD和原结果计时器。</summary><param name="now">精确Fire时点，非本帧终点。</param>
    private void Fire(long now)
    {
        singing = false; startTime = now; current.Start = now;
        // 原OnStart将秒和时间倍率分别截到两位小数后计算毫秒。
        long scale = (uint)(current.TimeScale * 100);
        endTime = now + (uint)(MoshentaSkillCatalog.F(current.Skill.Data, "SkillStateTime") * 100) * scale / 10;
        current.End = now + (uint)(MoshentaSkillCatalog.F(current.Skill.Data, "Time") * 100) * scale / 10 + 2;
        events.Add(new MoshentaSkillEvent { Kind = MoshentaSkillEvent.Finish, Cast = current, At = current.End });
        cooldowns[current.Skill.Id] = now + (long)(MoshentaSkillCatalog.F(current.Skill.Effect, "PVECoolTime") * current.TimeScale * 1000);
        Actor.Owner.PublishSkillAction(Actor, MonsterActionPhase.Attack, current.Skill.Id, current.Level, current.TargetId, current.Point, now, endTime);
        for (int index = 0; index < current.Skill.Results.Count; index++)
        {
            // 原LoopResult重复结果；Loop只影响DamageResult.Count。
            XElement point = current.Skill.Results[index];
            int count = 1;
            if (MoshentaSkillCatalog.I(point, "LoopResult") != 0) count = Math.Max(1, MoshentaSkillCatalog.I(point, "LoopResultCount"));
            for (int repeat = 0; repeat < count; repeat++)
                events.Add(new MoshentaSkillEvent { Kind = MoshentaSkillEvent.Result, Cast = current, Data = point, Index = index,
                    At = now + 1 + (long)(MoshentaSkillCatalog.F(point, "At") * current.TimeScale * 1000)
                        + repeat * ((long)(MoshentaSkillCatalog.F(point, "LoopResultCycle") * current.TimeScale * 1000) + 1) });
        }
        AddEvents("AIEventList", MoshentaSkillEvent.Scene);
        AddEvents("MobUnitList", MoshentaSkillEvent.Mob);
        AddEvents("ChargeList", MoshentaSkillEvent.Charge);
    }
    /// <summary>将原动作列表加入本次冻结上下文。</summary><param name="list">XML列表名。</param><param name="kind">强类型事件种类常量。</param>
    private void AddEvents(string list, int kind)
    {
        XElement node = current.Skill.Data.Element(list);
        if (node == null) return;
        List<XElement> items = node.Elements().ToList();
        for (int index = 0; index < items.Count; index++)
            events.Add(new MoshentaSkillEvent { Kind = kind, Cast = current, Data = items[index], At = current.Start + 1 + (long)(MoshentaSkillCatalog.F(items[index], "At") * current.TimeScale * 1000) });
    }
    /// <summary>处理当前时间以内全部实际事件，保持原先后时点。</summary><param name="now">场景逻辑毫秒。</param>
    internal void Update(long now)
    {
        Buffs.Update(now); UpdatePlayers(Actor.Owner, now);
        if (Actor.IsDead) { Cancel(); return; }
        if (singing && now >= endTime) Fire(endTime);
        while (events.Count > 0)
        {
            // 选择最早事件，等时点保持原插入顺序。
            int first = 0;
            for (int index = 1; index < events.Count; index++) if (events[index].At < events[first].At) first = index;
            if (events[first].At > now) break;
            MoshentaSkillEvent next = events[first]; events.RemoveAt(first);
            Execute(next);
            if (Actor.IsDead) return;
        }
        for (int index = bullets.Count - 1; index >= 0; index--) if (!bullets[index].Update(now)) bullets.RemoveAt(index);
        if (chargeEnd > 0)
        {
            // 本闭包6202没有位移曲线，原默认沿时间比例线性推进。
            float progress = Math.Min(1, (float)(now - chargeStart) / (chargeEnd - chargeStart));
            Vector3 position = chargeFrom + chargeOffset * progress;
            // 既有导航仅有PathFind，使用首直线段截断；此适配不声称等同原KickBlockedTest。
            Vector3 allowed = Actor.ClampSkillStep(position);
            if (Horizontal(allowed, position) > 0.001f) chargeEnd = 0;
            position = allowed;
            Actor.Monster.Transform.X = position.X; Actor.Monster.Transform.Y = position.Y; Actor.Monster.Transform.Z = position.Z;
            if (now >= chargeEnd) chargeEnd = 0;
        }
        if (current != null && !singing && now >= endTime)
        {
            current = null;
        }
    }
    /// <summary>取消尚未发生的技能结果；死亡和场景清理同时清空原Buff。</summary>
    internal void Cancel()
    {
        current = null; singing = false; events.Clear(); bullets.Clear(); chargeEnd = 0;
        if (Actor.IsDead) Buffs.Clear();
    }
    /// <summary>实体从实例移除后释放真实Lua状态，不影响其他实体公式。</summary>
    internal void Dispose() { Cancel(); Buffs.Clear(); Calculator.Dispose(); }
    /// <summary>执行原时点事件，表现事件由客户端对应原数据播放。</summary><param name="item">已到期事件。</param>
    private void Execute(MoshentaSkillEvent item)
    {
        if (item.Kind == MoshentaSkillEvent.Finish) Buffs.SkillEnded();
        else if (item.Kind == MoshentaSkillEvent.Scene) Actor.Owner.SceneEvent(MoshentaSkillCatalog.S(item.Data, "Event"), 0, 0, Actor.UnitId);
        else if (item.Kind == MoshentaSkillEvent.Mob)
        {
            Vector3 point = Actor.Position + Rotate(new Vector3(MoshentaSkillCatalog.F(item.Data, "OffsetAtX"), MoshentaSkillCatalog.F(item.Data, "OffsetAtY"), MoshentaSkillCatalog.F(item.Data, "OffsetAtZ")), item.Cast.Facing);
            if (MoshentaSkillCatalog.S(item.Data, "MobPositionType").EndsWith("RANDOM_CIRCLE", StringComparison.Ordinal))
            {
                float radius = MoshentaSkillCatalog.F(item.Data, "MinRadius") + (float)Actor.Owner.Random.NextDouble() * (MoshentaSkillCatalog.F(item.Data, "MaxRadius") - MoshentaSkillCatalog.F(item.Data, "MinRadius"));
                float angle = (float)(Actor.Owner.Random.NextDouble() * Math.PI * 2);
                point += new Vector3((float)Math.Sin(angle) * radius, 0, (float)Math.Cos(angle) * radius);
            }
            Actor.Owner.Summon(Actor, MoshentaSkillCatalog.I(item.Data, "TemplateID"), point, item.Cast.Facing, (int)(MoshentaSkillCatalog.F(item.Data, "LifeTime") * 1000));
        }
        else if (item.Kind == MoshentaSkillEvent.Charge)
        {
            chargeFrom = Actor.Position; chargeStart = item.At;
            chargeEnd = chargeStart + (long)((MoshentaSkillCatalog.F(item.Data, "End") - MoshentaSkillCatalog.F(item.Data, "At")) * 1000);
            float distance = MoshentaSkillCatalog.F(item.Data, "DistanceZ");
            if (MoshentaSkillCatalog.I(item.Data, "AimTarget") != 0) distance += Horizontal(item.Cast.Point, chargeFrom);
            chargeOffset = Rotate(new Vector3(MoshentaSkillCatalog.F(item.Data, "DistanceX"), 0, distance), item.Cast.Facing);
        }
        else if (item.Kind == MoshentaSkillEvent.Delayed) HitRange(item.Cast, item.Index, item.Data.Element("DamageRange"), TargetPosition(item.TargetId), item.TargetId, true);
        else if (MoshentaSkillCatalog.I(item.Data, "LongAttackEffect") != 0) bullets.Add(new MoshentaSkillBullet(this, item));
        else HitRange(item.Cast, item.Index, item.Data, ResultCenter(item.Cast), item.Cast.TargetId, false);
    }
    /// <summary>原目标位置；无目标为施法者当前位置。</summary><param name="targetId">原目标UID。</param>
    internal Vector3 TargetPosition(ulong targetId)
    {
        OnlineRole role = Actor.Owner.FindRole(targetId);
        if (role != null) return role.mainRoleInfo.BaseInfo.Pos.ToVector3();
        MoshentaActor target = Actor.Owner.FindActor(targetId);
        if (target != null) return target.Position;
        return Actor.Position;
    }
    /// <summary>原结果中心按NeedTarget使用目标当前位置，否则为施法点。</summary><param name="cast">冻结施法上下文。</param>
    private Vector3 ResultCenter(MoshentaSkillCast cast)
    {
        if (cast.Skill.NeedTarget) return TargetPosition(cast.TargetId);
        int type = MoshentaSkillCatalog.I(cast.Skill.Table, "SkillTargetRangeType");
        if (type == 1 || type == 3) return Actor.Position;
        return cast.Point;
    }
    /// <summary>根据原范围、目标数、随机选择和延迟范围确定实际命中玩家。</summary><param name="cast">原施法。</param><param name="index">零基结果号。</param><param name="range">原范围节点。</param><param name="center">当前范围中心。</param><param name="directTarget">直接目标UID。</param><param name="delayed">是否已走过DelayDamageTime。</param>
    internal void HitRange(MoshentaSkillCast cast, int index, XElement range, Vector3 center, ulong directTarget, bool delayed)
    {
        XElement result = cast.Skill.Results[index];
        if (MoshentaSkillCatalog.S(result, "TargetType") == "ESkillTargetType_self")
        {
            HitSelf(cast, index); return;
        }
        int count = (int)MoshentaSkillCatalog.Indexed(cast.Skill.Effect, "AttackCount", index);
        if (delayed) count = MoshentaSkillCatalog.I(range, "RangeCount");
        List<OnlineRole> targets = new List<OnlineRole>();
        List<OnlineRole> players = Actor.Owner.Players;
        for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            OnlineRole player = players[playerIndex];
            if (player.mainRoleInfo.BaseInfo.CurrHp <= 0) continue;
            bool direct = cast.Skill.NeedTarget && count <= 1 && !delayed;
            if (direct)
            {
                if (player.GetID() != directTarget) continue;
            }
            else
            {
                // 原延迟范围followTargetFace=false使用世界forward，不用原施法朝向。
                float facing = cast.Facing;
                if (delayed) facing = 0;
                if (!InRange(range, center, facing, player.mainRoleInfo.BaseInfo.Pos.ToVector3())) continue;
            }
            targets.Add(player);
        }
        if (MoshentaSkillCatalog.S(result, "TargetSelectType") == "ESkillTargetSelectType_Random")
        {
            for (int position = targets.Count - 1; position > 0; position--)
            {
                int chosen = Actor.Owner.Random.Next(position + 1);
                OnlineRole swap = targets[position]; targets[position] = targets[chosen]; targets[chosen] = swap;
            }
        }
        // 原非直接目标分支AttackCount=0就是不选任何目标，不代表无限。
        bool oneDirectTarget = cast.Skill.NeedTarget && count <= 1 && !delayed;
        if (!oneDirectTarget && targets.Count > count) targets.RemoveRange(count, targets.Count - count);
        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
        {
            OnlineRole target = targets[targetIndex];
            if (!delayed && MoshentaSkillCatalog.F(result, "DelayDamageTime") > 0)
            {
                events.Add(new MoshentaSkillEvent { Kind = MoshentaSkillEvent.Delayed, Cast = cast, Data = result, Index = index,
                    At = Actor.Owner.Now + (long)(MoshentaSkillCatalog.F(result, "DelayDamageTime") * 1000), Point = target.mainRoleInfo.BaseInfo.Pos.ToVector3(), TargetId = target.GetID() });
            }
            else HitPlayer(cast, index, target, targetIndex + 1, targets.Count);
        }
    }
    /// <summary>在实际结果点运行原Lua并施加该结果对应的原Buff。</summary><param name="cast">冻结攻击属性。</param><param name="index">结果号。</param><param name="target">真实命中角色。</param><param name="targetIndex">本结果目标顺序。</param><param name="count">本结果命中人数。</param>
    internal void HitPlayer(MoshentaSkillCast cast, int index, OnlineRole target, int targetIndex, int count)
    {
        if (PlayerBuffs(Actor.Owner, target).Invulnerable) return;
        MoshentaDamageResult damage = Calculator.Calculate(cast.Skill, index, cast.Snapshot, PlayerSnapshot(Actor.Owner, target), targetIndex, count);
        Actor.Owner.ApplyPlayerDamage(Actor, target, cast.Skill.Id, damage.Damage, damage.Critical, index);
        if (damage.Type == 1 || damage.Type == 2) AddResultBuff(cast, index, target);
    }
    /// <summary>自身结果也走原伤害/效果语义。</summary><param name="cast">本次施法。</param><param name="index">结果号。</param>
    private void HitSelf(MoshentaSkillCast cast, int index)
    {
        if (!Buffs.Invulnerable)
        {
            MoshentaDamageResult damage = Calculator.Calculate(cast.Skill, index, cast.Snapshot, MoshentaDamageSnapshot.Capture(Actor), 1, 1);
            if (damage.Damage > 0) Actor.Owner.ApplyMonsterDamage(Actor, Actor, cast.Skill.Id, damage.Damage, damage.Critical);
        }
        AddResultBuff(cast, index, null);
    }
    /// <summary>CreateBuff按ResultList索引取一项，不将全数组重复加到每次命中。</summary><param name="cast">原技能。</param><param name="index">零基结果号。</param><param name="target">命中玩家，空为自己。</param>
    private void AddResultBuff(MoshentaSkillCast cast, int index, OnlineRole target)
    {
        string[] entries = MoshentaSkillCatalog.S(cast.Skill.Effect, "CreateBuff").Split('|');
        if (index >= entries.Length || entries[index].Length == 0) return;
        string[] fields = entries[index].Split('=');
        int id = int.Parse(fields[0], CultureInfo.InvariantCulture);
        if (id == 0) return;
        if (fields.Length > 1 && Actor.Owner.Random.Next(0, 10000) >= float.Parse(fields[1], CultureInfo.InvariantCulture)) return;
        if (target == null) AddBuff(id); else AddPlayerBuff(Actor, target, id);
    }
    /// <summary>原水平范围测试：圆环扇形及延迟十字矩形。</summary><param name="range">原范围字段。</param><param name="center">原中心。</param><param name="facing">原方向度数。</param><param name="position">真实角色位置。</param>
    internal static bool InRange(XElement range, Vector3 center, float facing, Vector3 position)
    {
        // 先处理随原方向旋转的本地偏移。
        center += Rotate(new Vector3(MoshentaSkillCatalog.F(range, "OffsetX"), 0, MoshentaSkillCatalog.F(range, "OffsetZ")), facing + MoshentaSkillCatalog.F(range, "AngleShift"));
        // 原DAMAGE_DISTANCE_HEIGHT=6米，跨楼层目标不能被水平范围击中。
        if (Math.Abs(position.Y - center.Y) > 6) return false;
        Vector3 local = Rotate(position - center, -facing - MoshentaSkillCatalog.F(range, "AngleShift"));
        float far = MoshentaSkillCatalog.F(range, "RangeFar");
        float scope = MoshentaSkillCatalog.F(range, "Scope");
        if (MoshentaSkillCatalog.S(range, "RangeType") == "ESkillRangeType_CrossRect")
        {
            bool vertical = Math.Abs(local.X) <= scope / 2 && Math.Abs(local.Z) <= far / 2;
            bool horizontal = Math.Abs(local.Z) <= scope / 2 && Math.Abs(local.X) <= far / 2;
            return vertical || horizontal;
        }
        float distance = (float)Math.Sqrt(local.X * local.X + local.Z * local.Z);
        if (distance >= far || distance < MoshentaSkillCatalog.F(range, "RangeLow")) return false;
        if (scope >= 360 || distance == 0) return true;
        return Math.Abs(Math.Atan2(local.X, local.Z) * 180 / Math.PI) <= scope / 2;
    }
    /// <summary>将本地偏移按原水平朝向旋转。</summary><param name="value">本地米坐标。</param><param name="degrees">角度。</param>
    internal static Vector3 Rotate(Vector3 value, float degrees)
    {
        double angle = degrees * Math.PI / 180;
        return new Vector3(value.X * (float)Math.Cos(angle) + value.Z * (float)Math.Sin(angle), value.Y, value.Z * (float)Math.Cos(angle) - value.X * (float)Math.Sin(angle));
    }
    /// <summary>水平米距离，技能射程忽略地形高度。</summary><param name="left">起点。</param><param name="right">终点。</param>
    internal static float Horizontal(Vector3 left, Vector3 right) { Vector3 delta = left - right; return (float)Math.Sqrt(delta.X * delta.X + delta.Z * delta.Z); }
    /// <summary>原怪物Buff查询。</summary><param name="family">原家族。</param>
    internal bool HasBuff(int family) { return Buffs.Has(family); }
    /// <summary>原怪物自加Buff。</summary><param name="fullBuffId">原行号。</param>
    internal void AddBuff(int fullBuffId) { Buffs.Add(Actor, fullBuffId); }
    /// <summary>原SceneAI向怪物施加Buff时保留场景来源。</summary><param name="fullBuffId">原具体等级行号。</param>
    internal void AddSceneBuff(int fullBuffId) { Buffs.Add(null, fullBuffId); }
    /// <summary>原群体施加保留实际施法者，目标容器不冒充来源。</summary><param name="caster">原怪物施法者。</param><param name="fullBuffId">原具体等级行号。</param>
    internal void AddBuffFrom(MoshentaActor caster, int fullBuffId) { Buffs.Add(caster, fullBuffId); }
    /// <summary>按原家族移除怪物Buff。</summary><param name="family">原家族。</param>
    internal void RemoveBuff(int family) { Buffs.Remove(family); }
    /// <summary>获取此场景实际角色的原Buff容器。</summary><param name="scene">实例。</param><param name="role">真实角色。</param>
    private static MoshentaBuffRuntime PlayerBuffs(SceneWaveSpawner scene, OnlineRole role)
    {
        Dictionary<ulong, MoshentaBuffRuntime> container;
        if (!playerBuffs.TryGetValue(scene, out container)) { container = new Dictionary<ulong, MoshentaBuffRuntime>(); playerBuffs.Add(scene, container); }
        MoshentaBuffRuntime result;
        if (!container.TryGetValue(role.GetID(), out result)) { result = new MoshentaBuffRuntime(scene, null, role); container.Add(role.GetID(), result); }
        return result;
    }
    /// <summary>读取命中时玩家面板和原Buff属性。</summary><param name="scene">实例。</param><param name="role">真实角色。</param>
    internal static MoshentaDamageSnapshot PlayerSnapshot(SceneWaveSpawner scene, OnlineRole role)
    {
        int attackers = 0;
        List<MoshentaActor> actors = scene.Actors;
        for (int index = 0; index < actors.Count; index++) if (actors[index].Target == role && !actors[index].IsDead) attackers++;
        MoshentaDamageSnapshot result = MoshentaDamageSnapshot.Capture(role, attackers);
        PlayerBuffs(scene, role).Snapshot(result); return result;
    }
    /// <summary>供AI查询同实例玩家家族。</summary><param name="scene">实例。</param><param name="role">玩家。</param><param name="family">家族。</param>
    internal static bool HasPlayerBuff(SceneWaveSpawner scene, OnlineRole role, int family) { return PlayerBuffs(scene, role).Has(family); }
    /// <summary>供AI及实际技能结果添加原Buff。</summary><param name="caster">来源。</param><param name="role">玩家。</param><param name="fullBuffId">原行号。</param>
    internal static void AddPlayerBuff(MoshentaActor caster, OnlineRole role, int fullBuffId) { PlayerBuffs(caster.Owner, role).Add(caster, fullBuffId); }
    /// <summary>场景树没有怪物施加者，按原场景来源施加固定Buff。</summary><param name="scene">真实副本。</param><param name="role">同实例玩家。</param><param name="fullBuffId">原行号。</param>
    internal static void AddScenePlayerBuff(SceneWaveSpawner scene, OnlineRole role, int fullBuffId) { PlayerBuffs(scene, role).Add(null, fullBuffId); }
    /// <summary>供原场景脚本删除玩家符号等家族。</summary><param name="scene">实例。</param><param name="role">玩家。</param><param name="family">家族。</param>
    internal static void RemovePlayerBuff(SceneWaveSpawner scene, OnlineRole role, int family) { PlayerBuffs(scene, role).Remove(family); }
    /// <summary>推进玩家Buff；原实例自身截止时间保证每帧多宿主调用不会重复光环。</summary><param name="scene">实例。</param><param name="now">逻辑毫秒。</param>
    private static void UpdatePlayers(SceneWaveSpawner scene, long now)
    {
        Dictionary<ulong, MoshentaBuffRuntime> container;
        if (!playerBuffs.TryGetValue(scene, out container)) return;
        List<MoshentaBuffRuntime> values = new List<MoshentaBuffRuntime>(container.Values);
        for (int index = 0; index < values.Count; index++)
        {
            if (scene.FindRole(values[index].Role.GetID()) == null) { values[index].Clear(); container.Remove(values[index].Role.GetID()); }
            else values[index].Update(now);
        }
    }
    /// <summary>场景关闭时精确清除玩家原Buff，保留其他来源Gene。</summary><param name="scene">即将释放的实例。</param>
    internal static void ClearScene(SceneWaveSpawner scene)
    {
        Dictionary<ulong, MoshentaBuffRuntime> container;
        if (!playerBuffs.TryGetValue(scene, out container)) return;
        List<MoshentaBuffRuntime> values = new List<MoshentaBuffRuntime>(container.Values);
        for (int index = 0; index < values.Count; index++) values[index].Clear();
        playerBuffs.Remove(scene);
    }
    /// <summary>入场沿真实存活Buff重建持续视觉，避免仅补发技能一次性FX。</summary><param name="scene">实际实例。</param><param name="entry">本次正式入场快照。</param>
    internal static void FillSceneBuffs(SceneWaveSpawner scene, TeamEnterDungeonNtf entry)
    {
        List<MoshentaActor> actors = scene.Actors;
        for (int index = 0; index < actors.Count; index++) entry.Buffs.AddRange(actors[index].Skills.Buffs.ActiveData());
        Dictionary<ulong, MoshentaBuffRuntime> container;
        if (!playerBuffs.TryGetValue(scene, out container)) return;
        List<MoshentaBuffRuntime> values = new List<MoshentaBuffRuntime>(container.Values);
        for (int index = 0; index < values.Count; index++) entry.Buffs.AddRange(values[index].ActiveData());
    }
}
