using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;
using ServerMonster = GameServer.SceneEntity.MonsterEntity;

/// <summary>实例原AI与技能的窄宿主，复用MonsterEntity和OnlineRole的权威资源。</summary>
public sealed partial class SceneWaveSpawner
{
    /// <summary>当前场景是否使用本次实际移植的十二棵树。</summary>
    internal bool IsMoshenta { get { return Dungeon.SceneID == 4060; } }
    /// <summary>所有实际AI实体，包含无HP机制与动态召唤。</summary>
    internal List<MoshentaActor> Actors { get; private set; } = new List<MoshentaActor>();
    /// <summary>本帧的统一可靠通知出口，出生阶段也保留事件。</summary>
    internal MonsterAttrUpdateNtf Notice { get; private set; } = new MonsterAttrUpdateNtf();
    /// <summary>实例独立随机序列，两种时间轴共享同一源。</summary>
    internal Random Random { get; set; } = new Random();
    /// <summary>实例逻辑毫秒，重置副本时回零。</summary>
    internal long Now { get; private set; }
    /// <summary>由TeamManager绑定的实例身份。</summary>
    private ulong instanceId;
    /// <summary>TeamManager的原成员列表引用，进出队会同步生效。</summary>
    private List<ulong> members = new List<ulong>();
    /// <summary>初始Flow尚未赋值前创建实体的出生回调。</summary>
    private readonly List<MoshentaActor> births = new List<MoshentaActor>();
    /// <summary>唯一场景树，独立于每只怪物的黑板。</summary>
    private MoshentaBehaviorTree sceneTree;
    /// <summary>原SceneAI默认1秒心跳，使无新事件的10秒开墙计时仍继续执行。</summary>
    private long nextSceneThink;
    /// <summary>原SceneVar的类型和值。</summary>
    private readonly Dictionary<string, MoshentaSceneVariable> variables = new Dictionary<string, MoshentaSceneVariable>();
    /// <summary>原延时场景事件，派发保持同刻插入顺序。</summary>
    private readonly List<MoshentaScheduledEvent> scheduled = new List<MoshentaScheduledEvent>();
    /// <summary>当前启用的原共享生命组，元素保存真实实体引用。</summary>
    private readonly Dictionary<ulong, List<MoshentaActor>> sharedHp = new Dictionary<ulong, List<MoshentaActor>>();
    /// <summary>本次Gene世界帧已通过共享增量处理的实体，帧后差值不可再次扣血或通知。</summary>
    private readonly HashSet<ulong> sharedGeneChanged = new HashSet<ulong>();
    /// <summary>本次Gene世界帧共享组是否首次产生死亡，供统一清波事件使用。</summary>
    private bool sharedGeneDied;
    /// <summary>原副本胜利条件使用的EntityId死亡计数。</summary>
    private readonly Dictionary<int, int> corpseCounts = new Dictionary<int, int>();
    /// <summary>实例内动态出生递增编号，重置也不复用。</summary>
    private uint summonSequence;

