using System;
using UnityEngine;

/// <summary>魔神塔实际AI节点引用的原MessageTable、BlackCurtain和头顶表情只读数据。</summary>
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
    /// <summary>本副本七条原AI公告。</summary>
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
    /// <summary>本资源仅在首次收到魔神塔原AI通知时加载。</summary>
    private static MSceneMonsterNoticeCatalog instance;

    /// <summary>读取本副本原表现表裁剪数据。</summary>
    public static MSceneMonsterNoticeCatalog Load()
    {
        if (instance == null)
            instance = JsonUtility.FromJson<MSceneMonsterNoticeCatalog>(Resources.Load<TextAsset>("Dungeons/Moshenta/MonsterNotices").text);
        return instance;
    }
}
