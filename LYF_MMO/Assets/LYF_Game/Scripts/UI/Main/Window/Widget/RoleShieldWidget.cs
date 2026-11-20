using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>将本地角色的权威护盾数值投影到头顶，白色弧条按剩余比例填充。</summary>
public sealed class RoleShieldWidget : MonoBehaviour
{
    /// <summary>头部锚点到护盾控件下沿的世界间距，单位米。</summary>
    private const float HeadGap = 0.28f;

    /// <summary>可整体隐藏的护盾内容，锚点位于主画布中央。</summary>
    [SerializeField] private RectTransform _content;

    /// <summary>从左到右填充的弧条，值域为零到一。</summary>
    [SerializeField] private Image _fill;

    /// <summary>显示整点剩余吸收量的数字，零盾时随内容一起隐藏。</summary>
    [SerializeField] private TMP_Text _amount;

    /// <summary>同一画布的角色名牌，护盾要显示在昵称和称谓上方。</summary>
    [SerializeField] private RoleNameHudWidget _nameHud;

    /// <summary>护盾生效期间跟随的本地场景角色。</summary>
    private PlayerEntity _role;

    /// <summary>随主界面实例存活的画布，用于屏幕坐标换算。</summary>
    private Canvas _canvas;

    /// <summary>初始化时取得主界面的画布引用。</summary>
    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>根据服务端快照刷新数字和白条，清盾快照直接收起提示。</summary>
    /// <param name="role">当前护盾所属的本地场景角色。</param>
    /// <param name="remaining">当前剩余可吸收量，单位为生命值点数。</param>
    /// <param name="capacity">该次开盾时的最大可吸收量，单位为生命值点数。</param>
    public void SetShield(PlayerEntity role, int remaining, int capacity)
    {
        if (remaining <= 0 || capacity <= 0)
        {
            Clear();
            return;
        }

        _role = role;
        _amount.SetText(remaining.ToString(CultureInfo.InvariantCulture));
        _fill.fillAmount = Mathf.Clamp01(remaining / (float)capacity);
    }

    /// <summary>移除当前角色的头顶提示，停止对其位置的逐帧跟随。</summary>
    public void Clear()
    {
        _role = null;
        _content.gameObject.SetActive(false);
    }

    /// <summary>角色与相机本帧移动完成后，按同一画布坐标定位护盾提示。</summary>
    private void LateUpdate()
    {
        if (_role == null)
        {
            return;
        }

        // 主场景相机负责世界到屏幕投影，切换场景期间可能暂时为空。
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            _content.gameObject.SetActive(false);
            return;
        }

        // 模型就绪时以实际头骨定位，动作或身高变化都会带动提示。
        Animator animator = _role.Model.Ator.UnityAnimator;
        // 模型尚未加载时，按实体的世界位置与身高提供同一角色的临时锚点。
        Vector3 head = _role.Position + Vector3.up * _role.Height;
        if (animator != null && animator.isHuman)
        {
            // 人形骨架中 Head 关节的位置是经过本帧动画更新的世界坐标。
            Transform bone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (bone != null)
            {
                head = bone.position;
            }
        }
        // 投影结果的横纵坐标为屏幕像素，深度为负表示目标位于相机背后。
        Vector3 screen = worldCamera.WorldToScreenPoint(head + Vector3.up * HeadGap);
        if (screen.z <= 0f)
        {
            _content.gameObject.SetActive(false);
            return;
        }

        // 覆盖画布使用空相机，主界面的相机画布使用自身绑定的 UI 相机。
        Camera uiCamera = _canvas.worldCamera;
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null;
        }

        // 控件根完整覆盖所在画布，转换结果以中心点为原点，单位为 UI 单位。
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, uiCamera,
            out Vector2 anchor))
        {
            if (_nameHud != null)
            {
                anchor.y += _nameHud.GetShieldOffset(_role);
            }
            _content.anchoredPosition = anchor;
            _content.gameObject.SetActive(true);
        }
    }
}
