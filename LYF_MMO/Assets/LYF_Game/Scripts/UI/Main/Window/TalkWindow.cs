using System;
using TMPro;
using UnityEngine;

/// <summary>
/// NPC 对话窗口，只接收展示数据并抛出商城或原副本对话选项事件。
/// </summary>
public class TalkWindow : WindowBase
{
    /// <summary>对话正文文本控件，绑定预制体已有的 tex_content。</summary>
    [SerializeField] private TMP_Text _talkContent;
    /// <summary>NPC 名字文本控件，绑定预制体已有的标题 Image/Text (TMP)。</summary>
    [SerializeField] private TMP_Text _talkTitle;

    /// <summary>既有任务、商店等NPC功能按钮容器，普通NPC仍使用此分支。</summary>
    [SerializeField] private GameObject _standardOptions;
    /// <summary>正式预制体保存的原20186帮助/暂离两个选项，不在运行时创建按钮。</summary>
    [SerializeField] private GameObject _moshentaBossOptions;
    /// <summary>玩家明确选择原“帮助他（进入战斗）”选项。</summary>
    public event Action MoshentaBattleRequested;
    /// <summary>玩家明确选择原“暂时离开”选项。</summary>
    public event Action TalkLeaveRequested;

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
        _standardOptions.SetActive(!viewData.ShowMoshentaBossChoices);
        _moshentaBossOptions.SetActive(viewData.ShowMoshentaBossChoices);
    }

    /// <summary>正式帮助按钮只通知控制器，由控制器发送原剧情事件。</summary>
    public void OnMoshentaBattleClick()
    {
        MoshentaBattleRequested?.Invoke();
    }

    /// <summary>正式暂离按钮只通知控制器停止对话，不发开战事件。</summary>
    public void OnTalkLeaveClick()
    {
        TalkLeaveRequested?.Invoke();
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
