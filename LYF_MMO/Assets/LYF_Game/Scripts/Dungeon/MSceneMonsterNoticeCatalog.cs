using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>当前副本实际 AI 节点引用的原 MessageTable、BlackCurtain 和头顶表情只读数据。</summary>
[Serializable]
public sealed class MSceneMonsterNoticeCatalog
{
    /// <summary>原MessageTable中本副本实际使用的一条公告。</summary>
    [Serializable]
    public sealed class Notice
    {
        /// <summary>原MessageTable.ID。</summary>
        public int Id;
        /// <summary>未经替换的原简体中文文案。</summary>
        public string Text;
    }
    /// <summary>本副本实际引用的原 AI 公告。</summary>
    public Notice[] Notices;
    /// <summary>原黑幕20002的五行文字，顺序来自Content字段。</summary>
    public string[] CurtainLines;
    /// <summary>原黑幕每行渐入秒数。</summary>
    public float[] CurtainLineTimes;
    /// <summary>原BlackCurtainMgr默认背景渐入秒数。</summary>
    public float FadeIn;
    /// <summary>原每行渐入后额外等待秒数。</summary>
    public float LinePause;
    /// <summary>原BlackCurtainTable.Time的整段等待秒数。</summary>
    public float ParagraphPause;
    /// <summary>原内容区淡出秒数。</summary>
    public float ContentFadeOut;
    /// <summary>原背景最终淡出秒数。</summary>
    public float FadeOut;
    /// <summary>原ShowExpressionTable.ID5对应的正式特效与挂点偏移。</summary>
    public MSceneMonsterFxData HeadExpression;
    /// <summary>原奸笑事件导出的声音资源。</summary>
    public string HeadAudio;
    /// <summary>按 Resources 根路径隔离的只读通知表，同号通知不会跨副本借用。</summary>
    private static readonly Dictionary<string, MSceneMonsterNoticeCatalog> loaded = new Dictionary<string, MSceneMonsterNoticeCatalog>();

    /// <summary>读取登记根目录中的通知表，未登记战斗表现时返回空。</summary>
    /// <param name="resourceRoot">当前场景登记的战斗表现 Resources 根路径。</param>
    public static MSceneMonsterNoticeCatalog Load(string resourceRoot)
    {
        if (string.IsNullOrEmpty(resourceRoot)) return null;
        // 同一路径复用解析结果，另一场景配置保留自己的文本和时间参数。
        MSceneMonsterNoticeCatalog catalog;
        if (!loaded.TryGetValue(resourceRoot, out catalog))
        {
            catalog = JsonUtility.FromJson<MSceneMonsterNoticeCatalog>(Resources.Load<TextAsset>(resourceRoot + "/MonsterNotices").text);
            loaded.Add(resourceRoot, catalog);
        }
        return catalog;
    }
}
