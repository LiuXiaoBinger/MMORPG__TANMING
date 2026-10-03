using System.Collections.Generic;
using cfg;

/// <summary>
/// 将 Luban 的技能效果和职业表现表装配成客户端长期使用的技能核心。
/// 配置查找仅在技能列表同步或服务器开始广播时执行，不进入逐帧状态更新。
/// </summary>
public static class MSkillFactory
{
    /// <summary>按角色已学等级查找效果，并装配该职业默认武器的技能核心。</summary>
    /// <param name="skillId">角色技能列表下发的旧技能主表 ID。</param>
    /// <param name="level">角色已学习的正数等级。</param>
    /// <param name="professionId">角色基础信息中的职业 ID。</param>
    /// <returns>效果表和职业匹配时返回核心，配置缺失时返回空。</returns>
    public static MSkillCore CreateCore(int skillId, int level, int professionId)
    {
        if (skillId <= 0 || level <= 0 || professionId <= 0)
        {
            return null;
        }
        // 本等级效果配置，所有权威 CD 与费用均来自该行而非旧 SkillInfo 字段。
        SkillEffectInfo effect = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(
            skillId, level);
        return CreateCoreFromEffect(skillId, effect, professionId);
    }

    /// <summary>按服务器广播的效果 ID 装配其他角色的技能核心。</summary>
    /// <param name="skillId">广播中的旧技能主表 ID。</param>
    /// <param name="effectId">服务器按已学等级解析出的效果 ID。</param>
    /// <param name="professionId">施法角色职业 ID。</param>
    /// <returns>效果归属与职业匹配时返回核心，配置缺失时返回空。</returns>
    public static MSkillCore CreateCoreByEffect(int skillId, int effectId,
        int professionId)
    {
        if (skillId <= 0 || effectId <= 0 || professionId <= 0)
        {
            return null;
        }
        // 按效果主键查到的行必须再次核对技能 ID，避免错表播放其他技能。
        Dictionary<int, SkillEffectInfo> effects =
            LubanMgr.Instance.GetSkillEffectInfos();
        SkillEffectInfo effect;
        if (effects == null || !effects.TryGetValue(effectId, out effect) ||
            effect == null || effect.SkillId != skillId)
        {
            return null;
        }
        return CreateCoreFromEffect(skillId, effect, professionId);
    }

    /// <summary>验证旧技能与职业外键，并结合表现表生成不持有 Luban 可变状态的核心。</summary>
    /// <param name="skillId">需要装配的旧技能 ID。</param>
    /// <param name="effect">当前等级对应的效果配置。</param>
    /// <param name="professionId">施法角色职业 ID。</param>
    /// <returns>成功装配的技能核心；旧技能或效果不存在时为空。</returns>
    private static MSkillCore CreateCoreFromEffect(int skillId,
        SkillEffectInfo effect, int professionId)
    {
        if (effect == null || effect.SkillId != skillId || effect.Level <= 0)
        {
            return null;
        }
        // 原技能行与职业行共同保证本次效果属于施法者当前职业。
        SkillInfo skill = LubanMgr.Instance.GetSkillInfoById(skillId);
        ProfessionInfo profession = LubanMgr.Instance.GetProfessionInfoById(
            professionId);
        if (skill == null || profession == null || skill.JobId != professionId)
        {
            return null;
        }

        // 表现配置可以缺失；此时仍保留权威效果配置，客户端不预播动画。
        List<MSkillData> presentations = FindPresentations(effect.Id,
            profession);
        // 第一段提供旧接口的默认表现；释放时由核心按 RO 的使用次数选择段号。
        string clipLocation = string.Empty;
        // 尚无表现行时保持零时长，不伪造动画。
        float animationDurationSeconds = 0f;
        if (presentations.Count > 0)
        {
            clipLocation = presentations[0].AnimationLocation;
            animationDurationSeconds = presentations[0].DurationSeconds;
        }
        // 初始核心持有权随后交给技能组件的字典。
        MSkillCore core = MCommonObjectPool<MSkillCore>.Get();
        core.Initialize(skillId, effect.Level, effect.Id,
            effect.TargetMode, effect.CooldownMs * 0.001f,
            effect.CooldownGroupId, effect.GroupCooldownMs * 0.001f,
            effect.CastMs * 0.001f, effect.MpCost, clipLocation,
            animationDurationSeconds, presentations);
        MListPool<MSkillData>.Release(presentations);
        return core;
    }

