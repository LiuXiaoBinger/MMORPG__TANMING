using UnityEngine;

/// <summary>主角附近可提交给服务器的门户或采集物身份，不保存本地完成状态。</summary>
public class MSceneInteraction
{
    /// <summary>原 XML 门户编号；零号也是有效物件。</summary>
    public int ObjectId;
    /// <summary>采集波次编号；非采集交互为零。</summary>
    public int WaveId;
    /// <summary>按钮显示的操作名。</summary>
    public string Title;
    /// <summary>交互区域中心，单位米。</summary>
    public Vector3 Position;
    /// <summary>区域朝向，用于将主角坐标转回原盒体坐标。</summary>
    public Quaternion Rotation;
    /// <summary>原配置交互盒体的半尺寸，单位米。</summary>
    public Vector3 Extents;

    /// <summary>判断主角是否靠近原交互盒；竖向容忍角色脚底与原盒中心差异。</summary>
    /// <param name="position">主角世界坐标，单位米。</param>
    public bool Contains(Vector3 position)
    {
        // 转回配置盒坐标后按三个轴独立判断。
        Vector3 local = Quaternion.Inverse(Rotation) * (position - Position);
        if (Mathf.Abs(local.y) > Mathf.Max(Extents.y, 2f)) return false;
        return Mathf.Abs(local.x) <= Extents.x && Mathf.Abs(local.z) <= Extents.z;
    }
}
