using UnityEngine;
using UnityEngine.UI;

namespace ChatTest.UI
{
    /// <summary>
    /// 控制聊天设置面板和频道开关组的显隐；优先使用 Inspector 引用，未指定面板时使用自身对象。
    /// </summary>
    public sealed class ChatSettingController : MonoBehaviour
    {
        /// <summary>需要打开或关闭的设置面板；未配置时在 Awake 中使用自身对象。</summary>
        [Header("外部引用")]
        [SerializeField] private GameObject chatSettingPanel;
        /// <summary>由主开关控制显隐的频道 Toggle 组。</summary>
        [SerializeField] private GameObject targetToggleGroup;
        /// <summary>保留的设置按钮引用；当前脚本没有为它注册点击回调。</summary>
        [SerializeField] private Button settingButton;
        /// <summary>关闭设置面板的按钮；未指定时查找子节点中的第一个 Button。</summary>
        [SerializeField] private Button closeButton;
        /// <summary>控制频道开关组是否显示的总开关。</summary>
        [SerializeField] private Toggle mainToggle;
        /// <summary>频道组采用禁用对象还是移出屏幕的方式隐藏。</summary>
        [SerializeField] private GameObjectHideType toggleGroupHideType = GameObjectHideType.Deactivate;
        /// <summary>设置面板的隐藏方式，显示时也用同一方式决定是否恢复位置。</summary>
        [SerializeField] private GameObjectHideType settingPanelHideType = GameObjectHideType.Deactivate;

        /// <summary>补齐缺失的面板和关闭按钮引用；不创建新的 UI 对象。</summary>
        private void Awake()
        {
            // 没有配置面板引用时，把本组件所在对象当作设置面板。
            if (chatSettingPanel == null)
            {
                chatSettingPanel = gameObject;
            }

            if (closeButton == null)
            {
                closeButton = GetComponentInChildren<Button>(true);
            }

        }

        /// <summary>订阅关闭按钮与总开关，并立即同步频道组显隐。</summary>
        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(HideSettings);
            if (mainToggle != null) mainToggle.onValueChanged.AddListener(OnMainToggleChanged);

            // 启用时按主开关刷新频道组；没有主开关引用时默认显示。
            OnMainToggleChanged(mainToggle == null || mainToggle.isOn);
        }

        /// <summary>解除本组件添加的关闭按钮与总开关监听。</summary>
        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(HideSettings);
            if (mainToggle != null) mainToggle.onValueChanged.RemoveListener(OnMainToggleChanged);
        }

        /// <summary>使用配置的显隐方式打开设置面板。</summary>
        public void ShowSettings()
        {
            SetSettingsVisible(true);
        }

        /// <summary>使用配置的显隐方式隐藏设置面板。</summary>
        public void HideSettings()
        {
            SetSettingsVisible(false);
        }

        /// <summary>将面板显隐交给共用扩展方法，引用缺失时仅记录警告。</summary>
        /// <param name="visible">true 显示设置面板，false 隐藏。</param>
        private void SetSettingsVisible(bool visible)
        {
            if (chatSettingPanel == null)
            {
                Debug.LogWarning("ChatSettingController requires a Chat Setting Panel reference.", this);
                return;
            }

            chatSettingPanel.SetVisible(visible, settingPanelHideType);
        }

        /// <summary>主开关变化时同步频道组显隐。</summary>
        /// <param name="isOn">频道组是否应该显示。</param>
        private void OnMainToggleChanged(bool isOn)
        {
            if (targetToggleGroup != null)
            {
                targetToggleGroup.SetVisible(isOn, toggleGroupHideType);
            }
        }
    }
}
