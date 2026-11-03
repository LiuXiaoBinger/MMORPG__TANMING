using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从 Resources 同步读取施法规则，并按技能 ID 建立查询表。
/// 整个进程只尝试加载一次；缺文件或校验失败时保持空表，查询结果为不允许。
/// </summary>
public static class SkillCastingRulesCatalog
{

    #region 字段与常量
    /// <summary>同步 Resources 配置键；正式 JSON 位于 Data/Skill/Resources 下。</summary>
    private const string ResourceName = "SkillCastingRules";

    /// <summary>按技能主表 ID 保存本进程的规则；解析失败时保持空表。</summary>
    private static readonly Dictionary<int, SkillCastingRules> _rules = new Dictionary<int, SkillCastingRules>();

    /// <summary>首次装载尝试已完成，包括缺文件或解析失败；失败默认不会被异步资源替换。</summary>
    private static bool _initialized;
    #endregion

    #region 配置读取与解析
    /// <summary>同步读取并校验整份配置，通过后建立索引；失败时记录日志，查询仍返回不允许。</summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        // Resources TextAsset 同步装载；其文本在解析后即可卸载，不保留 Unity 资源句柄。
        TextAsset asset = Resources.Load<TextAsset>(ResourceName);
        if (asset == null)
        {
            Debug.LogWarning("技能施法规则未找到，使用禁止互断、禁止施法移动、禁止技能中主动跳跃的默认策略。");
            return;
        }
        try
        {
            // 与服务器使用同一 JSON 根和字段名，不引用异步表现资产。
            SkillCastingRulesCollection config = JsonUtility.FromJson<SkillCastingRulesCollection>(asset.text);
            // 数据错误必须整份拒绝，不能用半份权限替换进程默认。
            string error;
            if (config == null || !SkillCastingRulesValidation.Validate(config.Rules, out error))
            {
                Debug.LogError("技能施法规则无效，使用稳定禁止策略。");
                return;
            }
            // 配置规则行的零起始下标，初始化后索引只读。
            for (int index = 0; index < config.Rules.Count; index++)
            {
                // 本次进程独占的 JSON 解析行，与编辑器 SO 没有共享可变实例。
                SkillCastingRules rule = config.Rules[index];
                _rules.Add(rule.SkillId, rule);
            }
        }
        catch (Exception exception)
        {
            _rules.Clear();
            Debug.LogError("技能施法规则解析失败，使用稳定禁止策略：" + exception.Message);
        }
        finally
        {
            Resources.UnloadAsset(asset);
        }
    }
    #endregion

    #region 业务逻辑：施法规则查询
    /// <summary>查询新技能是否可打断旧技能，方向与 RO InteruptSkill 一致。</summary>
    /// <param name="newSkillId">当前准备释放的新技能主表 ID。</param>
    /// <param name="activeSkillId">正占用动作槽的旧技能主表 ID。</param>
    /// <returns>新技能的 InteruptSkill 包含旧技能 ID 时为 true。</returns>
    public static bool CanInterrupt(int newSkillId, int activeSkillId)
    {
        Initialize();
        // 新技能对应的只读权限行；没有配置时禁止互断。
        SkillCastingRules rule;
        return _rules.TryGetValue(newSkillId, out rule) &&
            rule.InteruptSkill.Contains(activeSkillId);
    }

    /// <summary>查询当前技能是否允许普通水平移动，不改变主动起跳禁用策略。</summary>
    /// <param name="skillId">正在释放的技能主表 ID。</param>
    /// <returns>配置 MoveCasting 为 true 时允许水平输入和积分。</returns>
    public static bool IsMoveCasting(int skillId)
    {
        Initialize();
        // 当前正在释放的技能规则；没有配置时不允许施法中普通水平移动。
        SkillCastingRules rule;
        return _rules.TryGetValue(skillId, out rule) && rule.MoveCasting;
    }
    #endregion
}
