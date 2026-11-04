using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using ServerMonster = GameServer.SceneEntity.MonsterEntity;

/// <summary>4060 第18波的邪骸兽人，实例独立保存行为树、仇恨、路线和进行中的普攻。</summary>
internal sealed class MoshentaOrc
{
    /// <summary>所有对本怪产生实际非致死伤害的玩家，等仇恨按最早入表顺序。</summary>
    private readonly List<Threat> hatred = new List<Threat>();
    /// <summary>场景实际出生的同一怪物对象。</summary>
    private readonly ServerMonster monster;
    /// <summary>跨端出生标识，保留波次及波内序号。</summary>
    private readonly ulong spawnId;
    /// <summary>所属副本实例号，只接受本实例内仍在线的玩家。</summary>
    private ulong instance;
    /// <summary>原场景巡逻线的有序路径点及停留信息。</summary>
    private readonly XElement[] patrolPoints;
    /// <summary>使用原始 StandBy XML 的每怪黑板。</summary>
    private readonly OrcStandByTree tree;
    /// <summary>控制实体 Transform 的寻路与行走。</summary>
    private readonly OrcNavigation navigation;
    /// <summary>下一次巡逻要到达的原配置点序号。</summary>
    private int patrolIndex;
    /// <summary>乒乓巡逻的推进方向，正向1、反向-1。</summary>
    private int direction = 1;
    /// <summary>当前移动是否由巡逻节点下达，用于到点后的停留与换点。</summary>
    private bool patrolLeg;
    /// <summary>到点停留结束的逻辑毫秒。</summary>
    private long waitUntil;
    /// <summary>从实例创建起累加的毫秒。</summary>
    private long now;
    /// <summary>下次决策时刻。实体表 AIStartTime 为1秒。</summary>
    private long nextThink = MoshentaOrcDefinition.FirstThink;
    /// <summary>本次攻击开始时刻，用于恢复当前动作进度。</summary>
    private long attackStart;
    /// <summary>本刀动作结束时刻，由技能动作时间决定，独立于冷却。</summary>
    private long attackEnd;
    /// <summary>普通攻击的下次允许起手时刻。</summary>
    private long cooldownEnd;
    /// <summary>本刀还未结算的技能结果时刻，已结算或取消后设为最大值。</summary>
    private long nextResult = long.MaxValue;
    /// <summary>当前已经开始的这一刀锁定的玩家，后续换仇恨不改变本刀归属。</summary>
    private OnlineRole attackTarget;
    /// <summary>对外同步的播放状态。</summary>
    private MonsterActionPhase phase;
    /// <summary>最近一次按仇恨选择的有效对象。</summary>
    internal OnlineRole Target { get; private set; }
    /// <summary>服务端怪物 UID，供原树直接目标为空时的身份回退使用。</summary>
    internal ulong UnitId { get { return monster.UID; } }
    /// <summary>供位置读取节点选择宿主时使用的权威世界坐标。</summary>
    internal Vector3 Position { get { return navigation.Position; } }
    /// <summary>提供给行为树状态比较节点的 RO 状态名。</summary>
    internal string CurrentState
    {
        get
        {
            if (Target != null) return "Combat";
            return "Idle";
        }
    }

    /// <summary>单只怪对单个玩家保留的伤害仇恨，不跨实例保存。</summary>
    private sealed class Threat
    {
        /// <summary>施害者在线实体，离场或死亡即移除。</summary>
        public OnlineRole Role;
        /// <summary>该玩家对兽人实际造成的累计扣血量。</summary>
        public long Amount;
    }

    /// <summary>给实例内指定出生位初始化独立的行为和导航。</summary>
    /// <param name="owner">权威怪物实体。</param>
    /// <param name="id">该出生位的跨端编号。</param>
    /// <param name="points">场景 XML 选中巡逻线的点。</param>
    internal MoshentaOrc(ServerMonster owner, ulong id, XElement[] points)
    {
        monster = owner;
        spawnId = id;
        patrolPoints = points;
        navigation = new OrcNavigation(owner);
        tree = new OrcStandByTree(this);
    }

