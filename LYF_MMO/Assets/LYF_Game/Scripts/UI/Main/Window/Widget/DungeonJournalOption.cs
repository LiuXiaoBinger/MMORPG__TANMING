using UnityEngine;
using UnityEngine.UI;

/// <summary>副本列表、难度或层数的一行选择项，样式由各自的预制体模板指定。</summary>
public sealed class DungeonJournalOption : MonoBehaviour
{
    /// <summary>整行的点击区域。</summary>
    [SerializeField] private Button button;
    /// <summary>此行当前展示的副本名、难度名或层数。</summary>
    [SerializeField] private Text title;
    /// <summary>选中时出现的高亮底纹。</summary>
    [SerializeField] private GameObject selection;
    /// <summary>副本列表行的场景缩略图，难度与层数行无插画。</summary>
    [SerializeField] private RawImage illustration;
    /// <summary>选中行使用的文字颜色，由不同底色的模板分别指定。</summary>
    [SerializeField] private Color selectedText = Color.white;
    /// <summary>未选中行使用的文字颜色。</summary>
    [SerializeField] private Color normalText = Color.gray;

    /// <summary>给窗口绑定这一项的选择操作。</summary>
    public Button Button => button;

    /// <summary>显示该行的名称，可选用副本场景图。</summary>
    /// <param name="label">此项的玩家可见名称。</param><param name="texture">副本条目的场景缩略图。</param>
    public void Bind(string label, Texture texture = null)
    {
        title.text = label;
        if (illustration != null) illustration.texture = texture;
    }

    /// <summary>在原控件上切换选中色，滚动列表保持当前位置。</summary>
    /// <param name="active">是否为当前选择。</param>
    public void Select(bool active)
    {
        selection.SetActive(active);
        title.color = normalText;
        if (active) title.color = selectedText;
    }
    /// <summary>当前选项在视口外时滚动到可见位置，适用于重新打开高层或列表后段的副本。</summary>
    public void Reveal()
    {
        // 各种选项的滚动容器由正式预制体持有。
        ScrollRect scroll = GetComponentInParent<ScrollRect>();
        Canvas.ForceUpdateCanvases();
        // 超出视口的高度决定归一化滚动量。
        float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
        if (overflow <= 0f) return;
        // 在同一个视口坐标系中比较这一行的顶边和底边。
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
        // 视口上下边界均以视口自身的中心为参考。
        Rect viewport = scroll.viewport.rect;
        if (bounds.min.y < viewport.yMin)
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + (bounds.min.y - viewport.yMin) / overflow);
        else if (bounds.max.y > viewport.yMax)
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + (bounds.max.y - viewport.yMax) / overflow);
    }
}
