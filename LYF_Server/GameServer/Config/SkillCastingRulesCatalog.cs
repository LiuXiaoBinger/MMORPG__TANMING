using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

/// <summary>游戏服同步读取客户端编辑器导出的同源施法权限；首次装载后只读，不在请求中热更新。</summary>
public static class SkillCastingRulesCatalog
{
    /// <summary>各技能主表 ID 对应的互断/移动权限，JSON 无效或缺失时为空。</summary>
    private static readonly Dictionary<int, SkillCastingRules> _rules = new Dictionary<int, SkillCastingRules>();

    /// <summary>首次同步装载已完成，包括采用稳定禁止默认值的失败分支。</summary>
    private static bool _initialized;

    /// <summary>读取与游戏服一起部署的正式 JSON；缺文件时明确采用稳定禁止默认，不读取开发目录。</summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        // 部署文件与 GameServer.exe 一起发布；不从网络请求读取文件路径。
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "SkillCastingRules.json");
        if (!File.Exists(path))
        {
            LogMsg.Info("技能施法规则未找到，采用禁止互断与施法移动的默认策略。", LogMsgType.Error);
            return;
        }
        try
        {
            // JSON 序列化属于边界适配，领域数据仍使用明确的规则类型和字段。
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            // 与客户端导出文件相同的强类型配置根；读取后不再持有文件或解析器。
            SkillCastingRulesCollection config = serializer.Deserialize<SkillCastingRulesCollection>(File.ReadAllText(path));
            // 校验失败整体拒绝，禁止只装载部分规则。
            string error;
            if (config == null || !SkillCastingRulesValidation.Validate(config.Rules, out error))
            {
                LogMsg.Info("技能施法规则无效，采用稳定禁止策略。", LogMsgType.Error);
                return;
            }
            // 配置行下标，仅在角色创建前执行一次，不在逻辑帧中分配。
            for (int index = 0; index < config.Rules.Count; index++)
            {
                // 当前技能规则行，两端共用同一 DTO 源文件，避免字段漂移。
                SkillCastingRules rule = config.Rules[index];
                _rules.Add(rule.SkillId, rule);
            }
        }
        catch (Exception exception)
        {
            _rules.Clear();
            LogMsg.Info("技能施法规则读取失败，采用稳定禁止策略：" + exception.Message, LogMsgType.Error);
        }
    }

    /// <summary>判断新技能能否打断正在施放的旧技能，沿用 RO 的新技能名单方向。</summary>
    /// <param name="newSkillId">经过学习/职业/冷却/MP 校验的新技能主表 ID。</param>
    /// <param name="activeSkillId">游戏服当前活动施法的旧技能主表 ID。</param>
    /// <returns>新技能 InteruptSkill 包含旧技能 ID 时为 true。</returns>
    public static bool CanInterrupt(int newSkillId, int activeSkillId)
    {
        Initialize();
        // 新技能的进程只读权限行，未配置的技能默认不能打断其他技能。
        SkillCastingRules rule;
        return _rules.TryGetValue(newSkillId, out rule) && rule.InteruptSkill.Contains(activeSkillId);
    }
}
