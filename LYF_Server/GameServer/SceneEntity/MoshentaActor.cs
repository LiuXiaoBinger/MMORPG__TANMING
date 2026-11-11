using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using ServerMonster = GameServer.SceneEntity.MonsterEntity;

/// <summary>魔神塔一只真实实体的AI宿主；动作、技能与仇恨各自保留跨帧状态。</summary>
internal sealed class MoshentaActor
{
    /// <summary>所属独立副本，所有查询和结果都限定到此实例。</summary>
    internal SceneWaveSpawner Owner { get; private set; }
    /// <summary>持有权威HP、Transform和Gene的原实体。</summary>
    internal ServerMonster Monster { get; private set; }
    /// <summary>按原技能表执行结果点和Buff的组件。</summary>
    internal MoshentaSkillRuntime Skills { get; private set; }
    /// <summary>原MobAttrType_FirerSnapShot召唤物保留施加者释放时的属性；配置属性召唤为空。</summary>
    internal MoshentaDamageSnapshot SummonSnapshot { get; set; }
    /// <summary>原召唤所属者身份，供双向仇恨联动；固定波次为0。</summary>
    internal ulong SummonerId { get; set; }
    /// <summary>原EntityTable编号。</summary>
    internal int EntityId { get; private set; }
    /// <summary>固定波次或动态召唤的客户端身份。</summary>
    internal ulong SpawnId { get; private set; }
    /// <summary>场景实体UID，区别于跨端SpawnId。</summary>
    internal ulong UnitId { get { return Monster.UID; } }
    /// <summary>固定波次号，动态召唤为-1。</summary>
    internal int WaveId { get; private set; }
    /// <summary>原实体等级，技能公式从运行表读取。</summary>
    internal int Level { get; private set; }
    /// <summary>原IsDummy实体可执行机制，但不是玩家技能伤害目标。</summary>
    private readonly bool dummy;
    /// <summary>原DeadComponent实际尸体保留毫秒，Dummy不读取表值而为0。</summary>
    private readonly int corpseDuration;
    /// <summary>首次进入真实死亡状态的实例毫秒。</summary>
    private long deathStarted;
    /// <summary>原死亡定时器在保留时间后加1毫秒触发移除。</summary>
    private long corpseRemoveAt = long.MaxValue;
    /// <summary>原运行表DEF/MDEF，复用现有玩家普攻防御结算。</summary>
    internal MonsterDefenseAttributes Defense { get; private set; }
    /// <summary>原EntityTable.UnitTypeLevel=2 的真实首领类别；破竹、罗汉等按此判定而非创建工厂类型。</summary>
    internal bool IsBoss { get; private set; }
    /// <summary>原Dummy和不可受击Buff共同决定是否能被伤害。</summary>
    internal bool CanBeAttacked { get { return !dummy && !IsDead && !Skills.Buffs.Invulnerable; } }
    /// <summary>与所属实例一致的当前逻辑毫秒。</summary>
    internal long Now { get; private set; }
    /// <summary>当前权威世界坐标。</summary>
    internal Vector3 Position { get { return new Vector3(Monster.Transform.X, Monster.Transform.Y, Monster.Transform.Z); } }
    /// <summary>当前水平朝向角度。</summary>
    internal float Facing { get { return Monster.Transform.Yaw; } }
    /// <summary>权威当前生命值；零上限机制不能据此判死。</summary>
    internal int CurrentHp { get { return Monster.CurrentHp; } }
    /// <summary>真实战斗生命上限。</summary>
    internal int MaxHp { get { return Monster.MaxHp; } }
    /// <summary>当前最高仇恨的同实例存活玩家。</summary>
    internal OnlineRole Target { get; private set; }
    /// <summary>原ActivePassive控制的主动/被动仇恨门控。</summary>
    internal bool Passive { get; set; }
    /// <summary>显式死亡、移除或有HP实体归零才停止AI；Dummy零HP不代表死亡。</summary>
    internal bool IsDead { get { return removed || dead || (MaxHp > 0 && CurrentHp == 0); } }
    /// <summary>动态召唤到期移除的逻辑毫秒，无时限为最大值。</summary>
    internal long ExpireTime { get; set; } = long.MaxValue;
    /// <summary>出生位置供原随机移动和回拉节点使用。</summary>
    internal Vector3 BornPosition { get; private set; }
    /// <summary>实际选择的原XML树名，空名仍保留机制实体。</summary>
    internal string TreeName { get; private set; }
    /// <summary>原实体技能最小射程，写入各树的公共SharedFloat。</summary>
    internal float AttackRange { get; private set; }
    /// <summary>原行为状态；施法期间仍沿原状态优先级阻止重复请求。</summary>
    internal string CurrentState { get { if (Target != null || (includeDeadHatred && deadThreats.Count > 0)) return "Combat"; return "Idle"; } }
    /// <summary>保留插入顺序的仇恨角色，保证同仇恨优先旧目标。</summary>
    private readonly List<OnlineRole> threats = new List<OnlineRole>();
    /// <summary>每位仇恨角色的累计实际伤害值。</summary>
    private readonly Dictionary<ulong, long> threatValues = new Dictionary<ulong, long>();
    /// <summary>原路径寻路和分段行走，使用本实体米速。</summary>
    private readonly OrcNavigation navigation;
    /// <summary>本波配置的路径点，未配置则为空数组。</summary>
    private readonly XElement[] patrol;
    /// <summary>本波的ScenePatrolType名称。</summary>
    private readonly string patrolType;
    /// <summary>下一个原路径点。</summary>
    private int patrolIndex;
    /// <summary>PingPong沿原路径推进的正负方向。</summary>
    private int patrolDirection = 1;
    /// <summary>当前移动段是否属于巡逻。</summary>
    private bool patrolLeg;
    /// <summary>巡逻到点停留截止毫秒。</summary>
    private long waitUntil;
    /// <summary>下一次原AI心跳时间。</summary>
    private long nextThink;
    /// <summary>原切轴事件要求重跑AI，当前树结束时不能用普通心跳覆盖。</summary>
    private bool thinkRequested;
    /// <summary>下一原动作结果、转角、心跳或寿命截止，供实例精确推进。</summary>
    internal long NextEventTime
    {
        get
        {
            if (removed) return long.MaxValue;
            if (dead) return corpseRemoveAt;
            long next = Math.Min(nextThink, Math.Min(ExpireTime, Skills.NextEventTime));
            if (navigation.Moving) next = Math.Min(next, navigation.End);
            return next;
        }
    }
    /// <summary>当前真实Gene状态是否允许移动；霸体等抵抗沿用Gene既有计算。</summary>
    private bool CanMove { get { return Monster.Components.Get<GeneComponent>(MMoRpgCommon.SceneEntity.EntityComponentType.Gene).Runtime.CanMove; } }
    /// <summary>空闲心跳毫秒。</summary>
    private readonly int idleInterval;
    /// <summary>战斗心跳毫秒。</summary>
    private readonly int combatInterval;
    /// <summary>原主动索敌半径，单位米。</summary>
    private readonly float sight;
    /// <summary>原InitSharedData按FightTargetOffsetDegree一次抽取的追近偏角，单位度。</summary>
    private readonly float fightRotation;
    /// <summary>EntityTable配置是否主动建立仇恨。</summary>
    private readonly bool initiative;
    /// <summary>原首次仇恨是否拉入视距内所有敌人。</summary>
    private readonly bool buildSightHatred;
    /// <summary>原HatredDeleteType=1死亡列表仍保留战斗关联。</summary>
    private readonly bool includeDeadHatred;
    /// <summary>原死亡仇恨暂存，用于同实例复活时以1点重新建立。</summary>
    private readonly List<OnlineRole> deadThreats = new List<OnlineRole>();
    /// <summary>实际节点运行器，黑板只属于本实体。</summary>
    internal MoshentaBehaviorTree Tree { get; private set; }
    /// <summary>当前副本登记为本实体选择的原时间轴；未登记的普通怪物为空。</summary>
    internal MoshentaTimeline Timeline { get; private set; }
    /// <summary>显式死亡标记，区别于无生命机制。</summary>
    private bool dead;
    /// <summary>实体已经从场景移除，后续不再发送动作。</summary>
    private bool removed;
    /// <summary>当前客户端动作快照，技能组件只修改该动作描述。</summary>
    private MonsterActionData action;

