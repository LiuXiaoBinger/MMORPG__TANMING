using System;
using cfg;

/// <summary>把魔剑士两门技能的等级行转换成实际施加的状态、属性与真气效果。</summary>
internal static class MjsSkillGenes
{
    /// <summary>苍蓝屏障的技能主表编号，决定专属起手和命中效果。</summary>
    internal const int BarrierSkillId = 10120;
    /// <summary>龙卷斩的技能主表编号，决定专属起手和真气命中效果。</summary>
    internal const int TornadoSkillId = 10130;
    /// <summary>可由苍蓝屏障解掉的状态，其他来源及无敌等效果继续按自身寿命运作。</summary>
    private const GeneState BreakableControl = GeneState.Stun | GeneState.Root | GeneState.Silence | GeneState.Knockdown;
    /// <summary>屏障施放后的短时霸体身份。</summary>
    private const int BarrierArmor = -1012001;
    /// <summary>屏障吸收时反施给攻击者的风冻累计身份。</summary>
    private const int Frost = -1012002;
    /// <summary>风冻达到设定层数时的定身身份。</summary>
    private const int Frozen = -1012003;
    /// <summary>屏障爆炸触发的击飞控制身份，使用现有击倒动作限制。</summary>
    private const int BurstControl = -1012004;
    /// <summary>龙卷斩命中后施加的真气易伤身份。</summary>
    private const int QiVulnerability = -1013001;
    /// <summary>两目标时获得的霸体身份，与屏障霸体各自计时。</summary>
    private const int SplitArmor = -1013002;
    /// <summary>两目标时获得的内外功减伤身份。</summary>
    private const int SplitProtection = -1013003;
    /// <summary>护体施放后单独衰减的外防身份。</summary>
    private const int OuterGuard = -1013004;
    /// <summary>护体施放后单独衰减的内防身份。</summary>
    private const int InnerGuard = -1013005;

    /// <summary>屏障正式提交时解除控制并登记自己的限时霸体。</summary>
    /// <param name="role">已提交施放的角色。</param>
    /// <param name="effect">当前角色学习等级对应的屏障效果。</param>
    internal static void BeginBarrier(OnlineRole role, SkillEffectInfo effect)
    {
        role.Gene.Runtime.DispelStates(BreakableControl);
        AddState(role.Gene.Runtime, BarrierArmor, "苍蓝屏障·霸体", GeneState.SuperArmor, effect.SuperArmorMs, role.GetID());
    }

    /// <summary>龙卷斩正式提交时登记两条按时间衰减的基础防御。</summary>
    /// <param name="role">成功施放的角色。</param>
    /// <param name="effect">本次施放的龙卷斩等级行。</param>
    internal static void BeginTornado(OnlineRole role, SkillEffectInfo effect)
    {
        // 两条防御按各自裸值增加同样百分点，撤回时只影响护体这一来源。
        float percent = effect.GuardDefensePermyriad / 100f;
        AddStat(role.Gene.Runtime, OuterGuard, "护体·外防", GeneAttributeType.DefExternal,
            percent, effect.GuardRecoverMs, role.GetID(), 1, true);
        AddStat(role.Gene.Runtime, InnerGuard, "护体·内防", GeneAttributeType.DefInternal,
            percent, effect.GuardRecoverMs, role.GetID(), 1, true);
    }

    /// <summary>每次盾池实际吸收后给攻击来源增加一层风冻，满层换成定身。</summary>
    /// <param name="attacker">实际打中护盾的战斗实体基因容器。</param>
    /// <param name="effect">开盾时快照的等级行。</param>
    /// <param name="source">施加风冻的角色身份，区分不同角色的叠层。</param>
    /// <param name="attackerIsBoss">首领不因风冻满层进入定身。</param>
    internal static void ApplyFrost(GeneRuntime attacker, SkillEffectInfo effect, ulong source, bool attackerIsBoss)
    {
        if (attacker == null || effect.FrostMaxStack <= 0 || effect.FrostSlowDurationMs <= 0) return;
        // 同一来源重复施加时由 GeneRuntime 增加并刷新原节点，读取累计的真实层数。
        SkillAttributeGene slow = new SkillAttributeGene(GeneAttributeType.AttackSpeedSlow)
        {
            GeneID = Frost, DisplayName = "风冻", Value = effect.FrostSlowPermyriad / 100f,
            DurationSeconds = effect.FrostSlowDurationMs / 1000f, AllowsStacking = true,
            MaxStack = effect.FrostMaxStack
        };
        // 返回现存的效果实例，不从伤害次数另外推算层数。
        GeneBase applied = attacker.AddGene(slow, source);
        if (applied.StackCount < effect.FrostMaxStack) return;
        if (attackerIsBoss) return;
        attacker.RemoveGene(applied);
        AddState(attacker, Frozen, "风冻·定身", GeneState.Root, effect.FrostRootMs, source);
    }