    /// <summary>将实际伤害记给玩家，怪物死亡时立即取消路径和本刀未到的结果。</summary>
    /// <param name="caster">由队伍实例校验过的实际攻击者。</param>
    /// <param name="damage">该次实际扣掉的生命值。</param>
    /// <param name="notice">当前实例同批属性与动作通知。</param>
    internal void OnHit(OnlineRole caster, int damage, MonsterAttrUpdateNtf notice)
    {
        instance = notice.InstanceId;
        if (monster.CurrentHp == 0)
        {
            navigation.Stop();
            nextResult = long.MaxValue;
            attackTarget = null;
            Target = null;
            hatred.Clear();
            phase = MonsterActionPhase.Dead;
            notice.MonsterActions.Add(Snapshot());
            return;
        }
        if (damage <= 0 || !Valid(caster)) return;
        // 已登记玩家复用原条目，保留同仇恨时的顺序。
        Threat entry = hatred.Find(item => item.Role == caster);
        if (entry == null)
        {
            entry = new Threat { Role = caster };
            hatred.Add(entry);
        }
        entry.Amount += damage;
        nextThink = now;
    }

    /// <summary>玩家在线、存活且仍与兽人同实例时才可成为该怪目标。</summary>
    /// <param name="role">待检查的在线角色。</param>
    private bool Valid(OnlineRole role)
    {
        if (role == null || role.State != OnlineRoleState.Online) return false;
        // 在线角色当前的权威地图、实例及血量。
        RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
        return info.MapId == TeamManager.MoshentaMapId && (ulong)info.InstanceId == instance && info.CurrHp > 0;
    }

    /// <summary>清除无效玩家并选择最高仇恨，覆盖旧目标。</summary>
    private void SelectTarget()
    {
        Target = null;
        // 有效条目最高伤害值，等值时保持更早加入的玩家。
        long best = -1;
        // 无效项原地移除后仍处理相同下标上的下一项。
        for (int index = 0; index < hatred.Count;)
        {
            // 此玩家可能在两次心跳间死亡或退出副本。
            Threat entry = hatred[index];
            if (!Valid(entry.Role))
            {
                hatred.RemoveAt(index);
                continue;
            }
            if (entry.Amount > best)
            {
                best = entry.Amount;
                Target = entry.Role;
            }
            index++;
        }
    }

    /// <summary>推进路线和技能事件，可跨越多个转角和心跳而不丢失定时点。</summary>
    /// <param name="elapsed">逻辑帧耗时，毫秒。</param>
    /// <param name="notice">该实例本帧有变化时发出的数据。</param>
    internal void Update(int elapsed, MonsterAttrUpdateNtf notice)
    {
        if (phase == MonsterActionPhase.Dead) return;
        instance = notice.InstanceId;
        if (phase == MonsterActionPhase.Attack && !Valid(attackTarget))
        {
            nextResult = long.MaxValue;
            attackTarget = null;
            Target = null;
            Stop(notice);
            nextThink = now;
        }
        else if (Target != null && !Valid(Target)) nextThink = now;
        // 截止点前按实际先后处理转角、结果和决策。
        long until = now + elapsed;
        while (now < until)
        {
            // 最早需要处理的定时点，未到定时点时只插值 Transform。
            long next = Math.Min(until, Math.Min(nextThink, nextResult));
            if (navigation.Moving) next = Math.Min(next, navigation.End);
            if (phase == MonsterActionPhase.Attack) next = Math.Min(next, attackEnd);
            now = next;
            if (navigation.Moving)
            {
                navigation.Advance(now);
                if (now == navigation.End) OnCorner(notice);
            }
            if (now == nextResult) ApplyResult(notice);
            if (phase == MonsterActionPhase.Attack && now == attackEnd)
            {
                phase = MonsterActionPhase.Idle;
                attackTarget = null;
                // 普攻定时器到期即重试当前目标，不等下一次0.3秒战斗心跳。
                nextThink = now;
                notice.MonsterActions.Add(Snapshot());
            }
            if (now == nextThink)
            {
                SelectTarget();
                if (phase == MonsterActionPhase.Move && patrolLeg != (Target == null)) Stop(notice);
                tree.Tick(notice);
                nextThink = now + MoshentaOrcDefinition.IdleThink;
                if (Target != null) nextThink = now + MoshentaOrcDefinition.CombatThink;
                else if (waitUntil > now) nextThink = Math.Min(nextThink, waitUntil);
            }
        }
    }

    /// <summary>到一个导航转角时续走，整段巡逻结束才读取停留并调转巡逻方向。</summary>
    /// <param name="notice">待广播的下一段或停步。</param>
    private void OnCorner(MonsterAttrUpdateNtf notice)
    {
        if (navigation.Next(now))
        {
            notice.MonsterActions.Add(Snapshot());
            return;
        }
        if (patrolLeg)
        {
            ArrivedAtPatrolPoint();
            nextThink = Math.Min(nextThink, waitUntil);
        }
        Stop(notice);
    }

