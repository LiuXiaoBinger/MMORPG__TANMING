using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>武学窗口持有的秘笈视图，展示角色回包并把用户操作交回技能控制器。</summary>
public sealed class MartialManualPanel : MonoBehaviour
{
    /// <summary>出战人物、目录、详情、配置列表、培养弹层与说明弹层。</summary>
    [SerializeField] private GameObject formation, catalogue, details, editorList, training, info;
    /// <summary>普通出战的工具栏、脚注、编辑的保存与放弃按钮组。</summary>
    [SerializeField] private GameObject formationTools, formationFooter, editButtons;
    /// <summary>人物和五个弧形槽的移动根节点。</summary>
    [SerializeField] private RectTransform stage;
    /// <summary>最后一个位置尚未开放时显示的锁。</summary>
    [SerializeField] private GameObject lockedSlot;
    /// <summary>从左至右的五个出战位置。</summary>
    [SerializeField] private ManualCard[] slots;
    /// <summary>目录、配置、培养三个滚动列表的内容节点。</summary>
    [SerializeField] private RectTransform[] lists;
    /// <summary>列表行及其分组标题的预制体内模板。</summary>
    [SerializeField] private ManualCard cardTemplate;
    [SerializeField] private TMP_Text groupTemplate;
    /// <summary>已收录的书册、卷轴与竹简图。</summary>
    [SerializeField] private Sprite[] covers;
    /// <summary>当前书籍与培养中心的封面。</summary>
    [SerializeField] private Image detailIcon, trainIcon;
    /// <summary>详情名、题记、属性正文及没有选中秘笈时的提示。</summary>
    [SerializeField] private TMP_Text detailTitle, description, body, empty;
    /// <summary>目录和方案按钮下方的选项层，选项复用预制按钮模板。</summary>
    [SerializeField] private RectTransform options;
    [SerializeField] private Button optionTemplate;
    /// <summary>武学标题栏显示角色真实铜币。</summary>
    [SerializeField] private TMP_Text copperBalance;
    /// <summary>培养消耗材料图示与下级箭头。</summary>
    [SerializeField] private Image materialIcon, previewArrow;
    /// <summary>培养材料与原本的预制图示。</summary>
    [SerializeField] private Sprite trainingMaterial;
    /// <summary>可选中时显示的详情区块。</summary>
    [SerializeField] private GameObject detailContent;
    /// <summary>目录筛选、已明悟次数、当前方案与底部提示。</summary>
    [SerializeField] private TMP_Text filterText, insightsText, schemeText, feedback;
    /// <summary>属性预览、下级数值、材料及铜币。</summary>
    [SerializeField] private TMP_Text currentStats, nextStats, costText;
    /// <summary>培养标题、说明弹层正文及确定操作的文字。</summary>
    [SerializeField] private TMP_Text trainTitle, infoText, actionText;
    /// <summary>随选中页签一起着色的图示。</summary>
    [SerializeField] private Image[] trainGlyphs;
    /// <summary>按升级、进阶、明悟排列的培养页签。</summary>
    [SerializeField] private Button[] trainTabs;
    /// <summary>预制体上的配置、保存、放弃及方案按钮。</summary>
    [SerializeField] private Button configure, save, discard, schemeButton;
    /// <summary>筛选、领悟加成和底部明悟合计说明。</summary>
    [SerializeField] private Button filter, bonuses, passiveInfo;
    /// <summary>装备或领悟、明悟和培养入口。</summary>
    [SerializeField] private Button equip, insight, cultivate;
    /// <summary>关闭和执行培养，以及关闭说明。</summary>
    [SerializeField] private Button closeTraining, execute, closeInfo;
    /// <summary>装备入口在未领悟状态下改为领悟。</summary>
    [SerializeField] private TMP_Text equipText;
    /// <summary>发送给已持有该窗口的控制器。</summary>
    public event Action<ManualRequest> Requested;
    /// <summary>最近一次服务端回执。</summary>
    private ManualReply state;
    /// <summary>当前显示目录或出战页。</summary>
    private bool listPage;
    /// <summary>出战配置正在本地预览，保存后才写入角色。</summary>
    public bool Editing { get; private set; }
    /// <summary>正在查看的书号、培养页及筛选项。</summary>
    private int selected, tab, filterMode;
    /// <summary>尚未保存的槽位，元素是目录书号，零表示空。</summary>
    private readonly List<int> draft = new List<int>();
    /// <summary>点空槽后优先放入的位置，负数表示自动找空槽。</summary>
    private int targetSlot = -1;
    /// <summary>目录加载或角色切换后重新请求。</summary>
    private bool initialized;

