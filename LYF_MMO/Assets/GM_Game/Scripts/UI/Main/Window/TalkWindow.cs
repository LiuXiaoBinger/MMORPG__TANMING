using System;
using TMPro;
using UnityEngine;

/// <summary>
/// NPC 对话窗口，只接收展示数据并抛出商城操作事件。
/// </summary>
public class TalkWindow : WindowBase
{
    /// <summary>对话正文文本控件，绑定预制体已有的 tex_content。</summary>
    [SerializeField] private TMP_Text _talkContent;
    /// <summary>NPC 名字文本控件，绑定预制体已有的标题 Image/Text (TMP)。</summary>
    [SerializeField] private TMP_Text _talkTitle;

    /// <summary>打开 NPC 商城事件。</summary>
    public event Action ShopRequested;

    /// <summary>接收控制器生成的 NPC 对话展示数据。</summary>
    /// <param name="obj">既有 UI 基类边界参数，内部只按 NpcTalkViewData 解析，不另开业务接口。</param>
    public override void ReFreshUI(object obj)
    {
        // 基类边界保持 object，强类型转换失败时保留原界面。
        NpcTalkViewData viewData = obj as NpcTalkViewData;
        if (viewData == null)
        {
            return;
        }

        // 标题写入 NPC 名字，正文按 _ 分隔的短句逐段换行。
        _talkTitle.text = viewData.Name;
        _talkContent.text = viewData.Talk.Replace("_", "\n");
    }

    /// <summary>Inspector 绑定入口：请求打开当前 NPC 的商店。</summary>
    public void OnShopBtnClick()
    {
        if (ShopRequested != null)
        {
            ShopRequested.Invoke();
        }
    }
}
