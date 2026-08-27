
using System.Numerics;

/// <summary>
/// 扩展工具类
/// </summary>


public static class ExitUtils
{
    /// <summary>
    /// 将字符串转换成Vector3
    /// </summary>
    /// <param name="pos"></param>
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