    /// <summary>打开页面，只在进入或重新进入时向服务器读取。</summary>
    /// <param name="showList">是否显示秘笈列表页。</param>
    public void Open(bool showList)
    {
        if (initialized && gameObject.activeSelf && state != null && listPage == showList) return;
        if (!initialized) Bind();
        if (listPage != showList) { ResetOverlays(); selected = 0; feedback.text = ""; }
        listPage = showList;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (state == null) { feedback.text = "正在读取秘笈…"; Requested?.Invoke(new ManualRequest()); }
        Render();
    }

    /// <summary>离开窗口或切换角色时清空上一角色的数据。</summary>
    private void OnDisable()
    {
        state = null;
        copperBalance.text = "";
        selected = 0;
        filterMode = 0;
        ResetOverlays();
        detailContent.SetActive(false);
        formation.SetActive(false);
        catalogue.SetActive(false);
        details.SetActive(false);
    }

    /// <summary>收起培养与说明，丢弃尚未提交的槽位。</summary>
    private void ResetOverlays()
    {
        Editing = false;
        targetSlot = -1;
        training.SetActive(false);
        editorList.SetActive(false);
        info.SetActive(false);
        options.gameObject.SetActive(false);
    }

    /// <summary>接收服务端确认的消耗、进度与槽位。</summary><param name="reply">当前角色的最新回执。</param>
    public void Apply(ManualReply reply)
    {
        feedback.text = reply.Message;
        if (reply.Catalog == null) return;
        state = reply;
        if (reply.Success && reply.Action == ManualAction.ManualSave)
        {
            Editing = false;
            if (!listPage) selected = FirstEquipped();
            feedback.text = "出战方案已保存";
        }
        if (reply.Success && reply.Action == ManualAction.ManualUpgrade) feedback.text = "升级成功";
        if (reply.Success && reply.Action == ManualAction.ManualRank) feedback.text = "进阶成功";
        Render();
    }

    /// <summary>绑定预制体中的正式控件。</summary>
    private void Bind()
    {
        initialized = true;
        configure.onClick.AddListener(() => Edit(-1));
        save.onClick.AddListener(Save);
        discard.onClick.AddListener(() => { Editing = false; selected = FirstEquipped(); Render(); });
        schemeButton.onClick.AddListener(() => { if (state != null) Menu(false); });
        filter.onClick.AddListener(() => { Menu(true); });
        bonuses.onClick.AddListener(ShowBonuses);
        passiveInfo.onClick.AddListener(() => ShowInfo("青龙永镇（未激活）\n累计明悟秘笈次数激活增益被动技能"));
        equip.onClick.AddListener(EquipOrLearn);
        insight.onClick.AddListener(() => StartTraining(2));
        cultivate.onClick.AddListener(() => StartTraining(0));
        closeInfo.onClick.AddListener(() => info.SetActive(false));
        closeTraining.onClick.AddListener(() => { training.SetActive(false); Render(); });
        execute.onClick.AddListener(Train);
        for (int i = 0; i < trainTabs.Length; i++)
        {
            // 记录此按钮固定的页签序号。
            int page = i;
            trainTabs[i].onClick.AddListener(() => { tab = page; Render(); });
        }
    }

    /// <summary>在原按钮下方展示筛选项或出战方案。</summary><param name="isFilter">选择筛选或方案菜单。</param>
    private void Menu(bool isFilter)
    {
        if (options.gameObject.activeSelf) { options.gameObject.SetActive(false); return; }
        for (int i = options.childCount - 1; i >= 0; i--)
        {
            options.GetChild(i).gameObject.SetActive(false);
            Destroy(options.GetChild(i).gameObject);
        }
        // 定位于画布固定设计坐标的按钮下方。
        options.anchoredPosition = new Vector2(700.5f, -300);
        string[] choices = new List<string>(state.Catalog.Schemes).ToArray();
        if (isFilter)
        {
            options.anchoredPosition = new Vector2(118.5f, -303);
            choices = new[] { "全部", "已领悟", "未领悟" };
        }
        options.gameObject.SetActive(true);
        for (int i = 0; i < choices.Length; i++)
        {
            // 每个行按钮只持有本选项的序号。
            int index = i;
            Button option = Instantiate(optionTemplate, options);
            option.gameObject.SetActive(true);
            ((RectTransform)option.transform).anchoredPosition = new Vector2(0, -i * 57);
            option.GetComponentInChildren<TMP_Text>().text = choices[i];
            option.onClick.AddListener(() =>
            {
                options.gameObject.SetActive(false);
                if (isFilter)
                {
                    filterMode = index;
                    selected = 0;
                    Render();
                }
                else if (state.Scheme != index + 1)
                    Requested?.Invoke(new ManualRequest { Action = ManualAction.ManualSelectScheme, Scheme = index + 1 });
            });
        }
    }