    /// <summary>Boss 的控制门禁留在命中调用方，仅向可受控的目标添加击飞控制。</summary>
    /// <param name="target">盾爆命中的活体目标。</param>
    /// <param name="effect">此次盾爆对应的技能等级行。</param>
    /// <param name="source">引爆屏障的玩家。</param>
    internal static void ApplyBurstControl(GeneRuntime target, SkillEffectInfo effect, ulong source)
    {
        AddState(target, BurstControl, "苍蓝屏障·击飞", GeneState.Knockdown, effect.ShieldBurstControlMs, source);
    }

    /// <summary>龙卷斩命中后先按已有易伤扣真气，再应用本次的易伤。</summary>
    /// <param name="target">本次命中的实体基因容器。</param>
    /// <param name="effect">本次龙卷斩的技能等级行。</param>
    /// <param name="caster">施放角色，提供自身与队友的秘笈真气增伤。</param>
    internal static void HitTornado(GeneRuntime target, SkillEffectInfo effect, OnlineRole caster = null)
    {
        // 从目标实体自己的权威属性读取资源，真气伤害不进入生命结算。
        IAttributeComponent attributes = target.Owner.AttrComp;
        // 使用当前已存在的易伤，新的易伤只影响之后的命中。
        double vulnerability = target.CalculateAttribute(GeneAttributeType.TrueQiDamageTaken, 0);
        // 取整后钳制到目标余额，确保只扣到零且使用现有资源同步。
        // 清心诀分别提供自身和全队加成，施法者也属于全队。
        double bonus = 0;
        if (caster != null)
            bonus = caster.AttrComp.CalculateAttribute(GeneAttributeType.TrueQiDamageBonus, 0)
                + caster.AttrComp.CalculateAttribute(GeneAttributeType.PartyTrueQiDamageBonus, 0)
                + TeamManager.Instance.OtherMemberBonus(caster, GeneAttributeType.PartyTrueQiDamageBonus);
        int amount = Math.Min(attributes.CurrTrueQi, (int)(effect.TrueQiDamage * (1 + vulnerability / 100) * (1 + bonus / 100)));
        if (amount > 0) attributes.TryConsumeTrueQi(amount);
        // 同一技能的易伤刷新为当前数值，多个施加者也不会重复相乘。
        AddStat(target, QiVulnerability, "破强·真气易伤", GeneAttributeType.TrueQiDamageTaken,
            effect.ChiVulnPermyriad / 100f, effect.ChiVulnDurationMs, 0);
    }

    /// <summary>恰有两只目标通过同一范围判定时登记自保效果。</summary>
    /// <param name="role">完成双目标命中的施法者。</param>
    /// <param name="effect">本次龙卷斩使用的完整等级行。</param>
    internal static void ApplySplitProtection(OnlineRole role, SkillEffectInfo effect)
    {
        AddState(role.Gene.Runtime, SplitArmor, "风散·霸体", GeneState.SuperArmor, effect.DualTargetSuperArmorMs, role.GetID());
        AddStat(role.Gene.Runtime, SplitProtection, "风散·伤害减免", GeneAttributeType.PhysicalDamageReduction,
            effect.DualTargetProtectionPermyriad / 100f, effect.DualTargetReduceMs, role.GetID());
    }

    /// <summary>风冻减速换算为动作时长倍率，25% 降速对应 4/3 的间隔。</summary>
    /// <param name="runtime">正在进入攻击动作的实体。</param>
    /// <returns>大于等于 1 的时间倍率。</returns>
    internal static double AttackTimeScale(GeneRuntime runtime)
    {
        // 降速最高计算到 80%，避免配置异常时攻击间隔除以零。
        double slow = Math.Min(80, runtime.CalculateAttribute(GeneAttributeType.AttackSpeedSlow, 0));
        return 100 / (100 - slow);
    }

    /// <summary>处理内外功结果上的风散减伤，既有元素与装备减伤保持自己的结算。</summary>
    /// <param name="role">收到内外功伤害的角色。</param>
    /// <param name="incoming">已经按原伤害公式或装备效果处理的伤害点数。</param>
    /// <returns>应用风散后送入屏障的伤害点数。</returns>
    internal static int ReducePhysicalDamage(OnlineRole role, int incoming)
    {
        // 同一角色的临时减伤值来自其基因组件，不直接改写装备属性。
        double percent = role.Gene.Runtime.CalculateAttribute(GeneAttributeType.PhysicalDamageReduction, 0);
        // 强控读取霸体抵消后的有效状态；浮空沿用服务端击倒状态。
        GeneState control = GeneState.Stun | GeneState.Root | GeneState.Knockdown;
        double controlledReduction = 0;
        if ((role.AttrComp.States & control) != 0)
            controlledReduction = role.AttrComp.CalculateAttribute(GeneAttributeType.ControlledDamageReduction, 0);
        return (int)(incoming * (1 - percent / 100) * Math.Max(0, 1 - controlledReduction / 100));
    }

