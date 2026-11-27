using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>副本纪事内的终绝周赏，展示本周进度、七阶预览及上周可领取的一个宝箱。</summary>
public sealed class DungeonFinalJournalWidget : MonoBehaviour
{
    /// <summary>返回副本纪事的关闭按钮。</summary>
    [SerializeField] private Button dismiss;
    /// <summary>切回正在进行的北京周。</summary>
    [SerializeField] private Button thisWeek;
    /// <summary>上周有通关成绩时显示，已领取后仍可查看。</summary>
    [SerializeField] private Button lastWeek;
    /// <summary>存在多个未处理的周时依次查看。</summary>
    [SerializeField] private Button otherWeek;
    /// <summary>本期开始日期以及当前展示的最高层。</summary>
    [SerializeField] private Text heading;
    /// <summary>本周距结算的时间或上期领箱状态。</summary>
    [SerializeField] private Text timing;
    /// <summary>按一至四种不同副本排列的进度行。</summary>
    [SerializeField] private Text[] milestones;
    /// <summary>每种通关进度旁的 RO 宝箱图，达到条件后切换开盖状态。</summary>
    [SerializeField] private Image[] milestoneImages;
    /// <summary>RO 原始未开盖宝箱。</summary>
    [SerializeField] private Sprite closedChest;
    /// <summary>RO 原始开盖宝箱。</summary>
    [SerializeField] private Sprite openedChest;
    /// <summary>RO 之塔的蓝色选中侧签。</summary>
    [SerializeField] private Sprite activeTab;
    /// <summary>RO 之塔的浅色普通侧签。</summary>
    [SerializeField] private Sprite normalTab;
    /// <summary>七档奖励的预览入口。</summary>
    [SerializeField] private Button[] tiers;
    /// <summary>选中的预览阶位名称。</summary>
    [SerializeField] private Text tierName;
    /// <summary>必得区的物品格，按该阶实际物品数复制。</summary>
    [SerializeField] private DungeonRewardPreviewWidget fixedTemplate;
    /// <summary>随机区的物品格，其父节点负责多行布局与滚动。</summary>
    [SerializeField] private DungeonRewardPreviewWidget poolTemplate;
    /// <summary>在窗口生命周期内复用的必得物品格。</summary>
    private readonly List<DungeonRewardPreviewWidget> fixedSlots = new List<DungeonRewardPreviewWidget>();
    /// <summary>在窗口生命周期内复用的随机候选物品格。</summary>
    private readonly List<DungeonRewardPreviewWidget> poolSlots = new List<DungeonRewardPreviewWidget>();
    /// <summary>当前阶位随机区标题，显示实际抽取次数。</summary>
    [SerializeField] private Text randomLoot;
    /// <summary>上期打开宝箱入口。</summary>
    [SerializeField] private Button claim;
    /// <summary>领取按钮上的状态文字。</summary>
    [SerializeField] private Text claimLabel;
    /// <summary>该玩家可以回看的周期日序。</summary>
    private List<int> history = new List<int>();
    /// <summary>打开时绑定的当前角色，窗口关闭时退订。</summary>
    private RoleCountComponent counts;
    /// <summary>与游戏服共用 XML 的阶位配置。</summary>
    private DungeonFinalRewardTier[] configuration;
    /// <summary>当前浏览的周期，独立于预览阶位。</summary>
    private int selectedWeek;
    /// <summary>当前浏览的配置阶位下标。</summary>
    private int selectedTier;
    /// <summary>上次重绘的北京时间周期。</summary>
    private int shownCurrent;
    /// <summary>上次刷新倒计时的 UTC 分钟。</summary>
    private long shownMinute;
    /// <summary>交给现有控制器发送的个人领取请求，只带周期。</summary>
    public event Action<int> ClaimRequested;

    /// <summary>首次打开时接入预制体按钮，每次打开读取当前角色的真实存档。</summary>
    /// <param name="roleCounts">当前登录角色的个人次数组件。</param>
    public void Open(RoleCountComponent roleCounts)
    {
        if (configuration == null)
        {
            configuration = DungeonFinalRewardTier.Read(Resources.Load<TextAsset>("Dungeons/FinalWeeklyRewards").text);
            dismiss.onClick.AddListener(() => gameObject.SetActive(false));
            thisWeek.onClick.AddListener(() => SwitchWeek(DungeonFinalWeek.Period(DateTime.UtcNow)));
            lastWeek.onClick.AddListener(() => SwitchWeek(shownCurrent - 7));
            otherWeek.onClick.AddListener(NextOutstanding);
            claim.onClick.AddListener(() => ClaimRequested?.Invoke(selectedWeek));
            for (int index = 0; index < tiers.Length; index++)
            {
                // 每个预览按钮只切换自己的阶位，不改变服务器根据最高层判定的宝箱。
                int row = index;
                tiers[index].onClick.AddListener(() => Preview(row));
            }
        }
        if (counts != null) counts.DungeonFinalUpdated -= Refresh;
        counts = roleCounts;
        if (counts != null) counts.DungeonFinalUpdated += Refresh;
        gameObject.SetActive(true);
        SwitchWeek(DungeonFinalWeek.Period(DateTime.UtcNow));
    }

    /// <summary>隐藏时解除旧角色的变化监听。</summary>
    private void OnDisable()
    {
        if (counts != null) counts.DungeonFinalUpdated -= Refresh;
        counts = null;
    }

    /// <summary>切换周期，同时把预览定位到该周最高层对应档位。</summary><param name="week">北京周五日序。</param>
    private void SwitchWeek(int week)
    {
        selectedWeek = week;
        selectedTier = 0;
        DungeonFinalRewardTier achieved = DungeonFinalRewardTier.ForFloor(configuration, Value(DungeonFinalWeek.Highest));
        if (achieved != null) selectedTier = Array.IndexOf(configuration, achieved);
        Refresh();
    }