    /// <summary>查找书籍在当前目录及进度数组中的位置。</summary><param name="id">书号。</param>
    private int Index(int id)
    {
        for (int i = 0; i < state.Catalog.Books.Count; i++) if (state.Catalog.Books[i].Id == id) return i;
        return -1;
    }

    /// <summary>第一个已装备的位置，没有装备时返回零。</summary>
    private int FirstEquipped()
    {
        for (int i = 0; i < state.Slots.Count; i++) if (state.Slots[i] != 0) return state.Slots[i];
        return 0;
    }

    /// <summary>按已收录书籍选择对应封面。</summary><param name="id">书号。</param>
    private Sprite Cover(int id)
    {
        if (id <= 0) return null;
        if (id == 1) return covers[0];
        if (id == 4 || id == 6) return covers[2];
        return covers[1];
    }

    /// <summary>进入草稿配置并显示可出战的已领悟书籍。</summary><param name="slot">指定空槽或负一。</param>
    private void Edit(int slot)
    {
        if (state == null) return;
        draft.Clear();
        draft.AddRange(state.Slots);
        Editing = true;
        targetSlot = slot;
        Render();
    }

    /// <summary>放置秘笈，重复移动会清除它原来的槽。</summary><param name="id">已学书号。</param><param name="slot">目标槽。</param>
    private void Place(int id, int slot)
    {
        if (slot < 0 || slot >= draft.Count) { feedback.text = "请先选择一个出战位置"; return; }
        // 清除同一本书的原位置，再按指针位置放置。
        int old = draft.IndexOf(id);
        if (old >= 0) draft[old] = 0;
        draft[slot] = id;
        targetSlot = -1;
        selected = id;
        Render();
    }

    /// <summary>提交整个方案，回包确认后关闭编辑。</summary>
    private void Save()
    {
        // 空位一并提交，服务端能区分卸下和没有提交。
        ManualRequest request = new ManualRequest { Action = ManualAction.ManualSave, Scheme = state.Scheme };
        request.Slots.Add(draft);
        Requested?.Invoke(request);
    }

    /// <summary>点击槽时查看详情，配置中则选择空槽或卸下。</summary><param name="index">点击的槽序。</param>
    private void ClickSlot(int index)
    {
        if (index >= state.Slots.Count) { ShowInfo("此出战位置尚未解锁"); return; }
        if (Editing)
        {
            draft[index] = 0;
            targetSlot = index;
            Render();
        }
        else if (state.Slots[index] == 0) Edit(index);
        else { selected = state.Slots[index]; Render(); }
    }

    /// <summary>从目录装备已学秘笈，或使用残页领悟。</summary>
    private void EquipOrLearn()
    {
        // 当前目录行的权威等级。
        ManualProgress progress = state.Progress[Index(selected)];
        if (progress.Level == 0) { Send(ManualAction.ManualLearn); return; }
        draft.Clear();
        draft.AddRange(state.Slots);
        // 已装备项可直接卸下，其他项优先填第一个空位。
        int slot = draft.IndexOf(selected);
        if (slot >= 0) draft[slot] = 0;
        else
        {
            slot = draft.IndexOf(0);
            if (slot < 0) { ShowInfo("请在出战秘笈的配置中更换装备"); return; }
            draft[slot] = selected;
        }
        Save();
    }

    /// <summary>显示培养窗口并选中对应页。</summary><param name="page">升级、进阶、明悟依次为零至二。</param>
    private void StartTraining(int page) { feedback.text = ""; tab = page; training.SetActive(true); Render(); }

