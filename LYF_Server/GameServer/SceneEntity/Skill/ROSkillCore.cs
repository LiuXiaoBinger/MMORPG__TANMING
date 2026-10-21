using cfg;

/// <summary>对应 RO 主动技能核心；引用本项目主表和等级效果，供组件复用同一等级配置。</summary>
public sealed class ROSkillCore
{
    /// <summary>稳定技能主表引用，职业、类别和滑行取消规则来自该行。</summary>
    private readonly SkillInfo skill_table_;
    /// <summary>该学习等级的效果引用，冷却、法力消耗和持续时间以毫秒计。</summary>
    private readonly SkillEffectInfo skill_effect_data_;

    /// <summary>装载已匹配技能和等级的两份只读配置。</summary>
    /// <param name="skill_table">角色技能对应的稳定主表行。</param>
    /// <param name="skill_effect_data">由服务端已学等级解析的唯一效果行。</param>
    public ROSkillCore(SkillInfo skill_table, SkillEffectInfo skill_effect_data)
    {
        skill_table_ = skill_table;
        skill_effect_data_ = skill_effect_data;
    }

    /// <summary>返回稳定主表技能 ID，不把等级效果 ID 当作请求技能 ID。</summary>
    public int SkillID() { return skill_table_.Id; }

    /// <summary>返回这一学习等级的效果 ID，用于权威开始通知。</summary>
    public int SkillEffectID() { return skill_effect_data_.Id; }

    /// <summary>返回已装载的正整数学习等级。</summary>
    public int Level() { return skill_effect_data_.Level; }

    /// <summary>返回同一主表配置引用，组件只读使用。</summary>
    public SkillInfo GetSkillTable() { return skill_table_; }

    /// <summary>返回同一等级效果引用，组件只读使用。</summary>
    public SkillEffectInfo GetEffect() { return skill_effect_data_; }
}
