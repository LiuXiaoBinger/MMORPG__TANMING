using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using MMoRpgCommon.SceneEntity;

/// <summary>本副本原Buff触发器适配；属性和状态交给宿主已有Gene，原计时、光环与召唤沿原表。</summary>
internal sealed class MoshentaBuffRuntime
{
    /// <summary>该容器所属场景，隔离其他副本。</summary>
    internal readonly SceneWaveSpawner Scene;
    /// <summary>怪物宿主；玩家容器为空。</summary>
    internal readonly MoshentaActor Actor;
    /// <summary>玩家宿主；怪物容器为空。</summary>
    internal readonly OnlineRole Role;
    /// <summary>当前原Buff实例。</summary>
    private readonly List<MoshentaBuffInstance> buffs = new List<MoshentaBuffInstance>();
    /// <summary>逻辑线程分配的Buff实例身份；不同场景也不复用旧身份。</summary>
    private static ulong nextInstanceId;
    /// <summary>宿主现有Gene容器。</summary>
    private GeneRuntime genes
    {
        get
        {
            // Actor先构造，world.Add随后初始化组件；出生后才读取实际容器。
            if (Actor != null) return Actor.Monster.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime;
            return Role.GetComponent<RoleEquipComponent>().Runtime;
        }
    }
    /// <summary>绑定实际怪物或玩家，两者恰好一个非空。</summary>
    /// <param name="scene">同实例宿主。</param><param name="actor">真实怪物。</param><param name="role">真实玩家。</param>
    internal MoshentaBuffRuntime(SceneWaveSpawner scene, MoshentaActor actor, OnlineRole role)
    {
        Scene = scene; Actor = actor; Role = role;
    }
    /// <summary>当前原家族是否存在，不把完整行号误当家族。</summary><param name="family">原BuffId家族。</param>
    internal bool Has(int family)
    {
        for (int index = 0; index < buffs.Count; index++) if (buffs[index].Id / 100 == family) return true;
        return false;
    }
    /// <summary>检查原不可受击属性，早于伤害及受击Buff。</summary>
    internal bool Invulnerable
    {
        get
        {
            if (Has(32524) || Has(36051)) return true;
            return Has(36052) || Has(36059);
        }
    }
    /// <summary>原寿命或光环的最早截止，供场景精确分步。</summary>
    internal long NextTime
    {
        get
        {
            long next = long.MaxValue;
            for (int index = 0; index < buffs.Count; index++)
            {
                next = Math.Min(next, buffs[index].End);
                List<long> periods = new List<long>(buffs[index].Next.Values);
                for (int period = 0; period < periods.Count; period++) next = Math.Min(next, periods[period]);
            }
            return next;
        }
    }
    /// <summary>将本容器原属性叶子送回原Lua输入，不对Gene结果二次相乘。</summary><param name="snapshot">该宿主当前快照。</param>
    internal void Snapshot(MoshentaDamageSnapshot snapshot)
    {
        for (int index = 0; index < buffs.Count; index++)
        {
            // 原家族等级由Lua的HasBuff/GetBuff读取。
            MoshentaBuffInstance buff = buffs[index];
            snapshot.Buffs[buff.Id / 100] = buff.Id % 100;
            for (int effectIndex = 0; effectIndex < buff.Effects.Count; effectIndex++)
            {
                // 仅原属性叶子影响伤害边界；状态由Gene维护。
                XElement effect = buff.Effects[effectIndex];
                if (MoshentaSkillCatalog.I(effect, "EffectType") == 4)
                {
                    // 原特殊属性按来源计数；141/142不是免疫眩晕或定身。
                    string[] specialIds = MoshentaSkillCatalog.S(effect, "EffectParam").Split('|');
                    for (int part = 0; part < specialIds.Length; part++)
                    {
                        // 使用原AttrType名称保留免位移、免僵直和不可选中等区别。
                        string name = AttributeName(int.Parse(specialIds[part], CultureInfo.InvariantCulture));
                        snapshot.Set(name, snapshot.Get(name) + 1);
                    }
                    continue;
                }
                if (MoshentaSkillCatalog.I(effect, "EffectType") != 2) continue;
                // 本闭包六个实际属性，不构造未知属性映射。
                string[] pairs = MoshentaSkillCatalog.S(effect, "EffectParam").Split('|');
                for (int part = 0; part < pairs.Length; part++)
                {
                    // 原属性枚举编号和整数值。
                    string[] pair = pairs[part].Split('=');
                    string name = AttributeName(int.Parse(pair[0], CultureInfo.InvariantCulture));
                    snapshot.Set(name, snapshot.Get(name) + int.Parse(pair[1], CultureInfo.InvariantCulture));
                }
            }
        }
    }
    /// <summary>原枚举编号到公式属性名的严格闭包映射。</summary><param name="id">原AttrType整数。</param>
    private static string AttributeName(int id)
    {
        switch (id)
        {
            case 27: return "ATTR_BASIC_WEAPON_ATK_FINAL";
            case 36: return "ATTR_BASIC_WEAPON_MATK_FINAL";
            case 67: return "ATTR_PERCENT_DAM_REDUC";
            case 68: return "ATTR_PERCENT_MDAM_REDUC";
            case 141: return "ATTR_SPECIAL_NO_CHARGE_MOVE";
            case 142: return "ATTR_SPECIAL_NO_STIFF";
            case 146: return "ATTR_SPECIAL_NO_ATTACKED";
            case 149: return "ATTR_SPECIAL_NO_STOP_SINGING_BY_HURT";
            case 150: return "ATTR_SPECIAL_NO_STOP_SINGING";
            case 151: return "ATTR_SPECIAL_NO_STOP_SKILL";
            case 155: return "ATTR_SPECIAL_IGNORE_HATRED";
            case 171: return "ATTR_SPECIAL_UNABLE_SELECT";
            case 189: return "ATTR_PERCENT_GHOSS_SKILL_DEF";
            default: throw new InvalidOperationException("本副本未导入的RO属性 " + id);
        }
    }
    /// <summary>按原概率、寿命和替换规则添加一项Buff。</summary><param name="caster">真实来源。</param><param name="id">原完整Buff行号。</param>
    internal void Add(MoshentaActor caster, int id)
    {
        // 原具体等级行，不回退到其他技能Buff。
        XElement row = Scene.Battle.Catalog.Buff(id);
        if (MoshentaSkillCatalog.I(row, "IsDebuff") != 0 && Has(32524)) return;
        if (MoshentaSkillCatalog.I(row, "BuffEffect") != 0 && Has(9035)) return;
        // DestroyTiming第一项是种类：1时间，3触发次数。
        string[] timing = MoshentaSkillCatalog.S(row, "DestroyTiming").Split('=');
        double duration = -1;
        if (timing.Length == 2 && float.Parse(timing[0], CultureInfo.InvariantCulture) == 1)
            duration = double.Parse(timing[1], CultureInfo.InvariantCulture);
        // 原添加/寿命Lua在真实属性上执行。
        if (MoshentaSkillCatalog.S(row, "AddCondition").Length > 0 || MoshentaSkillCatalog.S(row, "DestroyLua").Length > 0)
        {
            // SceneAI的36060/36063没有Lua条件，仅实际怪物攻击施加的异常状态进入此分支。
            MoshentaDamageSnapshot target = Capture();
            duration = caster.Skills.Calculator.BuffDuration(row, MoshentaDamageSnapshot.Capture(caster), target, duration);
        }
        if (duration == -2) return;
        // 同源规则1保留其他来源；规则2/6按家族替换。
        int rule = int.Parse(MoshentaSkillCatalog.S(row, "ReplaceRule").Split('|')[0], CultureInfo.InvariantCulture);
        for (int index = buffs.Count - 1; index >= 0; index--)
        {
            // 仅当前同家族、符合源规则的原实例会被替换。
            MoshentaBuffInstance old = buffs[index];
            if (old.Id / 100 != id / 100) continue;
            if (rule == 1 && old.Caster != caster) continue;
            RemoveAt(index, false);
        }
        // 永久状态用明确截止值，不以额外Gene计时覆盖原Buff。
        MoshentaBuffInstance added = new MoshentaBuffInstance { Id = id, InstanceId = ++nextInstanceId, Caster = caster, Row = row, End = long.MaxValue };
        if (timing.Length == 2 && float.Parse(timing[0], CultureInfo.InvariantCulture) == 3)
            added.RemainingTriggers = (int)float.Parse(timing[1], CultureInfo.InvariantCulture);
        if (duration >= 0) added.End = Scene.Now + (long)(duration * 1000);
        // 原EffectGroup顺序即原叶子执行顺序。
        string[] effectIds = MoshentaSkillCatalog.S(row, "EffectGroup").Split('|');
        for (int index = 0; index < effectIds.Length; index++)
        {
            if (effectIds[index].Length == 0) continue;
            added.Effects.Add(Scene.Battle.Catalog.BuffEffect(int.Parse(effectIds[index], CultureInfo.InvariantCulture)));
        }
        for (int index = 0; index < added.Effects.Count; index++)
        {
            if (MoshentaSkillCatalog.I(added.Effects[index], "TriggerTiming") == 2)
                added.Next[index] = Scene.Now + (long)(MoshentaSkillCatalog.F(added.Effects[index], "IntervalTime") * 1000);
        }
        buffs.Add(added);
        Scene.Notice.BuffChanges.Add(ToData(added, false));
        // 原状态号：401眩晕、106沉默，使用已有动作门控。
        int status = MoshentaSkillCatalog.I(row, "BuffEffect");
        if (status == 401) MoshentaGeneBridge.Add(genes, added, 0, GeneAttributeType.None, 0, false, GeneState.Stun, () => RemainingMilliseconds(added));
        if (status == 106) MoshentaGeneBridge.Add(genes, added, 0, GeneAttributeType.None, 0, false, GeneState.Silence, () => RemainingMilliseconds(added));
        Trigger(added, 0);
        if (Role != null) Role.GetComponent<RoleEquipComponent>().NotifyAttributeUpdate();
    }
    /// <summary>读取实际宿主，原Buff附加属性仅在Snapshot方法中补一次。</summary>
    private MoshentaDamageSnapshot Capture()
    {
        if (Actor != null) return MoshentaDamageSnapshot.Capture(Actor);
        // 原Buff概率不依赖围攻数量。
        MoshentaDamageSnapshot snapshot = MoshentaDamageSnapshot.Capture(Role, 0);
        Snapshot(snapshot); return snapshot;
    }
    /// <summary>移除该家族全部来源。</summary><param name="family">原Buff家族。</param>
    internal void Remove(int family)
    {
        for (int index = buffs.Count - 1; index >= 0; index--) if (buffs[index].Id / 100 == family) RemoveAt(index, true);
    }
    /// <summary>准确释放叶子并按原删除时机执行自杀等事件。</summary><param name="index">当前实例下标。</param><param name="trigger">是否触发原删除事件。</param>
    private void RemoveAt(int index, bool trigger)
    {
        // 先摘除避免自杀触发死亡清理时重入同一Buff。
        MoshentaBuffInstance buff = buffs[index]; buffs.RemoveAt(index);
        Scene.Notice.BuffChanges.Add(ToData(buff, true));
        for (int geneIndex = 0; geneIndex < buff.Genes.Count; geneIndex++) genes.RemoveGene(buff.Genes[geneIndex]);
        if (trigger) Trigger(buff, 6);
        if (Role != null) Role.GetComponent<RoleEquipComponent>().NotifyAttributeUpdate();
    }
    /// <summary>离场或死亡清理，仅移除本容器产生的Gene。</summary>
    internal void Clear()
    {
        for (int index = buffs.Count - 1; index >= 0; index--) RemoveAt(index, false);
    }
    /// <summary>将尚未移除的原Buff按实际剩余时间序列化给晚加入角色。</summary>
    internal List<MonsterBuffData> ActiveData()
    {
        List<MonsterBuffData> result = new List<MonsterBuffData>();
        for (int index = 0; index < buffs.Count; index++) result.Add(ToData(buffs[index], false));
        return result;
    }
    /// <summary>只读取原Buff寿命；Gene同步与界面不会接管这里的到期移除。</summary>
    /// <param name="buff">原容器中实际生效的实例。</param>
    /// <returns>距离原 End 的毫秒数，永久为 -1。</returns>
    private long RemainingMilliseconds(MoshentaBuffInstance buff)
    {
        if (buff.End == long.MaxValue) return -1;
        return Math.Max(0, buff.End - Scene.Now);
    }
    /// <summary>同一个实际原实例用于增删通知及入场快照。</summary><param name="buff">原Buff实例。</param><param name="removed">是否已移除。</param>
    private MonsterBuffData ToData(MoshentaBuffInstance buff, bool removed)
    {
        MonsterBuffData data = new MonsterBuffData { BuffInstanceId = buff.InstanceId, BuffId = buff.Id, Removed = removed, RemainingMs = -1 };
        if (buff.Caster != null) data.CasterSpawnId = buff.Caster.SpawnId;
        if (Actor != null) data.TargetSpawnId = Actor.SpawnId;
        else data.TargetRoleId = Role.mainRoleInfo.BaseInfo.RoleId;
        if (buff.End != long.MaxValue) data.RemainingMs = (int)Math.Max(0, buff.End - Scene.Now);
        return data;
    }
    /// <summary>原技能结束触发，供4024自杀链使用。</summary>
    internal void SkillEnded()
    {
        // 使用原实例引用快照，触发结果可以删除本容器。
        List<MoshentaBuffInstance> current = new List<MoshentaBuffInstance>(buffs);
        for (int index = 0; index < current.Count; index++) Trigger(current[index], 15);
    }
    /// <summary>推进到期和光环，单位为场景逻辑毫秒。</summary><param name="now">当前场景时间。</param>
    internal void Update(long now)
    {
        if (Role != null && Role.mainRoleInfo.BaseInfo.CurrHp <= 0) { Clear(); return; }
        // 到期触发可能杀死宿主并清空列表，每次重新定位原对象。
        List<MoshentaBuffInstance> current = new List<MoshentaBuffInstance>(buffs);
        for (int index = 0; index < current.Count; index++)
        {
            // 当前仍在容器中的原实例。
            MoshentaBuffInstance buff = current[index];
            int liveIndex = buffs.IndexOf(buff);
            if (liveIndex < 0) continue;
            if (now >= buff.End) { RemoveAt(liveIndex, true); continue; }
            for (int effectIndex = 0; effectIndex < buff.Effects.Count; effectIndex++)
            {
                // 本闭包的周期效果是每0.2秒检查进入范围的角色。
                XElement effect = buff.Effects[effectIndex];
                if (MoshentaSkillCatalog.I(effect, "TriggerTiming") != 2) continue;
                long next;
                if (buff.Next.TryGetValue(effectIndex, out next) && now < next) continue;
                buff.Next[effectIndex] = now + (long)(MoshentaSkillCatalog.F(effect, "IntervalTime") * 1000);
                // 原A7DianKuangCheck检查指定符号家族，条件正文由原Lua执行。
                List<OnlineRole> players = Scene.Players;
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    // 同实例活着且位于原一米光环内的玩家。
                    OnlineRole player = players[playerIndex];
                    if (player.mainRoleInfo.BaseInfo.CurrHp <= 0) continue;
                    if (Vector3.Distance(Position, player.mainRoleInfo.BaseInfo.Pos.ToVector3()) > MoshentaSkillCatalog.F(effect, "BuffRange")) continue;
                    MoshentaActor executor = buff.Caster;
                    if (executor == null) executor = Actor;
                    if (!executor.Skills.Calculator.BuffCondition(effect, MoshentaSkillRuntime.PlayerSnapshot(Scene, player))) continue;
                    int fullId = int.Parse(MoshentaSkillCatalog.S(effect, "EffectParam"), CultureInfo.InvariantCulture);
                    if (buff.Caster == null) MoshentaSkillRuntime.AddScenePlayerBuff(Scene, player, fullId);
                    else MoshentaSkillRuntime.AddPlayerBuff(buff.Caster, player, fullId);
                }
            }
        }
    }
    /// <summary>当前宿主位置，供原光环和召唤使用。</summary>
    private Vector3 Position { get { if (Actor != null) return Actor.Position; return Role.mainRoleInfo.BaseInfo.Pos.ToVector3(); } }
    /// <summary>按实际触发时机解释原Buff效果闭包。</summary><param name="buff">原实例。</param><param name="timing">原BuffTriggerTimeType。</param>
    private void Trigger(MoshentaBuffInstance buff, int timing)
    {
        // 次数寿命按一次触发器派发计数，不按每个叶子重复消耗。
        bool triggered = false;
        for (int index = 0; index < buff.Effects.Count; index++)
        {
            // 只有当前触发点对应的原叶子才会运行。
            XElement effect = buff.Effects[index];
            if (MoshentaSkillCatalog.I(effect, "TriggerTiming") != timing) continue;
            triggered = true;
            int type = MoshentaSkillCatalog.I(effect, "EffectType");
            string parameter = MoshentaSkillCatalog.S(effect, "EffectParam");
            if (type == 2) AttributeGenes(buff, index, parameter);
            else if (type == 4)
            {
                // 141/142只免被动位移和僵直，不能映射会免Root/Stun的Gene.SuperArmor。
                // 不可受击仍有独立提前判定，Gene供既有非RO伤害入口。
                if (parameter.Contains("146")) MoshentaGeneBridge.Add(genes, buff, index + 1, GeneAttributeType.DamageTaken, -100, false, GeneState.None, () => RemainingMilliseconds(buff));
            }
            else if (type == 7) Remove(int.Parse(parameter, CultureInfo.InvariantCulture));
            else if (type == 11)
            {
                // 原Lua结果返回明确动作；Suicide对零HP机制也发真实死亡事件。
                string action = buff.Caster.Skills.Calculator.BuffResult(effect, MoshentaDamageSnapshot.Capture(buff.Caster), Capture());
                if (action == "suicide" && Actor != null) Scene.KillActor(Actor);
            }
            else if (type == 14 && parameter == "6=-10000")
            {
                if (Actor != null) Scene.KillActor(Actor);
            }
            else if (type == 15)
            {
                if (Actor != null) Actor.Skills.Cancel();
            }
            else if (type == 35)
            {
                // 原Type35七字段：模板/寿命种类/秒/怪种类/仇恨/施法者/属性来源。
                string[] values = parameter.Split('=');
                MoshentaActor summoned = Scene.Summon(buff.Caster, int.Parse(values[0]), Position, buff.Caster.Facing, (int)(float.Parse(values[2], CultureInfo.InvariantCulture) * 1000));
                if (values[6] == "1") summoned.SummonSnapshot = MoshentaDamageSnapshot.Capture(buff.Caster, true);
            }
            // 23免疫由Add前判定；26缩放/半透及33连接FX属于客户端表现，原数据完整保留。
        }
        if (triggered && buff.RemainingTriggers > 0)
        {
            buff.RemainingTriggers--;
            int liveIndex = buffs.IndexOf(buff);
            if (buff.RemainingTriggers == 0 && liveIndex >= 0) RemoveAt(liveIndex, true);
        }
    }
    /// <summary>将实际属性叶子映射到已有Gene；公式额外属性仍保留原枚举。</summary><param name="buff">源Buff。</param><param name="index">原叶子编号。</param><param name="parameter">原属性编号与值序列。</param>
    private void AttributeGenes(MoshentaBuffInstance buff, int index, string parameter)
    {
        if (parameter == "67=-8000|68=-8000") MoshentaGeneBridge.Add(genes, buff, index + 1, GeneAttributeType.DamageTaken, 80, false, GeneState.None, () => RemainingMilliseconds(buff));
        if (parameter == "27=1000|36=1000")
        {
            MoshentaGeneBridge.Add(genes, buff, 1, GeneAttributeType.AtkExternalMin, 1000, false, GeneState.None, () => RemainingMilliseconds(buff));
            MoshentaGeneBridge.Add(genes, buff, 2, GeneAttributeType.AtkExternalMax, 1000, false, GeneState.None, () => RemainingMilliseconds(buff));
            MoshentaGeneBridge.Add(genes, buff, 3, GeneAttributeType.AtkInternalMin, 1000, false, GeneState.None, () => RemainingMilliseconds(buff));
            MoshentaGeneBridge.Add(genes, buff, 4, GeneAttributeType.AtkInternalMax, 1000, false, GeneState.None, () => RemainingMilliseconds(buff));
        }
    }
}
