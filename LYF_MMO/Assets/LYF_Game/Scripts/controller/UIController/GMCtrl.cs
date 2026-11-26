using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>对应 RO 的 GMCtrl，连接聊天入口、窗口、快捷指令、命令历史和 GmMgr。</summary>
internal sealed class GMCtrl : CtrlBase
{
    /// <summary>设备本地最近十条手输命令共用的持久化键。</summary>
    private const string HistoryKey = "GM_HISTORY_COMMAND";
    /// <summary>主视图在正式预制体上绑定的 GM 控件。</summary>
    private readonly GMPanel panel;
    /// <summary>本视图生命周期内的 GM 请求和回包入口。</summary>
    private readonly GmMgr gmMgr;
    /// <summary>最近的命令排在首位，重复执行会移至首位。</summary>
    private readonly List<string> history = new List<string>();

    /// <summary>按 RO 面板职责设置入口、命令输入、快捷按钮和历史选择。</summary>
    /// <param name="view">已创建 GM 窗口的主视图。</param>
    internal GMCtrl(MainView view) : base(view)
    {
        panel = view.GM;
        gmMgr = new GmMgr(ShowGMInfo);
        panel.GMButton.onClick.AddListener(TogglePanel);
        panel.BtnClose.onClick.AddListener(TogglePanel);
        panel.TextCommandBtn.onClick.AddListener(Submit);
        panel.DropdownHistory.onValueChanged.AddListener(SelectHistory);
        panel.BtnAddBuff.onClick.AddListener(FillBuff);
        panel.BtnItem.onClick.AddListener(FillItem);
        panel.BtnMoney.onClick.AddListener(FillMoney);
        panel.BtnBaseLv.onClick.AddListener(SetBaseLevel);
        panel.BtnBaseExp.onClick.AddListener(AddBaseExp);
        RefreshGMHistory();
    }

    /// <summary>上次命令按当前角色区分，切换角色后读取该角色自己的输入。</summary>
    private static string LastCommandKey()
    {
        return "GM_LAST_COMMAND_" + Global.Instance.SceneWorld.LocalRole.GetID();
    }

    /// <summary>从聊天入口打开，从窗口内收起，显隐沿用主视图管理。</summary>
    private void TogglePanel()
    {
        if (!panel.gameObject.activeSelf)
        {
            if (Global.Instance.SceneWorld.LocalRole == null) return;
            panel.InputTextCommand.text = PlayerPrefs.GetString(LastCommandKey(), "");
        }
        ShowMainWindow(WindowType.GMPanel);
    }

    /// <summary>提交命令行文本并在成功送出后记录历史。</summary>
    private void Submit()
    {
        // 只保存手输命令，等级和经验快捷按钮沿用 RO 的直接发送路径。
        string command = panel.InputTextCommand.text.Trim();
        if (command.Length == 0 || !SendCommand(command)) return;
        PlayerPrefs.SetString(LastCommandKey(), command);
        InsertNewKey(command);
    }

    /// <summary>快速填写添加基因命令，光标直接定位到编号处。</summary>
    private void FillBuff() { FillCommand("addbuff "); }
    /// <summary>快速填写 RO 道具发放前缀。</summary>
    private void FillItem() { FillCommand("item add "); }
    /// <summary>快速填写 RO 货币发放前缀。</summary>
    private void FillMoney() { FillCommand("addmoney "); }
    /// <summary>提交角色基础等级快捷输入。</summary>
    private void SetBaseLevel() { SendCommand("baselevel " + panel.InputBaseLv.text); }
    /// <summary>提交角色经验快捷输入。</summary>
    private void AddBaseExp() { SendCommand("addexp " + panel.InputBaseExp.text); }

    /// <summary>让玩家在模板或历史命令末尾继续编辑。</summary>
    /// <param name="command">需要填回输入框的文本。</param>
    private void FillCommand(string command)
    {
        panel.InputTextCommand.text = command;
        panel.InputTextCommand.ActivateInputField();
        panel.InputTextCommand.MoveTextEnd(false);
    }

    /// <summary>发送本次选择的命令，同时保留真实 RPC 回执用于展示。</summary>
    /// <param name="command">编辑框或快捷控件提供的命令。</param>
    private bool SendCommand(string command)
    {
        if (Global.Instance.SceneWorld.LocalRole == null || !gmMgr.SendCommand(command))
        {
            ShowGMInfo("尚未连接游戏服");
            return false;
        }
        return true;
    }

    /// <summary>加载设备上最近的命令，供所有角色复用。</summary>
    private void RefreshGMHistory()
    {
        // 输入框为单行，每个保存行都是一条完整命令。
        string[] saved = PlayerPrefs.GetString(HistoryKey, "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < saved.Length && history.Count < 10; i++) history.Add(saved[i]);
        RefreshDropdown();
    }

    /// <summary>按 RO 的顺序去重、移至首位并限制为十条。</summary>
    /// <param name="command">刚提交的命令。</param>
    private void InsertNewKey(string command)
    {
        history.Remove(command);
        history.Insert(0, command);
        if (history.Count > 10) history.RemoveAt(10);
        PlayerPrefs.SetString(HistoryKey, string.Join("\n", history));
        PlayerPrefs.Save();
        RefreshDropdown();
    }

    /// <summary>首项为下拉提示，使连续两次回填同一命令也能触发选择。</summary>
    private void RefreshDropdown()
    {
        // 下拉列表比记录多一个提示项，记录下标从一开始显示。
        List<string> options = new List<string> { "最近命令" };
        options.AddRange(history);
        panel.DropdownHistory.ClearOptions();
        panel.DropdownHistory.AddOptions(options);
        panel.DropdownHistory.SetValueWithoutNotify(0);
        panel.DropdownHistory.RefreshShownValue();
    }

    /// <summary>只把选中的历史内容填回输入框，等待玩家执行。</summary>
    /// <param name="index">选项下标，零为提示行。</param>
    private void SelectHistory(int index)
    {
        if (index <= 0) return;
        FillCommand(history[index - 1]);
        panel.DropdownHistory.SetValueWithoutNotify(0);
    }

    /// <summary>显示最近一次游戏服结果或本地断线提示。</summary>
    /// <param name="msg">直接显示给当前操作者的反馈。</param>
    private void ShowGMInfo(string msg) { panel.InputInfo.text = msg; }

    /// <summary>释放主城时对称移除控件和网络监听。</summary>
    public override void Dispose()
    {
        gmMgr.Dispose();
        panel.GMButton.onClick.RemoveListener(TogglePanel);
        panel.BtnClose.onClick.RemoveListener(TogglePanel);
        panel.TextCommandBtn.onClick.RemoveListener(Submit);
        panel.DropdownHistory.onValueChanged.RemoveListener(SelectHistory);
        panel.BtnAddBuff.onClick.RemoveListener(FillBuff);
        panel.BtnItem.onClick.RemoveListener(FillItem);
        panel.BtnMoney.onClick.RemoveListener(FillMoney);
        panel.BtnBaseLv.onClick.RemoveListener(SetBaseLevel);
        panel.BtnBaseExp.onClick.RemoveListener(AddBaseExp);
    }
}
