using UnityEngine;
using UnityEngine.UI;

/// <summary>绘制贴在右上角的四分之一黄铜罗盘。uGUI 通过 ICanvasRaycastFilter 将子控件点击限制在同一扇区。</summary>
public sealed class MinimapCompassRim : MaskableGraphic, ICanvasRaycastFilter
{
    /// <summary>开启时给 uGUI Mask 提供扇形网格，关闭时绘制可见的罗盘边框。</summary>
    [SerializeField] private bool _maskDisc;
    /// <summary>将九十度圆弧均分为 48 段，使放大后仍有平滑的外缘。</summary>
    private const int Segments = 48;

    /// <summary>只在 uGUI 重建几何时生成网格。</summary><param name="mesh">当前 Graphic 的顶点缓存。</param>
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        // 画布局部矩形，右上顶点是两条直边的交点。
        Rect bounds = GetPixelAdjustedRect();
        // 同心的外弧与可视遮罩都从各自的右上角向左下绘制。
        Vector2 center = bounds.max;
        // 扇区占一整个半径的宽与高，单位为画布像素。
        float radius = Mathf.Min(bounds.width, bounds.height);
        if (_maskDisc)
        {
            Band(mesh, center, 0f, radius, Color.white, Color.white);
            return;
        }
        // 下层衬底也使用扇形，弧线之外透出游戏场景。
        Band(mesh, center, 0f, radius, new Color32(69,55,42,255), new Color32(52,39,30,255));
        Band(mesh, center, radius - 2.4f, radius - 1f, new Color32(248,225,177,255), new Color32(174,132,86,255));
        Band(mesh, center, radius - 8.6f, radius - 2.4f, new Color32(132,105,76,255), new Color32(77,61,46,255));
        Band(mesh, center, radius - 11f, radius - 8.6f, new Color32(234,205,151,255), new Color32(162,122,72,255));
        // 弧内每五度放一枚刻度，十五度刻线长一些。
        for (int index = 1; index < 18; index++)
        {
            // 本次刻线的方位，以扇区左端为零。
            float bearing = index * 5f * Mathf.Deg2Rad;
            // 径向决定刻线长度，切向决定刻线宽度。
            Vector2 axis = new Vector2(-Mathf.Cos(bearing), -Mathf.Sin(bearing));
            Vector2 tangent = new Vector2(axis.y, -axis.x);
            // 小刻线和主刻线相对外弧的内缩距离与半宽。
            float inset = 5.8f;
            float halfWidth = .55f;
            if (index % 3 == 0) { inset = 8f; halfWidth = .95f; }
            // 刻线的内外端点均位于黄铜环上。
            Vector2 low = center + axis * (radius - inset);
            Vector2 high = center + axis * (radius - 3f);
            Quad(mesh, low - tangent * halfWidth, high - tangent * halfWidth, high + tangent * halfWidth, low + tangent * halfWidth, new Color32(235,210,164,255));
        }
    }

    /// <summary>判断视口局部坐标是否处在两条直边和左下圆弧之内。</summary>
    /// <param name="point">待绘制路标或点击的视口局部坐标，单位为画布像素。</param>
    public bool ContainsPoint(Vector2 point)
    {
        // 与生成网格时一致的可见矩形及半径。
        Rect bounds = GetPixelAdjustedRect();
        float radius = Mathf.Min(bounds.width, bounds.height);
        // 从右上角往左、往下为正。
        Vector2 inset = bounds.max - point;
        return inset.x >= 0f && inset.y >= 0f && inset.sqrMagnitude <= radius * radius;
    }

    /// <summary>uGUI 射线检查使用与可见地图相同的轮廓，弧外的场景照常接收点击。</summary>
    /// <param name="screenPoint">指针的屏幕像素位置。</param><param name="eventCamera">当前画布射线相机，覆盖画布时可为空。</param>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // 指针投影到本控件平面后的局部位置。
        Vector2 localPoint;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out localPoint) && ContainsPoint(localPoint);
    }

    /// <summary>沿玩家指向目标的射线，在直边或圆弧前收住传送图标，保留目的地方向。</summary>
    /// <param name="origin">角色锚点在视口中的局部坐标。</param><param name="offset">从玩家到门心的画布像素位移。</param>
    /// <param name="padding">边缘留给图标的像素间距。</param>
    public Vector2 ClampMarkerOffset(Vector2 origin, Vector2 offset, float padding)
    {
        // 顶边、右边和外弧共用右上角原点。
        Rect bounds = GetPixelAdjustedRect();
        Vector2 start = origin - bounds.max;
        // 允许行进的位移比例，1 表示门心就在可视范围内。
        float fraction = 1f;
        if (offset.x > 0f) fraction = Mathf.Min(fraction, (-padding - start.x) / offset.x);
        if (offset.y > 0f) fraction = Mathf.Min(fraction, (-padding - start.y) / offset.y);
        // 图标中心必须处于向内缩过的弧线内。
        float radius = Mathf.Min(bounds.width, bounds.height) - padding;
        if ((start + fraction * offset).sqrMagnitude > radius * radius)
        {
            // 取射线从内部向外穿过圆弧的正根；同点位移不会进入此分支。
            float lengthSquared = offset.sqrMagnitude;
            float projection = Vector2.Dot(start, offset);
            float discriminant = projection * projection + lengthSquared * (radius * radius - start.sqrMagnitude);
            fraction = (-projection + Mathf.Sqrt(discriminant)) / lengthSquared;
        }
        return offset * fraction;
    }

    /// <summary>绘制一段九十度的同心金属弧或实心遮罩，使用相同的端点避免接缝。</summary>
    /// <param name="mesh">当前画布几何。</param><param name="center">局部圆心。</param>
    /// <param name="inner">内半径，零表示实心。</param><param name="outer">外半径。</param>
    /// <param name="light">左上侧的亮部颜色。</param><param name="shade">右下侧的暗部颜色。</param>
    private static void Band(VertexHelper mesh, Vector2 center, float inner, float outer, Color light, Color shade)
    {
        // 每条辐射线提供内外一对顶点，相邻辐射线共享顶点。
        int first = mesh.currentVertCount;
        for (int index = 0; index <= Segments; index++)
        {
            // 从左侧直边到下侧圆弧端点的角度及局部单位方向。
            float angle = .5f * Mathf.PI * index / Segments;
            Vector2 normal = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle));
            // 靠左的黄铜面较亮，右下逐步转入暗部。
            Color tint = Color.Lerp(shade, light, 0.5f + .35f * (normal.y - normal.x));
            mesh.AddVert(center + normal * inner, tint, Vector2.zero);
            mesh.AddVert(center + normal * outer, tint, Vector2.zero);
            if (index == 0) continue;
            // 上一条辐射线的内侧顶点，与当前线组成两枚三角形。
            int previous = first + 2 * (index - 1);
            mesh.AddTriangle(previous, previous + 2, previous + 1);
            mesh.AddTriangle(previous + 1, previous + 2, previous + 3);
        }
    }

    /// <summary>将一枚刻度的四角写入画布。</summary><param name="mesh">当前画布几何。</param>
    /// <param name="a">内侧左角。</param><param name="b">外侧左角。</param><param name="c">外侧右角。</param>
    /// <param name="d">内侧右角。</param><param name="tint">刻度颜色。</param>
    private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        // 本枚刻度在总顶点缓存中的起点。
        int first = mesh.currentVertCount;
        mesh.AddVert(a, tint, Vector2.zero);
        mesh.AddVert(b, tint, Vector2.zero);
        mesh.AddVert(c, tint, Vector2.zero);
        mesh.AddVert(d, tint, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
