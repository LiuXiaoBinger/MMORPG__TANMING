using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 角色当前信息窗口，只负责角色状态和技能快捷栏显示。
/// </summary>
public class RoleCurrInfoWindow : WindowBase
{
    /// <summary>正式瞄准按钮点击通知，控制器接入 RO 同名目标切换流程。</summary>
    public event Action AimButtonClicked;
    /// <summary>当前窗口生命周期内创建的瞄准按钮，重复初始化时复用。</summary>
    private Button _aimButton;
    /// <summary>当前目标的中文文字提示，空目标显示未选中。</summary>
    private TMP_Text _targetText;

    /// <summary>角色生命值进度条。</summary>
    [SerializeField, Header("角色血量信息")] private Slider _sliderHP;
    /// <summary>角色法力值进度条。</summary>
    [SerializeField, Header("角色法力信息")] private Slider _sliderMP;
    /// <summary>中央左侧真气弧条的可填充部分，初始按预制体显示满弧，后续由真气数据更新。</summary>
    [SerializeField, Header("真气弧条")] private Image _trueQiFill;
    /// <summary>中央右侧气力弧条的可填充部分，与怒气和法力各自独立。</summary>
    [SerializeField, Header("气力弧条")] private Image _staminaFill;
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
    /// <summary>中央槽最近收到的满怒展示状态，初始为未满，供异步创建槽位时同步。</summary>
    private bool _rageIsFull;

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
    /// <param name="viewData">角色真实生命、法力比例；空值不改变现有条形显示。</param>
    public void UpdateRoleInfo(RoleCurrentViewData viewData)
    {
        if (viewData == null)
        {
            return;
        }
        if (_sliderHP != null)
        {
            _sliderHP.value = viewData.HpRatio;
        }
        if (_sliderMP != null)
        {
            _sliderMP.value = viewData.MpRatio;
        }
    }