    /// <summary>培养按钮对应当前页的服务端操作。</summary>
    private void Train()
    {
        if (tab == 0) Send(ManualAction.ManualUpgrade);
        else if (tab == 1) Send(ManualAction.ManualRank);
        else Send(ManualAction.ManualInsight);
    }

    /// <summary>将点击时所见的进度发给服务端核对。</summary><param name="action">所选操作。</param>
    private void Send(ManualAction action)
    {
        // 客户端从不发送奖励、费用或余额。
        ManualProgress p = state.Progress[Index(selected)];
        Requested?.Invoke(new ManualRequest { Action = action, Book = selected, Level = p.Level, Rank = p.Rank, Insights = p.Insights });
    }

    /// <summary>展示简短的详情提示。</summary><param name="message">实际提示内容。</param>
    private void ShowInfo(string message) { infoText.text = message; info.SetActive(true); }

    /// <summary>汇总已领悟书籍的永久收益。</summary>
    private void ShowBonuses()
    {
        // 所有已领悟书籍的累计外防与生命。
        int def = 0, hp = 0;
        for (int i = 0; i < state.Progress.Count; i++)
        {
            // 零级书籍尚未提供收集属性。
            int level = state.Progress[i].Level;
            if (level == 0) continue;
            ManualBook b = state.Catalog.Books[i];
            def += b.CollectDef + (level - 1) * b.CollectDefStep;
            hp += b.CollectHp + (level - 1) * b.CollectHpStep;
        }
        ShowInfo("领悟加成\n\n外功防御 +" + def + "\n气血上限 +" + hp);
    }

    /// <summary>根据当前页、草稿与权威数据刷新显示。</summary>
    private void Render()
    {
        if (state == null) return;
        formation.SetActive(!listPage);
        catalogue.SetActive(listPage);
        details.SetActive(!Editing);
        editorList.SetActive(Editing);
        formationTools.SetActive(!Editing);
        formationFooter.SetActive(!Editing);
        editButtons.SetActive(Editing);
        stage.anchoredPosition = Vector2.zero;
        if (Editing) stage.anchoredPosition = new Vector2(660, 0);
        copperBalance.text = state.Copper.ToString("N0");
        schemeText.text = state.Catalog.Schemes[state.Scheme - 1];
        if (!listPage && !Editing && !training.activeSelf && !state.Slots.Contains(selected)) selected = FirstEquipped();
        if (listPage && Index(selected) < 0)
        {
            // 切换筛选后，详情跟随筛选结果中的第一本书。
            for (int i = 0; i < state.Progress.Count; i++)
            {
                if (filterMode == 1 && state.Progress[i].Level == 0) continue;
                if (filterMode == 2 && state.Progress[i].Level > 0) continue;
                selected = state.Progress[i].Book;
                break;
            }
        }
        for (int i = 0; i < slots.Length; i++)
        {
            // 记住序号供点击/拖入回调读取。
            int slot = i, book = 0;
            string sign = "";
            if (i < state.Slots.Count)
            {
                book = state.Slots[i];
                if (Editing) book = draft[i];
                sign = "＋";
                if (book > 0) sign = "";
            }
            Action<int> onDrop = null;
            if (Editing && i < draft.Count) onDrop = id => Place(id, slot);
            // 标记正在查看的书，编辑空位置时改为标记目标槽。
            bool highlight = book > 0 && book == selected;
            if (Editing && targetSlot == i) highlight = true;
            if (i == 4) lockedSlot.SetActive(i >= state.Slots.Count);
            slots[i].Bind(book, Cover(book), sign, "", highlight, () => ClickSlot(slot), onDrop);
        }
        if (listPage) FillList(0);
        if (Editing) FillList(1);
        // 没有装备时右侧保留空态。
        int index = Index(selected);
        empty.gameObject.SetActive(index < 0);
        detailContent.SetActive(index >= 0);
        if (index < 0) return;
        ManualBook bookInfo = state.Catalog.Books[index];
        ManualProgress progress = state.Progress[index];
        DrawDetails(bookInfo, progress);
        if (training.activeSelf) { FillList(2); DrawTraining(bookInfo, progress); }
    }

