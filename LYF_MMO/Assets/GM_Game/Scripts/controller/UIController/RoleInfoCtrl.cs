using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;

/// <summary>
/// 角色信息控制器，负责把角色和装备模型转换成界面展示数据。
/// </summary>
public sealed class RoleInfoCtrl : CtrlBase
{
    /// <summary>角色当前状态窗口。</summary>
    private readonly RoleCurrInfoWindow _roleCurrentWindow;
    /// <summary>角色装备属性窗口。</summary>
    private readonly RoleAttriibuteWindow _roleAttributeWindow;
    /// <summary>技能窗口，用于更新职业和点数标题。</summary>
    private readonly SkillInfoWindow _skillWindow;

    /// <summary>创建角色信息控制器并初始化当前角色显示。</summary>
    /// <param name="mainView">提供状态栏、装备窗口和技能标题的主界面。</param>
    public RoleInfoCtrl(MainView mainView) : base(mainView)
    {
        _roleCurrentWindow = mainView.RoleCurrentWindow;
        _roleAttributeWindow = mainView.RoleAttributeWindow;
        _skillWindow = mainView.SkillWindow;
        RefreshCurrentRole();
    }

    /// <summary>从全局角色模型生成当前角色信息展示数据。</summary>
    public void RefreshCurrentRole()
    {
        if (_roleCurrentWindow == null || Global.Instance == null ||
            Global.Instance.SceneWorld == null)
        {
            return;
        }
        // 当前场景里的本地角色，信息窗口不持有跨角色缓存。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null)
        {
            return;
        }
        // 已装载的完整角色数据，生命、法力、职业和等级取 BaseInfo。
        MainRoleInfo roleInfo = localRole.RoleInfo;
        if (roleInfo == null || roleInfo.BaseInfo == null)
        {
            return;
        }

        // 职业名称优先取名称列表；未知编号直接显示其数字。
        string jobName = roleInfo.BaseInfo.JobId.ToString();
        if (roleInfo.BaseInfo.JobId >= 0 &&
            roleInfo.BaseInfo.JobId < ConstDefine.RoleJobName.Count)
        {
            jobName = ConstDefine.RoleJobName[roleInfo.BaseInfo.JobId];
        }
        // 当前仅为职业 1 配置头像路径，其他职业传空路径。
        string headIconPath = string.Empty;
        if (roleInfo.BaseInfo.JobId == 1)
        {
            headIconPath = "Icon/head_jianxiu";
        }

        // 生命条使用裁剪到 0～1 的比例。
        float hpRatio = CalculateRatio(roleInfo.BaseInfo.CurrHp, roleInfo.BaseInfo.MaxHp);
        // 法力条使用裁剪到 0～1 的比例。
        float mpRatio = CalculateRatio(roleInfo.BaseInfo.CurrMp, roleInfo.BaseInfo.MaxMp);
        // 生命正文同时显示当前值、上限和整数百分比。
        string hpText = roleInfo.BaseInfo.CurrHp + "/" + roleInfo.BaseInfo.MaxHp + " " +
            (hpRatio * 100f).ToString("F0") + "%";
        // 法力正文同时显示当前值、上限和整数百分比。
        string mpText = roleInfo.BaseInfo.CurrMp + "/" + roleInfo.BaseInfo.MaxMp + " " +
            (mpRatio * 100f).ToString("F0") + "%";
        _roleCurrentWindow.UpdateRoleInfo(new RoleCurrentViewData(roleInfo.BaseInfo.Nickname,
            jobName, roleInfo.BaseInfo.Level, headIconPath, hpRatio, mpRatio, hpText, mpText));
        if (_skillWindow != null)
        {
            // 当前协议尚无独立技能点字段，沿用旧界面使用角色等级的表现。
            _skillWindow.SetHeader(jobName, roleInfo.BaseInfo.Level);
        }
    }

    /// <summary>把当前穿戴背包转换成装备槽展示列表。</summary>
    /// <param name="knapsackInfo">服务端背包快照，只读取其中的当前穿戴装备列表。</param>
    public void RefreshEquipment(RoleKanpsackInfo knapsackInfo)
    {
        if (_roleAttributeWindow == null || knapsackInfo == null)
        {
            return;
        }

        // 只收录能确定装备部位的条目，交给窗口按部位显示。
        List<EquipmentViewData> equipments = new List<EquipmentViewData>();
        for (int index = 0; index < knapsackInfo.RoleCurrtEquipPack.Count; index++)
        {
            RoleItemInfo roleItem = knapsackInfo.RoleCurrtEquipPack[index];
            EquipmentViewData viewData;
            if (TryBuildEquipmentViewData(roleItem, out viewData))
            {
                equipments.Add(viewData);
            }
        }
        _roleAttributeWindow.ReFreshUI(equipments);
    }

    /// <summary>计算并约束角色属性比例。</summary>
    /// <param name="currentValue">当前生命或法力。</param>
    /// <param name="maximumValue">对应上限；不为正时返回 0。</param>
    private static float CalculateRatio(float currentValue, float maximumValue)
    {
        if (maximumValue <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(currentValue / maximumValue);
    }

    /// <summary>将协议装备数据和 Luban 配置合成为装备展示数据。</summary>
    /// <param name="roleItem">一件当前穿戴装备的协议数据。</param>
    /// <param name="viewData">成功时返回装备部位、名称、图标和强化等级，失败为空。</param>
    private static bool TryBuildEquipmentViewData(RoleItemInfo roleItem,
        out EquipmentViewData viewData)
    {
        viewData = null;
        if (roleItem == null || roleItem.ItemTypeId <= 0)
        {
            return false;
        }

        // 配置提供显示名称、图标，以及协议未提供有效部位时的备用部位。
        EquipInfo equipConfig = LubanMgr.Instance.GetEquipInfoById(roleItem.ItemTypeId);
        // 最终使用的装备部位编号，优先采用协议中的有效枚举值。
        int equipTypeValue = 0;
        // 是否已从协议或配置中取得可识别的装备部位。
        bool hasValidEquipType = false;
        if (roleItem.EquipInfo != null &&
            Enum.IsDefined(typeof(EquipType), roleItem.EquipInfo.EquipType))
        {
            equipTypeValue = roleItem.EquipInfo.EquipType;
            hasValidEquipType = true;
        }
        if (!hasValidEquipType && equipConfig != null &&
            Enum.IsDefined(typeof(EquipType), equipConfig.EquipType))
        {
            equipTypeValue = equipConfig.EquipType;
            hasValidEquipType = true;
        }
        if (!hasValidEquipType)
        {
            return false;
        }

        // 配置缺失时使用物品编号作为显示名。
        string name = roleItem.ItemTypeId.ToString();
        // 配置缺失时按物品编号拼接历史图标路径。
        string iconPath = "Icon/Item/Item_" + roleItem.ItemTypeId;
        if (equipConfig != null)
        {
            name = equipConfig.EquipName;
            iconPath = equipConfig.Icon;
        }
        // 强化等级来自协议装备附加数据，未携带时显示 0。
        int strengthenLevel = 0;
        if (roleItem.EquipInfo != null)
        {
            strengthenLevel = roleItem.EquipInfo.StrengthenLevel;
        }
        viewData = new EquipmentViewData((EquipType)equipTypeValue, roleItem.ItemTypeId,
            name, iconPath, strengthenLevel);
        return true;
    }
}
