using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>创建独立技能权限编辑资产，并从一份配置同步导出客户端与游戏服 JSON，保留旧表格。</summary>
public static class SkillCastingRulesExportTool
{
    /// <summary>正式技能权限 SO 路径，五技能共用一份规则资产。</summary>
    public const string ConfigAssetPath = "Assets/GM_Game/Scripts/Data/Skill/SkillCastingRulesConfig.asset";

    /// <summary>客户端同步读取的 Resources JSON 路径，仍归档到用户指定的 Skill 目录。</summary>
    private const string ClientJsonPath = "Assets/GM_Game/Scripts/Data/Skill/Resources/SkillCastingRules.json";

    /// <summary>从项目根目录开始的游戏服 JSON 相对路径，编译时复制到部署输出。</summary>
    private const string ServerJsonRelativePath = "LYF_Server/GameServer/Config/SkillCastingRules.json";

    /// <summary>首次创建五技能默认规则；已存在资产不覆写编辑内容，然后导出两端同源 JSON。</summary>
    [MenuItem("Tools/技能/YCombat/创建并导出施法规则")]
    public static void CreateDefaultAssetAndExport()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("请退出运行模式后编辑和导出施法规则。规则在启动前同步装载。");
        }
        // 现有权限资产为唯一编辑源，生成器不能覆盖策划已编辑的规则。
        SkillCastingRulesConfig config = AssetDatabase.LoadAssetAtPath<SkillCastingRulesConfig>(ConfigAssetPath);
        if (config == null)
        {
            // 首次创建时使用此前已生效的全部禁止策略，不把旧取消标记冒充可运行权限。
            config = ScriptableObject.CreateInstance<SkillCastingRulesConfig>();
            // 本项目五个既有技能主表 ID；Q 三段只生成一行 10010。
            int[] skillIds = new int[] { 10010, 10040, 10050, 10060, 10070 };
            // 新建规则行的零起始下标，每个技能默认空互断名单、禁止施法移动。
            for (int index = 0; index < skillIds.Length; index++)
            {
                config.Rules.Add(new SkillCastingRules { SkillId = skillIds[index] });
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigAssetPath));
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
        }
        ExportRules(config);
    }

    /// <summary>验证并将当前权限资产导出成内容完全相同的客户端和服务端 JSON。</summary>
    /// <param name="config">策划编辑的正式技能权限资产，导出过程不修改其中任何行。</param>
    public static void ExportRules(SkillCastingRulesConfig config)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("运行中不能导出权限，防止界面显示已修改但本次进程仍使用旧规则。");
        }
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }
        // 两端共用校验拒绝重复或非法 ID，不导出半份规则。
        string error;
        if (!SkillCastingRulesValidation.Validate(config.Rules, out error))
        {
            throw new InvalidOperationException(error);
        }
        // 旧 Luban 表只用于身份校验，保持原二进制和 Excel 内容。
        ValidateKnownSkills(config);
        // 正式 JSON 的强类型根，临时引用源行只用于同步序列化，不改变源资产。
        SkillCastingRulesCollection collection = new SkillCastingRulesCollection { Rules = config.Rules };
        // 带换行的 UTF-8 JSON，两个目标文件使用同一字符串保证字段和值一致。
        string json = JsonUtility.ToJson(collection, true) + Environment.NewLine;
        // 项目根目录在 Assets 的上两级，临时与服务端输出都限定在此根目录下。
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        // 生成中间文件位于统一 _temp，不散落在客户端和服务端目录。
        string temporaryDirectory = Path.Combine(projectRoot, "_temp", "ycombat-casting-rules");
        Directory.CreateDirectory(temporaryDirectory);
        // 唯一一次序列化产物同时复制到两个正式目标。
        string temporaryPath = Path.Combine(temporaryDirectory, "SkillCastingRules.json");
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        // 客户端正式输出位于 Skill/Resources，保证内置同步 Resources 装载。
        string clientPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ClientJsonPath);
        // 游戏服正式输出位置，部署项目会复制到 Config/SkillCastingRules.json。
        string serverPath = Path.Combine(projectRoot, ServerJsonRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(clientPath));
        Directory.CreateDirectory(Path.GetDirectoryName(serverPath));
        File.Copy(temporaryPath, clientPath, true);
        File.Copy(temporaryPath, serverPath, true);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("施法规则已同源导出：" + ClientJsonPath + "；" + ServerJsonRelativePath + "。重新进入运行模式或重启服务器后生效。");
    }

    /// <summary>对照客户端正式技能主表验证规则引用，不修改旧表和运行时 LubanMgr。</summary>
    /// <param name="config">结构已经校验通过的完整编辑配置。</param>
    private static void ValidateKnownSkills(SkillCastingRulesConfig config)
    {
        // Unity 与服务器沿用同一份正式 Luban 二进制，编辑阶段不初始化网络或全局业务。
        string projectRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
        // 正式主表路径与 SkillEditorAssetTools 的读取目录一致，不读取 _temp 中的历史生成物。
        string tablePath = Path.Combine(projectRoot, "LYF_Server", "Network", "LubanManage", "Tb", "tbskillinfo.bytes");
        if (!File.Exists(tablePath))
        {
            throw new InvalidOperationException("未找到正式 tbskillinfo.bytes，拒绝导出无法验证的技能权限。");
        }
        // 只读解析旧 Luban 主表，禁止覆盖表文件或生成代码。
        cfg.TbSkillInfo skills = new cfg.TbSkillInfo(new Luban.ByteBuf(File.ReadAllBytes(tablePath)));
        // 权限行的零起始下标，每个新技能 ID 和被打断 ID 都须存在于原主表。
        for (int index = 0; index < config.Rules.Count; index++)
        {
            // 当前规则行，结构校验保证不为空。
            SkillCastingRules rule = config.Rules[index];
            if (!skills.DataMap.ContainsKey(rule.SkillId))
            {
                throw new InvalidOperationException("权限技能不在旧主表中：" + rule.SkillId);
            }
            // 当前互断列表下标，核对允许打断的技能身份。
            for (int interruptIndex = 0; interruptIndex < rule.InteruptSkill.Count; interruptIndex++)
            {
                if (!skills.DataMap.ContainsKey(rule.InteruptSkill[interruptIndex]))
                {
                    throw new InvalidOperationException("InteruptSkill 引用不存在的旧技能：" + rule.InteruptSkill[interruptIndex]);
                }
            }
        }
    }
}
