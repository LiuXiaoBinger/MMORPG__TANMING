using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;
using UnityEngine.UI;

/// <summary>副本纪事主页，在同一窗口浏览副本、首领、掉落及难度，挑战交回现有队伍控制器。</summary>
public sealed class ThemeDungeonArroundWindow : WindowBase
{
    /// <summary>当前章节对应的全屏场景插画。</summary>
    [SerializeField] private RawImage backdrop;
    /// <summary>左侧可滚动的副本选项模板。</summary>
    [SerializeField] private DungeonJournalOption dungeonTemplate;
    /// <summary>右侧难度选项模板。</summary>
    [SerializeField] private DungeonJournalOption difficultyTemplate;
    /// <summary>分层难度的纵向层数选项模板。</summary>
    [SerializeField] private DungeonJournalOption layerTemplate;
    /// <summary>只有分层难度才显示的滚动列。</summary>
    [SerializeField] private GameObject layerPanel;
    /// <summary>中央首领头像行的正式模板。</summary>
    [SerializeField] private DungeonBossPortraitWidget bossTemplate;
    /// <summary>当前副本名。</summary>
    [SerializeField] private Text dungeonTitle;
    /// <summary>当前章节与推荐等级。</summary>
    [SerializeField] private Text chapter;
    /// <summary>配置正文的首句，完整内容可在档案中查看。</summary>
    [SerializeField] private Text synopsis;
    /// <summary>当前角色在本难度、本周期已击败的首领数。</summary>
    [SerializeField] private Text progress;
    /// <summary>所选首领的掉落物品与数量。</summary>
    [SerializeField] private Text rewardName;
    /// <summary>该难度的掉落周期说明。</summary>
    [SerializeField] private Text rewardCycle;
    /// <summary>横向滚动掉落栏内的物品格模板。</summary>
    [SerializeField] private DungeonRewardPreviewWidget rewardTemplate;
    /// <summary>进入原有组队确认流程的按钮。</summary>
    [SerializeField] private Button challenge;
    /// <summary>寻找队伍入口。</summary>
    [SerializeField] private Button findTeam;
    /// <summary>打开项目既有剧情档案的入口。</summary>
    [SerializeField] private Button archive;
    /// <summary>终绝难度进入本周进度与上周宝箱的入口。</summary>
    [SerializeField] private Button weeklyButton;
    /// <summary>预制体内与副本主页共用生命周期的终绝周赏。</summary>
    [SerializeField] private DungeonFinalJournalWidget weekly;
    /// <summary>关闭副本纪事。</summary>
    [SerializeField] private Button close;
    /// <summary>选中首领后显示在页面右侧的资料面板。</summary>
    [SerializeField] private GameObject dossier;
    /// <summary>关闭资料后继续浏览当前副本和难度。</summary>
    [SerializeField] private Button closeDossier;
    /// <summary>右侧资料的首领名。</summary>
    [SerializeField] private Text dossierName;
    /// <summary>右侧资料中的首领原图。</summary>
    [SerializeField] private RawImage dossierPortrait;
    /// <summary>正式怪物目录与难度表中的数值说明。</summary>
    [SerializeField] private Text dossierStats;
    /// <summary>首领技能栏目，仅在该首领有已接入技能时显示。</summary>
    [SerializeField] private GameObject skillSection;
    /// <summary>当前首领技能表中用于展示的名字。</summary>
    [SerializeField] private Text dossierSkills;
    /// <summary>查看此首领时对应的个人奖励。</summary>
    [SerializeField] private Text dossierDrop;

