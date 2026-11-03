using UnityEngine;

/// <summary>沿用 RO 模型准备数据，保存模型空间包围盒、蒙皮列表和阴影挂点。</summary>
public class MModelPrepareData : MonoBehaviour
{
    /// <summary>本模型的蒙皮渲染器，换装 NPC 可包含多个部件。</summary>
    public SkinnedMeshRenderer[] SkinnedMeshRenderers;
    /// <summary>模型未缩放时的包围盒尺寸，单位米。</summary>
    public Vector3 ProjectSize;
    /// <summary>模型根节点局部空间包围盒中心，单位米。</summary>
    public Vector3 ProjectCenter;
    /// <summary>包围盒最低、最高的局部 Y 坐标。</summary>
    public Vector2 BoundRange;
    /// <summary>RO 模型的阴影挂点，缺省允许为空。</summary>
    public Transform ShadowPoint;
    /// <summary>原模型是否忽略投影，本组件不创建额外阴影。</summary>
    public bool IgnoreShadow;
}