    /// <summary>按效果、职业和模型名装配默认武器的动作列表，按变体序号对应 RO 的武器动作数据。</summary>
    /// <param name="effectId">当前等级效果配置 ID。</param>
    /// <param name="profession">施法角色职业配置，用于验证其表现表和模型资源。</param>
    /// <returns>从第零段起排序的动作列表，未配置时返回空列表。</returns>
    private static List<MSkillData> FindPresentations(int effectId,
        ProfessionInfo profession)
    {
        // 仅收集当前职业默认武器的数据，不推测尚未接入的武器属性。
        List<MSkillData> result = MListPool<MSkillData>.Get();
        if (profession == null)
        {
            return result;
        }
        // 职业绑定的表现表定义正式模型键，不从技能 Action 数字猜资源路径。
        PresentTable present = LubanMgr.Instance.GetPresentTableById(
            profession.PresentTableID);
        if (present == null || string.IsNullOrEmpty(present.Prefab))
        {
            return result;
        }
        // 表现行快照；该查询发生于技能装配而非逐帧逻辑。
        Dictionary<int, SkillPresentationInfo> presentations =
            LubanMgr.Instance.GetSkillPresentationInfos();
        if (presentations == null)
        {
            return result;
        }
        // 字典值快照允许按照项目要求使用索引遍历。
        List<SkillPresentationInfo> rows = MListPool<SkillPresentationInfo>.Get();
        try
        {
            rows.AddRange(presentations.Values);
            // index 为表现行引用快照的零起始下标，收集顺序在结束后按动作序号统一排序。
            for (int index = 0; index < rows.Count; index++)
            {
                // 当前候选行必须与实际职业模型及默认武器/变体精确匹配。
                SkillPresentationInfo row = rows[index];
                if (row != null && row.EffectId == effectId &&
                    row.ProfessionId == profession.Id &&
                    row.ModelKey == present.Prefab && row.WeaponType == 0 &&
                    row.VariantIndex >= 0)
                {
                    // 本表现行由最终核心独占，直到核心的最后持有者解除引用。
                    MSkillData data = MCommonObjectPool<MSkillData>.Get();
                    data.Initialize(row.VariantIndex, row.ClipLocation,
                        row.DurationMs * 0.001f, row.SkillStateTimeMs * 0.001f);
                    result.Add(data);
                }
            }
        }
        finally
        {
            MListPool<SkillPresentationInfo>.Release(rows);
        }
        result.Sort(ComparePresentationOrder);
        return result;
    }

    /// <summary>按表现表的动作序号排序，使释放次数取模得到稳定的动作顺序。</summary>
    /// <param name="left">排序比较的左侧动作数据。</param>
    /// <param name="right">排序比较的右侧动作数据。</param>
    /// <returns>左动作应排在前面时返回负数，顺序相同时返回零。</returns>
    private static int ComparePresentationOrder(MSkillData left, MSkillData right)
    {
        return left.VariantIndex.CompareTo(right.VariantIndex);
    }

    /// <summary>借出一个空施法实例；调用方在成功或失败终止时均须归还。</summary>
    /// <returns>当前动作槽独占的实例。</returns>
    public static MSkill CreateSkill()
    {
        return MCommonObjectPool<MArtSkill>.Get();
    }

    /// <summary>清理单次施法并归还池，核心引用随清理解除。</summary>
    /// <param name="skill">已从动作槽或旧施法列表移除的实例。</param>
    public static void ReturnSkill(MSkill skill)
    {
        if (skill == null)
        {
            return;
        }
        skill.Reset();
        skill.Recycle();
    }

    /// <summary>核心最后持有者调用此入口，将动作段与预测记录一并归还。</summary>
    /// <param name="core">持有数已降至零的技能核心。</param>
    public static void ReleaseCore(MSkillCore core)
    {
        core.Reset();
        MCommonObjectPool<MSkillCore>.Release(core);
    }
}
