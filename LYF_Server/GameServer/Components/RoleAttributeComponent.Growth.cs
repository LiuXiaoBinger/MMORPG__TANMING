using System;

/// <summary>沿用角色属性组件和存档版本，处理 GM 调整的基础等级和本级经验。</summary>
public partial class RoleAttributeComponent
{
    /// <summary>最近一次等级或经验变化应写入的下一份角色存档版本，零表示已保存。</summary>
    private long growthSaveVersion;

    /// <summary>按 RO 的 SetLevel 调整基础等级，保留角色已有经验。</summary>
    /// <param name="level">角色等级表中的实际等级。</param>
    internal string SetLevel(int level)
    {
        if (LubanMgr.Instance.GetRoleLevelInfoById(level) == null) return "角色等级不在配置范围内";
        return SetGrowth(level, Owner.mainRoleInfo.Exp);
    }

    /// <summary>按 RO 取输入数量的绝对值，使用本项目每级的 NeedExp 处理连续升级。</summary>
    /// <param name="amount">要发放的经验点数。</param>
    internal string AddExp(int amount)
    {
        // 当前等级和经验来自服务端角色资料，64 位中间值用于跨越多个等级。
        int level = Owner.mainRoleInfo.BaseInfo.Level;
        long exp = Owner.mainRoleInfo.Exp + Math.Abs((long)amount);
        // 等级表末行的需求为零，满级后不再累积经验。
        cfg.RoleLevelInfo row = LubanMgr.Instance.GetRoleLevelInfoById(level);
        if (row == null) return "当前角色等级未配置";
        while (row.NeedExp > 0 && exp >= row.NeedExp)
        {
            exp -= row.NeedExp;
            row = LubanMgr.Instance.GetRoleLevelInfoById(level + 1);
            if (row == null) return "缺少下一等级配置";
            level++;
        }
        if (row.NeedExp <= 0) exp = 0;
        return SetGrowth(level, (int)exp);
    }

    /// <summary>更新权威成长数值、标记存档，并用属性通知刷新角色面板。</summary>
    /// <param name="level">最终基础等级。</param><param name="exp">升级结算后的本级经验。</param>
    private string SetGrowth(int level, int exp)
    {
        Owner.mainRoleInfo.BaseInfo.Level = level;
        Owner.mainRoleInfo.Exp = exp;
        growthSaveVersion = Owner.GetSaveVersion() + 1;
        NotifyAttributeUpdate();
        return "当前等级 " + level + "，经验 " + exp;
    }

    /// <summary>等级或经验尚未获得存档确认时参与下一次角色保存。</summary>
    public override bool NeedSave() => growthSaveVersion != 0;

    /// <summary>把同一时刻的等级和经验加入当前角色快照。</summary>
    /// <param name="fieldManager">角色已有的存档组包器。</param><param name="saveVersion">本次角色保存使用的版本。</param>
    public override bool Save(RoleDataFieldManager fieldManager, long saveVersion)
    {
        if (!NeedSave()) return false;
        // 角色级快照在游戏线程上一次性取值。
        SaveRoleDataReq request = fieldManager.GetRequest();
        request.GrowthLevel = Owner.mainRoleInfo.BaseInfo.Level;
        request.GrowthExp = Owner.mainRoleInfo.Exp;
        return true;
    }

    /// <summary>旧确认不能清除提交后又修改过的等级或经验。</summary>
    /// <param name="saveVersion">中心服已提交的同一角色版本。</param>
    public override void OnSaveAcknowledged(long saveVersion)
    {
        if (growthSaveVersion <= saveVersion) growthSaveVersion = 0;
    }
}