    /// <summary>停在当前真实坐标，结束旧导航。</summary>
    /// <param name="notice">待广播的待机动作。</param>
    private void Stop(MonsterAttrUpdateNtf notice)
    {
        navigation.Stop();
        phase = MonsterActionPhase.Idle;
        notice.MonsterActions.Add(Snapshot());
    }

    /// <summary>按原巡逻线的停留秒数和 PingPong 方向选择下一点。</summary>
    private void ArrivedAtPatrolPoint()
    {
        // 当前到达点的原场景元数据。
        XElement point = patrolPoints[patrolIndex];
        waitUntil = now + (int)(1000 * ((float)point.Element("stayTime") + (float)point.Element("rotateStayTime")));
        if (patrolIndex == patrolPoints.Length - 1) direction = -1;
        if (patrolIndex == 0) direction = 1;
        patrolIndex += direction;
    }

    /// <summary>在两端之间从当前点循序巡逻，等待阶段保持原地。</summary>
    /// <param name="notice">待广播的下一段行走。</param>
    internal bool Patrol(MonsterAttrUpdateNtf notice)
    {
        if (phase != MonsterActionPhase.Idle || now < waitUntil) return false;
        // 由该波绑定的巡逻线读取当前点，单位米。
        XElement p = patrolPoints[patrolIndex].Element("position");
        // 当前待抵达点的实际三维场景位置。
        Vector3 goal = new Vector3((float)p.Element("x"), (float)p.Element("y"), (float)p.Element("z"));
        patrolLeg = true;
        // RO Patrol 在水平距离小于 NEAR_BY_PATROL(1.5米)时执行本点停留。出生即贴近首点也会停留。
        Vector3 offset = goal - navigation.Position;
        if (offset.X * offset.X + offset.Z * offset.Z < 2.25f)
        {
            ArrivedAtPatrolPoint();
            return true;
        }
        if (!navigation.Begin(goal, now)) return false;
        phase = MonsterActionPhase.Move;
        notice.MonsterActions.Add(Snapshot());
        return true;
    }

    /// <summary>计算权威 XZ 平面上的水平距离，Y 高度另由导航面处理。</summary>
    /// <param name="role">仇恨目标。</param>
    internal float DistanceTo(OnlineRole role)
    {
        // 使用服务器世界坐标 X、Z 的差，单位米。
        Vector3 delta = role.mainRoleInfo.BaseInfo.Pos.ToVector3() - navigation.Position;
        return (float)Math.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
    }

    /// <summary>计算原子树的近战站位，目标不可达时向选择器返回失败。</summary>
    /// <param name="role">行为树目标。</param>
    /// <param name="range">实际射程减去靠近容差，单位米。</param>
    /// <param name="destination">有效时返回水平偏移后的目标站位。</param>
    internal bool FightPosition(OnlineRole role, float range, out Vector3 destination)
    {
        // 玩家和兽人的实时位置决定目标外侧的近战站位。
        Vector3 player = role.mainRoleInfo.BaseInfo.Pos.ToVector3();
        // 从目标指向兽人的水平方向，同时用于近距离判定。
        Vector3 delta = navigation.Position - player;
        delta.Y = 0;
        destination = navigation.Position;
        if (delta.LengthSquared() <= range * range) return true;
        destination = player + Vector3.Normalize(delta) * range;
        return navigation.Reachable(destination);
    }

    /// <summary>开始或继续向最终站位寻路；施法中保持当前动作。</summary>
    /// <param name="dest">子树算出的最终目标。</param>
    /// <param name="notice">待下发的新段。</param>
    internal bool Chase(Vector3 dest, MonsterAttrUpdateNtf notice)
    {
        if (phase == MonsterActionPhase.Attack) return false;
        if (!patrolLeg && navigation.GoingTo(dest)) return true;
        patrolLeg = false;
        if (!navigation.Begin(dest, now))
        {
            if (phase == MonsterActionPhase.Move) Stop(notice);
            return false;
        }
        phase = MonsterActionPhase.Move;
        notice.MonsterActions.Add(Snapshot());
        return true;
    }

