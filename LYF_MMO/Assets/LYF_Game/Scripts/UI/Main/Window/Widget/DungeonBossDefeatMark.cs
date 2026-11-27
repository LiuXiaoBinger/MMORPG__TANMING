using UnityEngine;
using UnityEngine.UI;

/// <summary>在已击败的首领头像上绘制由左上至右下、两端收尖的红色斩痕。</summary>
public sealed class DungeonBossDefeatMark : MaskableGraphic
{
    /// <summary>按头像实际矩形绘制暗红外缘、亮红刃口与短白色高光。</summary>
    /// <param name="mesh">UGUI 为本控件提供的顶点与索引缓冲。</param>
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Blade(mesh, 0.06f, 0.94f, 0.96f, 0.06f, 0.088f, new Color32(121, 7, 14, 242));
        Blade(mesh, 0.09f, 0.91f, 0.94f, 0.08f, 0.049f, new Color32(250, 35, 36, 255));
        Blade(mesh, 0.29f, 0.73f, 0.75f, 0.28f, 0.022f, new Color32(255, 225, 211, 255));
    }

    /// <summary>沿斜线添加一片实色刃面，尺寸随头像等比缩放。</summary>
    /// <param name="mesh">当前网格。</param><param name="x1">起点横向比例。</param><param name="y1">起点纵向比例。</param>
    /// <param name="x2">终点横向比例。</param><param name="y2">终点纵向比例。</param><param name="width">刃面半宽占矩形的比例。</param><param name="tint">本层颜色。</param>
    private void Blade(VertexHelper mesh, float x1, float y1, float x2, float y2, float width, Color32 tint)
    {
        // 实际画布内的四个局部坐标共用同一个矩形。
        Rect area = rectTransform.rect;
        // 菱形刃面的中点略靠左上，和斩痕的运动方向一致。
        Vector2 mid = Vector2.Lerp(new Vector2(x1, y1), new Vector2(x2, y2), 0.46f);
        // 此片首个顶点的位置，便于将相邻三角形复用同一条边。
        int start = mesh.currentVertCount;
        mesh.AddVert(Rect.NormalizedToPoint(area, new Vector2(x1, y1)), tint, Vector2.zero);
        mesh.AddVert(Rect.NormalizedToPoint(area, mid + Vector2.one * width), tint, Vector2.zero);
        mesh.AddVert(Rect.NormalizedToPoint(area, new Vector2(x2, y2)), tint, Vector2.zero);
        mesh.AddVert(Rect.NormalizedToPoint(area, mid - Vector2.one * width), tint, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start, start + 2, start + 3);
    }
}