    /// <summary>按已学技能等级处理玩家命中时的力强或目标真气亏损增伤。</summary>
    /// <param name="role">实际造成伤害的角色。</param>
    /// <param name="target">损失真气作为增伤参照的实际目标。</param>
    /// <param name="skillId">导致本次命中的技能主表编号。</param>
    /// <param name="damage">进入原伤害管线后的有效伤害。</param>
    /// <returns>加成后的伤害点数。</returns>
    internal static int ApplyLearnedDamageBonus(OnlineRole role, IAttributeComponent target, int skillId, int damage)
    {
        // 命中技能的主表定义提供普通攻击标记，快捷键与动作编号不参与增伤归类。
        SkillInfo definition = LubanMgr.Instance.GetSkillInfoById(skillId);
        if (definition == null) return damage;
        // 权威学习组件提供当前有效等级，玩家请求没有覆盖它的入口。
        RoleSkillComponent skills = role.GetComponent<RoleSkillComponent>();
        if (definition.IsBasicAttack)
        {
            // 力强只为已经习得屏障的普通攻击计算其当前等级加成。
            SkillEffectInfo barrier = skills.FindLearnedEffect(BarrierSkillId);
            if (barrier != null) return ScaleDamage(damage, barrier.BasicAttackBonusPermyriad);
            return damage;
        }
        // 只对主动武学补苍云斩的常驻增幅，滑步、普攻和无来源伤害不参加。
        if (definition.Type != 2 || target.MaxTrueQi <= 0) return damage;
        // 真气亏空占上限的比例在扣减本次命中的真气之前读取。
        SkillEffectInfo tornado = skills.FindLearnedEffect(TornadoSkillId);
        if (tornado == null) return damage;
        // 目标已有缺口越大，增伤越接近等级行的封顶值。
        int bonus = (int)((long)tornado.MissingTrueQiBonusPermyriad * (target.MaxTrueQi - target.CurrTrueQi) / target.MaxTrueQi);
        return ScaleDamage(damage, bonus);
    }

    /// <summary>按万分比加成或衰减一次既有伤害值。</summary>
    /// <param name="damage">待修正的非负伤害点数。</param>
    /// <param name="bonusPermyriad">万分比变动，负数是减伤。</param>
    /// <returns>以长整数计算后取整的伤害点数。</returns>
    internal static int ScaleDamage(int damage, int bonusPermyriad)
    {
        return (int)Math.Min(int.MaxValue, (long)damage * (10000L + bonusPermyriad) / 10000L);
    }

    /// <summary>按本次等级行挂载持续的控制或霸体状态。</summary>
    /// <param name="runtime">实际受到状态的基因容器。</param>
    /// <param name="id">状态的稳定运行编号。</param>
    /// <param name="name">客户端显示用的效果名称。</param>
    /// <param name="states">本次施加的状态位。</param>
    /// <param name="milliseconds">状态持续毫秒数，零时禁用。</param>
    /// <param name="source">本次实际施加者或状态归属编号。</param>
    private static void AddState(GeneRuntime runtime, int id, string name, GeneState states, int milliseconds, ulong source)
    {
        if (milliseconds <= 0) return;
        if (states != GeneState.SuperArmor && (runtime.GetStates() & GeneState.SuperArmor) != 0) return;
        runtime.AddGene(new StateGene
        {
            GeneID = id, DisplayName = name, States = states, DurationSeconds = milliseconds / 1000f
        }, source);
    }

    /// <summary>向当前宿主挂载数值型技能效果，刷新时用本次等级行替换旧寿命。</summary>
    /// <param name="runtime">处于真实战斗场景中的基因容器。</param>
    /// <param name="id">兼顾显示和运行查找的效果编号。</param>
    /// <param name="name">现有效果列表使用的名称。</param>
    /// <param name="attribute">受影响的属性。</param>
    /// <param name="value">直接属性值或百分点。</param>
    /// <param name="milliseconds">持续毫秒数，零时禁用。</param>
    /// <param name="source">独立的施加来源；单一不叠加效果使用零。</param>
    /// <param name="valueType">0 为直接数值，1 为基础值变化百分点。</param>
    /// <param name="decay">按寿命线性将数值降回零。</param>
    private static void AddStat(GeneRuntime runtime, int id, string name, GeneAttributeType attribute, float value,
        int milliseconds, ulong source, int valueType = 0, bool decay = false)
    {
        if (milliseconds <= 0 || value <= 0) return;
        runtime.RemoveGene(id, source);
        runtime.AddGene(new SkillAttributeGene(attribute, decay)
        {
            GeneID = id, DisplayName = name, Value = value, ValueType = valueType,
            DurationSeconds = milliseconds / 1000f
        }, source);
    }
}
