using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO EmailItemTemplate，绑定左栏的一封邮件和选中效果。</summary>
public sealed class EmailItemTemplate : MonoBehaviour
{
    /// <summary>整行点击热区。</summary>
    [SerializeField] private Button button;
    /// <summary>当前信封的标题。</summary>
    [SerializeField] private Text title;
    /// <summary>显示未读、已读、附件或已领取。</summary>
    [SerializeField] private Text state;
    /// <summary>当前选中的行底图。</summary>
    [SerializeField] private GameObject selected;
    /// <summary>未读重要信件的角标。</summary>
    [SerializeField] private GameObject important;
    /// <summary>当前绑定的邮件编号。</summary>
    private long uid;
    /// <summary>行点击后通知交互控制器。</summary>
    private Action<long> onClick;

    /// <summary>实例化的行只绑定一次点击，刷新时读取当前 UID。</summary>
    private void Awake() { button.onClick.AddListener(Select); }

    /// <summary>以最新摘要和选中状态刷新行。</summary>
    /// <param name="mail">本行对应的信封。</param><param name="active">右侧是否显示该信。</param>
    /// <param name="clicked">点击信封后的回调。</param>
    public void SetData(MailBaseInfo mail, bool active, Action<long> clicked)
    {
        uid = mail.MailUid;
        onClick = clicked;
        title.text = mail.Title;
        if (mail.IsRead)
        {
            state.text = "已读";
            if (mail.IsHasItem) state.text = "已领取";
        }
        else
        {
            state.text = "未读";
            if (mail.IsHasItem) state.text = "未读  ·  有附件";
        }
        selected.SetActive(active);
        important.SetActive(mail.Important && !mail.IsRead);
    }

    /// <summary>报告当前行绑定的邮件 UID。</summary>
    private void Select() { onClick?.Invoke(uid); }
}