    /// <summary>绑定已创建的真实实体并按原AI索引夹取选树。</summary>
    /// <param name="owner">所属副本。</param><param name="monster">权威实体。</param><param name="entityId">原EntityId。</param><param name="spawnId">跨端出生号。</param><param name="waveId">原波号，召唤为-1。</param><param name="aiIndex">原零基树索引。</param><param name="points">原巡逻点。</param><param name="type">原巡逻方式。</param>
    internal MoshentaActor(SceneWaveSpawner owner, ServerMonster monster, int entityId, ulong spawnId, int waveId, int aiIndex, XElement[] points, string type)
    {
        Owner = owner; Monster = monster; EntityId = entityId; SpawnId = spawnId; WaveId = waveId;
        Now = owner.Now; BornPosition = Position; patrol = points; patrolType = type;
        // 实际运行EntityTable提供AI与属性，不再沿用兽人常量。
        XElement row = Owner.Battle.Catalog.Entity(entityId);
        Level = MoshentaSkillCatalog.I(row, "UnitLevel");
        dummy = MoshentaSkillCatalog.I(row, "IsDummy") != 0;
        if (!dummy) corpseDuration = (int)(MoshentaSkillCatalog.F(row, "BodyDisappearTime") * 1000);
        Defense = new MonsterDefenseAttributes { Id = entityId, DefExternal = MoshentaSkillCatalog.I(row, "DEF"), DefInternal = MoshentaSkillCatalog.I(row, "MDEF") };
        // RO kUnitTypeLevel_BOSS=2；正式5501/5502/5503为2，5515与5456为0，创建工厂仍统一给Monster类型。
        IsBoss = MoshentaSkillCatalog.I(row, "UnitTypeLevel") == 2;
        initiative = MoshentaSkillCatalog.I(row, "IsInitiative") != 0;
        Passive = !initiative;
        buildSightHatred = MoshentaSkillCatalog.I(row, "HatredBuildType") == 1;
        includeDeadHatred = MoshentaSkillCatalog.I(row, "HatredDeleteType") == 1;
        sight = MoshentaSkillCatalog.F(row, "Sight");
        idleInterval = Math.Max(200, (int)(MoshentaSkillCatalog.F(row, "AIHeartInterval") * 1000));
        combatInterval = Math.Max(200, (int)(MoshentaSkillCatalog.F(row, "AIHeartIntervalCombat") * 1000));
        nextThink = Now + (int)(MoshentaSkillCatalog.F(row, "AIStartTime") * 1000);
        navigation = new OrcNavigation(monster, MoshentaSkillCatalog.F(row, "MoveSpeed"), owner.Battle.Navigation);
        // 索引超上界与原GetAIByIndex一致夹到最后，负数表示无树。
        string configured = MoshentaSkillCatalog.S(row, "AI");
        TreeName = "";
        if (configured.Length > 0 && aiIndex >= 0)
        {
            // 原EntityTable竖线序列保留其声明顺序。
            string[] names = configured.Split('|');
            TreeName = names[Math.Min(aiIndex, names.Length - 1)];
        }
        // 普通攻击原技能射程供CloseToEnemy使用，机制树无需此字段。
        AttackRange = 0;
        string[] bindings = MoshentaSkillCatalog.S(row, "UnitSkillID").Split('|');
        for (int index = 0; index < bindings.Length; index++)
        {
            // 每条技能绑定的首项是SkillTable编号。
            string[] fields = bindings[index].Split('=');
            int skillId;
            if (!int.TryParse(fields[0], out skillId)) continue;
            // min_attack_range来自所有已绑定技能的最小有效射程。
            float range = MoshentaSkillCatalog.F(Owner.Battle.Catalog.Effect(skillId), "FarRange");
            if (range > 0 && (AttackRange == 0 || range < AttackRange)) AttackRange = range;
        }
        Skills = new MoshentaSkillRuntime(this);
        if (TreeName.Length > 0)
        {
            // 原AI组件在创建本实体黑板时抽一次偏角，后续心跳沿用。
            float offset = MoshentaSkillCatalog.F(row, "FightTargetOffsetDegree");
            fightRotation = (float)((owner.Random.NextDouble() * 2 - 1) * offset);
            Tree = new MoshentaBehaviorTree(owner, this, TreeName);
        }
        Timeline = MoshentaTimeline.Create(this);
        action = new MonsterActionData { SpawnId = spawnId, Phase = MonsterActionPhase.Idle };
    }

