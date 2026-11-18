using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>预制体中的一个可发送表情，提供正式标记、名称和图集区域。</summary>
public sealed class YimengChatEmojiTile : MonoBehaviour
{
    /// <summary>选中此表情的按钮，依发言频道切换可用状态。</summary>
    [SerializeField] private Button _button;
    /// <summary>格子下方的表情名称，也用于聊天速览。</summary>
    [SerializeField] private TMP_Text _caption;
    /// <summary>引用通用表情图集的贴图，UV 指定此格子的绘制范围。</summary>
    [SerializeField] private RawImage _icon;
    /// <summary>写入现有聊天文本字段的完整标记，供旧记录和广播按原文匹配。</summary>
    [SerializeField] private string _marker;

    /// <summary>选择面板用于控制发言能力与注册点击的按钮。</summary>
    internal Button Button => _button;
    /// <summary>速览使用的表情显示名，和格子上的标题一致。</summary>
    internal string Caption => _caption.text;
    /// <summary>整条消息匹配时识别的正式文本标记。</summary>
    internal string Marker => _marker;

    /// <summary>让消息行绘制与选中格子相同的图集区域。</summary>
    /// <param name="target">正式消息气泡内的表情图像控件。</param>
    internal void CopyIconTo(RawImage target)
    {
        target.texture = _icon.texture;
        target.uvRect = _icon.uvRect;
    }
}
