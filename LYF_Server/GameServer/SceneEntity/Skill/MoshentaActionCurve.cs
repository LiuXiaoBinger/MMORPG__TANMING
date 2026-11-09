using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>沿用原服务器逐帧Curve文本及ROActionCurve::Evaluate插值。</summary>
internal sealed class MoshentaActionCurve
{
    /// <summary>原曲线采样时间。</summary>
    private readonly List<float> times = new List<float>();
    /// <summary>对应原值。</summary>
    private readonly List<float> values = new List<float>();
    /// <summary>读取原Curve，首行是WrapMode不是关键帧。</summary><param name="catalog">本副本选择的原曲线目录。</param><param name="name">原曲线名。</param>
    internal MoshentaActionCurve(MoshentaSkillCatalog catalog, string name)
    {
        // 原动作曲线只从当前副本的数据目录读取。
        string[] lines = File.ReadAllLines(Path.Combine(catalog.DirectoryPath, "Curve", Path.GetFileName(name) + ".txt"));
        for (int index = 1; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index])) continue;
            string[] pair = lines[index].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            times.Add(float.Parse(pair[0], CultureInfo.InvariantCulture)); values.Add(float.Parse(pair[1], CultureInfo.InvariantCulture));
        }
    }
    /// <summary>保留原33.3333ms索引和float线性插值。</summary><param name="seconds">子弹已经飞行秒数。</param>
    internal float Evaluate(float seconds)
    {
        int left = Math.Min((int)Math.Floor(seconds / 0.0333333f), values.Count - 1);
        int right = Math.Min((int)Math.Ceiling(seconds / 0.0333333f), values.Count - 1);
        if (left == right) return values[left];
        return values[left] + (values[right] - values[left]) * (seconds - times[left]) / (times[right] - times[left]);
    }
}