    /// <summary>从权威清单生成分组、可滚动的卡片。</summary><param name="which">目录、配置或培养列表编号。</param>
    private void FillList(int which)
    {
        // 三处列表共享同一种预制卡，目录排两列，其他排一列。
        RectTransform root = lists[which];
        for (int i = root.childCount - 1; i >= 0; i--) { root.GetChild(i).gameObject.SetActive(false); Destroy(root.GetChild(i).gameObject); }
        int learned = 0, insightCount = 0;
        for (int i = 0; i < state.Progress.Count; i++)
        { if (state.Progress[i].Level > 0) learned++; insightCount += state.Progress[i].Insights; }
        insightsText.text = "已明悟:" + insightCount + "次";
        string[] filters = { "全部", "已领悟", "未领悟" };
        filterText.text = filters[filterMode];
        float y = 0;
        for (int group = 0; group <= 1; group++)
        {
            if (group == 1 && which != 0) break;
            if (which == 0 && filterMode > 0 && filterMode != group + 1) continue;
            if (which == 0)
            {
                // 分组数量反映当前配置，不使用录屏账号的总数。
                TMP_Text header = Instantiate(groupTemplate, root);
                header.gameObject.SetActive(true);
                header.rectTransform.anchoredPosition = new Vector2(0, -y);
                header.text = "已领悟 (" + learned + ")";
                if (group == 1) header.text = "未领悟 (" + (state.Progress.Count - learned) + ")";
                y += 48;
            }
            int count = 0, columns = 1;
            if (which == 0) columns = 2;
            for (int i = 0; i < state.Progress.Count; i++)
            {
                // 捕获本卡片的书号，未学卡仅在目录显示。
                ManualProgress p = state.Progress[i];
                ManualBook b = state.Catalog.Books[i];
                if ((p.Level == 0) != (group == 1)) continue;
                ManualCard card = Instantiate(cardTemplate, root);
                card.gameObject.SetActive(true);
                ((RectTransform)card.transform).anchoredPosition = new Vector2(count % columns * 404, -y - count / columns * 144);
                string note = p.Level + "级";
                if (p.Level == 0 && b.Learn != null) note = "残页  " + Held(b.Learn) + "/" + b.Learn.Amount;
                if (state.Slots.Contains(b.Id)) note += "  ·  已装";
                if (p.Level > 0) note += "\n" + new string('◆', Mathf.Min(p.Rank, 5)) + new string('◇', Mathf.Max(0, 5 - p.Rank));
                card.Bind(b.Id, Cover(b.Id), b.Name, note, selected == b.Id, () => Pick(b.Id, which), null);
                count++;
            }
            y += ((count + columns - 1) / columns) * 144;
        }
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
    }

    /// <summary>选中或放置列表中的一本书。</summary><param name="id">书号。</param><param name="which">列表用途。</param>
    private void Pick(int id, int which)
    {
        if (which == 1)
        {
            int slot = targetSlot;
            if (slot < 0) slot = draft.IndexOf(0);
            Place(id, slot);
        }
        else { selected = id; Render(); }
    }

    /// <summary>格式化一级基础值和当前等级带来的增量。</summary>
    /// <param name="b">书籍配置。</param><param name="level">要预览的等级。</param><param name="equipped">使用装备属性。</param>
    private static string Stats(ManualBook b, int level, bool equipped)
    {
        // 一级基础值不含升级增量。
        int step = Mathf.Max(0, level - 1);
        if (equipped) return "外功防御+" + (b.EquipDef + step * b.EquipDefStep) + "\n气血上限+" + (b.EquipHp + step * b.EquipHpStep);
        return "外功防御+" + (b.CollectDef + step * b.CollectDefStep) + "\n气血上限+" + (b.CollectHp + step * b.CollectHpStep);
    }

