using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>附近交谈列表的一行，姓名、点击事件与同一个 NPC 实体绑定。</summary>
public sealed class NpcTalkPromptRow : MonoBehaviour
{
    /// <summary>玩家可以点击整行来与这位 NPC 交谈。</summary>
    [SerializeField] private Button _button;
    /// <summary>第二行显示 NPC 的具体名字，“交谈”操作文字保存在按钮预制体。</summary>
    [SerializeField] private TMP_Text _speakerName;
    /// <summary>当前按钮指向的人物，离开附近后清空。</summary>
    internal NpcEntity Speaker { get; private set; }
    /// <summary>点击时通知所属窗口，参数为本行显示的人物。</summary>
    internal event Action<NpcEntity> TalkRequested;

    /// <summary>首次激活时绑定一次点击，小范围移动只更换姓名和人物引用。</summary>
    private void Awake() => _button.onClick.AddListener(RequestTalk);

    /// <summary>将按钮分配给一位人物，或隐藏供下次复用。</summary>
    /// <param name="speaker">该行显示的 NPC，空值表示从列表收回。</param>
    internal void Bind(NpcEntity speaker)
    {
        Speaker = speaker;
        if (speaker != null) _speakerName.text = speaker.Name;
        gameObject.SetActive(speaker != null);
    }

    /// <summary>将本行的具体人物交给窗口，忽略已被收回的按钮。</summary>
    private void RequestTalk()
    {
        if (Speaker != null) TalkRequested?.Invoke(Speaker);
    }
}
