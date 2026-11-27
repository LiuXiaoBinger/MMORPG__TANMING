using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>战斗界面原有的两个药品快捷位，显示本角色装备的药水与背包余量。</summary>
public sealed class PotionBarWidget : MonoBehaviour
{
    /// <summary>一个快捷位在预制体中的按钮、图标、键帽与库存文字。</summary>
    [Serializable]
    private sealed class Slot
    {
        /// <summary>点击整个方格即可用药。</summary>
        public Button button;
        /// <summary>当前装备的药品图标，空槽隐藏。</summary>
        public Image icon;
        /// <summary>显示热键设置里已经应用的物理键。</summary>
        public TMP_Text key;
        /// <summary>显示已装备药品在背包中的合计数量。</summary>
        public TMP_Text count;
        /// <summary>本次图标请求对应的地址，换药时忽略旧回调。</summary>
        [NonSerialized] public string iconPath;
    }

    /// <summary>默认 F1 的法力药位。</summary>
    [SerializeField] private Slot manaSlot;
    /// <summary>默认 F2 的生命药位。</summary>
    [SerializeField] private Slot healthSlot;
    /// <summary>玩家点击药位时通知背包控制器，真代表法力药。</summary>
    public event Action<bool> UseRequested;

    /// <summary>给预制体的两处药位按钮注册各自的使用动作。</summary>
    private void Awake()
    {
        manaSlot.button.onClick.AddListener(() => UseRequested?.Invoke(true));
        healthSlot.button.onClick.AddListener(() => UseRequested?.Invoke(false));
    }

    /// <summary>进入场景或 HUD 再次显示时同步本机键位。</summary>
    private void OnEnable()
    {
        GameHotkeys.Changed += RefreshKeys;
        RefreshKeys();
    }

    /// <summary>HUD 隐藏后解除热键订阅。</summary>
    private void OnDisable() { GameHotkeys.Changed -= RefreshKeys; }

    /// <summary>使用设置中对应动作的实际键帽文字。</summary>
    private void RefreshKeys()
    {
        manaSlot.key.text = GameHotkeys.CommandLabel(GameHotkeys.Kind.Potion, GameHotkeys.ManaPotion);
        healthSlot.key.text = GameHotkeys.CommandLabel(GameHotkeys.Kind.Potion, GameHotkeys.HealthPotion);
    }

    /// <summary>装备变化时换图标，仅数量变化时更新文字和灰态。</summary>
    /// <param name="mana">真代表法力药位。</param><param name="path">药品配置图标地址，空串表示空槽。</param>
    /// <param name="held">背包同品种的当前总数。</param>
    public void Render(bool mana, string path, int held)
    {
        // 本次要更新的实际 HUD 方格。
        Slot slot = healthSlot;
        if (mana) slot = manaSlot;
        slot.count.text = string.Empty;
        if (!string.IsNullOrEmpty(path)) slot.count.text = held.ToString();
        slot.icon.color = Color.white;
        if (held == 0) slot.icon.color = Color.gray;
        if (slot.iconPath == path) return;
        slot.iconPath = path;
        slot.icon.enabled = false;
        if (string.IsNullOrEmpty(path)) return;
        ResourceMgr.Instance.LoadSpriteAsync(path, sprite =>
        {
            if (this == null || slot.iconPath != path) return;
            slot.icon.sprite = sprite;
            slot.icon.enabled = sprite != null;
        });
    }
}
