using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO 的 emojiPackageTemplate，用原始 ExpressionTable 的基础包索引发送 [z编号] 并还原图片。</summary>
public sealed class EmojiPackageTemplate : MonoBehaviour
{
    /// <summary>按 RO 表情编号 1 至 13 顺序保存的原图按钮。</summary>
    [SerializeField] private Button[] expressions;
    /// <summary>收起表情面板。</summary>
    [SerializeField] private Button close;
    /// <summary>将选中的原始标记送到当前好友或房间。</summary>
    internal event Action<string> Selected;
    /// <summary>按原表编号绑定各个表情。</summary>
    private void Awake()
    {
        close.onClick.AddListener(() => gameObject.SetActive(false));
        // 格子索引加一即为 ExpressionTable 的编号。
        for (int i = 0; i < expressions.Length; i++)
        {
            // 原表首个表达式编号为 1。
            string marker = "[z" + (i + 1) + "]";
            expressions[i].onClick.AddListener(() => { gameObject.SetActive(false); Selected?.Invoke(marker); });
        }
    }
    /// <summary>根据消息中完整的 RO 表情标记，返回同一格子的正式图集子图。</summary><param name="content">服务端返回的原始文本。</param>
    internal Sprite GetChatExpression(string content)
    {
        if (!content.StartsWith("[z", StringComparison.Ordinal) || !content.EndsWith("]", StringComparison.Ordinal)) return null;
        // 只识别当前面板已提供的基础包。
        if (!int.TryParse(content.Substring(2, content.Length - 3), out int id)) return null;
        if (id < 1 || id > expressions.Length || content != "[z" + id + "]") return null;
        return expressions[id - 1].image.sprite;
    }
}
