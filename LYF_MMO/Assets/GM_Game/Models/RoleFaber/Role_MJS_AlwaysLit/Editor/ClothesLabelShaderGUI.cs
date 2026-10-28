// ClothesLabelShaderGUI.cs
// 用途：只服务 IDA_Clothes.shadergraph 生成着色器的「中英双语标签」最小 ShaderGUI。
// 做法：不重写任何绘制逻辑；在本次 OnGUI 开始前把 ShaderGraph 内置面板文案临时换成双语，
//       绘制原样交给内置的 BuiltInLitGUI（无 _Metallic_Strength 时用 BuiltInUnlitGUI），
//       绘制结束后在 finally 里把文案还原，避免影响其他同样使用内置面板的材质。
// 边界：不改属性引用键（ReferenceName）、不改 GUID、不改贴图引用、不改任何材质数值。
// 守卫：本文件所在目录虽名为 Editor，但 GM_Game.asmdef 未限定 Editor 平台，脚本会编进 GM_Game.dll；
//       用 UNITY_EDITOR 包住整文件，保证打包时这段 UnityEditor 代码不参与编译。
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ClothesLabelShaderGUI : ShaderGUI
{
    // 内置面板 GUI 的完整类型名；工程程序集未引用 ShaderGraph 编辑器程序集，因此按名查找
    private const string LitGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI";
    private const string UnlitGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInUnlitGUI";
    private const string BaseGuiTypeName = "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInBaseShaderGUI";

    // 代理面板实例：按类型缓存，避免每帧重建导致折叠区重复注册
    private static ShaderGUI s_inner = null;
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

    // 下拉框选项的对照表：原文 -> 译文，逐字符数组一一对应
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
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        ShaderGUI inner = ResolveInner(materialEditor);
        if (inner == null)
        {
            // 拿不到内置面板时退回默认属性绘制，保证面板仍可用
            base.OnGUI(materialEditor, properties);
            return;
        }

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
    private static ShaderGUI ResolveInner(MaterialEditor materialEditor)
    {
        string want = UnlitGuiTypeName;
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

    // 内置文案：把 Styles 里的文本替换为双语，只影响当前这一帧的绘制
    private static void ApplyBuiltInLabels()
    {
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
        Type baseT = FindType(BaseGuiTypeName);
        if (baseT == null)
        {
            return null;
        }
        return baseT.GetNestedType("Styles", BindingFlags.Public | BindingFlags.NonPublic);
    }

    // 改单个静态 GUIContent 的正文
    private static void SetContent(Type stylesT, string fieldName, string text)
    {
        FieldInfo f = stylesT.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null)
        {
            return;
        }
        GUIContent gc = f.GetValue(null) as GUIContent;
        if (gc == null)
        {
            return;
        }
        gc.text = text;
    }

    // 改字符串数组里的显示名；按原英文名匹配，长度与顺序都不变，索引语义不变
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
        FieldInfo f = stylesT.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null)
        {
            return;
        }
        string[] arr = f.GetValue(null) as string[];
        if (arr == null)
        {
            return;
        }
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
    private static Type FindType(string fullName)
    {
        Type t = Type.GetType(fullName);
        if (t != null)
        {
            return t;
        }
        Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
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
        // 被改过正文的 GUIContent
        private readonly List<GUIContent> m_contents = new List<GUIContent>();
        // 与 m_contents 一一对应的原始正文
        private readonly List<string> m_texts = new List<string>();
        // 被改过内容的静态数组
        private readonly List<string[]> m_arrays = new List<string[]>();
        // 与 m_arrays 一一对应的原值副本
        private readonly List<string[]> m_clones = new List<string[]>();

        // 记录当前静态文案原值
        public void Capture()
        {
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
                string[] arr = m_arrays[i];
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
