using UnityEngine;
using UnityEngine.UI;

/// <summary>将副本插画上的遮罩从左向右淡出，保留右侧场景细节。</summary>
public sealed class DungeonJournalShade : BaseMeshEffect
{
    /// <summary>文字一侧的遮罩颜色与不透明度。</summary>
    [SerializeField] private Color left;
    /// <summary>场景一侧的遮罩颜色与不透明度。</summary>
    [SerializeField] private Color right;

    /// <summary>在 UGUI 原有矩形顶点上插值，遮罩随画布宽度变化。</summary>
    /// <param name="mesh">当前图片用于绘制的网格。</param>
    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive()) return;
        // 归一化位置以该控件的实际绘制范围为准。
        Rect area = graphic.rectTransform.rect;
        // 只改颜色，保留原有几何和裁剪。
        for (int index = 0; index < mesh.currentVertCount; index++)
        {
            // 从当前顶点位置取左右插值比例。
            UIVertex vertex = default;
            mesh.PopulateUIVertex(ref vertex, index);
            vertex.color = Color.Lerp(left, right, Mathf.InverseLerp(area.xMin, area.xMax, vertex.position.x));
            mesh.SetUIVertex(vertex, index);
        }
    }
}