    /// <summary>九个主题副本使用的正式目录。</summary>
    private ThemeDungeonEntry[] entries;
    /// <summary>当前展示的副本数组下标。</summary>
    private int selectedDungeon;
    /// <summary>正在浏览的难度配置编号，新秀为一。</summary>
    private int degree = 1;
    /// <summary>所选层数，非分层难度为零。</summary>
    private int layer;
    /// <summary>中央头像列表当前查看的下标。</summary>
    private int selectedBoss;
    /// <summary>上一次刷新头像时的周期起始日，用于跨刷新点更新可见标记。</summary>
    private int displayedPeriod;
    /// <summary>上一次完成选项定位时的窗口尺寸，用于横屏比例改变后保持当前选项可见。</summary>
    private Vector2 displayedSize;
    /// <summary>横向奖品栏已经分配的物品格，切换首领时复用。</summary>
    private readonly List<DungeonRewardPreviewWidget> rewardSlots = new List<DungeonRewardPreviewWidget>();
    /// <summary>当前场景已迁入的表现与基础生命配置。</summary>
    private ROSceneCreatureCatalog creatures;
    /// <summary>当前选定的副本和难度对应的首领奖励行，按配置顺序展示。</summary>
    private readonly List<AwardTable> awards = new List<AwardTable>();
    /// <summary>来自预制体模板的副本选择行。</summary>
    private readonly List<DungeonJournalOption> dungeonOptions = new List<DungeonJournalOption>();
    /// <summary>来自难度表的难度选择行，与该表顺序一致。</summary>
    private readonly List<DungeonJournalOption> difficultyOptions = new List<DungeonJournalOption>();
    /// <summary>按分层上限复用的层数行。</summary>
    private readonly List<DungeonJournalOption> layerOptions = new List<DungeonJournalOption>();
    /// <summary>复用于不同主题的首领头像格。</summary>
    private readonly List<DungeonBossPortraitWidget> portraits = new List<DungeonBossPortraitWidget>();
    /// <summary>由已有 MainView 注入的剧情档案窗口。</summary>
    private ThemeStoryWindow story;
    /// <summary>队伍控制器接收当前副本、难度与层数后发起挑战。</summary>
    public event Action<ThemeDungeonEntry, int, int> OnChallengeRequested;
    /// <summary>队伍控制器按当前目标打开寻找队伍页。</summary>
    public event Action<ThemeDungeonEntry, int, int> FindTeamRequested;
    /// <summary>玩家明确点击上周宝箱后，由控制器发送周期到游戏服。</summary>
    public event Action<int> WeeklyClaimRequested;

    /// <summary>绑定 MainView 持有的剧情窗口，保持其已有挑战回调。</summary>
    /// <param name="window">同一主视图中的正式剧情窗口。</param>
    public void SetStoryWindow(ThemeStoryWindow window)
    {
        story = window;
        story.OnEnterDungeon = (entry, difficulty, floor) => OnChallengeRequested?.Invoke(entry, difficulty, floor);
    }

    /// <summary>从正式预制体绑定各个操作，首次打开时再读取副本配置。</summary>
    public override void InitWindow()
    {
        if (entries != null) return;
        entries = JsonUtility.FromJson<ThemeDungeonConfig>(Resources.Load<TextAsset>("ThemeDungeon/theme_dungeon").text).items;
        close.onClick.AddListener(CloseWindow);
        closeDossier.onClick.AddListener(CloseDossier);
        archive.onClick.AddListener(OpenArchive);
        weeklyButton.onClick.AddListener(() => weekly.Open(Global.Instance?.SceneWorld?.LocalRole?.GetRoleComponent<RoleCountComponent>()));
        weekly.ClaimRequested += week => WeeklyClaimRequested?.Invoke(week);
        challenge.onClick.AddListener(() => OnChallengeRequested?.Invoke(entries[selectedDungeon], degree, layer));
        findTeam.onClick.AddListener(() => FindTeamRequested?.Invoke(entries[selectedDungeon], degree, layer));
        // 每个副本保留正式配置顺序，列表可滚动到最后一项。
        for (int index = 0; index < entries.Length; index++)
        {
            // 回调捕获行自己的配置下标。
            int row = index;
            // 复制已经有图、字和按钮的正式模板。
            DungeonJournalOption option = AddOption(dungeonTemplate, entries[row].themeName, () => SelectDungeon(row), Art(entries[row].scenePic));
            dungeonOptions.Add(option);
        }
    }