    /// <summary>出生初始化独立于构造，确保实例已登记本实体后再处理出生Buff或场景事件。</summary>
    internal void Born()
    {
        // 原BornBuff条目为完整BuffID=万分比概率。
        string[] buffs = MoshentaSkillCatalog.S(Owner.Battle.Catalog.Entity(EntityId), "BornBuff").Split('|');
        for (int index = 0; index < buffs.Length; index++)
        {
            // 零字符串表示此实体未配置出生Buff。
            string[] fields = buffs[index].Split('=');
            int buffId;
            if (!int.TryParse(fields[0], out buffId)) continue;
            if (fields.Length > 1 && Owner.Random.Next(0, 10000) >= int.Parse(fields[1])) continue;
            Skills.AddBuff(buffId);
        }
    }
    /// <summary>检查角色当前是否仍是同实例在线存活玩家。</summary><param name="role">待检查角色。</param>
    internal bool Valid(OnlineRole role) { return role != null && Owner.FindRole(role.GetID()) == role && role.mainRoleInfo.BaseInfo.CurrHp > 0; }
    /// <summary>接受本次实际伤害仇恨，死亡立即取消未发生技能结果。</summary><param name="role">玩家施害者，可空。</param><param name="damage">实际扣血。</param>
    internal void OnHit(OnlineRole role, int damage)
    {
        if (IsDead) { Die(); return; }
        if (!Valid(role) || damage <= 0) return;
        AddHatred(role, damage, false);
        nextThink = Now;
    }
    /// <summary>原SetEntityHatred与120%换目标规则；联盟通知只传播第一点，避免循环联动。</summary><param name="role">实际角色。</param><param name="amount">新增仇恨，初始为1。</param><param name="fromAlliance">是否来自原波次联盟。</param>
    internal void AddHatred(OnlineRole role, long amount, bool fromAlliance)
    {
        if (!Valid(role) || IsDead) return;
        bool first = threats.Count == 0;
        if (!threatValues.ContainsKey(role.GetID())) { threats.Add(role); threatValues[role.GetID()] = 0; }
        threatValues[role.GetID()] += Math.Max(1, amount);
        long current = 0; if (Target != null) threatValues.TryGetValue(Target.GetID(), out current);
        if (Target == null || threatValues[role.GetID()] > (long)(current * 1.2f)) Target = role;
        if (!first) return;
        if (!fromAlliance) Owner.NotifyAlliance(this, role);
        if (buildSightHatred) AddNearbyHatred();
    }
    /// <summary>原主动视距敌人按距离排序，各建立1点初始仇恨；已有值不重复增加。</summary>
    private void AddNearbyHatred()
    {
        List<OnlineRole> players = Owner.Players;
        players.Sort((first, second) => Vector3.DistanceSquared(Position, first.mainRoleInfo.BaseInfo.Pos.ToVector3()).CompareTo(Vector3.DistanceSquared(Position, second.mainRoleInfo.BaseInfo.Pos.ToVector3())));
        for (int index = 0; index < players.Count; index++)
        {
            OnlineRole role = players[index];
            if (!Valid(role) || threatValues.ContainsKey(role.GetID())) continue;
            if (DistanceTo(role.mainRoleInfo.BaseInfo.Pos.ToVector3()) <= sight) AddHatred(role, 1, false);
        }
    }
    /// <summary>移除失效仇恨并按原主动索敌范围选择实际玩家。</summary>
    private void SelectTarget()
    {
        if (!Valid(Target)) Target = null;
        // 同值保留原列表中更早建立仇恨的目标。
        long best = -1;
        for (int index = 0; index < threats.Count;)
        {
            // 玩家退出/死亡后不再被旧技能或仇恨继续引用。
            OnlineRole role = threats[index];
            if (!Valid(role)) { if (Owner.FindRole(role.GetID()) == role) deadThreats.Add(role); threatValues.Remove(role.GetID()); threats.RemoveAt(index); continue; }
            if (Target == null && threatValues[role.GetID()] > best) { best = threatValues[role.GetID()]; }
            index++;
        }
        // 目标消失时取当前最大仇恨；存在目标时保持原120%换人迟滞。
        if (Target == null) for (int index = 0; index < threats.Count; index++) if (threatValues[threats[index].GetID()] == best) { Target = threats[index]; break; }
        for (int index = deadThreats.Count - 1; index >= 0; index--)
        {
            OnlineRole role = deadThreats[index];
            if (Owner.FindRole(role.GetID()) != role) { deadThreats.RemoveAt(index); continue; }
            if (Valid(role)) { deadThreats.RemoveAt(index); AddHatred(role, 1, false); }
        }
        if (!Passive) AddNearbyHatred();
    }
    /// <summary>推进到指定实例时刻，技能结果和路径转角先于同刻AI心跳。</summary><param name="now">实例逻辑毫秒。</param>
    internal void Update(long now)
    {
        Now = now;
        if (removed) return;
        if (IsDead) { Die(); if (now >= corpseRemoveAt) Owner.RemoveActor(this); return; }
        if (now >= ExpireTime) { Owner.KillActor(this); return; }
        bool casting = Skills.IsCasting;
        Skills.Update(now);
        if (IsDead) { Die(); return; }
        if (casting && !Skills.IsCasting) SetIdle();
        if (navigation.Moving && !CanMove) SetIdle();
        if (navigation.Moving)
        {
            navigation.Advance(Math.Min(now, navigation.End));
            if (now >= navigation.End)
            {
                if (!navigation.Next(now)) { if (patrolLeg) Arrive(); SetIdle(); }
                else PublishMotion();
            }
        }
        if (now < nextThink) return;
        thinkRequested = false;
        SelectTarget();
        if (Tree != null) Tree.Tick();
        nextThink = now + idleInterval;
        if (Target != null) nextThink = now + combatInterval;
        if (thinkRequested) nextThink = now + 1;
    }
    /// <summary>时间轴切轴按原AIEventFire请求下一逻辑步继续跑树。</summary>
    internal void RequestThink() { thinkRequested = true; nextThink = Now; }
    /// <summary>当前宿主到世界坐标的水平距离。</summary><param name="point">目标米制坐标。</param>
    internal float DistanceTo(Vector3 point) { point -= Position; return (float)Math.Sqrt(point.X * point.X + point.Z * point.Z); }
    /// <summary>计算目标近战站位并核对导航连通性。</summary><param name="point">目标坐标。</param><param name="range">目标外侧距离米。</param><param name="destination">最终站位。</param>
    internal bool FightPosition(Vector3 point, float range, out Vector3 destination)
    {
        // 原距离判定只用XZ，旋转归一化保留真实高差。
        Vector3 delta = Position - point; destination = Position;
        if (delta.X * delta.X + delta.Z * delta.Z < range * range) return true;
        // 原HorizontalRotate先按整度查表，再归一化方向；每只怪物保持自己的偏角。
        Vector3 direction = OrcNavigation.RotateDirection(delta, fightRotation);
        if (direction.LengthSquared() > 0) direction = Vector3.Normalize(direction);
        destination = point + direction * range;
        return navigation.Reachable(destination);
    }
    /// <summary>下达原移动动作；技能中不覆盖当前施法。</summary><param name="destination">权威世界目的地。</param>
    internal bool Move(Vector3 destination)
    {
        if (Skills.IsCasting || IsDead || !CanMove) return false;
        if (Owner.Flow.Blocks(Position, destination)) return false;
        if (navigation.GoingTo(destination)) return true;
        patrolLeg = false;
        if (!navigation.Begin(destination, Now)) return false;
        PublishMotion(); return true;
    }
    /// <summary>按原波次巡逻线移动，未配置路径时返回失败。</summary>
    internal bool Patrol()
    {
        if (!CanMove) return false;
        if (patrol.Length == 0 || Skills.IsCasting || Now < waitUntil) return false;
        if (navigation.Moving) return false;
        // 当前原巡逻点的世界坐标。
        Vector3 goal = MoshentaSceneFlow.Position(patrol[patrolIndex].Element("position"));
        if (DistanceTo(goal) < 1.5f) { Arrive(); return true; }
        if (!navigation.Begin(goal, Now)) return false;
        patrolLeg = true; PublishMotion(); return true;
    }
    /// <summary>到点停留后按PingPong或循环方式推进。</summary>
    private void Arrive()
    {
        // 原路径点的停留与转向停留共同计秒。
        XElement point = patrol[patrolIndex];
        waitUntil = Now + (int)(1000 * ((float?)point.Element("stayTime") ?? 0) + 1000 * ((float?)point.Element("rotateStayTime") ?? 0));
        if (patrol.Length <= 1) return;
        if (patrolType == "ScenePatrolType_PingPong")
        {
            if (patrolIndex == patrol.Length - 1) patrolDirection = -1;
            if (patrolIndex == 0) patrolDirection = 1;
            patrolIndex += patrolDirection;
        }
        else patrolIndex = (patrolIndex + 1) % patrol.Length;
    }
    /// <summary>真实技能开始时停止导航，面向施法点并保存动作。</summary><param name="data">技能组件产生的权威动作。</param>
    internal void SetSkillAction(MonsterActionData data) { navigation.Stop(); navigation.Face(new Vector3(data.CastX, data.CastY, data.CastZ)); action = data; Owner.Notice.MonsterActions.Add(Snapshot()); }
    /// <summary>结束当前移动或技能表现并发布待机快照。</summary>
    internal void SetIdle() { navigation.Stop(); action = new MonsterActionData { SpawnId = SpawnId, Phase = MonsterActionPhase.Idle }; Owner.Notice.MonsterActions.Add(Snapshot()); }
    /// <summary>发布当前路径段，客户端从真实当前位置插值。</summary>
    private void PublishMotion() { action = new MonsterActionData { SpawnId = SpawnId, Phase = MonsterActionPhase.Move }; Owner.Notice.MonsterActions.Add(Snapshot()); }
    /// <summary>当前位置瞬移，停止旧路径与未结束动作。</summary><param name="position">原技能指定目的地。</param>
    internal void Teleport(Vector3 position) { navigation.Stop(); Monster.Transform.X = position.X; Monster.Transform.Y = position.Y; Monster.Transform.Z = position.Z; SetIdle(); }
    /// <summary>原冲锋曲线的位置交给现有导航首段约束，控制期间停在当前位置。</summary><param name="desired">该时点原期望位置。</param>
    internal Vector3 ClampSkillStep(Vector3 desired) { if (!CanMove) return Position; return navigation.ClampSkillStep(desired); }
    /// <summary>显式死亡与清理保持不同语义，死亡立即撤销未到的技能结果。</summary>
    internal void Die()
    {
        if (dead) return;
        Now = Owner.Now; deathStarted = Now; corpseRemoveAt = Now + corpseDuration + 1;
        dead = true; navigation.Stop(); Skills.Cancel(); Target = null; threats.Clear(); threatValues.Clear();
        action = new MonsterActionData { SpawnId = SpawnId, Phase = MonsterActionPhase.Dead };
        Owner.Notice.MonsterActions.Add(Snapshot());
    }
    /// <summary>释放实例资源，不执行额外击杀或结算事件。</summary>
    internal void Remove() { removed = true; navigation.Stop(); Skills.Dispose(); if (Timeline != null) Timeline.Dispose(); }
    /// <summary>入场和实时广播使用同一时刻的可续播动作。</summary>
    internal MonsterActionData Snapshot()
    {
        // 克隆避免后续更新改变已排入同帧的动作消息。
        MonsterActionData data = action.Clone();
        data.X = Position.X; data.Y = Position.Y; data.Z = Position.Z; data.Face = Facing;
        if (Target != null) data.TargetRoleId = Target.mainRoleInfo.BaseInfo.RoleId;
        if (navigation.Moving)
        {
            data.ToX = navigation.To.X; data.ToY = navigation.To.Y; data.ToZ = navigation.To.Z;
            data.DurationMs = (int)Math.Max(0, navigation.End - Now);
        }
        if (Skills.IsCasting)
        {
            data.DurationMs = (int)Math.Max(0, Skills.EndTime - Now);
            data.ElapsedMs = (int)Math.Max(0, Now - Skills.StartTime);
        }
        if (dead)
        {
            data.DurationMs = (int)Math.Max(0, corpseRemoveAt - Owner.Now);
            data.ElapsedMs = (int)Math.Max(0, Owner.Now - deathStarted);
        }
        return data;
    }
}
