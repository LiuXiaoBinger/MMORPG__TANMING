using UnityEngine;
using UnityEngine.UI;

namespace ChatTest.UI
{
    /// <summary>
    /// 聊天设置控制器。挂在 Chatseting 预制体外部的场景对象上，所有对象引用均在 Inspector 中拖拽设置。
    /// </summary>
    public sealed class ChatSettingController : MonoBehaviour
    {
        [Header("外部引用")]
        [SerializeField] private GameObject chatSettingPanel;
        [SerializeField] private GameObject targetToggleGroup;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Toggle mainToggle;
        [SerializeField] private GameObjectHideType toggleGroupHideType = GameObjectHideType.Deactivate;
        [SerializeField] private GameObjectHideType settingPanelHideType = GameObjectHideType.Deactivate;

        private void Awake()
        {
            // 控制器挂在面板根节点时，面板引用必须始终指向自身，防止嵌套预制体实例丢失引用。
            if (chatSettingPanel == null)
            {
                chatSettingPanel = gameObject;
            }

            if (closeButton == null)
            {
                closeButton = GetComponentInChildren<Button>(true);
            }

        }

        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(HideSettings);
            if (mainToggle != null) mainToggle.onValueChanged.AddListener(OnMainToggleChanged);

            // 初始状态由 MainTogle 决定，避免设置面板隐藏时丢失频道栏状态。
            OnMainToggleChanged(mainToggle == null || mainToggle.isOn);
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(HideSettings);
            if (mainToggle != null) mainToggle.onValueChanged.RemoveListener(OnMainToggleChanged);
        }

        public void ShowSettings()
        {
            SetSettingsVisible(true);
        }

        public void HideSettings()
        {
            SetSettingsVisible(false);
        }

        private void SetSettingsVisible(bool visible)
        {
            if (chatSettingPanel == null)
            {
                Debug.LogWarning("ChatSettingController requires a Chat Setting Panel reference.", this);
                return;
            }

            chatSettingPanel.SetVisible(visible, settingPanelHideType);
        }

        private void OnMainToggleChanged(bool isOn)
        {
            if (targetToggleGroup != null)
            {
                targetToggleGroup.SetVisible(isOn, toggleGroupHideType);
            }
        }
    }
}
