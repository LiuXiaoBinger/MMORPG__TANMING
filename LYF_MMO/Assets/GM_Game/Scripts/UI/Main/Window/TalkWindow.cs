using System;

/// <summary>
/// NPC 对话窗口，只接收展示数据并抛出商城操作事件。
/// </summary>
public class TalkWindow : WindowBase
{
    /// <summary>打开 NPC 商城事件。</summary>
    public event Action ShopRequested;

    /// <summary>接收控制器生成的 NPC 对话展示数据。</summary>
    public override void ReFreshUI(object obj)
    {
        NpcTalkViewData viewData = obj as NpcTalkViewData;
        if (viewData == null)
        {
            return;
        }

        // 当前预制体没有对话文本控件，展示数据留待界面补齐后直接绑定。
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