    /// <summary>只刷新窗口中的 MP 进度条，供施法权威回包增量更新。</summary>
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
    }

    /// <summary>设置中央槽怒气外观，当前无玩法数据调用时始终保持默认暗态。</summary>
    /// <param name="currentRage">真实来源提供的当前怒气点数，不在窗口内累积或消耗。</param>
    /// <param name="maxRage">同一来源的怒气上限；非正数表示未接入，不显示满怒。</param>
    public void UpdateRage(int currentRage, int maxRage)
    {
        _rageIsFull = maxRage > 0 && currentRage >= maxRage;
        // 已创建的中央 R 槽只更新外观，不改变快捷键、冷却或施法条件。
        SkillSlotWidget centralSlot;
        if (_slots.TryGetValue("R", out centralSlot) && centralSlot != null)
        {
            centralSlot.SetRageFull(_rageIsFull);
        }
    }

    /// <summary>刷新中央左侧真气弧条，只展示外部提供的数值，不累积或消耗真气。</summary>
    /// <param name="currentTrueQi">同一真实来源的当前真气点数，负值按空条显示。</param>
    /// <param name="maxTrueQi">真气点数上限；非正数表示未接入，保持空条。</param>
    public void UpdateTrueQi(int currentTrueQi, int maxTrueQi)
    {
        if (_trueQiFill == null)
        {
            return;
        }
        // 原生Radial360以整圆为单位，两侧各保留三分之一圆弧的可填充范围。
        float fillRatio = 0f;
        if (maxTrueQi > 0)
        {
            fillRatio = Mathf.Clamp01((float)currentTrueQi / maxTrueQi);
        }
        _trueQiFill.fillAmount = fillRatio / 3f;
    }

    /// <summary>刷新中央右侧气力弧条，只展示外部提供的数值，不从移动或其他资源推算。</summary>
    /// <param name="currentStamina">同一真实来源的当前气力点数，负值按空条显示。</param>
    /// <param name="maxStamina">气力点数上限；非正数表示未接入，保持空条。</param>
    public void UpdateStamina(int currentStamina, int maxStamina)
    {
        if (_staminaFill == null)
        {
            return;
        }
        // 气力只更新右弧，独立于左侧真气、中央怒气和下方HP/MP。
        float fillRatio = 0f;
        if (maxStamina > 0)
        {
            fillRatio = Mathf.Clamp01((float)currentStamina / maxStamina);
        }
        _staminaFill.fillAmount = fillRatio / 3f;
    }

    /// <summary>初始化固定技能槽及正式瞄准和目标提示区域。</summary>
    public override void InitWindow()
    {
        EnsureTargetControls();
        EnsureSkillSlots();
    }

    /// <summary>在已有 HUD 内创建简洁瞄准按钮与目标提示，保持技能槽预制体和原布局。</summary>
    private void EnsureTargetControls()
    {
        if (_aimButton != null)
        {
            return;
        }
        // 按钮使用当前角色窗口父级定位，不依赖新的资源或预制体绑定。
        GameObject buttonObject = new GameObject("AimButton", typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);
        // 以屏幕底部中央定位，按钮位于技能栏右上方，尺寸为 Canvas 单位。
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(510f, 170f);
        buttonRect.sizeDelta = new Vector2(100f, 38f);
        // 简背景用于显示和 UI 射线阻挡，避免瞄准点击穿到世界实体。
        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.13f, 0.17f, 0.20f, 0.95f);
        _aimButton = buttonObject.GetComponent<Button>();
        _aimButton.targetGraphic = background;
        _aimButton.onClick.AddListener(OnAimButtonClicked);
        // 使用项目现有中文字体，沿正式 Resources 路径取得，不修改字体资产。
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("UI/Font/Common SDF");
        // 按钮中文说明，只显示选择操作和桌面快捷键。
        TMP_Text buttonText = CreateTargetText("AimLabel", buttonObject.transform, font);
        buttonText.text = "瞄准 Tab";
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;
        _targetText = CreateTargetText("TargetLabel", transform, font);
        _targetText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        _targetText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        _targetText.rectTransform.anchoredPosition = new Vector2(0f, 210f);
        _targetText.rectTransform.sizeDelta = new Vector2(350f, 32f);
        // UI 晚于选择建立时仍显示当前玩家选中对象。
        MEntity target = null;
        if (Global.Instance != null && Global.Instance.SceneWorld != null &&
            Global.Instance.SceneWorld.LocalRole != null)
        {
            target = Global.Instance.SceneWorld.LocalRole.Target;
        }
        UpdateTarget(target);
    }

    /// <summary>创建当前选择区域的文字；文字不拦截世界和技能交互。</summary>
    /// <param name="name">窗口内部节点名，用于辨认瞄准标签或目标标签。</param>
    /// <param name="parent">该文字所属的正式 HUD 节点。</param>
    /// <param name="font">项目现有中文 TMP 字体。</param>
    /// <returns>供当前窗口更新的文本组件。</returns>
    private TMP_Text CreateTargetText(string name, Transform parent, TMP_FontAsset font)
    {
        // 每个控件只在窗口初始化时创建一次，选择更新只改文字。
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        // 采用统一的小字号和明亮文本，保持现有 HUD 的简洁风格。
        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.85f, 0.89f, 0.92f, 1f);
        text.raycastTarget = false;
        return text;
    }

    /// <summary>正式瞄准点击只通知控制器选敌，不直接施法。</summary>
    private void OnAimButtonClicked()
    {
        AimButtonClicked?.Invoke();
    }

    /// <summary>显示选中实体名称；怪物暂未接名称表，使用现配置编号辨认。</summary>
    /// <param name="target">当前世界的原选中实体，空值显示未选中目标。</param>
    public void UpdateTarget(MEntity target)
    {
        if (_targetText == null)
        {
            return;
        }
        if (target == null)
        {
            _targetText.text = "未选中目标";
            return;
        }
        // 玩家已有同步昵称，点击友方只显示选择，不作为自动攻击目标。
        RoleEntity role = target as RoleEntity;
        if (role != null && role.BaseInfo != null)
        {
            _targetText.text = "目标：" + role.BaseInfo.Nickname;
        }
        else
        {
            _targetText.text = "目标：怪物 " + target.ConfigId;
        }
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
        // 中央 R 槽只替换圆形外观，仍使用同一技能槽组件及真实快捷键绑定。
        string slotPrefabPath = "UIPrefabs/SkillSlotWidget";
        if (key == "R")
        {
            slotPrefabPath = "UIPrefabs/SkillCoreWidget";
        }
        // slotObject 是资源层交付的独立槽位实例，后续挂到预制体对应按键锚点。
        ResourceMgr.Instance.LoadPrefabAsync(slotPrefabPath, slotObject =>
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
        // 同名按键锚点负责左右分组和中央尺寸；旧界面没有锚点时保留原父节点接入。
        Transform slotAnchor = _skillSlotParent.Find(key);
        if (slotAnchor == null)
        {
            slotAnchor = _skillSlotParent;
        }
        slotObject.transform.SetParent(slotAnchor, false);
        // 预制体上的技能槽组件，只有存在时才纳入固定快捷栏。
        SkillSlotWidget slot = slotObject.GetComponent<SkillSlotWidget>();
        if (slot != null)
        {
            slot.SetKey(key);
            slot.Clear();
            if (key == "R")
            {
                slot.SetRageFull(_rageIsFull);
            }
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
            // 旧绑定槽位缺失。
            bool bindingMissing = previous == null;
            // 旧绑定存在时，依次比较技能 ID、等级与图标路径是否发生变化。
            bool bindingChanged = false;
            if (!bindingMissing)
            {
                bool skillIdChanged = previous.SkillId != skill.SkillId;
                bool levelChanged = previous.Level != skill.Level;
                bool iconChanged = previous.IconPath != skill.IconPath;
                bindingChanged = skillIdChanged || levelChanged || iconChanged;
            }
            if (bindingMissing || bindingChanged)
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
