using UnityEngine;
using UnityEngine.UI;

/// <summary>对应 RO 的 GMPanel，持有主聊天栏入口及功能层窗口的正式预制体引用。</summary>
public sealed class GMPanel : WindowBase
{
    /// <summary>聊天栏旁的 GM 入口，随开发版主界面显示。</summary>
    [SerializeField] internal Button GMButton;
    /// <summary>收起当前窗口的按钮。</summary>
    [SerializeField] internal Button BtnClose;
    /// <summary>提交当前命令文本的按钮。</summary>
    [SerializeField] internal Button TextCommandBtn;
    /// <summary>输入 RO 命令和参数的单行文本框。</summary>
    [SerializeField] internal InputField InputTextCommand;
    /// <summary>选中后回填命令行的最近十条记录。</summary>
    [SerializeField] internal Dropdown DropdownHistory;
    /// <summary>显示最近一次本地提示或游戏服文字回执。</summary>
    [SerializeField] internal Text InputInfo;
    /// <summary>预填添加基因命令。</summary>
    [SerializeField] internal Button BtnAddBuff;
    /// <summary>预填发放道具命令。</summary>
    [SerializeField] internal Button BtnItem;
    /// <summary>预填发放货币命令。</summary>
    [SerializeField] internal Button BtnMoney;
    /// <summary>直接提交指定基础等级的快捷按钮。</summary>
    [SerializeField] internal Button BtnBaseLv;
    /// <summary>基础等级快捷命令的数值输入框。</summary>
    [SerializeField] internal InputField InputBaseLv;
    /// <summary>直接提交发放经验的快捷按钮。</summary>
    [SerializeField] internal Button BtnBaseExp;
    /// <summary>经验快捷命令的点数输入框。</summary>
    [SerializeField] internal InputField InputBaseExp;
}
