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
    /// <summary>各固定快捷槽最近应用的稳定技能内容，避免冷却刷新取消异步图标加载。</summary>
    private readonly Dictionary<string, SkillViewData> _slotBindings =
        new Dictionary<string, SkillViewData>();
    /// <summary>等待技能槽创建完成后应用的最新技能快照。</summary>
    private List<SkillViewData> _pendingSkills;
    /// <summary>技能槽预制体是否正在异步加载。</summary>
    private bool _slotsLoading;
    /// <summary>角色头像异步绑定版本。</summary>
    private int _roleBindingVersion;

    /// <summary>应用控制器生成的完整技能快照。</summary>
    public override void ReFreshUI(object obj)
    {
        // WindowBase 的 object 签名属于 UI 框架边界，此处立即转换为强类型技能列表。
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

    /// <summary>只刷新窗口中的 MP 进度和文本，供施法权威回包增量更新。</summary>
    /// <param name="currentMp">当前 MP 点数，显示前按范围约束。</param>
    /// <param name="maxMp">最大 MP 点数，非正数时进度显示为零。</param>
    public void UpdateMp(int currentMp, int maxMp)
    {
        // 窗口层只处理显示数据，不修改角色模型中的 MP。
        if (currentMp < 0)
        {
            currentMp = 0;
        }
        if (maxMp < 0)
        {
            maxMp = 0;
        }
        if (currentMp > maxMp)
        {
            currentMp = maxMp;
        }
        // 当前 MP 比例，最大值为零时保持空条。
        float mpRatio = 0f;
        if (maxMp > 0)
        {
            mpRatio = Mathf.Clamp01((float)currentMp / maxMp);
        }
        if (_sliderMP != null)
        {
            _sliderMP.value = mpRatio;
        }
        if (_texMp != null)
        {
            _texMp.text = currentMp + "/" + maxMp + " " +
                (mpRatio * 100f).ToString("F0") + "%";
        }
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

    /// <summary>逐个异步创建缺失的固定技能槽，单个资源加载失败不阻断其他槽位。</summary>
    private void EnsureSkillSlots()
    {
        if (_slotsLoading)
        {
            return;
        }
        // 固定槽位是否仍有缺口，部分成功的加载也允许在下次刷新时补建。
        bool hasMissingSlot = false;
        // 固定快捷键数组的检查索引，逐项确认是否需要补建。
        for (int index = 0; index < _skilKkey.Length; index++)
        {
            // 当前索引对应的固定快捷键。
            string key = _skilKkey[index];
            // 当前键上已创建的组件引用，空引用视为仍需补建。
            SkillSlotWidget existingSlot;
            if (!_slots.TryGetValue(key, out existingSlot) || existingSlot == null)
            {
                hasMissingSlot = true;
                break;
            }
        }
        if (!hasMissingSlot)
        {
            return;
        }
        _slotsLoading = true;
        LoadNextSkillSlot(0);
    }

    /// <summary>按固定键顺序装载缺失槽位，当前加载失败后仍继续后续键。</summary>
    /// <param name="index">下一次检查的快捷键数组索引，范围为零到数组长度。</param>
    private void LoadNextSkillSlot(int index)
    {
        if (index >= _skilKkey.Length)
        {
            _slotsLoading = false;
            return;
        }
        // 本次异步请求对应的固定快捷键，回调到达时不受后续索引变化影响。
        string key = _skilKkey[index];
        // 当前键的存活槽位，存在时直接跳到后一个固定键。
        SkillSlotWidget existingSlot;
        if (_slots.TryGetValue(key, out existingSlot) && existingSlot != null)
        {
            LoadNextSkillSlot(index + 1);
            return;
        }
        ResourceMgr.Instance.LoadPrefabAsync("UIPrefabs/SkillSlotWidget", slotObject =>
        {
            AddSkillSlot(slotObject, key);
            RefreshSkillSlots();
            LoadNextSkillSlot(index + 1);
        });
    }

    /// <summary>把新技能槽实例挂到界面，并清除旧槽位的绑定缓存。</summary>
    /// <param name="slotObject">异步加载得到的独立技能槽实例，失败时为空。</param>
    /// <param name="key">该槽位在固定快捷栏中的按键。</param>
    private void AddSkillSlot(GameObject slotObject, string key)
    {
        if (slotObject == null || _skillSlotParent == null)
        {
            return;
        }
        slotObject.transform.SetParent(_skillSlotParent, false);
        // 预制体上的技能槽组件，只有存在时才纳入固定快捷栏。
        SkillSlotWidget slot = slotObject.GetComponent<SkillSlotWidget>();
        if (slot != null)
        {
            slot.SetKey(key);
            slot.Clear();
            _slots[key] = slot;
            _slotBindings.Remove(key);
        }
    }

    /// <summary>按固定快捷键比较稳定绑定，仅变更技能时重新加载图标。</summary>
    private void RefreshSkillSlots()
    {
        if (_pendingSkills == null || _slots.Count == 0)
        {
            return;
        }

        // 固定快捷栏索引，确保每个已创建槽位分别比较本次技能绑定。
        for (int keyIndex = 0; keyIndex < _skilKkey.Length; keyIndex++)
        {
            // 当前固定槽位对应的快捷键。
            string key = _skilKkey[keyIndex];
            // 当前键的存活槽位，尚未异步创建时跳过。
            SkillSlotWidget slot;
            if (!_slots.TryGetValue(key, out slot) || slot == null)
            {
                continue;
            }
            // 当前快照中最后一项有效绑定，保持原有重复键时的覆盖顺序。
            SkillViewData skill = null;
            // 控制器技能快照的遍历索引，用于找到当前固定键的绑定。
            for (int skillIndex = 0; skillIndex < _pendingSkills.Count; skillIndex++)
            {
                // 当前快照条目，未学习或其他快捷键均不覆盖本槽。
                SkillViewData candidate = _pendingSkills[skillIndex];
                if (candidate != null && candidate.Level > 0 &&
                    candidate.BindKey == key)
                {
                    skill = candidate;
                }
            }
            // 上一次应用到该槽的稳定绑定内容。
            SkillViewData previous;
            _slotBindings.TryGetValue(key, out previous);
            if (skill == null)
            {
                if (previous != null)
                {
                    slot.Clear();
                    _slotBindings.Remove(key);
                }
                continue;
            }
            if (previous == null || previous.SkillId != skill.SkillId ||
                previous.Level != skill.Level || previous.IconPath != skill.IconPath)
            {
                slot.ReFreshUI(skill);
                _slotBindings[key] = skill;
            }
            else
            {
                slot.StartCooldown(skill.RemainingCooldown);
            }
        }
    }
}