    /// <summary>构建分节属性说明并选择正确的操作。</summary><param name="b">书籍配置。</param><param name="p">角色进度。</param>
    private void DrawDetails(ManualBook b, ManualProgress p)
    {
        detailIcon.sprite = Cover(b.Id);
        detailTitle.text = b.Name;
        if (p.Level > 0) detailTitle.text += " <color=#98DCCB>(" + p.Level + "级)</color>";
        description.text = b.Description;
        string text = "";
        if (b.Bleed != null && p.Rank < b.Bleed.ChancePercent.Count)
            text += "◇ 被动技能\n每次攻击有" + b.Bleed.ChancePercent[p.Rank] + "%概率为目标附加" + b.Bleed.DurationSeconds + "秒的流血效果。\n\n";
        else if (b.Passive.Length > 0) text += "◇ 被动技能\n" + b.Passive + "\n\n";
        if (b.CollectDef != 0 || b.CollectHp != 0) text += "◇ 领悟属性\n" + Stats(b, p.Level, false) + "\n\n◇ 装备属性\n" + Stats(b, p.Level, true) + "\n\n";
        if (b.RankText.Count > 0) text += "◇ 进阶属性\n" + Ranks(b);
        if (text.Length == 0 && b.Learn != null) text = "◇ 领悟条件\n" + b.Name + "残页  " + Held(b.Learn) + "/" + b.Learn.Amount;
        if (b.Source.Length > 0) text += "\n\n◇ 获取途径\n" + b.Source;
        body.text = text;
        body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, body.preferredHeight + 16);
        equip.gameObject.SetActive(listPage);
        equipText.text = "装备";
        if (state.Slots.Contains(b.Id)) equipText.text = "卸下";
        if (p.Level == 0) equipText.text = "领悟";
        insight.gameObject.SetActive(p.Level > 0);
        cultivate.gameObject.SetActive(p.Level > 0);
    }

    /// <summary>格式化目录已提供的各阶说明。</summary><param name="b">书籍配置。</param>
    private static string Ranks(ManualBook b)
    {
        // 每一行对应一阶预览。
        List<string> lines = new List<string>();
        for (int i = 0; i < b.RankText.Count; i++) lines.Add((i + 1) + "阶  " + b.RankText[i]);
        return string.Join("\n\n", lines);
    }

    /// <summary>读取该材料的背包持有数。</summary><param name="cost">培养或领悟的费用。</param>
    private int Held(ManualCost cost)
    {
        int amount;
        state.Materials.TryGetValue(cost.Item, out amount);
        return amount;
    }

    /// <summary>刷新升级、进阶和明悟的当前与下档预览。</summary><param name="b">当前书。</param><param name="p">当前等级与阶数。</param>
    private void DrawTraining(ManualBook b, ManualProgress p)
    {
        trainIcon.sprite = Cover(b.Id);
        trainTitle.text = "秘笈培养";
        // 仅在配置了下一档时允许扣除资源。
        ManualCost cost = null;
        string action = "升级";
        if (tab == 0)
        {
            currentStats.text = "◇ 基础属性\n" + Stats(b, p.Level, true) + "\n\n◇ 收集属性\n" + Stats(b, p.Level, false);
            nextStats.text = "已达当前等级上限";
            if (p.Level <= b.Upgrades.Count)
            {
                cost = b.Upgrades[p.Level - 1];
                nextStats.text = "下级预览\n" + Stats(b, p.Level + 1, true) + "\n\n下级预览\n" + Stats(b, p.Level + 1, false);
            }
        }
        else if (tab == 1)
        {
            action = "进阶";
            currentStats.text = Ranks(b);
            nextStats.text = "";
            if (p.Rank < b.Ranks.Count) cost = b.Ranks[p.Rank];
        }
        else
        {
            action = "明悟";
            currentStats.text = "已明悟  " + p.Insights + "次";
            nextStats.text = "";
            if (p.Insights < b.Insights.Count) cost = b.Insights[p.Insights];
        }
        for (int i = 0; i < trainTabs.Length; i++)
        {
            // 与武学主页相同，浅底深字为当前页。
            Image plate = trainTabs[i].GetComponent<Image>();
            TMP_Text label = trainTabs[i].GetComponentInChildren<TMP_Text>();
            plate.color = new Color32(38, 57, 54, 230);
            label.color = new Color32(235, 234, 226, 255);
            if (i == tab)
            {
                plate.color = new Color32(201, 211, 207, 255);
                label.color = new Color32(28, 42, 42, 255);
            }
            trainGlyphs[i].color = label.color;
        }
        actionText.text = action;
        execute.interactable = false;
        costText.text = "当前暂无下一阶段";
        materialIcon.enabled = cost != null;
        previewArrow.enabled = tab == 0 && cost != null;
        if (cost == null) return;
        materialIcon.sprite = trainingMaterial;
        if (tab == 1) materialIcon.sprite = Cover(b.Id);
        costText.text = "消耗    " + Held(cost) + "/" + cost.Amount + "     铜币  " + cost.Copper;
        if (p.Level < cost.Level) costText.text = "需要" + cost.Level + "级　" + costText.text;
        execute.interactable = p.Level >= cost.Level && Held(cost) >= cost.Amount && state.Copper >= cost.Copper;
    }
}
