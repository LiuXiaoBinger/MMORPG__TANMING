using System;
using System.Collections.Generic;
using System.IO;
using cfg;

/// <summary>从构建输出目录实际装载 Luban 表并核对新旧技能映射的独立测试入口。</summary>
public static class SkillConfigSmoke
{
    /// <summary>加载运行目录的配置表，并验证五个现阶段配置的一级技能。</summary>
    /// <param name="args">命令行参数；此测试不接受参数。</param>
    /// <returns>全部表格和映射有效时为零，否则为非零错误码。</returns>
    public static int Main(string[] args)
    {
        // 可执行文件所在的临时构建目录，必须包含发布时一起复制的表数据。
        string tableDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "LubanManage", "Tb");
        if (!File.Exists(Path.Combine(tableDirectory, "tbskillinfo.bytes")) ||
            !File.Exists(Path.Combine(tableDirectory, "tbskilleffectinfo.bytes")) ||
            !File.Exists(Path.Combine(tableDirectory, "tbskillpresentationinfo.bytes")))
        {
            Console.Error.WriteLine("构建输出缺少技能表：" + tableDirectory);
            return 1;
        }

        try
        {
            LubanMgr.Instance.Init();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Luban 运行时装载失败：" + exception);
            return 2;
        }

        // 五个稳定技能 ID 对应旧表保留的技能记录。
        int[] skillIds = { 10010, 10040, 10050, 10060, 10070 };
        // 按技能顺序排列的一级效果 ID，验证等级到效果的查找索引。
        int[] effectIds = { 2001001, 2004001, 2005001, 2006001, 2007001 };
        // 按效果顺序排列的职业一默认表现 ID。
        int[] presentationIds = { 3001001, 3004001, 3005001, 3006001, 3007001 };
        // LubanMgr 对外提供的表现表索引，内容应来自当前构建输出。
        Dictionary<int, SkillPresentationInfo> presentations =
            LubanMgr.Instance.GetSkillPresentationInfos();
        for (int index = 0; index < skillIds.Length; index++)
        {
            // 旧表中的稳定技能定义，验证老数据仍能读取。
            SkillInfo skill = LubanMgr.Instance.GetSkillInfoById(skillIds[index]);
            // 一级已学技能解析出的新效果配置。
            SkillEffectInfo effect = LubanMgr.Instance.GetSkillEffectInfoBySkillAndLevel(
                skillIds[index], 1);
            // 当前职业默认模型的单段动画表现配置。
            SkillPresentationInfo presentation;
            if (skill == null || effect == null || effect.Id != effectIds[index] ||
                effect.SkillId != skillIds[index] || effect.Level != 1 ||
                !presentations.TryGetValue(presentationIds[index], out presentation) ||
                presentation.EffectId != effect.Id || presentation.ProfessionId != 1 ||
                presentation.ModelKey != "Role_MJS")
            {
                Console.Error.WriteLine("技能表映射失败：" + skillIds[index]);
                return 3;
            }
        }

        Console.WriteLine("Luban 运行时装载通过：旧技能 5、新效果 5、新表现 5；表目录=" +
            tableDirectory);
        return 0;
    }
}