    /// <summary>依次查看早于上周且尚未领取的各期，同一角色漏登多周也能取回宝箱。</summary>
    private void NextOutstanding()
    {
        List<int> older = history.FindAll(week => week < shownCurrent - 7);
        if (older.Count > 0) SwitchWeek(older[(older.IndexOf(selectedWeek) + 1) % older.Count]);
    }

    /// <summary>读取当前角色在所选周的一个业务数值。</summary><param name="action">最高层、不同种数或领取标记。</param>
    private int Value(int action)
    {
        if (counts == null) return 0;
        return (int)counts.GetCount((RoleCountAction)action, selectedWeek);
    }

    /// <summary>重绘本期种数、最高层及领取按钮，过期未领取的成绩可继续查阅。</summary>
    private void Refresh()
    {
        // 正在进行的周期由固定东八区周五计算。
        DateTime now = DateTime.UtcNow;
        shownCurrent = DungeonFinalWeek.Period(now);
        shownMinute = now.Ticks / TimeSpan.TicksPerMinute;
        if (counts != null) history = counts.DungeonFinalHistory(shownCurrent);
        else history.Clear();
        bool past = selectedWeek < shownCurrent;
        bool claimed = Value(DungeonFinalWeek.Claimed) > 0;
        int highest = Value(DungeonFinalWeek.Highest);
        int distinct = Value(DungeonFinalWeek.Distinct);
        thisWeek.image.sprite = activeTab;
        if (past) thisWeek.image.sprite = normalTab;
        lastWeek.gameObject.SetActive(history.Contains(shownCurrent - 7));
        lastWeek.image.sprite = normalTab;
        if (selectedWeek == shownCurrent - 7) lastWeek.image.sprite = activeTab;
        otherWeek.gameObject.SetActive(history.Exists(week => week < shownCurrent - 7));
        string label = "本周";
        if (past) label = "上周";
        if (selectedWeek < shownCurrent - 7) label = "往期";
        heading.text = label + "  " + DungeonFinalWeek.Friday(selectedWeek).ToString("MM.dd") + " 起    最高 " + highest + " 层";
        for (int index = 0; index < milestones.Length; index++)
        {
            // 重打本周已通关的同一副本不累计到第二种。
            int target = index + 1;
            string sign = "◇";
            milestoneImages[index].sprite = closedChest;
            if (distinct >= target)
            {
                sign = "◆";
                milestoneImages[index].sprite = openedChest;
            }
            milestones[index].text = sign + "  通关 " + target + " 种不同终绝副本      " + Mathf.Min(distinct, target) + " / " + target;
        }
        claim.gameObject.SetActive(past);
        claim.interactable = past && !claimed && highest > 0;
        claimLabel.text = "领取宝箱";
        if (claimed) claimLabel.text = "已领取";
        if (past) timing.text = "每位角色每周可开启一个终绝宝箱";
        else
        {
            // 下期开始的北京时间转换回 UTC 后与服务器时间口径相同。
            TimeSpan left = DungeonFinalWeek.Friday(shownCurrent + 7).AddHours(-8) - now;
            timing.text = "距周五结算  " + left.Days + "天 " + left.Hours + "时 " + left.Minutes + "分";
        }
        Preview(selectedTier);
    }

    /// <summary>跨周时留在本周页的玩家自动进入新一期，日常只按分钟刷新倒计时。</summary>
    private void Update()
    {
        if (configuration == null) return;
        DateTime now = DateTime.UtcNow;
        if (now.Ticks / TimeSpan.TicksPerMinute == shownMinute) return;
        if (selectedWeek == shownCurrent && DungeonFinalWeek.Period(now) != shownCurrent) SwitchWeek(DungeonFinalWeek.Period(now));
        else Refresh();
    }

    /// <summary>显示选中阶位的真实物品清单与随机次数。</summary><param name="index">从零开始的阶位下标。</param>
    private void Preview(int index)
    {
        selectedTier = index;
        // 查阅的阶位可以高于本周达成阶位，不会改变实际结算。
        DungeonFinalRewardTier tier = configuration[index];
        for (int row = 0; row < tiers.Length; row++) tiers[row].interactable = row != index;
        tierName.text = tier.Name + "宝箱  ·  通关 " + tier.Floor + " 层";
        randomLoot.text = "随机奖励  ·  抽取 " + tier.Draws + " 次";
        ShowItems(tier.Fixed, fixedTemplate, fixedSlots);
        ShowItems(tier.Pool, poolTemplate, poolSlots);
    }

    /// <summary>依照选中档位的物品顺序复用正式预制体格子。</summary>
    /// <param name="items">该档必得清单或随机候选清单。</param><param name="template">这一栏的布局模板。</param>
    /// <param name="slots">该栏已创建的物品格，切换阶位时继续使用。</param>
    private static void ShowItems(MailItem[] items, DungeonRewardPreviewWidget template, List<DungeonRewardPreviewWidget> slots)
    {
        for (int index = 0; index < items.Length; index++)
        {
            // 配置条目同时提供正式物品编号及每次获得的件数。
            MailItem reward = items[index];
            if (index == slots.Count) slots.Add(Instantiate(template, template.transform.parent));
            slots[index].Bind(LubanMgr.Instance.GetItemConfigById(reward.ItemId), reward.Count);
        }
        for (int index = 0; index < slots.Count; index++)
            slots[index].gameObject.SetActive(index < items.Length);
    }
}
