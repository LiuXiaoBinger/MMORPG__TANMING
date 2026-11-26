using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>RO Gift 预制体的赠礼窗口，保留好友、礼物、数量和寄出按钮的位置及原图集。</summary>
public sealed class GiftPanel : WindowBase
{
    /// <summary>RO 设计尺寸的完整赠礼面板。</summary>
    [SerializeField] private RectTransform frame;
    /// <summary>输入同区收礼角色完整昵称。</summary>
    [SerializeField] internal InputField nickname;
    /// <summary>将输入的角色保存为赠礼好友。</summary>
    [SerializeField] internal Button addFriend;
    /// <summary>请求寄送当前选择的礼物。</summary>
    [SerializeField] internal Button send;
    /// <summary>收起赠礼面板并回到原背包。</summary>
    [SerializeField] private Button close;
    /// <summary>左列好友列表的隐藏行。</summary>
    [SerializeField] private GiftRow friendTemplate;
    /// <summary>右列物品和加减操作的隐藏行。</summary>
    [SerializeField] private GiftRow itemTemplate;
    /// <summary>右列没有可赠物品时显示的 RO 空态。</summary>
    [SerializeField] internal GameObject empty;
    /// <summary>好友列表为空时的操作提示。</summary>
    [SerializeField] internal GameObject noFriends;
    /// <summary>展示请求失败或寄出结果。</summary>
    [SerializeField] internal Text status;
    /// <summary>显示本次所选总件数。</summary>
    [SerializeField] internal Text total;
    /// <summary>左列本次窗口生命周期复用的好友行。</summary>
    private readonly List<GiftRow> friends = new List<GiftRow>();
    /// <summary>右列本次窗口生命周期复用的物品行。</summary>
    private readonly List<GiftRow> items = new List<GiftRow>();

    /// <summary>绑定赠礼窗口自身的关闭动作。</summary>
    private void Awake() { close.onClick.AddListener(CloseWindow); }

    /// <summary>适配小屏时等比缩放完整赠礼框。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (frame == null) return;
        // 当前功能层绘制区域，尺寸以 Canvas 单位计算。
        Rect area = ((RectTransform)transform).rect;
        frame.localScale = Vector3.one * Mathf.Min(1f, area.width / frame.sizeDelta.x, area.height / frame.sizeDelta.y);
    }

    /// <summary>按联系人条数准备左列，空列表保留添加入口。</summary><param name="count">服务端返回的可见联系人数量。</param>
    internal List<GiftRow> FriendRows(int count)
    {
        noFriends.SetActive(count == 0);
        return Rows(friends, friendTemplate, count);
    }

    /// <summary>按真实背包堆数准备右列，零堆显示 RO 空态。</summary><param name="count">当前可寄出的物品堆数。</param>
    internal List<GiftRow> ItemRows(int count)
    {
        empty.SetActive(count == 0);
        return Rows(items, itemTemplate, count);
    }

    /// <summary>复用已实例化的行，列表排列与滚动由预制体控制。</summary>
    /// <param name="rows">本列当前持有的行。</param><param name="template">本列隐藏模板。</param><param name="count">本次展示行数。</param>
    private static List<GiftRow> Rows(List<GiftRow> rows, GiftRow template, int count)
    {
        while (rows.Count < count) rows.Add(Instantiate(template, template.transform.parent));
        // 索引达到本次行数后隐藏多余的池中实例。
        for (int index = 0; index < rows.Count; index++) rows[index].gameObject.SetActive(index < count);
        return rows;
    }
}
