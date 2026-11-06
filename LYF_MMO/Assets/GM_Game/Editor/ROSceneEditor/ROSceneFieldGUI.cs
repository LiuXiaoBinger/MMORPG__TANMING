using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>原 XML 字段的编辑面板；只改用户选中的值，保留完整的 RO 机制参数。</summary>
public sealed class ROSceneFieldGUI
{
    /// <summary>RO 原前置波次枚举值，保存时不替换为中文显示名称。</summary>
    private static readonly string[] preWaveValues = { "PreWaveOptionTypeNone", "PreWaveOptionTypeAnd", "PreWaveOptionTypeOr", "PreWaveOptionTypeWaitEvent", "PreWaveOptionTypeTriggerEvent" };
    /// <summary>前置条件在属性面板中的策划名称，与原枚举一一对应。</summary>
    private static readonly string[] preWaveLabels = { "无前置", "全部前置满足", "任一前置满足", "等待 AI 事件", "等待触发器事件" };
    /// <summary>当前窗口各原 XML 容器的展开状态。</summary>
    private readonly Dictionary<XElement, bool> expanded = new Dictionary<XElement, bool>();
    /// <summary>常用原字段的中文名称，不改动序列化字段名。</summary>
    private static readonly Dictionary<string, string> labels = new Dictionary<string, string>
    {
        { "base_info", "波次基础" }, { "id", "编号" }, { "spawn_type", "生成类型" },
        { "pre_wave_option", "前置波次条件" }, { "pre_wave_list", "前置波次列表" },
        { "pre_scene_ai_event", "等待事件名" }, { "end_scene_ai_event", "结束发送 AI 事件" },
        { "delay_create_time", "创建延迟（毫秒）" }, { "not_active_create", "不主动创建" },
        { "init_invisible", "出生不可见" }, { "monster_info", "怪物配置" },
        { "entity_id", "怪物表 ID" }, { "monster_base_data", "怪物基础数据" },
        { "entity_info", "生成参数" }, { "position_list", "刷点列表" },
        { "position", "位置（米）" }, { "face", "朝向（度）" },
        { "count", "数量" }, { "real_num", "实际刷新数量" }, { "born_radius", "出生半径（米）" },
        { "npc_info", "NPC 配置" }, { "npc_id", "NPC 表 ID" }, { "position_info", "刷点" },
        { "timing_type", "触发时机" }, { "result_type", "触发结果" },
        { "condition_type", "触发条件" }, { "scene_trigger_obj_id", "区域/对象 ID" },
        { "result_index", "执行顺序" }, { "enable", "启用" }, { "reset", "允许重置" },
        { "box", "箱形区域" }, { "scale", "尺寸" }, { "offset", "偏移" },
        { "ignore_init", "等待显式创建" }, { "pre_wave_id", "前置波次 ID" },
        { "pre_wave_blood", "前置血量阈值（%）" }, { "pre_wave_num", "前置数量阈值（%）" },
        { "wave_event_info", "波次事件" }, { "wave_event", "事件名" },
        { "scene_ai_event", "场景 AI 事件" }, { "ai_event", "AI 事件名" },
        { "timer_info", "延迟执行" }, { "delay_time", "延迟（毫秒）" },
        { "trans_position_info", "传送" }, { "related_to_trigger_info", "关联触发器组" },
        { "trigger_group_id", "触发器组 ID" }, { "interval_rush_data", "分批刷新" },
        { "interval", "间隔（秒）" }, { "is_related", "关联启用" }
    };

    /// <summary>绘制一个原数据对象的所有子字段。</summary>
    /// <param name="data">所选节点的原序列化数据。</param>
    public void Draw(XElement data)
    {
        if (data == null) { EditorGUILayout.HelpBox("该节点没有独立数据对象。", MessageType.Info); return; }
        // 子元素数组保证绘制过程中仍引用原对象。
        XElement[] children = data.Elements().ToArray();
        for (int i = 0; i < children.Length; i++) DrawElement(children[i], 0);
    }

