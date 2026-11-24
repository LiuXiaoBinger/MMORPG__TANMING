using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>在本地角色名牌上方显示盾量，再在盾量上方显示按剩余比例填充的弧条。</summary>
// 与名牌一起在世界相机更新后计算位置和尺寸。
[DefaultExecutionOrder(110)]
public sealed class RoleShieldWidget : MonoBehaviour
{
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

        // 与昵称、称谓使用同一帧的场景相机及同一个头部锚点。
        Camera worldCamera = Camera.main;
        // anchor 是称谓底边中点，scale 是该角色本帧的投影比例。
        if (worldCamera == null || !_nameHud.TryGetHeadAnchor(_role, worldCamera, out Vector2 anchor, out float scale))
        {
            _content.gameObject.SetActive(false);
            return;
        }
        anchor.y += _nameHud.GetShieldOffset(_role) * scale;
        _content.localScale = Vector3.one * scale;
        _content.anchoredPosition = anchor;
        _content.gameObject.SetActive(true);
    }
}