    /// <summary>按已有窗口生命周期打开，返回本页时保留所选副本和难度。</summary>
    /// <param name="isShow">是否打开。</param><param name="obj">Unity 窗口基类要求的可选刷新参数。</param>
    public override void Show(bool isShow = true, object obj = null)
    {
        base.Show(isShow, obj);
        weekly.gameObject.SetActive(false);
        if (!isShow) return;
        transform.SetAsLastSibling();
        InitWindow();
        if (difficultyOptions.Count == 0) BuildDifficulties();
        // 从战斗内进入时，展示玩家当前实例的准确副本、难度和层数。
        TeamData team = TeamData.Instance;
        if (team.DungeonInstanceId != 0)
        {
            // 通过实际入场副本编号查主题，不依赖副本标题。
            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index].storyDungeonId != team.DungeonId) continue;
                selectedDungeon = index;
                degree = team.DungeonsDegree;
                layer = team.DungeonLayer;
                break;
            }
        }
        SelectDungeon(selectedDungeon);
    }

    /// <summary>从难度表建立六档选项，文字、主键与挑战请求使用同一来源。</summary>
    private void BuildDifficulties()
    {
        // 服务器与客户端共用的难度行，按策划顺序排列。
        List<DungeonDifficultyTable> rows = LubanMgr.Instance.ConfigTables.TbDungeonDifficultyTable.DataList;
        // 每个按钮捕获该行主键。
        for (int index = 0; index < rows.Count; index++)
        {
            // 本行实际由协议发给服务器的难度号。
            DungeonDifficultyTable row = rows[index];
            difficultyOptions.Add(AddOption(difficultyTemplate, row.Name, () => SelectDegree(row.Id)));
        }
    }

    /// <summary>在对应列表中实例化正式选项模板。</summary>
    /// <param name="template">含点击区、选中底纹和字体的模板。</param><param name="label">显示名。</param>
    /// <param name="click">本行的选择回调。</param><param name="texture">仅副本行使用的插画。</param>
    private static DungeonJournalOption AddOption(DungeonJournalOption template, string label, UnityEngine.Events.UnityAction click, Texture texture = null)
    {
        // 模板在布局中保持隐藏，实例继承其尺寸和序列化引用。
        DungeonJournalOption option = Instantiate(template, template.transform.parent);
        option.Bind(label, texture);
        option.Button.onClick.AddListener(click);
        option.gameObject.SetActive(true);
        return option;
    }

    /// <summary>加载主题表中的插画或立绘，Resources 路径使用扩展名前的部分。</summary>
    /// <param name="path">配置中相对 UI/Texture 的资源名。</param>
    private static Texture Art(string path)
    {
        return Resources.Load<Texture2D>("UI/Texture/" + System.IO.Path.ChangeExtension(path, null));
    }

    /// <summary>切换左侧副本，刷新场景图与本难度的首领阵容。</summary>
    /// <param name="index">主题目录中的下标。</param>
    private void SelectDungeon(int index)
    {
        selectedDungeon = index;
        selectedBoss = 0;
        dossier.SetActive(false);
        // 配置条目中的插画、剧情、章节和等级均直接展示。
        ThemeDungeonEntry entry = entries[index];
        backdrop.texture = Art(entry.scenePic);
        dungeonTitle.text = entry.themeName;
        chapter.text = entry.chapterText + "  /  Lv." + entry.chapterLevel;
        // 详情页只取剧情首句，未注册战斗的主题也能浏览档案。
        string content = entry.chapterContent.Replace("\\n", "\n").Trim();
        int stop = content.IndexOfAny(new[] { '。', '\n', '！', '？', '…' });
        if (stop >= 0) content = content.Substring(0, stop + 1).Trim();
        if (content.Length > 37) content = content.Substring(0, 36) + "…";
        synopsis.text = content;
        // 副本主表将入口编号映射到实际场景，不使用主题宣传图中的首领替代战斗首领。
        DungeonsTable dungeon = LubanMgr.Instance.GetDungeonsTableById(entry.storyDungeonId);
        DungeonSceneResource registration = null;
        if (dungeon != null) registration = DungeonSceneResource.Load(dungeon.SceneID);
        creatures = null;
        if (registration != null) creatures = ROSceneCreatureCatalog.Load(registration.creatureResource);
        // 更新左侧选中的一行。
        for (int row = 0; row < dungeonOptions.Count; row++) dungeonOptions[row].Select(row == index);
        SelectDegree(degree);
        dungeonOptions[index].Reveal();
    }

    /// <summary>切换难度时同步重建对应的首领、奖励和可选层数。</summary>
    /// <param name="value">难度主键。</param>
    private void SelectDegree(int value)
    {
        if (degree != value) layer = 0;
        degree = value;
        // 是否分层及上限取自正式难度表。
        DungeonDifficultyTable difficulty = LubanMgr.Instance.GetDungeonDifficultyById(degree);
        if (difficulty.MaxLayer > 0) layer = Mathf.Clamp(layer, 1, difficulty.MaxLayer);
        else layer = 0;
        // 难度表的行位置与按钮一一对应，主键可以独立于列表位置。
        List<DungeonDifficultyTable> rows = LubanMgr.Instance.ConfigTables.TbDungeonDifficultyTable.DataList;
        for (int index = 0; index < rows.Count; index++) difficultyOptions[index].Select(rows[index].Id == degree);
        layerPanel.SetActive(difficulty.MaxLayer > 0);
        // 各难度复用已有层数行，上限增大时追加模板。
        for (int floor = layerOptions.Count + 1; floor <= difficulty.MaxLayer; floor++)
        {
            // 捕获具体层号，第一层为一。
            int selected = floor;
            layerOptions.Add(AddOption(layerTemplate, selected + " 层", () => SelectLayer(selected)));
        }
        // 若切换到层数较少的难度，隐藏超出上限的行。
        for (int index = 0; index < layerOptions.Count; index++) layerOptions[index].gameObject.SetActive(index < difficulty.MaxLayer);
        SelectLayer(layer);
        awards.Clear();
        // 预览读取结算端使用的同一组首领奖励行。
        List<AwardTable> source = LubanMgr.Instance.ConfigTables.TbAwardTable.DataList;
        for (int index = 0; index < source.Count; index++)
        {
            // 匹配当前基础副本和难度的首领。
            AwardTable award = source[index];
            if (award.DungeonId == entries[selectedDungeon].storyDungeonId && award.DifficultyId == degree) awards.Add(award);
        }
        selectedBoss = Mathf.Clamp(selectedBoss, 0, Mathf.Max(0, awards.Count - 1));
        BuildPortraits();
        RefreshBossMarks();
        RefreshReward();
        if (dossier.activeSelf) RefreshDossier();
    }

    /// <summary>更新将发给服务器的层号以及资料中对应的配置。</summary>
    /// <param name="value">非分层难度为零，终绝从一开始。</param>
    private void SelectLayer(int value)
    {
        layer = value;
        for (int index = 0; index < layerOptions.Count; index++) layerOptions[index].Select(index + 1 == layer);
        if (layer > 0) layerOptions[layer - 1].Reveal();
        if (dossier.activeSelf && awards.Count > 0) RefreshDossier();
    }

    /// <summary>创建实际需要的头像格，对尚未迁入的主题仅展示其正式档案首领。</summary>
    private void BuildPortraits()
    {
        // 每个战斗首领只占一格，纯档案主题展示一格原有立绘。
        int total = Mathf.Max(1, awards.Count);
        for (int index = 0; index < total; index++)
        {
            if (portraits.Count <= index)
            {
                // 模板序列化保存菱形遮罩、头像和红色斩痕。
                DungeonBossPortraitWidget slot = Instantiate(bossTemplate, bossTemplate.transform.parent);
                // 每个点击区保存自己的头像格下标。
                int row = index;
                slot.Button.onClick.AddListener(() => OpenDossier(row));
                portraits.Add(slot);
            }
            // 没有战斗配置时沿用章节的档案首领名及对应立绘。
            string name = entries[selectedDungeon].leaderName;
            Texture texture = null;
            if (awards.Count > 0)
            {
                name = awards[index].BossName;
                // 有实际战斗资源时按模型名取该首领的原版头像。
                ROSceneCreatureEntity creature = creatures?.FindEntity(awards[index].BossId);
                if (creature != null) texture = Resources.Load<Texture2D>("UI/Texture/DungeonBoss/" + creature.modelName);
            }
            if (texture == null) texture = Art(entries[selectedDungeon].painting);
            portraits[index].Bind(name, texture);
        }
        for (int index = 0; index < portraits.Count; index++) portraits[index].gameObject.SetActive(index < total);
    }

    /// <summary>从当前角色的权威记录逐只刷新斩痕和数量。</summary>
    public void RefreshBossMarks()
    {
        if (portraits.Count == 0) return;
        displayedPeriod = RoleCountComponent.BossPeriodDay(degree, DateTime.UtcNow);
        // 登录角色的快照与在线期间的首领记录保存在同一组件。
        PlayerEntity role = Global.Instance?.SceneWorld?.LocalRole;
        RoleCountComponent counts = role?.GetRoleComponent<RoleCountComponent>();
        // 本周期击杀数只计当前副本与难度下已记录的首领。
        int killed = 0;
        for (int index = 0; index < awards.Count; index++)
        {
            // 该首领的唯一进度键，切换难度后自然指向另一条记录。
            bool defeated = counts != null && counts.IsDungeonBossDefeated(awards[index].Id, displayedPeriod);
            if (defeated) killed++;
            portraits[index].SetState(defeated, dossier.activeSelf && selectedBoss == index);
        }
        progress.text = "首领进度   " + killed + " / " + awards.Count;
        if (awards.Count == 0)
        {
            portraits[0].SetState(false, dossier.activeSelf);
            progress.text = "副本档案";
        }
    }

    /// <summary>页面持续打开跨过日周边界时清除上一周期的红斩。</summary>
    private void Update()
    {
        if (portraits.Count == 0) return;
        if (displayedPeriod != RoleCountComponent.BossPeriodDay(degree, DateTime.UtcNow)) RefreshBossMarks();
        // 屏幕变窄或变宽后，原本可见的高层和后段副本可能移出滚动视口。
        Vector2 size = ((RectTransform)transform).rect.size;
        if (size == displayedSize) return;
        displayedSize = size;
        dungeonOptions[selectedDungeon].Reveal();
        if (layer > 0) layerOptions[layer - 1].Reveal();
    }

    /// <summary>显示当前首领实际配置的物品、单次入包数量和重置周期。</summary>
    private void RefreshReward()
    {
        weeklyButton.gameObject.SetActive(degree == DungeonFinalWeek.Difficulty);
        if (degree == DungeonFinalWeek.Difficulty)
        {
            for (int index = 0; index < rewardSlots.Count; index++) rewardSlots[index].gameObject.SetActive(false);
            rewardName.gameObject.SetActive(true);
            rewardName.text = "终绝周赏宝箱";
            rewardCycle.text = "北京时间周五零点结算，按最高通关层数领取一份";
            return;
        }
        // 与已发布的掉落编辑器使用相同的多包字段，空包也保留该首领的进度。
        int[] packIds = Array.Empty<int>();
        if (awards.Count > 0) packIds = awards[selectedBoss].PackIds;
        rewardName.text = "暂无掉落配置";
        rewardCycle.text = string.Empty;
        if (packIds.Length > 0)
        {
            rewardCycle.text = "各首领每周首次击败掉落";
            if (degree == 1) rewardCycle.text = "各首领每日首次击败掉落";
            // 名称与数量也用于首领档案；顺序与服务端发给背包的掉落一致。
            string[] names = new string[packIds.Length];
            for (int index = 0; index < packIds.Length; index++)
            {
                // 逐包读取真实的物品、数量和图标。
                AwardPackTable pack = LubanMgr.Instance.ConfigTables.TbAwardPackTable.Get(packIds[index]);
                ItemConfigBase item = LubanMgr.Instance.GetItemConfigById(pack.ItemId);
                names[index] = item.ItemName + "  ×" + pack.ItemCount;
                if (index == rewardSlots.Count) rewardSlots.Add(Instantiate(rewardTemplate, rewardTemplate.transform.parent));
                rewardSlots[index].Bind(item, pack.ItemCount);
            }
            rewardName.text = string.Join("、", names);
        }
        rewardName.gameObject.SetActive(packIds.Length <= 1);
        for (int index = 0; index < rewardSlots.Count; index++) rewardSlots[index].gameObject.SetActive(index < packIds.Length);
    }

    /// <summary>打开选中首领的资料抽屉。</summary><param name="index">被点击头像的下标。</param>
    private void OpenDossier(int index)
    {
        selectedBoss = index;
        dossier.SetActive(true);
        RefreshBossMarks();
        RefreshReward();
        RefreshDossier();
    }

    /// <summary>展示真实首领资料与当前难度掉落。</summary>
    private void RefreshDossier()
    {
        dossierPortrait.texture = portraits[selectedBoss].Portrait;
        dossierDrop.text = rewardName.text + "\n" + rewardCycle.text;
        skillSection.SetActive(false);
        dossierName.text = entries[selectedDungeon].leaderName;
        dossierStats.text = entries[selectedDungeon].chapterContent.Replace("\\n", "\n");
        if (awards.Count == 0) return;
        // 正式怪物基础血量与难度增益分开显示，不将基础值误当最终属性。
        AwardTable boss = awards[selectedBoss];
        dossierName.text = boss.BossName;
        dossierStats.text = string.Empty;
        ROSceneCreatureEntity creature = creatures?.FindEntity(boss.BossId);
        if (creature != null)
        {
            dossierStats.text = "基础气血   " + creature.maxHp.ToString("N0");
            if (creature.journalSkills != null && creature.journalSkills.Length > 0)
            {
                skillSection.SetActive(true);
                dossierSkills.text = string.Join("　·　", creature.journalSkills);
            }
        }
        // 当前副本、难度和层数使用的逐怪基因效果。
        DungeonMonsterDifficultyTable tuning = LubanMgr.Instance.GetDungeonMonsterDifficulty(boss.DungeonId, degree, layer, boss.BossId);
        if (tuning == null) return;
        for (int index = 0; index < tuning.GeneIds.Length; index++)
        {
            // 只显示该难度有数值的正式效果，百分比单位来自基因定义。
            Gene gene = LubanMgr.Instance.GetGeneInfoById(tuning.GeneIds[index]);
            if (gene == null || tuning.GeneValues[index] == 0) continue;
            string unit = string.Empty;
            if (gene.ValueType == 1) unit = "%";
            dossierStats.text += "\n" + gene.EffectDesc + "   " + tuning.GeneValues[index] + unit;
        }
    }

    /// <summary>关闭当前资料抽屉，保留已选的首领和掉落预览。</summary>
    private void CloseDossier()
    {
        dossier.SetActive(false);
        RefreshBossMarks();
    }

    /// <summary>从纪事标题栏打开项目既有剧情档案，带上当前选择。</summary>
    private void OpenArchive()
    {
        story.Show(true, entries[selectedDungeon]);
        story.SelectDifficulty(degree);
        if (layer > 0) story.SelectLayer(layer);
    }
}
