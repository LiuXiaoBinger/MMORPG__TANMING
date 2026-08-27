


using System.Collections.Generic;
using SqlSugar;

public class CentRoleModel
{
    private SqlSugarClient _db = null;
    public CentRoleModel(SqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 查询角色技能信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public RoleSkillInfoRet RoleSkillInfo(EnterWroldReq req)
    {
        RoleSkillInfoRet ret = new RoleSkillInfoRet();

        List< RoleSkillTable> roleSkillList = _db.Queryable<RoleSkillTable>().Where(v => v.RoleID == req.RoleId).ToList();
        if (roleSkillList != null && roleSkillList.Count > 0)
        {
            for (int i = 0; i < roleSkillList.Count; i++)
            {
                RoleSkillInfo roleSkillInfo = new RoleSkillInfo()
                {
                    SkillId =  roleSkillList[i].SkillID,
                    Level =  roleSkillList[i].SkillLevel,
                    BindKey = roleSkillList[i].Bindkey
                };
                ret.RoleSkillInfoList.Add(roleSkillInfo);
            }
        }
        else
        {
            ret.CmdCode = CmdCode.RoleNotExist;
        }
        
        return ret;
    }
}