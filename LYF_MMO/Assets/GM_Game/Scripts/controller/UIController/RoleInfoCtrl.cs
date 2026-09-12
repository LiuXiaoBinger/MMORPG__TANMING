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
        if (_roleCurrentWindow == null || Global.Instance == null)
        {
            return;
        }
        ClientRole localRole = Global.Instance.RoleWorld.LocalRole;
        if (localRole == null)
        {
            return;
        }
        MainRoleInfo roleInfo = localRole.RoleInfo;
        if (roleInfo == null || roleInfo.BaseInfo == null)
        {
            return;
        }

        string jobName = roleInfo.BaseInfo.JobId.ToString();
        if (roleInfo.BaseInfo.JobId >= 0 &&
            roleInfo.BaseInfo.JobId < ConstDefine.RoleJobName.Count)
        {
            jobName = ConstDefine.RoleJobName[roleInfo.BaseInfo.JobId];
        }
        string headIconPath = string.Empty;
        if (roleInfo.BaseInfo.JobId == 1)
        {
            headIconPath = "Icon/head_jianxiu";
        }

        float hpRatio = CalculateRatio(roleInfo.BaseInfo.CurrHp, roleInfo.BaseInfo.MaxHp);
        float mpRatio = CalculateRatio(roleInfo.BaseInfo.CurrMp, roleInfo.BaseInfo.MaxMp);
        string hpText = roleInfo.BaseInfo.CurrHp + "/" + roleInfo.BaseInfo.MaxHp + " " +
            (hpRatio * 100f).ToString("F0") + "%";
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
    public void RefreshEquipment(RoleKanpsackInfo knapsackInfo)
    {
        if (_roleAttributeWindow == null || knapsackInfo == null)
        {
            return;
        }

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
    private static float CalculateRatio(float currentValue, float maximumValue)
    {
        if (maximumValue <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(currentValue / maximumValue);
    }

    /// <summary>将协议装备数据和 Luban 配置合成为装备展示数据。</summary>
    private static bool TryBuildEquipmentViewData(RoleItemInfo roleItem,
        out EquipmentViewData viewData)
    {
        viewData = null;
        if (roleItem == null || roleItem.ItemTypeId <= 0)
        {
            return false;
        }

        EquipInfo equipConfig = LubanMgr.Instance.GetEquipInfoById(roleItem.ItemTypeId);
        int equipTypeValue = 0;
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

        string name = roleItem.ItemTypeId.ToString();
        string iconPath = "Icon/Item/Item_" + roleItem.ItemTypeId;
        if (equipConfig != null)
        {
            name = equipConfig.EquipName;
            iconPath = equipConfig.Icon;
        }
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
