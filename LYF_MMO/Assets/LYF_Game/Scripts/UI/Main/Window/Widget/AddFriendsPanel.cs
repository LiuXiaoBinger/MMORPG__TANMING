using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO AddFriendsPanel 在现有好友窗口内的添加弹层，控件与行都保存在预制体中。</summary>
public sealed class AddFriendsPanel : MonoBehaviour
{
    /// <summary>展开推荐分类。</summary>
    [SerializeField] private Button filter;
    /// <summary>显示当前筛选条件的按钮标题。</summary>
    [SerializeField] private Text filterLabel;
    /// <summary>与筛选按钮对齐的菜单。</summary>
    [SerializeField] private GameObject filterMenu;
    /// <summary>依次为全部、同门、等级相近、修为相近。</summary>
    [SerializeField] private Button[] options;
    /// <summary>请求换一组推荐。</summary>
    [SerializeField] internal Button next;
    /// <summary>当前实际登录区服的名称。</summary>
    [SerializeField] private Text serverName;
    /// <summary>搜索结果为空时可明确选择按完整名称或编号添加。</summary>
    [SerializeField] internal Button exactAdd;
    /// <summary>查询或推荐无结果时单独显示的提示。</summary>
    [SerializeField] internal Text empty;
    /// <summary>按屏幕从上到下排列的一页推荐行。</summary>
    [SerializeField] private AddFriendTemplate[] rows;
    /// <summary>没有专属职业插画时的 RO 默认人物形象。</summary>
    [SerializeField] private Sprite defaultPortrait;
    /// <summary>按职业号从一开始排列的默认形象。场景资料未下发个性化头像。</summary>
    [SerializeField] private Sprite[] professionPortraits;
    /// <summary>头像旁显示的 RO Common 职业徽记，顺序与职业号一致。</summary>
    [SerializeField] private Sprite[] professionBadges;
    /// <summary>当前模版一页可以容纳的人数。</summary>
    internal int PageSize { get { return rows.Length; } }
    /// <summary>打开弹层时读取当前区服和可见玩家。</summary>
    internal event Action Opened;
    /// <summary>筛选已选择，参数为对应菜单下标。</summary>
    internal event Action<int> FilterChanged;

    /// <summary>注册本弹层的菜单交互。</summary>
    private void Awake()
    {
        filter.onClick.AddListener(() => filterMenu.SetActive(!filterMenu.activeSelf));
        for (int index = 0; index < options.Length; index++)
        {
            // 回调保存本按钮的下标。
            int choice = index;
            options[index].onClick.AddListener(() => SelectFilter(choice));
        }
    }
    /// <summary>每次打开均从全部推荐开始。</summary>
    private void OnEnable() { filterMenu.SetActive(false); Opened?.Invoke(); }
    /// <summary>更新筛选栏并通知控制器重新排列。</summary><param name="index">分类的预制体顺序。</param>
    internal void SelectFilter(int index)
    {
        filterMenu.SetActive(false);
        filterLabel.text = options[index].GetComponentInChildren<Text>().text;
        FilterChanged?.Invoke(index);
    }
    /// <summary>显示实际登录区服；本页仅使用本区的玩家资料。</summary><param name="name">登录信息中的区名。</param>
    internal void SetServer(string name)
    {
        if (string.IsNullOrEmpty(name)) name = "当前区服";
        serverName.text = name;
    }
    /// <summary>复用四个位置显示当前页。</summary><param name="list">筛选后的真实角色。</param>
    /// <param name="start">本页首项。</param><param name="why">按当前筛选解释推荐原因。</param>
    /// <param name="onAdd">点击加号时发给控制器的角色编号。</param><param name="searching">是否正在展示搜索结果。</param>
    internal void Show(List<RoleBaseInfo> list, int start, Func<RoleBaseInfo, string> why, Action<int> onAdd, bool searching)
    {
        for (int index = 0; index < rows.Length; index++)
        {
            // 当前行在已排序候选中的位置。
            int offset = start + index;
            rows[index].gameObject.SetActive(offset < list.Count);
            if (offset >= list.Count) continue;
            // 职业和等级均来自线上下发的角色信息。
            RoleBaseInfo info = list[offset];
            // 缺少职业配置时仍有可辨识的角色头像。
            Sprite icon = defaultPortrait;
            if (info.JobId > 0 && info.JobId <= professionPortraits.Length) icon = professionPortraits[info.JobId - 1];
            // 只有该职业配置了原图才展示职业徽记。
            Sprite job = null;
            if (info.JobId > 0 && info.JobId <= professionBadges.Length) job = professionBadges[info.JobId - 1];
            rows[index].SetData(info, icon, job, why(info), onAdd);
        }
        empty.gameObject.SetActive(list.Count == 0);
        if (searching) empty.text = "当前可见玩家中没有匹配结果";
        else empty.text = "附近暂无符合条件的玩家";
        exactAdd.gameObject.SetActive(searching && list.Count == 0);
    }
}
