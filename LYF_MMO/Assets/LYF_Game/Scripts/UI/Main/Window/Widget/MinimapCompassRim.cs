using UnityEngine;
using UnityEngine.UI;

/// <summary>按画布尺寸绘制小地图的黄铜罗盘和圆形遮罩，在不同分辨率下保持表圈清晰。</summary>
public sealed class MinimapCompassRim : MaskableGraphic
{
    /// <summary>开启时只绘制用于 uGUI Mask 的圆片；关闭时绘制外框、刻度与铆钉。</summary>
    [SerializeField] private bool _maskDisc;
    /// <summary>圆周的分段数，直径约两百像素时能保持连续轮廓。</summary>
    private const int Segments = 120;

    /// <summary>只在 uGUI 重建几何时生成网格。</summary><param name="mesh">当前 Graphic 的顶点缓存。</param>
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        // 圆心、外半径均以当前 RectTransform 的像素坐标表示。
        Rect bounds = GetPixelAdjustedRect();
        Vector2 center = bounds.center;
        float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f;
        if (_maskDisc)
        {
            Band(mesh, center, 0f, radius, Color.white, Color.white);
            return;
        }
        // 阴影和轮廓压在地图下方，金属高光来自画布左上角。
        Band(mesh, center + Vector2.down * 2f, 0f, radius, new Color32(48,34,25,245), new Color32(38,27,24,245));
        Band(mesh, center, radius * .967f, radius, new Color32(247,221,163,255), new Color32(103,72,46,255));
        Band(mesh, center, radius * .944f, radius * .967f, new Color32(139,106,73,255), new Color32(65,47,37,255));
        Band(mesh, center, radius * .892f, radius * .944f, new Color32(103,82,62,255), new Color32(64,54,48,255));
        Band(mesh, center, radius * .873f, radius * .892f, new Color32(238,207,150,255), new Color32(163,125,78,255));
        // 四方长刻、斜向铆钉和中间短刻都留在表盘的金属环内。
        for (int index = 0; index < 40; index++)
        {
            float bearing = index * 2f * Mathf.PI / 40f;
            Vector2 axis = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing));
            Vector2 tangent = new Vector2(axis.y, -axis.x);
            float inner = .914f;
            float halfWidth = .6f;
            if (index % 5 == 0) { inner = .897f; halfWidth = 1.15f; }
            Vector2 low = center + axis * (radius * inner);
            Vector2 high = center + axis * (radius * .938f);
            Quad(mesh, low - tangent * halfWidth, high - tangent * halfWidth, high + tangent * halfWidth, low + tangent * halfWidth, new Color32(235,210,164,255));
        }
    }

    /// <summary>绘制金属环或遮罩圆片，使用同一圆周采样避免接缝。</summary>
    /// <param name="mesh">当前画布几何。</param><param name="center">局部圆心。</param>
    /// <param name="inner">内半径，零表示实心。</param><param name="outer">外半径。</param>
    /// <param name="light">左上侧的亮部颜色。</param><param name="shade">右下侧的暗部颜色。</param>
    private static void Band(VertexHelper mesh, Vector2 center, float inner, float outer, Color light, Color shade)
    {
        // 每条辐射线提供内外一对顶点，相邻辐射线共享顶点。
        int first = mesh.currentVertCount;
        for (int index = 0; index <= Segments; index++)
        {
            float angle = 2f * Mathf.PI * index / Segments;
            Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Color tint = Color.Lerp(shade, light, 0.5f + .35f * (normal.y - normal.x));
            mesh.AddVert(center + normal * inner, tint, Vector2.zero);
            mesh.AddVert(center + normal * outer, tint, Vector2.zero);
            if (index == 0) continue;
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
