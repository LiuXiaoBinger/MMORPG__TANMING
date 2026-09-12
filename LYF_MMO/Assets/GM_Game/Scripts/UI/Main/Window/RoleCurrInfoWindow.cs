using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色当前信息窗口，只负责角色状态和技能快捷栏显示。
/// </summary>
public class RoleCurrInfoWindow : WindowBase
{
    /// <summary>角色头像。</summary>
    [SerializeField, Header("头像信息")] private Image _imgHead;
    /// <summary>角色昵称文本。</summary>
    [SerializeField, Header("角色昵称")] private TMP_Text _texNickName;
    /// <summary>职业和等级文本。</summary>
    [SerializeField, Header("角色职业等级信息")] private TMP_Text _texJobLevel;
    /// <summary>角色生命值进度条。</summary>
    [SerializeField, Header("角色血量信息")] private Slider _sliderHP;
    /// <summary>角色法力值进度条。</summary>
    [SerializeField, Header("角色法力信息")] private Slider _sliderMP;
    /// <summary>角色生命值文本。</summary>
    [SerializeField, Header("HP")] private TMP_Text _texHp;
    /// <summary>角色法力值文本。</summary>
    [SerializeField, Header("Mp")] private TMP_Text _texMp;
    /// <summary>技能槽父节点。</summary>
    [SerializeField, Header("技能槽父组件")] private Transform _skillSlotParent;

    /// <summary>技能槽固定快捷键顺序。</summary>
    private readonly string[] _skilKkey = { "Q", "E", "R", "F", "1", "2", "3", "4", "5", "6" };
    /// <summary>按快捷键索引的技能槽视图。</summary>
    private readonly Dictionary<string, SkillSlotWidget> _slots =
        new Dictionary<string, SkillSlotWidget>();
    /// <summary>等待技能槽创建完成后应用的最新技能快照。</summary>
    private List<SkillViewData> _pendingSkills;
    /// <summary>技能槽预制体是否正在异步加载。</summary>
    private bool _slotsLoading;
    /// <summary>角色头像异步绑定版本。</summary>
    private int _roleBindingVersion;

    /// <summary>应用控制器生成的完整技能快照。</summary>
    public override void ReFreshUI(object obj)
    {
        List<SkillViewData> skills = obj as List<SkillViewData>;
        if (skills != null)
        {
            _pendingSkills = skills;
            EnsureSkillSlots();
            RefreshSkillSlots();
        }
    }

    /// <summary>按控制器生成的数据刷新角色信息。</summary>
    public void UpdateRoleInfo(RoleCurrentViewData viewData)
    {
        int bindingVersion = ++_roleBindingVersion;
        if (viewData == null)
        {
            return;
        }
        if (_texNickName != null)
        {
            _texNickName.text = viewData.Nickname;
        }
        if (_texJobLevel != null)
        {
            _texJobLevel.text = viewData.JobName + " Lv." + viewData.Level;
        }
        if (_sliderHP != null)
        {
            _sliderHP.value = viewData.HpRatio;
        }
        if (_sliderMP != null)
        {
            _sliderMP.value = viewData.MpRatio;
        }
        if (_texHp != null)
        {
            _texHp.text = viewData.HpText;
        }
        if (_texMp != null)
        {
            _texMp.text = viewData.MpText;
        }
        if (string.IsNullOrEmpty(viewData.HeadIconPath))
        {
            return;
        }
        ResourceMgr.Instance.LoadSpriteAsync(viewData.HeadIconPath, sprite =>
        {
            if (_imgHead != null && sprite != null && bindingVersion == _roleBindingVersion)
            {
                _imgHead.sprite = sprite;
            }
        });
    }

    /// <summary>初始化固定技能槽。</summary>
    public override void InitWindow()
    {
        EnsureSkillSlots();
    }

    /// <summary>播放指定快捷键对应技能槽的冷却表现。</summary>
    public void StartSkillCooldown(string key, float duration)
    {
        SkillSlotWidget slot;
        if (_slots.TryGetValue(key, out slot) && slot != null)
        {
            slot.StartCooldown(duration);
        }
    }

    /// <summary>异步创建尚不存在的固定技能槽。</summary>
    private void EnsureSkillSlots()
    {
        if (_slots.Count > 0 || _slotsLoading)
        {
            return;
        }
        _slotsLoading = true;
        ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/SkillSlotWidget", firstObject =>
        {
            _slotsLoading = false;
            if (firstObject == null)
            {
                return;
            }
            AddSkillSlot(firstObject, _skilKkey[0]);
            for (int index = 1; index < _skilKkey.Length; index++)
            {
                string key = _skilKkey[index];
                ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/SkillSlotWidget", slotObject =>
                {
                    AddSkillSlot(slotObject, key);
                    RefreshSkillSlots();
                });
            }
            RefreshSkillSlots();
        });
    }

    /// <summary>把技能槽实例挂到界面并绑定固定快捷键。</summary>
    private void AddSkillSlot(GameObject slotObject, string key)
    {
        if (slotObject == null || _skillSlotParent == null)
        {
            return;
        }
        slotObject.transform.SetParent(_skillSlotParent, false);
        SkillSlotWidget slot = slotObject.GetComponent<SkillSlotWidget>();
        if (slot != null)
        {
            slot.SetKey(key);
            _slots[key] = slot;
        }
    }

    /// <summary>清空旧绑定后应用最新完整技能快照。</summary>
    private void RefreshSkillSlots()
    {
        if (_pendingSkills == null || _slots.Count == 0)
        {
            return;
        }

        ClearSkillSlots();
        for (int index = 0; index < _pendingSkills.Count; index++)
        {
            SkillViewData skill = _pendingSkills[index];
            if (skill == null || skill.Level <= 0 || string.IsNullOrEmpty(skill.BindKey))
            {
                continue;
            }
            SkillSlotWidget slot;
            if (_slots.TryGetValue(skill.BindKey, out slot) && slot != null)
            {
                slot.ReFreshUI(skill);
            }
        }
    }

    /// <summary>清理全部技能槽的技能图标和冷却状态。</summary>
    private void ClearSkillSlots()
    {
        List<SkillSlotWidget> slotSnapshot = new List<SkillSlotWidget>(_slots.Values);
        for (int index = 0; index < slotSnapshot.Count; index++)
        {
            SkillSlotWidget slot = slotSnapshot[index];
            if (slot != null)
            {
                slot.Clear();
            }
        }
    }
}
