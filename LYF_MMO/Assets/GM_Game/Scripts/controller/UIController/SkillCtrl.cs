using System;
using System.Collections.Generic;
using cfg;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 技能控制器，负责技能配置、快捷键、释放判定和冷却状态。
/// </summary>
public sealed class SkillCtrl : CtrlBase
{
    /// <summary>技能列表窗口。</summary>
    private readonly SkillInfoWindow _skillWindow;
    /// <summary>角色快捷技能栏窗口。</summary>
    private readonly RoleCurrInfoWindow _roleCurrentWindow;
    /// <summary>按快捷键索引的可释放技能配置。</summary>
    private readonly Dictionary<string, SkillInfo> _skillsByKey = new Dictionary<string, SkillInfo>();
    /// <summary>按技能编号记录的冷却结束时间。</summary>
    private readonly Dictionary<int, float> _cooldownEndTimes = new Dictionary<int, float>();
    /// <summary>技能快捷键事件是否已注册。</summary>
    private bool _inputRegistered;

    /// <summary>创建技能控制器并注册协议和输入事件。</summary>
    public SkillCtrl(MainView mainView) : base(mainView)
    {
        _skillWindow = mainView.SkillWindow;
        _roleCurrentWindow = mainView.RoleCurrentWindow;
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleSkillInfoCode, OnRoleSkillInfo);
        RegisterInput();
    }

    /// <summary>幂等注册技能快捷键，兼容输入组件稍后初始化的场景。</summary>
    public void RegisterInput()
    {
        if (_inputRegistered || PlayerInputCtr.Instance == null)
        {
            return;
        }
        PlayerInputCtr.Instance.SkillKeyEvent += OnSkillKey;
        _inputRegistered = true;
    }

    /// <summary>解析角色技能回包并同时刷新技能窗口与快捷栏。</summary>
    private void OnRoleSkillInfo(ByteString data)
    {
        RoleSkillInfoRet result = RoleSkillInfoRet.Parser.ParseFrom(data);
        if (result == null || result.CmdCode != CmdCode.Succeed)
        {
            return;
        }

        _skillsByKey.Clear();
        List<SkillViewData> viewDataList = new List<SkillViewData>();
        for (int index = 0; index < result.RoleSkillInfoList.Count; index++)
        {
            RoleSkillInfo roleSkill = result.RoleSkillInfoList[index];
            SkillInfo skillConfig = LubanMgr.Instance.GetSkillInfoById(roleSkill.SkillId);
            if (skillConfig == null)
            {
                continue;
            }
            float remainingCooldown = GetRemainingCooldown(skillConfig.Id);
            SkillViewData viewData = new SkillViewData(skillConfig.Id, skillConfig.Name,
                skillConfig.Desc, skillConfig.Icon, roleSkill.Level, skillConfig.CDTime,
                roleSkill.BindKey, remainingCooldown);
            viewDataList.Add(viewData);
            if (roleSkill.Level > 0 && !string.IsNullOrEmpty(roleSkill.BindKey))
            {
                _skillsByKey[roleSkill.BindKey] = skillConfig;
            }
        }

        if (_skillWindow != null)
        {
            _skillWindow.ReFreshUI(viewDataList);
        }
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.ReFreshUI(viewDataList);
        }
    }

    /// <summary>获取控制器保存的技能剩余冷却时间。</summary>
    private float GetRemainingCooldown(int skillId)
    {
        float cooldownEndTime;
        if (!_cooldownEndTimes.TryGetValue(skillId, out cooldownEndTime))
        {
            return 0f;
        }
        float remainingCooldown = cooldownEndTime - Time.time;
        if (remainingCooldown <= 0f)
        {
            _cooldownEndTimes.Remove(skillId);
            return 0f;
        }
        return remainingCooldown;
    }

    /// <summary>处理快捷键，成功释放后记录控制器级冷却结束时间。</summary>
    private void OnSkillKey(string key)
    {
        SkillInfo skillInfo;
        if (!_skillsByKey.TryGetValue(key, out skillInfo) || skillInfo == null)
        {
            return;
        }

        float cooldownEndTime;
        if (_cooldownEndTimes.TryGetValue(skillInfo.Id, out cooldownEndTime) &&
            Time.time < cooldownEndTime)
        {
            return;
        }
        if (Global.Instance == null || Global.Instance.roleCtrlBase == null)
        {
            return;
        }
        if (!Global.Instance.roleCtrlBase.UseSkill(skillInfo))
        {
            return;
        }

        _cooldownEndTimes[skillInfo.Id] = Time.time + skillInfo.CDTime;
        if (_roleCurrentWindow != null)
        {
            _roleCurrentWindow.StartSkillCooldown(key, skillInfo.CDTime);
        }
    }

    /// <summary>注销技能协议和输入事件。</summary>
    public override void Dispose()
    {
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RoleSkillInfoCode);
        if (_inputRegistered && PlayerInputCtr.Instance != null)
        {
            PlayerInputCtr.Instance.SkillKeyEvent -= OnSkillKey;
            _inputRegistered = false;
        }
        _skillsByKey.Clear();
        _cooldownEndTimes.Clear();
    }
}