    /// <summary>递归显示原字段，向量、布尔和数值采用对应 Unity 控件。</summary>
    /// <param name="element">当前字段或容器。</param>
    /// <param name="depth">显示缩进层级。</param>
    private void DrawElement(XElement element, int depth)
    {
        // 中文标签同时通过提示保留原字段名，方便对照源项目。
        string label;
        if (!labels.TryGetValue(element.Name.LocalName, out label)) label = element.Name.LocalName;
        GUIContent content = new GUIContent(label, element.Name.LocalName);
        if (element.HasElements)
        {
            // 原向量固定包含 x/y/z 三个分量。
            if (element.Elements().Count() == 3 && element.Element("x") != null && element.Element("z") != null)
            {
                // 绘制前后的向量仅在发生修改时写回。
                Vector3 value = new Vector3(ROSceneXml.Float(element, "x"), ROSceneXml.Float(element, "y"), ROSceneXml.Float(element, "z"));
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.Vector3Field(content, value);
                if (EditorGUI.EndChangeCheck())
                {
                    element.Element("x").Value = value.x.ToString("R", CultureInfo.InvariantCulture);
                    element.Element("y").Value = value.y.ToString("R", CultureInfo.InvariantCulture);
                    element.Element("z").Value = value.z.ToString("R", CultureInfo.InvariantCulture);
                }
                return;
            }
            // 一级业务字段默认展开，深层列表按需查看。
            bool open;
            if (!expanded.TryGetValue(element, out open)) open = depth == 0;
            open = EditorGUILayout.Foldout(open, content, true);
            expanded[element] = open;
            if (!open) return;
            EditorGUI.indentLevel++;
            // 保持原 XML 子字段顺序，所有未知字段仍可查看编辑。
            XElement[] children = element.Elements().ToArray();
            for (int i = 0; i < children.Length; i++) DrawElement(children[i], depth + 1);
            if (element.Name.LocalName == "base_info")
            {
                // 原 XML 会省略空字符串，新波次仍需能够直接填写事件字段。
                DrawMissingText(element, "pre_scene_ai_event", "等待事件名");
                DrawMissingText(element, "end_scene_ai_event", "结束发送 AI 事件");
                DrawIntervalRush(element);
            }
            EditorGUI.indentLevel--;
            return;
        }
        // 空容器原样保留，禁止把无子元素的列表误写为文本。
        if (element.IsEmpty && element.Value.Length == 0)
        {
            if (!element.Name.LocalName.EndsWith("event")) return;
        }
        EditorGUI.BeginChangeCheck();
        // 输入字段优先按原保存值判断类型。
        string updated = element.Value;
        int integer;
        float number;
        if (element.Name.LocalName == "pre_wave_option")
        {
            // 未知枚举继续显示原文本，已知枚举通过下拉框避免拼写错误。
            int index = System.Array.IndexOf(preWaveValues, updated);
            if (index >= 0) updated = preWaveValues[EditorGUILayout.Popup(label, index, preWaveLabels)];
            else updated = EditorGUILayout.TextField(content, updated);
        }
        else if (updated == "true" || updated == "false")
        {
            // 原 XML 布尔固定保存小写文本。
            bool value = EditorGUILayout.Toggle(content, updated == "true");
            updated = value.ToString().ToLowerInvariant();
        }
        else if (int.TryParse(updated, out integer)) updated = EditorGUILayout.IntField(content, integer).ToString(CultureInfo.InvariantCulture);
        else if (float.TryParse(updated, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) updated = EditorGUILayout.FloatField(content, number).ToString("R", CultureInfo.InvariantCulture);
        else updated = EditorGUILayout.TextField(content, updated);
        if (EditorGUI.EndChangeCheck()) element.Value = updated;
    }

    /// <summary>允许编辑被原 XML 省略的空事件名，仅在用户输入后添加字段。</summary>
    /// <param name="data">波次基础数据容器。</param>
    /// <param name="name">原事件字段名。</param>
    /// <param name="label">属性面板显示名称。</param>
    private static void DrawMissingText(XElement data, string name, string label)
    {
        if (data.Element(name) != null) return;
        EditorGUI.BeginChangeCheck();
        // 空值保持原文件省略字段的状态。
        string value = EditorGUILayout.TextField(label, "");
        if (EditorGUI.EndChangeCheck()) data.Add(new XElement(name, value));
    }

    /// <summary>沿用原 interval_rush_data 配置间隔刷怪，取消时移除该可选容器。</summary>
    /// <param name="data">波次基础数据，保存原分批数量和秒数。</param>
    private static void DrawIntervalRush(XElement data)
    {
        // 已有字段由上面的递归面板编辑，此处只控制启用与关闭。
        XElement rush = data.Element("interval_rush_data");
        EditorGUI.BeginChangeCheck();
        // RO 将没有该可选对象解释为一次性刷新。
        bool enabled = EditorGUILayout.Toggle("启用间隔刷怪", rush != null);
        if (!EditorGUI.EndChangeCheck()) return;
        if (enabled) data.Add(new XElement("interval_rush_data", new XElement("count", 1), new XElement("interval", 1)));
        else rush.Remove();
    }
}
