using System.Globalization;
using cfg;

/// <summary>把魔剑士武功的权威等级数值转换为技能面板中的中文说明。</summary>
internal static class MjsSkillDescription
{
    /// <summary>用于展示实际效果数值与下一级预览的文字。</summary>
    /// <param name="current">当前可展示的技能等级行。</param>
    /// <param name="next">可提升后使用的下一等级行；满级时为空。</param>
    /// <param name="learned">角色已学习当前技能时才给出满级或下一级说明。</param>
    /// <returns>可直接交给技能面板着色的原始说明。</returns>
    internal static string Build(SkillEffectInfo current, SkillEffectInfo next, bool learned)
    {
        // 技能 ID 决定描述用哪个模板，各项战斗数值都取 Luban 等级行。
        bool barrier = current.SkillId == SkillCtrl.BlueBarrierSkillId;
        // 当前技能的完整效果，与相同等级的服务端行为一一对应。
        string description;
        if (barrier) description = DescribeBarrier(current) + DescribeElementDamage(current);
        else description = DescribeTornado(current);
        if (!learned) return description;
        if (next == null) return description + "\n\n已达技能最高等级（" + current.Level + "级）";
        // 下一等级只展示核心成长项并注明角色等级要求，完整的基因说明保留在当前等级。
        string growth;
        if (barrier)
        {
            growth = "普攻伤害提升" + Percent(next.BasicAttackBonusPermyriad) + "，护盾容量为最大生命的" +
                Percent(next.ShieldAbsorbPermyriad) + "。";
        }
        else
        {
            growth = "基础伤害 " + Damage(next) + "，命中扣除" + next.TrueQiDamage + "点真气。";
        }
        return description + "\n\n下一级（角色" + next.RequireRoleLevel + "级可提升）\n" + growth + DescribeElementDamage(next);
    }

    /// <summary>用武功当前和下一成长行显示真实调息、魔力和已接入的伤害成长。</summary>
    /// <param name="definition">技能主表文本与本技能身份。</param>
    /// <param name="current">当前可显示的成长配置。</param>
    /// <param name="next">可升级时的下一等级效果，满级时为空。</param>
    /// <param name="learned">角色已经习得这门武功时才显示提升预览或满级状态。</param>
    /// <returns>含目标等级实际效果的武功描述。</returns>
    internal static string BuildWeapon(SkillInfo definition, SkillEffectInfo current, SkillEffectInfo next, bool learned)
    {
        // 刀刃风暴的每段系数在当前技能等级行生效，随等级替换主表中的旧静态数字。
        bool bladeStorm = current.SkillId == 10110;
        // 其他武功继续使用原招式说明，实际增长的消耗和调息从等级行读出。
        string description = definition.Desc;
        if (bladeStorm)
            description = "挥动双刃回旋，连续斩击周围敌人" + current.HitCount + "次。每段技能伤害系数为" +
                Percent(current.DamagePermyriad) + "。";
        description += DescribeElementDamage(current);
        if (!learned) return description;
        if (next == null) return description + "\n\n已达技能最高等级（" + current.Level + "级）";
        // 下一行和服务器升级门槛同源，魔力是每次正式提交的消耗。
        string preview = "\n\n下一级（角色" + next.RequireRoleLevel + "级可提升）\n" +
            "调息 " + Seconds(next.CooldownMs) + " 秒，消耗魔力 " + next.MpCost + " 点。";
        if (bladeStorm) preview += "\n每段技能伤害系数 " + Percent(next.DamagePermyriad) + "。";
        return description + preview + DescribeElementDamage(next);
    }

    /// <summary>生成一次苍蓝屏障当前等级的说明。</summary>
    /// <param name="effect">屏障当前等级的全部效果。</param>
    /// <returns>包含盾量、返还条件与触发效果的正文。</returns>
    private static string DescribeBarrier(SkillEffectInfo effect)
    {
        return "解除自身控制，展开持续" + Seconds(effect.ShieldDurationMs) + "秒的屏障，容量为最大生命的" +
            Percent(effect.ShieldAbsorbPermyriad) + "。屏障自然到期时，按剩余盾量减少" +
            Seconds(effect.ShieldRefundMinMs) + "–" + Seconds(effect.ShieldRefundMaxMs) + "秒调息。\n" +
            "力强：习得后，普攻伤害提高" + Percent(effect.BasicAttackBonusPermyriad) + "。\n" +
            "风冻：攻击被屏障吸收时，攻击者每层攻速下降" + Percent(effect.FrostSlowPermyriad) +
            "，持续" + Seconds(effect.FrostSlowDurationMs) + "秒。达到" + effect.FrostMaxStack +
            "层时转为定身" + Seconds(effect.FrostRootMs) + "秒。\n" +
            "霸体：施放后获得" + Seconds(effect.SuperArmorMs) + "秒霸体。\n" +
            "盾爆：屏障自然到期或被打破时，对周围" + Meters(effect.ShieldBurstRadiusCm) + "米内敌人造成" +
            Percent(effect.ShieldBurstDamagePermyriad) + "外功攻击的基础伤害，并击飞可受控目标" +
            Seconds(effect.ShieldBurstControlMs) + "秒。";
    }

