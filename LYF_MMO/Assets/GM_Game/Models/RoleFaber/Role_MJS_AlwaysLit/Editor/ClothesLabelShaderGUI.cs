// 衣物 ShaderGraph 的双语材质面板：临时替换内置面板标签，绘制完成后还原。
// 本类只处理显示文案，实际属性控件仍交给 Unity 内置 Lit 或 Unlit 面板。
// 外层 UNITY_EDITOR 保证 UnityEditor 和反射面板代码只参与编辑器编译。
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>为衣物材质复用 Unity 内置面板，并在一次绘制期间显示中英双语标签。</summary>
public class ClothesLabelShaderGUI : ShaderGUI
{
    // 内置面板 GUI 的完整类型名；工程程序集未引用 ShaderGraph 编辑器程序集，因此按名查找
    private const string LitGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI";
    // 不含 _Metallic_Strength 属性时使用的内置无光照面板类型名。
    private const string UnlitGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInUnlitGUI";
    // 保存共用 Styles 标签的内置面板基类类型名。
    private const string BaseGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInBaseShaderGUI";

    // 最近一次创建的内置面板实例；所需类型未变时复用，Lit/Unlit 切换时重新创建
    private static ShaderGUI s_inner = null;
    // s_inner 实际对应的类型名，用来判断下次绘制是否可复用。
    private static string s_innerTypeName = "";

    // 需要临时改文案的 GUIContent 字段名（对应内置面板的折叠标题与控件标签）
    private static readonly string[] ContentFields = new string[]
    {
        "SurfaceOptions",
        "SurfaceInputs",
        "AdvancedLabel",
        "surfaceType",
        "blendingMode",
        "cullingText",
        "zwriteText",
        "ztestText",
        "alphaClipText",
        "queueControl",
        "queueSlider"
    };

    // 需要临时改显示名的字符串数组字段名（下拉框选项）
    private static readonly string[] ArrayFields = new string[]
    {
        "surfaceTypeNames",
        "blendModeNames",
        "renderFaceNames",
        "ztestNames"
    };

    // 按静态字段名返回下拉选项的原英文；未命中前三类时返回深度测试选项。
    /// <param name="fieldName">内置下拉选项数组的静态字段名。</param>
    private static string[] EnglishOf(string fieldName)
    {
        if (fieldName == "surfaceTypeNames")
        {
            return new string[] { "Opaque", "Transparent" };
        }
        if (fieldName == "blendModeNames")
        {
            return new string[] { "Alpha", "Premultiply", "Additive", "Multiply" };
        }
        if (fieldName == "renderFaceNames")
        {
            return new string[] { "Front", "Back", "Both" };
        }
        return new string[] { "Disabled", "Never", "Less", "Equal", "LessEqual", "Greater", "NotEqual", "GreaterEqual", "Always" };
    }

    // 下拉框选项的译文，与 EnglishOf 的下标严格对应
    /// <param name="fieldName">选项数组字段名；返回的中文与 EnglishOf 同下标配对。</param>
    private static string[] ChineseOf(string fieldName)
    {
        if (fieldName == "surfaceTypeNames")
        {
            return new string[] { "不透明", "透明" };
        }
        if (fieldName == "blendModeNames")
        {
            return new string[] { "Alpha 混合", "预乘", "叠加", "正片叠底" };
        }
        if (fieldName == "renderFaceNames")
        {
            return new string[] { "正面", "背面", "双面" };
        }
        return new string[] { "禁用", "从不", "小于", "等于", "小于等于", "大于", "不等于", "大于等于", "总是" };
    }

    // 绘制入口：临时换文案 -> 交给原面板绘制 -> 还原文案
    /// <param name="materialEditor">Unity 提供的当前材质编辑器，负责实际属性编辑。</param>
    /// <param name="properties">当前选择材质的属性集合，原样交给内置面板绘制。</param>
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // 本次选用的 Lit 或 Unlit 面板；找不到时退回 ShaderGUI 默认绘制。
        ShaderGUI inner = ResolveInner(materialEditor);
        if (inner == null)
        {
            // 拿不到内置面板时退回默认属性绘制，保证面板仍可用
            base.OnGUI(materialEditor, properties);
            return;
        }

