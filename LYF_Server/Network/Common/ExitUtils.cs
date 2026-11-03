
using System.Numerics;

/// <summary>
/// 服务端坐标文本解析扩展，解析数据库中用下划线连接的三维位置。
/// </summary>


public static class ExitUtils
{
    /// <summary>
    /// 把 x_y_z 文本转为 Vector3；不足三项返回零向量，单项解析失败时该坐标为 0。
    /// </summary>
    /// <param name="pos">下划线分隔的坐标，数字按服务器当前区域格式解析。</param>
    /// <returns></returns>
    public static Vector3 ToVector3(this string pos)
    {
        if (string.IsNullOrEmpty(pos)) return Vector3.Zero;
        Vector3 v = new Vector3();
        string[] posattr = pos.Split('_');
        if (posattr.Length >= 3)
        {
            float.TryParse(posattr[0], out v.X);
            float.TryParse(posattr[1], out v.Y);
            float.TryParse(posattr[2], out v.Z);
        }
        return v;
    }
}