    /// <summary>生成一次龙卷斩当前等级的说明。</summary>
    /// <param name="effect">龙卷斩当前等级的全部效果。</param>
    /// <returns>包含真气、双目标与护体效果的正文。</returns>
    private static string DescribeTornado(SkillEffectInfo effect)
    {
        return "以全部剩余真气催动前方龙卷，在落点" + Meters(effect.RadiusCm) + "米范围内命中最多" +
            effect.MaxTargets + "名敌人，造成" + Damage(effect) + "。" + DescribeElementDamage(effect) + "\n" +
            "苍云斩：龙卷斩增伤" + Percent(effect.TornadoDamageBonusPermyriad) + "，每消耗" + effect.TrueQiDamageStep +
            "点真气再增加" + Percent(effect.TrueQiStepBonusPermyriad) + "。习得后，主动技能按目标真气亏空比例最多增伤" +
            Percent(effect.MissingTrueQiBonusPermyriad) + "。\n" +
            "破强：命中扣除" + effect.TrueQiDamage + "点真气。此后" + Seconds(effect.ChiVulnDurationMs) +
            "秒内，目标受到的真气伤害提高" + Percent(effect.ChiVulnPermyriad) + "。\n" +
            "风散：同时命中两名敌人时，本次伤害降低" + Percent(effect.DualTargetReducePermyriad) +
            "，自身获得" + Seconds(effect.DualTargetSuperArmorMs) + "秒霸体和" +
            Seconds(effect.DualTargetReduceMs) + "秒内外功减伤" + Percent(effect.DualTargetProtectionPermyriad) + "。\n" +
            "护体：施放后基础内外防提高" + Percent(effect.GuardDefensePermyriad) + "，在" +
            Seconds(effect.GuardRecoverMs) + "秒内逐渐恢复。";
    }

    /// <summary>用技能等级行构成外功伤害表达。</summary>
    /// <param name="effect">本级配置的外攻系数和额外固定伤害。</param>
    /// <returns>不包含临时增益的基础伤害文字。</returns>
    private static string Damage(SkillEffectInfo effect)
    {
        return (effect.DamagePermyriad / 100.0).ToString("0.00", CultureInfo.InvariantCulture)
            + "%外功攻击+" + effect.FlatDamage + "点伤害";
    }

    /// <summary>显示本级附加的属攻倍率及固定元素伤害，名称沿用角色高级属性页。</summary>
    /// <param name="effect">当前级或下一级的权威技能效果行。</param>
    /// <returns>附加伤害说明行，无对应元素时为空。</returns>
    private static string DescribeElementDamage(SkillEffectInfo effect)
    {
        // 只扫描属性攻击组，不使用后续的穿透或抵抗名称。
        for (int index = 0; index < RoleAttributeViewData.AdvancedGroupSizes[0]; index++)
        {
            // 高级属性第一组依次为五系攻击，文字与角色属性面板保持一致。
            RoleAttributeViewData.AdvancedEntry entry = RoleAttributeViewData.AdvancedEntries[index];
            if (entry.Id != effect.ElementAttackAttributeId) continue;
            return "\n附加 " + Percent(effect.ElementDamagePermyriad) + entry.Label + " + " +
                effect.ElementFlatDamage + "点" + entry.Label[0] + "属性伤害。";
        }
        return string.Empty;
    }

    /// <summary>将万分比显示为玩家可读的百分比。</summary>
    /// <param name="value">表格万分比。</param>
    /// <returns>保留实际需要的至多两位小数及百分号。</returns>
    private static string Percent(int value)
    {
        return (value / 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>将用于服务器步进的毫秒数展示为秒。</summary>
    /// <param name="value">表格毫秒数。</param>
    /// <returns>保留分数秒的文字。</returns>
    private static string Seconds(int value)
    {
        return (value / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>将范围表的厘米数换成技能说明中的米。</summary>
    /// <param name="value">表格厘米数。</param>
    /// <returns>保留分数米的文字。</returns>
    private static string Meters(int value)
    {
        return (value / 100.0).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