        // 本次绘制独享的原文记录，用于 finally 中恢复共用静态标签。
        LabelScope scope = new LabelScope();
        try
        {
            scope.Capture();
            ApplyBuiltInLabels();
            inner.OnGUI(materialEditor, properties);
        }
        finally
        {
            // 无论绘制是否抛异常，都把内置面板文案还原成原样
            scope.Restore();
        }
    }

    // 按材质属性判断该用 Lit 还是 Unlit 面板，并创建/复用代理实例
    /// <param name="materialEditor">从其 target 读取当前材质及 _Metallic_Strength 属性是否存在。</param>
    private static ShaderGUI ResolveInner(MaterialEditor materialEditor)
    {
        // 默认使用 Unlit，检测到金属强度属性后改选 Lit。
        string want = UnlitGuiTypeName;
        // 当前编辑器的主目标材质，不是复制出来的新材质。
        Material mat = materialEditor.target as Material;
        if (mat != null)
        {
            if (mat.HasProperty("_Metallic_Strength"))
            {
                want = LitGuiTypeName;
            }
        }
        if (s_inner != null && s_innerTypeName == want)
        {
            return s_inner;
        }
        // 在已加载程序集里找到面板类型，缺失时无法创建代理。
        Type t = FindType(want);
        if (t == null)
        {
            return null;
        }
        try
        {
            s_inner = (ShaderGUI)Activator.CreateInstance(t);
            s_innerTypeName = want;
        }
        catch (Exception)
        {
            s_inner = null;
            s_innerTypeName = "";
        }
        return s_inner;
    }

    // 临时修改内置 Styles 的静态文案，作用持续到本次 OnGUI 的 finally 恢复为止。
    private static void ApplyBuiltInLabels()
    {
        // 内置面板中保存共享标签和选项数组的 Styles 类型。
        Type stylesT = ResolveStylesType();
        if (stylesT == null)
        {
            return;
        }
        SetContent(stylesT, "SurfaceOptions", "表面选项（Surface Options）");
        SetContent(stylesT, "SurfaceInputs", "表面输入（Surface Inputs）");
        SetContent(stylesT, "AdvancedLabel", "高级选项（Advanced Options）");
        SetContent(stylesT, "surfaceType", "表面类型（Surface Type）");
        SetContent(stylesT, "blendingMode", "混合模式（Blending Mode）");
        SetContent(stylesT, "cullingText", "渲染面（Render Face）");
        SetContent(stylesT, "zwriteText", "深度写入（Depth Write）");
        SetContent(stylesT, "ztestText", "深度测试（Depth Test）");
        SetContent(stylesT, "alphaClipText", "Alpha 裁剪（Alpha Clipping）");
        SetContent(stylesT, "queueControl", "队列控制（Queue Control）");
        SetContent(stylesT, "queueSlider", "排序优先级（Sorting Priority）");

        // 下拉框只换显示文字，存进材质的仍是原索引，语义不变
        for (int i = 0; i < ArrayFields.Length; i++)
        {
            LocalizeNames(stylesT, ArrayFields[i], EnglishOf(ArrayFields[i]), ChineseOf(ArrayFields[i]));
        }
    }

    // 取内置面板的 Styles 嵌套类型
    private static Type ResolveStylesType()
    {
        // 先找到内置基类，再从中取得公开或非公开的 Styles 嵌套类型。
        Type baseT = FindType(BaseGuiTypeName);
        if (baseT == null)
        {
            return null;
        }
        return baseT.GetNestedType("Styles", BindingFlags.Public | BindingFlags.NonPublic);
    }

    // 改单个静态 GUIContent 的正文
    /// <param name="stylesT">包含目标静态字段的 Styles 类型。</param>
    /// <param name="fieldName">要修改的 GUIContent 字段名。</param>
    /// <param name="text">本次绘制要显示的双语正文。</param>
    private static void SetContent(Type stylesT, string fieldName, string text)
    {
        // 按名称取得内置 Styles 的静态字段，公开和非公开字段都允许查找。
        FieldInfo f = stylesT.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null)
        {
            return;
        }
        // 反射读到的共享标签对象，只改 text，不替换对象引用或提示信息。
        GUIContent gc = f.GetValue(null) as GUIContent;
        if (gc == null)
        {
            return;
        }
        gc.text = text;
    }

    // 改字符串数组里的显示名；按原英文名匹配，长度与顺序都不变，索引语义不变
    /// <param name="stylesT">保存选项数组的 Styles 类型。</param>
    /// <param name="fieldName">要处理的静态字符串数组字段名。</param>
    /// <param name="en">按相同下标与中文配对的原英文选项。</param>
    /// <param name="zh">每个英文选项的中文译文，只有下标有效时替换。</param>
    private static void LocalizeNames(Type stylesT, string fieldName, string[] en, string[] zh)
    {
        if (en == null)
        {
            return;
        }
        if (zh == null)
        {
            return;
        }
        // 按名称取得内置 Styles 的静态字段，公开和非公开字段都允许查找。
        FieldInfo f = stylesT.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null)
        {
            return;
        }
        // 内置面板正在使用的选项数组，逐项原地修改而不改变选项顺序。
        string[] arr = f.GetValue(null) as string[];
        if (arr == null)
        {
            return;
        }
        // i 是内置选项的位置，k 用来查这项英文在翻译对照表中的位置。
        for (int i = 0; i < arr.Length; i++)
        {
            for (int k = 0; k < en.Length; k++)
            {
                if (arr[i] == en[k] && k < zh.Length)
                {
                    arr[i] = zh[k] + " " + en[k];
                    break;
                }
            }
        }
    }

    // 在所有已加载程序集里按全名找类型（内置面板类型不在本程序集引用范围内）
    /// <param name="fullName">含命名空间的类型全名，先直接查询，再遍历已加载程序集。</param>
    private static Type FindType(string fullName)
    {
        // 直接按全名查询的结果，成功后无需枚举程序集。
        Type t = Type.GetType(fullName);
        if (t != null)
        {
            return t;
        }
        // 当前 AppDomain 已加载的程序集快照，依次寻找内置 ShaderGraph 类型。
        Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
            // 当前程序集内与完整类型名相符的类型，未找到时继续查下一项。
            Type found = asms[i].GetType(fullName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    // 文案快照：记录被临时改动的 GUIContent 原文与字符串数组原值，用于绘制后原样还原
    private class LabelScope
    {
        // 本次保存过原文的 GUIContent 引用，Restore 会把文本写回同一对象
        private readonly List<GUIContent> m_contents = new List<GUIContent>();
        // 与 m_contents 一一对应的原始正文
        private readonly List<string> m_texts = new List<string>();
        // 本次保存过内容的静态字符串数组引用，恢复时不替换数组实例
        private readonly List<string[]> m_arrays = new List<string[]>();
        // 与 m_arrays 一一对应的原值副本
        private readonly List<string[]> m_clones = new List<string[]>();

        // 记录当前静态文案原值
        public void Capture()
        {
            // 本次需要保存文案的共享 Styles 类型。
            Type stylesT = ResolveStylesType();
            if (stylesT == null)
            {
                return;
            }
            for (int i = 0; i < ContentFields.Length; i++)
            {
                FieldInfo f = stylesT.GetField(ContentFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f == null)
                {
                    continue;
                }
                // 要保存原文并在绘制后恢复的标签对象。
                GUIContent gc = f.GetValue(null) as GUIContent;
                if (gc == null)
                {
                    continue;
                }
                m_contents.Add(gc);
                m_texts.Add(gc.text);
            }
            for (int i = 0; i < ArrayFields.Length; i++)
            {
                FieldInfo f = stylesT.GetField(ArrayFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f == null)
                {
                    continue;
                }
                // 要原地恢复的共享选项数组；下面同时保存其内容副本。
                string[] arr = f.GetValue(null) as string[];
                if (arr == null)
                {
                    continue;
                }
                m_arrays.Add(arr);
                m_clones.Add((string[])arr.Clone());
            }
        }

        // 把静态文案恢复为记录的原值
        public void Restore()
        {
            for (int i = 0; i < m_contents.Count; i++)
            {
                if (m_contents[i] == null)
                {
                    continue;
                }
                m_contents[i].text = m_texts[i];
            }
            for (int i = 0; i < m_arrays.Count; i++)
            {
                // 需要恢复的原数组实例，其他面板保存的引用仍指向它。
                string[] arr = m_arrays[i];
                // Capture 时复制的原选项文本，按同样顺序写回。
                string[] clone = m_clones[i];
                if (arr == null)
                {
                    continue;
                }
                if (clone == null)
                {
                    continue;
                }
                // 原地写回，保持数组引用不变，其他面板拿到的仍是同一个实例
                Array.Copy(clone, arr, clone.Length);
            }
            m_contents.Clear();
            m_texts.Clear();
            m_arrays.Clear();
            m_clones.Clear();
        }
    }
}
#endif