    /// <summary>对 5515 实际持有的组0普攻检查目标、射程和当前冷却。</summary>
    /// <param name="group">XML 筛选的技能组。</param>
    /// <param name="roleId">黑板保存的目标角色编号。</param>
    internal int FilterSkill(int group, ulong roleId)
    {
        if (group != MoshentaOrcDefinition.SkillGroup || Target == null || roleId != Target.GetID()) return 0;
        if (phase == MonsterActionPhase.Attack || now < cooldownEnd || DistanceTo(Target) > MoshentaOrcDefinition.Range) return 0;
        return MoshentaOrcDefinition.SkillId;
    }

    /// <summary>发出真实起手事件，到技能结果时刻才扣玩家生命。</summary>
    /// <param name="skill">组0筛选通过的技能。</param>
    /// <param name="roleId">本刀锁定的角色编号。</param>
    /// <param name="notice">需广播的攻击动作。</param>
    internal bool Attack(int skill, ulong roleId, MonsterAttrUpdateNtf notice)
    {
        if (skill != MoshentaOrcDefinition.SkillId || FilterSkill(MoshentaOrcDefinition.SkillGroup, roleId) == 0) return false;
        navigation.Stop();
        attackTarget = Target;
        navigation.Face(attackTarget.mainRoleInfo.BaseInfo.Pos.ToVector3());
        phase = MonsterActionPhase.Attack;
        attackStart = now;
        attackEnd = now + MoshentaOrcDefinition.ActionDuration;
        cooldownEnd = now + MoshentaOrcDefinition.Cooldown;
        nextResult = now + MoshentaOrcDefinition.ResultOffset;
        notice.MonsterActions.Add(Snapshot());
        return true;
    }

    /// <summary>技能结果点按施法时选中的有效玩家结算一次，不把新的仇恨目标替换进本刀。</summary>
    /// <param name="notice">同实例玩家收到的权威生命值变化。</param>
    private void ApplyResult(MonsterAttrUpdateNtf notice)
    {
        nextResult = long.MaxValue;
        if (!Valid(attackTarget)) return;
        // 实际受击玩家的服务端属性和剩余生命。
        RoleBaseInfo info = attackTarget.mainRoleInfo.BaseInfo;
        // 本刀实际可扣生命，命中、暴击、防御等取原伤害脚本。
        int damage = Math.Min(info.CurrHp, OrcBasicAttackDamage.Calculate(attackTarget));
        info.CurrHp -= damage;
        notice.PlayerHits.Add(new MonsterPlayerHitData { SpawnId = spawnId, TargetRoleId = info.RoleId,
            Hp = info.CurrHp, MaxHp = info.MaxHp, Damage = damage });
        if (info.CurrHp > 0) return;
        // 沿用在线角色死亡时停止正在施放的技能和吟唱的既有入口。
        RoleSkillComponent activeSkill;
        if (attackTarget.TryGetComponent(out activeSkill)) activeSkill.InterruptActiveCast(true);
        // 吟唱动作归还后，角色的零血判定会禁止后续起手。
        StateMachineComponent state;
        if (attackTarget.TryGetComponent(out state)) state.ForceToDefaultState(true);
    }

    /// <summary>给新入场成员或同实例动作通知生成同一时刻的可续播快照。</summary>
    internal MonsterActionData Snapshot()
    {
        // 默认站在当前点，移动和攻击再填各自的剩余时间。
        Vector3 p = navigation.Position;
        // 含当前动作、位置、朝向和可选攻击/路径进度的入场与实时共用消息。
        MonsterActionData data = new MonsterActionData { SpawnId = spawnId, Phase = phase, X = p.X, Y = p.Y, Z = p.Z,
            ToX = p.X, ToY = p.Y, ToZ = p.Z, Face = monster.Transform.Yaw };
        if (Target != null) data.TargetRoleId = Target.mainRoleInfo.BaseInfo.RoleId;
        if (phase == MonsterActionPhase.Move)
        {
            data.ToX = navigation.To.X;
            data.ToY = navigation.To.Y;
            data.ToZ = navigation.To.Z;
            data.DurationMs = (int)(navigation.End - now);
        }
        if (phase == MonsterActionPhase.Attack)
        {
            data.SkillId = MoshentaOrcDefinition.SkillId;
            data.TargetRoleId = attackTarget.mainRoleInfo.BaseInfo.RoleId;
            data.ElapsedMs = (int)(now - attackStart);
            data.DurationMs = (int)(attackEnd - now);
        }
        return data;
    }
}
