using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

/// <summary>RO 编辑 XML 的序列化边界；保留未知字段，避免精简数据类型丢失原副本配置。</summary>
public static class ROSceneXml
{
    /// <summary>按斜杠路径取得元素；缺少字段时返回空，不改写原文档。</summary>
    /// <param name="root">当前字段的父元素。</param>
    /// <param name="path">相对字段路径。</param>
    public static XElement Find(XElement root, string path)
    {
        // 字段层级按原 XML 顺序逐级定位。
        string[] parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (root == null) return null;
            root = root.Element(parts[i]);
        }
        return root;
    }

    /// <summary>取得字段文本，缺失字段采用调用方给出的默认值。</summary>
    /// <param name="root">字段所属 XML 对象。</param>
    /// <param name="path">字段相对路径。</param>
    /// <param name="fallback">字段未保存时的默认文本。</param>
    public static string Text(XElement root, string path, string fallback = "")
    {
        // 保持空字符串与缺失字段的区别。
        XElement element = Find(root, path);
        if (element == null) return fallback;
        return element.Value;
    }

    /// <summary>读取原配置的整数编号、计数或枚举数值。</summary>
    /// <param name="root">字段所属对象。</param>
    /// <param name="path">字段相对路径。</param>
    /// <param name="fallback">缺失或非整数时的默认值。</param>
    public static int Int(XElement root, string path, int fallback = 0)
    {
        // 转换结果只用于编辑字段，不改变原始文本。
        int value;
        if (int.TryParse(Text(root, path), out value)) return value;
        return fallback;
    }

    /// <summary>以固定小数点读取坐标、比例和时间。</summary>
    /// <param name="root">字段所属对象。</param>
    /// <param name="path">字段相对路径。</param>
    /// <param name="fallback">未配置时的默认浮点数。</param>
    public static float Float(XElement root, string path, float fallback = 0f)
    {
        // RO XML 使用英文小数点，不依赖当前系统区域。
        float value;
        if (float.TryParse(Text(root, path), NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return value;
        return fallback;
    }

    /// <summary>读取原配置中的三维向量，坐标和尺寸单位沿用米。</summary>
    /// <param name="root">字段所属对象。</param>
    /// <param name="path">包含 x、y、z 的父字段。</param>
    public static Vector3 Vector(XElement root, string path)
    {
        // 三个坐标分量共享同一父元素。
        XElement element = Find(root, path);
        return new Vector3(Float(element, "x"), Float(element, "y"), Float(element, "z"));
    }

    /// <summary>设置一个文本字段，仅为当前编辑字段补齐缺失层级。</summary>
    /// <param name="root">字段所属对象。</param>
    /// <param name="path">字段相对路径。</param>
    /// <param name="value">按 RO 格式保存的文本。</param>
    public static void Set(XElement root, string path, string value)
    {
        // 编辑器只创建用户正在修改的字段，不重新生成整份配置。
        string[] parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            // 当前层级已有元素优先保留其身份与属性。
            XElement child = root.Element(parts[i]);
            if (child == null)
            {
                child = new XElement(parts[i]);
                root.Add(child);
            }
            root = child;
        }
        root.Value = value;
    }

    /// <summary>将向量按原有 x/y/z 字段写回，保留父节点的其他内容。</summary>
    /// <param name="root">字段所属对象。</param>
    /// <param name="path">向量字段路径。</param>
    /// <param name="value">场景坐标、欧拉角或尺寸。</param>
    public static void SetVector(XElement root, string path, Vector3 value)
    {
        Set(root, path + "/x", value.x.ToString("R", CultureInfo.InvariantCulture));
        Set(root, path + "/y", value.y.ToString("R", CultureInfo.InvariantCulture));
        Set(root, path + "/z", value.z.ToString("R", CultureInfo.InvariantCulture));
    }

    /// <summary>读取节点对应的实际刷点，支持怪物、NPC 和采集物的原始字段。</summary>
    /// <param name="data">画布节点引用的配置对象。</param>
    public static XElement[] SpawnPoints(XElement data)
    {
        if (data == null) return new XElement[0];
        // 怪物数据保存多个 MScenePositionData；NPC 使用 position_info 单点。
        XElement[] points = data.Descendants("MScenePositionData").ToArray();
        if (points.Length > 0) return points;
        return data.Descendants("position_info").Where(element => element.Element("position") != null).ToArray();
    }
}
