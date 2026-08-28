


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
        ret.CmdCode = CmdCode.Succeed;
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

    /// <summary>
    /// 获取主角背包信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public RoleKanpsackInfoRet RoleKanpaskInfo(EnterWroldReq req)
    {
        //todo
        RoleKanpsackInfoRet ret = new RoleKanpsackInfoRet();

        RoleKnapsackTable roleKnapsackTable = _db.Queryable<RoleKnapsackTable>()
            .Where(v => v.RoleId == req.RoleId).First();
        if (roleKnapsackTable != null)
        {
            //已|分割
            string[] slotarr = roleKnapsackTable.Knapsack.Split('|');
            for (int i = 0; i < slotarr.Length; i++)
            {
                RoleKanpsackSlot slot = new RoleKanpsackSlot()
                {
                    SlotNo = i,
                    ItemId = int.Parse(slotarr[i].Split(',')[0]),
                    Count = int.Parse(slotarr[i].Split(',')[1])
                };
                ret.RoleItemLst.Add(slot);
            }
        }
        else
        {
            ret.CmdCode = CmdCode.ServerError;
        }
        
        return ret;
    }
}