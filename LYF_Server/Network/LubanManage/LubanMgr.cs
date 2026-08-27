

using System.Collections.Generic;
using System.IO;
using cfg;

public class LubanMgr:Singleton<LubanMgr>
{
    private Dictionary<int, SkillInfo> _skillInfos;
    public void Init()
    {
        Tables tables =new Tables((string file)=> new Luban.ByteBuf(File.ReadAllBytes(
            $"D:/unitypro/LYFMMORGP/LYF_Server/Network/LubanManage/Tb/{file}.bytes")));

        /*List<SkillInfo> lst = tables.TbSkillInfo.DataList;
        for (int i = 0; i < lst.Count; i++)
        {
            LogMsg.Info("Skill::"+lst[i].ToString());
        }*/
        _skillInfos = tables.TbSkillInfo.DataMap;
    }


    #region 技能相关

    public Dictionary<int, SkillInfo> GetSkillInfos()
    {
        return _skillInfos;
    }

    public SkillInfo GetSkillInfo(int id)
    {
        if (_skillInfos!=null&&_skillInfos.ContainsKey(id))
        {
            return _skillInfos[id];
        }
        return null;
    }
    /// <summary>
    /// 通过职业id获取技能信息
    /// </summary>
    /// <param name="jobid"></param>
    /// <returns></returns>
    public Dictionary<int, SkillInfo> GetSkillInfosByJob(int jobid)
    {
        Dictionary<int, SkillInfo> jobSkillInfos = new Dictionary<int, SkillInfo>();
        foreach (var ite in _skillInfos)
        {
            if (ite.Value.JobId == jobid)
            {
                jobSkillInfos.Add(ite.Key, ite.Value);
            }
        }
        
        return jobSkillInfos;
    }

    #endregion
}