    /// <summary>绑定真实队伍成员，角色筛选同时核对Map和Instance。</summary><param name="id">副本实例号。</param><param name="roles">原队员列表引用。</param>
    internal void BindInstance(ulong id, List<ulong> roles) { instanceId = id; members = roles; }
    /// <summary>取得当前在线且仍位于本实例的玩家快照。</summary>
    internal List<OnlineRole> Players
    {
        get
        {
            // 快照只存原OnlineRole引用，技能不会创建第二份角色状态。
            List<OnlineRole> result = new List<OnlineRole>();
            for (int index = 0; index < members.Count; index++)
            {
                OnlineRole role = RoleManager.Instance.GetOnlineRole((int)members[index]);
                if (role == null) continue;
                if (role.mainRoleInfo.BaseInfo.MapId != Dungeon.SceneID) continue;
                if ((ulong)role.mainRoleInfo.BaseInfo.InstanceId != instanceId) continue;
                result.Add(role);
            }
            return result;
        }
    }
    /// <summary>仅在本实例解析角色的原GetID身份。</summary><param name="id">角色运行时身份。</param>
    internal OnlineRole FindRole(ulong id) { List<OnlineRole> list = Players; for (int index = 0; index < list.Count; index++) if (list[index].GetID() == id) return list[index]; return null; }
    /// <summary>仅在本实例解析真实MonsterEntity UID。</summary><param name="uid">怪物运行时身份。</param>
    internal MoshentaActor FindActor(ulong uid) { for (int index = 0; index < Actors.Count; index++) if (Actors[index].UnitId == uid) return Actors[index]; return null; }
    /// <summary>按原场景树创建实例状态。</summary>
    private void InitializeAi() { Now = 0; nextSceneThink = 1000; if (IsMoshenta) sceneTree = new MoshentaBehaviorTree(this, null, "Dungeons_DaNaTuoSiZhiZhan_SceneAI_EZ"); }
    /// <summary>初始Flow构造结束后才执行出生副作用。</summary>
    private void FinishBirths() { for (int index = 0; index < births.Count; index++) births[index].Born(); births.Clear(); if (sceneTree != null) sceneTree.Tick(); }
    /// <summary>按最早原结果/转角/心跳截止点推进，保留同刻技能先于AI的调用顺序。</summary><param name="elapsed">本固定帧毫秒。</param>
    private void UpdateAi(int elapsed)
    {
        // 50毫秒仍推进飞行碰撞；更早的原结果点直接在对应毫秒执行。
        long end = Now + elapsed;
        while (Now < end)
        {
            long next = Math.Min(end, Now + 50);
            if (sceneTree != null) next = Math.Min(next, Math.Max(Now + 1, nextSceneThink));
            for (int index = 0; index < Actors.Count; index++) next = Math.Min(next, Math.Max(Now + 1, Actors[index].NextEventTime));
            for (int index = 0; index < scheduled.Count; index++) next = Math.Min(next, Math.Max(Now + 1, scheduled[index].At));
            Now = next;
            if (sceneTree != null && Now >= nextSceneThink) { sceneTree.Tick(); nextSceneThink = Now + 1000; }
            for (int index = 0; index < scheduled.Count;)
            {
                MoshentaScheduledEvent item = scheduled[index];
                if (item.At > Now) { index++; continue; }
                scheduled.RemoveAt(index); DispatchEvent(item.Event);
            }
            // 本步出生的召唤物下一步开始AI，保持出生Buff先于首心跳。
            MoshentaActor[] actors = Actors.ToArray();
            for (int index = 0; index < actors.Length; index++) actors[index].Update(Now);
        }
    }
    /// <summary>将树/技能的待发事件转入当前正式回包，外部场景事件也不会被下一帧Clear丢弃。</summary><param name="target">当前将广播的消息。</param>
    private void FlushAiNotice(MonsterAttrUpdateNtf target)
    {
        target.Monsters.Add(Notice.Monsters); target.MonsterActions.Add(Notice.MonsterActions); target.PlayerHits.Add(Notice.PlayerHits);
        target.MonsterSpawns.Add(Notice.MonsterSpawns); target.RemovedSpawnIds.Add(Notice.RemovedSpawnIds); target.AiNotices.Add(Notice.AiNotices);
        target.BuffChanges.Add(Notice.BuffChanges);
        Notice.Monsters.Clear(); Notice.MonsterActions.Clear(); Notice.PlayerHits.Clear(); Notice.MonsterSpawns.Clear(); Notice.RemovedSpawnIds.Clear(); Notice.AiNotices.Clear();
        Notice.BuffChanges.Clear();
    }
    /// <summary>查询原Entity UUID是否仍存在，包括已经死亡但未消失的实体。</summary><param name="uid">目标身份。</param>
    internal bool ContainsEntity(ulong uid) { return FindActor(uid) != null || FindRole(uid) != null; }
    /// <summary>原IsDead条件不把零上限机制误当尸体。</summary><param name="uid">目标身份。</param>
    internal bool IsEntityDead(ulong uid) { MoshentaActor actor = FindActor(uid); if (actor != null) return actor.IsDead; OnlineRole role = FindRole(uid); return role == null || role.mainRoleInfo.BaseInfo.CurrHp <= 0; }
    /// <summary>读取原权威Transform或角色位置。</summary><param name="uid">目标身份。</param>
    internal Vector3 EntityPosition(ulong uid) { MoshentaActor actor = FindActor(uid); if (actor != null) return actor.Position; OnlineRole role = FindRole(uid); if (role != null) return role.mainRoleInfo.BaseInfo.Pos.ToVector3(); return Vector3.Zero; }
    /// <summary>原群体玩家查找返回本实例在线角色顺序。</summary>
    internal List<ulong> PlayerIds() { List<OnlineRole> list = Players; List<ulong> result = new List<ulong>(); for (int index = 0; index < list.Count; index++) result.Add(list[index].GetID()); return result; }
    /// <summary>原Lua水平距离查找；存活筛选仍由调用节点控制。</summary><param name="center">中心米制位置。</param><param name="near">近距离米。</param><param name="far">远距离米。</param>
    internal List<ulong> PlayersInRange(Vector3 center, float near, float far)
    {
        List<ulong> result = PlayerIds();
        for (int index = result.Count - 1; index >= 0; index--)
        {
            Vector3 delta = EntityPosition(result[index]) - center; delta.Y = 0;
            if (delta.LengthSquared() < near * near || delta.LengthSquared() > far * far) result.RemoveAt(index);
        }
        return result;
    }
    /// <summary>保留原群体随机选择的无重复洗牌。</summary><param name="values">可原地修改的UUID列表。</param>
    internal void Shuffle(List<ulong> values) { for (int index = 0; index < values.Count; index++) { int other = Random.Next(index, values.Count); ulong value = values[index]; values[index] = values[other]; values[other] = value; } }
    /// <summary>原波次查找按配置号列表与出生顺序返回实体。</summary><param name="ids">竖线或逗号分隔的原波号。</param>
    internal List<ulong> WaveEntities(string ids)
    {
        string[] fields = ids.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        List<ulong> result = new List<ulong>();
        for (int index = 0; index < fields.Length; index++)
        {
            int wave = int.Parse(fields[index], CultureInfo.InvariantCulture);
            for (int actor = 0; actor < Actors.Count; actor++) if (Actors[actor].WaveId == wave) result.Add(Actors[actor].UnitId);
        }
        return result;
    }
    /// <summary>原GetAllianceMonster按XML联盟波与召唤关系联动，不合并所有Boss仇恨。</summary><param name="source">首次建立仇恨的实体。</param><param name="role">实际目标。</param>
    internal void NotifyAlliance(MoshentaActor source, OnlineRole role)
    {
        // 原GlobalTableFight.MvpAllianceRange为20米，仅用于召唤所属关系。
        for (int index = 0; index < Actors.Count; index++)
        {
            MoshentaActor other = Actors[index];
            if (other == source || other.IsDead) continue;
            bool linked = other.SummonerId == source.UnitId || source.SummonerId == other.UnitId;
            if (linked && source.DistanceTo(other.Position) <= 20) other.AddHatred(role, 1, true);
        }
        // 波次联盟来自当前正式XML，保留用户现有波次位置与配置。
        XElement wave = Config.Descendants("MSceneWaveData").FirstOrDefault(row => (int)row.Element("base_info").Element("id") == source.WaveId);
        if (wave == null) return;
        XElement alliance = wave.Descendants("alliance_info").FirstOrDefault();
        if (alliance == null) return;
        float range = (float)alliance.Element("alliance_range");
        XElement[] ids = alliance.Element("wave_ids").Elements().ToArray();
        for (int index = 0; index < ids.Length; index++)
            for (int other = 0; other < Actors.Count; other++)
                if (Actors[other].WaveId == (int)ids[index] && source.DistanceTo(Actors[other].Position) <= range) Actors[other].AddHatred(role, 1, true);
    }
    /// <summary>原怪物波所有实体均死亡；未生成波不算清空。</summary><param name="wave">波次号。</param>
    internal bool IsWaveDead(int wave) { if (Flow.IsComplete(wave)) return true; bool found = false; for (int index = 0; index < Actors.Count; index++) if (Actors[index].WaveId == wave) { found = true; if (!Actors[index].IsDead) return false; } return found; }
    /// <summary>读取场景变量原类型，不存在返回false。</summary><param name="key">原变量名。</param><param name="value">实际类型和值。</param>
    internal bool TrySceneVariable(string key, out MoshentaSceneVariable value) { return variables.TryGetValue(key, out value); }
    /// <summary>原字符串序列化入口按var_type写具名类型。</summary><param name="key">变量名。</param><param name="value">原序列化文本。</param><param name="type">原类型名称。</param>
    internal void SetSceneVariable(string key, string value, string type)
    {
        MoshentaSceneVariable item = new MoshentaSceneVariable { Type = type, StringValue = "" };
        if (type == "kTypeInt") item.IntValue = int.Parse(value, CultureInfo.InvariantCulture);
        else if (type == "kTypeFloat") item.FloatValue = float.Parse(value, CultureInfo.InvariantCulture);
        else item.StringValue = value;
        variables[key] = item;
    }
    /// <summary>Lua整数场景变量写入原黑板。</summary><param name="key">变量名。</param><param name="value">整数。</param>
    internal void SetSceneInt(string key, int value) { SetSceneVariable(key, value.ToString(CultureInfo.InvariantCulture), "kTypeInt"); }
    /// <summary>Lua原GetSceneVar未存在时使用零整数槽。</summary><param name="key">变量名。</param>
    internal int GetSceneInt(string key) { MoshentaSceneVariable value; if (variables.TryGetValue(key, out value)) return value.IntValue; return 0; }
    /// <summary>技能或场景图向原SceneAI派发事件。</summary><param name="name">事件名。</param><param name="intArg">原整型参数。</param><param name="floatArg">原浮点参数。</param><param name="senderId">原施加者UUID。</param>
    internal void SceneEvent(string name, int intArg = 0, float floatArg = 0, ulong senderId = 0) { ScheduleSceneEvent(name, intArg, floatArg, senderId, 0); }
    /// <summary>延迟为零立即派发，其余以本实例逻辑毫秒计时。</summary><param name="name">事件名。</param><param name="integer">整型参数。</param><param name="number">浮点参数。</param><param name="sender">施加者身份。</param><param name="delay">毫秒。</param>
    internal void ScheduleSceneEvent(string name, int integer, float number, ulong sender, int delay)
    {
        MoshentaAiEvent data = new MoshentaAiEvent { Name = name, IntArg = integer, FloatArg = number, SenderId = sender, Position = EntityPosition(sender) };
        if (delay <= 0) DispatchEvent(data);
        else scheduled.Add(new MoshentaScheduledEvent { At = Now + delay, Event = data });
    }
    /// <summary>目标事件归原目标组件，其他事件按原树同一次bool Update处理。</summary><param name="data">原场景事件。</param>
    private void DispatchEvent(MoshentaAiEvent data)
    {
        if (data.Name == "dungeonfinishtarget") { Flow.DungeonTargetEvent(data.Name, data.IntArg); return; }
        if (sceneTree == null) return;
        sceneTree.Event = data; sceneTree.Tick();
    }
    /// <summary>原TV/头顶表情/黑幕通过同实例通知进入客户端正式表现。</summary><param name="kind">1公告、2头顶、3黑幕。</param><param name="id">原表编号。</param><param name="source">实体宿主，场景树为空。</param><param name="target">指定玩家UUID或0广播。</param>
    internal void PublishAiNotice(int kind, int id, MoshentaActor source, ulong target)
    {
        MonsterAiNoticeData data = new MonsterAiNoticeData { Kind = kind, NoticeId = id };
        if (source != null) data.SourceSpawnId = source.SpawnId;
        OnlineRole role = FindRole(target); if (role != null) data.TargetRoleId = role.mainRoleInfo.BaseInfo.RoleId;
        Notice.AiNotices.Add(data);
    }
    /// <summary>原Actor或玩家Buff写入技能模块，禁止再建一份效果状态。</summary><param name="caster">原施加者。</param><param name="targetId">目标身份。</param><param name="fullBuffId">含等级Buff编号。</param>
    internal void AddBuff(MoshentaActor caster, ulong targetId, int fullBuffId)
    {
        // 场景AI没有怪物施加者，协议/公式保留原场景来源0。
        MoshentaActor actor = FindActor(targetId);
        if (actor != null)
        {
            if (caster == null) actor.Skills.AddSceneBuff(fullBuffId);
            else actor.Skills.AddBuffFrom(caster, fullBuffId);
            return;
        }
        OnlineRole role = FindRole(targetId);
        if (role == null) return;
        if (caster == null) MoshentaSkillRuntime.AddScenePlayerBuff(this, role, fullBuffId);
        else MoshentaSkillRuntime.AddPlayerBuff(caster, role, fullBuffId);
    }
    /// <summary>删除原Buff家族所有等级。</summary><param name="targetId">目标身份。</param><param name="family">不含末两位等级的家族号。</param>
    internal void RemoveBuff(ulong targetId, int family) { MoshentaActor actor = FindActor(targetId); if (actor != null) { actor.Skills.RemoveBuff(family); return; } OnlineRole role = FindRole(targetId); if (role != null) MoshentaSkillRuntime.RemovePlayerBuff(this, role, family); }
    /// <summary>原条件节点按Buff家族判定。</summary><param name="targetId">目标身份。</param><param name="family">家族号。</param>
    internal bool HasBuff(ulong targetId, int family) { MoshentaActor actor = FindActor(targetId); if (actor != null) return actor.Skills.HasBuff(family); OnlineRole role = FindRole(targetId); return role != null && MoshentaSkillRuntime.HasPlayerBuff(this, role, family); }
    /// <summary>仅扣技能公式已经结算的最终伤害，不能再次乘Gene承伤。</summary><param name="caster">技能施加者。</param><param name="target">实际玩家目标。</param><param name="skillId">原技能编号。</param><param name="damage">最终生命伤害。</param><param name="critical">原暴击结果。</param><param name="resultIndex">原结果点零基下标。</param>
    internal void ApplyPlayerDamage(MoshentaActor caster, OnlineRole target, int skillId, int damage, bool critical, int resultIndex = 0)
    {
        RoleBaseInfo info = target.mainRoleInfo.BaseInfo;
        RoleEquipComponent equipment = target.GetComponent<RoleEquipComponent>();
        int actual = Math.Min(info.CurrHp, Math.Max(0, damage)); info.CurrHp -= actual;
        if (actual > 0) equipment.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, caster.Monster.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime, skillId, actual, critical));
        Notice.PlayerHits.Add(new MonsterPlayerHitData { SpawnId = caster.SpawnId, TargetRoleId = info.RoleId, Hp = info.CurrHp, MaxHp = equipment.CalculateAttribute().MaxHp, Damage = actual, SkillId = skillId, ResultIndex = resultIndex, Critical = critical });
        if (info.CurrHp > 0) return;
        RoleSkillComponent skill; if (target.TryGetComponent(out skill)) skill.InterruptActiveCast(true);
        StateMachineComponent state; if (target.TryGetComponent(out state)) state.ForceToDefaultState(true);
    }
    /// <summary>技能最终伤害同样沿共享HP和真实死亡出口。</summary><param name="caster">施加者。</param><param name="target">实际怪物。</param><param name="skillId">原技能编号。</param><param name="damage">最终伤害。</param><param name="critical">暴击结果。</param>
    internal void ApplyMonsterDamage(MoshentaActor caster, MoshentaActor target, int skillId, int damage, bool critical) { if (!target.CanBeAttacked) return; DamageActor(target, damage, null); CheckCompletedWaves(); Flow.MonsterDied(); }
    /// <summary>原自杀Buff显式死亡，允许零HP机制使用该出口。</summary><param name="actor">实际执行自杀的实体。</param>
    internal void KillActor(MoshentaActor actor) { if (actor.IsDead) return; actor.Monster.CurrentHp = 0; actor.Die(); CountDeath(actor.EntityId); Notice.Monsters.Add(HpData(actor, 0)); CheckCompletedWaves(); Flow.MonsterDied(); }
    /// <summary>原CalcShareHp按受击前当前生命比例分摊，截断余量留给实际受击者。</summary><param name="target">实际被命中实体。</param><param name="damage">最终伤害。</param><param name="caster">产生仇恨的玩家。</param>
    private void DamageActor(MoshentaActor target, int damage, OnlineRole caster)
    {
        ChangeSharedHp(target, -Math.Max(0, damage), caster);
    }
    /// <summary>生命增减均沿原CalcShareHp分摊，正数为恢复，负数为伤害。</summary><param name="target">本次属性变化原目标。</param><param name="change">未按单体HP截断的增量。</param><param name="caster">产生仇恨的玩家来源。</param>
    private void ChangeSharedHp(MoshentaActor target, int change, OnlineRole caster)
    {
        List<MoshentaActor> group;
        if (!sharedHp.TryGetValue(target.UnitId, out group)) { ChangeActorHp(target, change, caster); return; }
        long total = 0; for (int index = 0; index < group.Count; index++) total += group[index].CurrentHp;
        long delta = Math.Max(-total, change);
        long remainder = delta;
        for (int index = 0; index < group.Count; index++)
        {
            MoshentaActor other = group[index]; if (other == target) continue;
            int share = 0; if (total > 0) share = (int)(delta * other.CurrentHp / total);
            if (total + delta < group.Count) share = -other.CurrentHp;
            remainder -= share; ChangeActorHp(other, share, null);
        }
        if (total + delta < group.Count) remainder = -target.CurrentHp;
        ChangeActorHp(target, (int)remainder, caster);
    }
    /// <summary>具体基因已通过属性组件算出生命增量，本宿主负责共享分摊。</summary><param name="actor">原Gene宿主。</param><param name="firerId">本次生命变化的实际施加角色编号。</param><param name="delta">最终生命增量。</param>
    private bool ApplySharedGene(MoshentaActor actor, ulong firerId, int delta)
    {
        List<MoshentaActor> group;
        if (!sharedHp.TryGetValue(actor.UnitId, out group)) return false;
        // 记住本次变化前仍存活的实体，重复脉冲不能重复死亡统计。
        int living = 0;
        for (int index = 0; index < group.Count; index++) { sharedGeneChanged.Add(group[index].UnitId); if (!group[index].IsDead) living++; }
        OnlineRole caster = FindRole(firerId);
        ChangeSharedHp(actor, delta, caster);
        int remaining = 0;
        for (int index = 0; index < group.Count; index++) if (!group[index].IsDead) remaining++;
        if (remaining < living) sharedGeneDied = true;
        return true;
    }
    /// <summary>一次原属性变化只计一次真实死亡，HP通知保留实际损失。</summary><param name="actor">真实实体。</param><param name="delta">生命增量。</param><param name="caster">玩家来源。</param>
    private void ChangeActorHp(MoshentaActor actor, int delta, OnlineRole caster)
    {
        if (actor.IsDead) return;
        int previous = actor.CurrentHp; actor.Monster.CurrentHp = Math.Max(0, Math.Min(actor.MaxHp, previous + delta));
        actor.OnHit(caster, Math.Max(0, previous - actor.CurrentHp));
        Notice.Monsters.Add(HpData(actor, Math.Max(0, previous - actor.CurrentHp)));
        if (previous > 0 && actor.CurrentHp == 0) CountDeath(actor.EntityId);
    }
    /// <summary>原共享HP启用时按总生命与各自上限归一，禁用时清除组关系。</summary><param name="waves">原波号序列。</param><param name="enabled">原节点开关。</param>
    internal void ShareHp(string waves, bool enabled)
    {
        List<ulong> ids = WaveEntities(waves); List<MoshentaActor> group = new List<MoshentaActor>();
        for (int index = 0; index < ids.Count; index++) { MoshentaActor actor = FindActor(ids[index]); if (actor != null) group.Add(actor); }
        long hp = 0; long maximum = 0;
        for (int index = 0; index < group.Count; index++)
        {
            // 钩子只在原AI启用共享期间存在，捕获当前原对象引用。
            MoshentaActor actor = group[index];
            hp += actor.CurrentHp; maximum += actor.MaxHp;
            // 实体已有的属性组件；firerId 为实际施加者，delta 为子类算好的生命增量。
            AttributesComponent attributes = actor.Monster.Components.Get<AttributesComponent>(EntityComponentType.Attributes);
            if (enabled) { sharedHp[actor.UnitId] = group; attributes.SharedHpChange = (firerId, delta) => ApplySharedGene(actor, firerId, delta); }
            else { sharedHp.Remove(actor.UnitId); attributes.SharedHpChange = null; }
        }
        if (!enabled || group.Count < 2 || hp == maximum) return;
        if (maximum == 0) return;
        long remaining = hp;
        for (int index = 1; index < group.Count; index++) { int value = Math.Max(1, (int)(group[index].MaxHp * hp / maximum)); remaining -= value; ChangeActorHp(group[index], value - group[index].CurrentHp, null); }
        ChangeActorHp(group[0], (int)remaining - group[0].CurrentHp, null);
    }
    /// <summary>原死亡统计同时供目标与DungeonsTable胜利条件使用。</summary><param name="entityId">原模板号。</param>
    private void CountDeath(int entityId) { int count; corpseCounts.TryGetValue(entityId, out count); corpseCounts[entityId] = count + 1; Flow.CountMonsterDeath(entityId); }
    /// <summary>原两个胜利条件组之间为OR，组内所有条件为AND。</summary>
    internal bool Victory { get { return CheckVictory(Dungeon.VictoryCondion1) || CheckVictory(Dungeon.VictoryCondion2); } }
    /// <summary>本副本实际需要原击杀条件；时间条件沿相同逻辑时钟。</summary><param name="conditions">原二维条件序列。</param>
    private bool CheckVictory(int[][] conditions)
    {
        if (conditions.Length == 0) return false;
        for (int index = 0; index < conditions.Length; index++)
        {
            int[] row = conditions[index];
            if (row[0] == 1 && Now < row[1] * 1000L) return false;
            if (row[0] == 2) { int count; corpseCounts.TryGetValue(row[1], out count); if (count < row[2]) return false; }
        }
        return true;
    }
    /// <summary>原技能召唤创建真实实体，身份独立于所有固定波次。</summary><param name="caster">原施加者。</param><param name="entityId">召唤模板。</param><param name="position">世界坐标。</param><param name="facing">水平朝向。</param><param name="lifetimeMs">存续毫秒，非正值为无时限。</param>
    internal MoshentaActor Summon(MoshentaActor caster, int entityId, Vector3 position, float facing, int lifetimeMs)
    {
        ServerMonster monster = (ServerMonster)EntityFactory.Create(EntityType.Monster, entityId);
        monster.MapId = Dungeon.SceneID; monster.Transform.X = position.X; monster.Transform.Y = position.Y; monster.Transform.Z = position.Z; monster.Transform.Yaw = facing;
        monster.MaxHp = MoshentaSkillCatalog.I(MoshentaSkillCatalog.Entity(entityId), "BaseHP"); monster.CurrentHp = monster.MaxHp;
        ulong spawnId = 0xFFFFFFFF00000000UL | ++summonSequence;
        MoshentaActor actor = new MoshentaActor(this, monster, entityId, spawnId, -1, 0, new XElement[0], "");
        actor.SummonerId = caster.UnitId;
        if (lifetimeMs > 0) actor.ExpireTime = Now + lifetimeMs;
        world_.Add(monster); monsters_.Add(monster); activePoints.Add(new SpawnPoint { SpawnId = spawnId, Position = position, Facing = facing, Creature = new ROSceneCreatureEntity { id = entityId } }); Actors.Add(actor);
        Notice.MonsterSpawns.Add(SpawnData(actor)); Notice.Monsters.Add(HpData(actor, 0)); actor.Born(); return actor;
    }
    /// <summary>尸体定时器或场景显式清理释放AI和真实实体，移除不重复死亡计数。</summary><param name="actor">需要移除的原实体。</param>
    internal void RemoveActor(MoshentaActor actor) { int index = monsters_.IndexOf(actor.Monster); actor.Remove(); Actors.Remove(actor); if (index >= 0) { monsters_.RemoveAt(index); activePoints.RemoveAt(index); } world_.Remove(actor.Monster.UID); Notice.RemovedSpawnIds.Add(actor.SpawnId); }
    /// <summary>生成动态出生完整位置数据。</summary><param name="actor">运行中的召唤物。</param>
    private static MonsterSpawnData SpawnData(MoshentaActor actor) { return new MonsterSpawnData { SpawnId = actor.SpawnId, EntityId = actor.EntityId, X = actor.Position.X, Y = actor.Position.Y, Z = actor.Position.Z, Face = actor.Facing }; }
    /// <summary>生成真实HP的同帧通知。</summary><param name="actor">实际实体。</param><param name="damage">本次真实损失。</param>
    private static MonsterHpData HpData(MoshentaActor actor, int damage) { return new MonsterHpData { SpawnId = actor.SpawnId, Hp = actor.CurrentHp, MaxHp = actor.MaxHp, Damage = damage }; }
    /// <summary>原强制怪物位置通过动作快照广播。</summary><param name="actor">实际怪物。</param><param name="position">目的地米制坐标。</param>
    internal void TeleportActor(MoshentaActor actor, Vector3 position) { actor.Teleport(position); }
    /// <summary>原强制玩家位置沿既有DungeonInteract可靠通道。</summary><param name="role">同实例玩家。</param><param name="position">目的地。</param>
    internal void TeleportRole(OnlineRole role, Vector3 position) { TeamManager.Instance.ForceDungeonPosition(role, position); }
    /// <summary>吟唱和真正FireSkill分别产生原Phase，保留施法点与技能等级。</summary><param name="actor">技能宿主。</param><param name="phase">吟唱/攻击状态。</param><param name="skillId">原技能编号。</param><param name="level">实际等级。</param><param name="targetId">原目标身份。</param><param name="castPoint">锁定施法点。</param><param name="startTime">阶段开始毫秒。</param><param name="endTime">阶段结束毫秒。</param>
    internal void PublishSkillAction(MoshentaActor actor, MonsterActionPhase phase, int skillId, int level, ulong targetId, Vector3 castPoint, long startTime, long endTime)
    {
        MonsterActionData data = new MonsterActionData { SpawnId = actor.SpawnId, Phase = phase, SkillId = skillId, SkillLevel = level, CastX = castPoint.X, CastY = castPoint.Y, CastZ = castPoint.Z, DurationMs = (int)Math.Max(0, endTime - Now), ElapsedMs = (int)Math.Max(0, Now - startTime) };
        OnlineRole role = FindRole(targetId); if (role != null) data.TargetRoleId = role.mainRoleInfo.BaseInfo.RoleId;
        actor.SetSkillAction(data);
    }
    /// <summary>重置释放Lua、技能、共享HP与场景事件，保留动态身份单调递增。</summary>
    private void ClearAi()
    {
        for (int index = 0; index < Actors.Count; index++)
        {
            // 固定波由流程快照重建，动态召唤必须显式移除客户端现有出生号。
            MoshentaActor actor = Actors[index];
            if (actor.WaveId < 0) Notice.RemovedSpawnIds.Add(actor.SpawnId);
            actor.Remove();
        }
        Actors.Clear(); births.Clear(); sharedHp.Clear(); variables.Clear(); scheduled.Clear(); corpseCounts.Clear(); sceneTree = null;
        MoshentaSkillRuntime.ClearScene(this);
    }
}
