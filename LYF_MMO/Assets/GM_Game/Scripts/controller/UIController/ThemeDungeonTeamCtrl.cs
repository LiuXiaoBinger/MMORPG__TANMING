using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主题副本队伍控制器。本轮按用户决定只做本地单人队伍：保存队伍状态、驱动正式队伍窗口与主界面快捷队伍栏，
/// 并在已有队伍时由详情页「开始挑战」进入魔神塔。不接入联网组队、匹配、邀请与助战。
/// </summary>
public sealed class ThemeDungeonTeamCtrl : CtrlBase
{
    /// <summary>
    /// 魔神塔在主题副本配置里的编号。RO 侧链路为 ThemeDungeonTable 第 7 条（达纳托斯之战）
    /// 指向 DungeonsTable 的 4060，再由 SceneTable 指向 sc_dungeon_moshenta_001；
    /// 本轮只把这一条绑定到魔神塔场景，其余八本不误进魔神塔。
    /// </summary>
    private const int MoshentaThemeDungeonId = 7;

    /// <summary>职业头像所在图集的 Resources 路径，用于按职业取已有精灵。</summary>
    private const string ProfessionAtlasResourcePath = "UI/Atlas/Common";

    /// <summary>职业头像精灵名前缀，拼上职业编号即完整精灵名。</summary>
    private const string ProfessionSpritePrefix = "UI_Common_Profession_Icon_0";

    /// <summary>主界面视图，用于取正式队伍窗、副本选择窗、详情窗与快捷队伍栏。</summary>
    private readonly MainView _mainView;

    /// <summary>正式队伍窗口实例，来自主界面序列化的正式引用。</summary>
    private readonly ThemeDungeonTeamCreateWindow _teamWindow;

    /// <summary>主界面左侧常驻快捷队伍栏，来自主界面序列化的正式引用。</summary>
    private readonly QuickTeamWidget _quickTeamWidget;

    /// <summary>按职业编号缓存的头像精灵，避免每次刷新都重新查图集。</summary>
    private readonly Dictionary<int, Sprite> _jobSprites = new Dictionary<int, Sprite>();

    /// <summary>本地单人队伍是否已经建立，关闭窗口后仍然保留。</summary>
    private bool _teamCreated;

    /// <summary>建队时记录的当前真实角色昵称。</summary>
    private string _memberName = string.Empty;

    /// <summary>建队时记录的当前真实角色等级。</summary>
    private int _memberLevel;

    /// <summary>建队时记录的当前真实角色职业编号。</summary>
    private int _memberJobId;

    /// <summary>当前生命比例，供队伍窗与快捷栏显示血条。</summary>
    private float _hpRatio;

    /// <summary>当前法力比例，供快捷栏显示蓝条。</summary>
    private float _mpRatio;

    /// <summary>本次挑战选中的副本配置，由副本选择窗的挑战事件传入。</summary>
    private ThemeDungeonEntry _selectedEntry;

    /// <summary>是否已经读取过职业头像图集，避免重复加载。</summary>
    private bool _jobAtlasLoaded;

    /// <summary>已加载的职业头像精灵，按精灵名索引。</summary>
    private Sprite[] _jobAtlasSprites = new Sprite[0];

    /// <summary>绑定队伍窗、快捷队伍栏与副本选择窗事件，事件都只在本控制器内处理。</summary>
    /// <param name="view">主界面视图，必须是 MainView 才能取到正式控件引用。</param>
    public ThemeDungeonTeamCtrl(UIBase view) : base(view)
    {
        _mainView = view as MainView;
        if (_mainView == null)
        {
            return;
        }
        _teamWindow = _mainView.ThemeDungeonTeamCreateWindow;
        _quickTeamWidget = _mainView.QuickTeamWidget;
        if (_mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.OnChallengeRequested += OnChallengeRequested;
        }
        if (_teamWindow != null)
        {
            _teamWindow.CreateRequested += OnCreateRequested;
            _teamWindow.CloseRequested += OnCloseRequested;
            _teamWindow.LeaveRequested += OnLeaveRequested;
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.OpenTeamRequested += OnOpenTeamRequested;
            _quickTeamWidget.OpenPageRequested += OnOpenPageRequested;
        }
        RefreshViews();
    }

    /// <summary>
    /// 详情页「开始挑战」：已有队伍直接进入魔神塔，没有队伍先打开正式队伍窗建队。
    /// 建队不会自动进图，必须再点一次开始挑战。
    /// </summary>
    /// <param name="entry">详情页当前确认挑战的副本配置。</param>
    private void OnChallengeRequested(ThemeDungeonEntry entry)
    {
        if (entry == null)
        {
            return;
        }
        _selectedEntry = entry;
        if (_teamCreated && entry.id == MoshentaThemeDungeonId)
        {
            EnterMoshenta();
            return;
        }
        OpenTeamWindow();
    }

    /// <summary>快捷栏「组队」：打开正式队伍窗，不改变当前选中副本。</summary>
    private void OnOpenTeamRequested()
    {
        OpenTeamWindow();
    }

    /// <summary>展开正式队伍窗，窗口实例由主界面持有，重复打开不会被收起。</summary>
    private void OpenTeamWindow()
    {
        if (_teamWindow == null)
        {
            return;
        }
        if (!_mainView.gameObject.activeSelf)
        {
            _mainView.Show();
        }
        RefreshViews();
        _teamWindow.transform.SetAsLastSibling();
        _teamWindow.Show(true);
        _teamWindow.OpenPage(TeamPage.Members);
    }

    /// <summary>从快捷栏打开已迁移的 RO 界面壳，只处理界面导航。</summary>
    /// <param name="page">快捷栏按钮对应的队伍界面。</param>
    private void OnOpenPageRequested(TeamPage page)
    {
        OpenTeamWindow();
        _teamWindow.OpenPage(page);
    }

    /// <summary>
    /// 创建本地单人队伍。业务只使用当前真实角色数据，主角未进入场景世界时不做任何占位建队。
    /// </summary>
    private void OnCreateRequested()
    {
        if (_teamCreated)
        {
            return;
        }
        // 本地场景世界中的当前真实玩家，作为单人队伍唯一成员。
        PlayerEntity localRole = ResolveLocalRole();
        if (localRole == null || localRole.BaseInfo == null)
        {
            // 正式业务只用真实角色；主角未就绪时不建队，也不写任何测试或占位数据。
            return;
        }
        _memberName = localRole.BaseInfo.Nickname;
        _memberLevel = localRole.BaseInfo.Level;
        _memberJobId = localRole.BaseInfo.JobId;
        _hpRatio = CalculateRatio(localRole.BaseInfo.CurrHp, localRole.BaseInfo.MaxHp);
        _mpRatio = CalculateRatio(localRole.BaseInfo.CurrMp, localRole.BaseInfo.MaxMp);
        _teamCreated = true;
        RefreshViews();
    }

    /// <summary>关闭队伍窗，队伍状态保留在控制器里，再次挑战仍是已建队。</summary>
    private void OnCloseRequested()
    {
        if (_teamWindow != null)
        {
            _teamWindow.CloseWindow();
        }
    }

    /// <summary>离开本地单人队伍，恢复快捷栏和正式窗口的未建队展示。</summary>
    private void OnLeaveRequested()
    {
        _teamCreated = false;
        _memberName = string.Empty;
        _memberLevel = 0;
        RefreshViews();
    }

    /// <summary>收起副本相关界面并加载魔神塔场景，主角由新场景的主城管理器重建。</summary>
    private void EnterMoshenta()
    {
        if (_selectedEntry == null || _selectedEntry.id != MoshentaThemeDungeonId)
        {
            return;
        }
        if (_mainView.ThemeStoryWindow != null)
        {
            _mainView.ThemeStoryWindow.CloseWindow();
        }
        if (_mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.CloseWindow();
        }
        if (_teamWindow != null)
        {
            _teamWindow.CloseWindow();
        }
        if (SceneMgr.Instance == null)
        {
            return;
        }
        SceneMgr.Instance.LoadSceneMode(SceneType.Scene_DungeonMoshenta);
    }

    /// <summary>把控制器保存的队伍状态同步到正式队伍窗与主界面快捷队伍栏。</summary>
    private void RefreshViews()
    {
        // 当前职业对应的现有图集头像。
        Sprite jobSprite = ResolveJobSprite(_memberJobId);
        if (_teamWindow != null)
        {
            _teamWindow.SetTeamCreated(_teamCreated, _memberName, _memberLevel);
            _teamWindow.SetProfessionSprite(jobSprite);
            if (_selectedEntry != null)
            {
                _teamWindow.SetTargetName(_selectedEntry.themeName);
            }
            else
            {
                _teamWindow.SetTargetName(string.Empty);
            }
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.SetTeamState(_teamCreated, _memberName, _memberLevel, _hpRatio,
                _mpRatio);
            _quickTeamWidget.SetProfessionSprite(jobSprite);
        }
    }

    /// <summary>取当前场景世界中的本地主角，未进入世界时返回空。</summary>
    /// <returns>本地玩家实体；全局或场景世界未就绪时返回 null。</returns>
    private PlayerEntity ResolveLocalRole()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return null;
        }
        return Global.Instance.SceneWorld.LocalRole;
    }

    /// <summary>按职业编号从已有图集取职业头像精灵，取不到时返回空并由控件保留默认图。</summary>
    /// <param name="jobId">服务器返回的职业配置编号。</param>
    /// <returns>职业头像精灵；图集内不存在该职业图时返回 null。</returns>
    private Sprite ResolveJobSprite(int jobId)
    {
        if (jobId <= 0)
        {
            return null;
        }
        // 已按职业编号查找过的头像结果。
        Sprite cached = null;
        if (_jobSprites.TryGetValue(jobId, out cached))
        {
            return cached;
        }
        if (!_jobAtlasLoaded)
        {
            _jobAtlasSprites = Resources.LoadAll<Sprite>(ProfessionAtlasResourcePath);
            _jobAtlasLoaded = true;
        }
        // RO 职业头像在 Common 图集中的精灵名。
        string targetName = ProfessionSpritePrefix + jobId;
        // 本次查找结果，不存在时保留预制体默认头像。
        Sprite result = null;
        // 当前图集精灵的零基索引。
        for (int index = 0; index < _jobAtlasSprites.Length; index++)
        {
            // 当前比较的现有图集精灵。
            Sprite current = _jobAtlasSprites[index];
            if (current != null && current.name == targetName)
            {
                result = current;
                break;
            }
        }
        _jobSprites[jobId] = result;
        return result;
    }

    /// <summary>计算比例，分母为零时返回零，结果夹到 0 到 1。</summary>
    /// <param name="current">当前值。</param>
    /// <param name="max">最大值。</param>
    /// <returns>0 到 1 之间的比例。</returns>
    private static float CalculateRatio(int current, int max)
    {
        if (max <= 0)
        {
            return 0f;
        }
        // 当前资源占最大资源的比例，无量纲。
        float ratio = (float)current / max;
        if (ratio < 0f)
        {
            return 0f;
        }
        if (ratio > 1f)
        {
            return 1f;
        }
        return ratio;
    }

    /// <summary>注销窗口、快捷栏与副本选择窗事件，避免控制器释放后仍被回调。</summary>
    public override void Dispose()
    {
        if (_mainView != null && _mainView.ThemeDungeonArroundWindow != null)
        {
            _mainView.ThemeDungeonArroundWindow.OnChallengeRequested -= OnChallengeRequested;
        }
        if (_teamWindow != null)
        {
            _teamWindow.CreateRequested -= OnCreateRequested;
            _teamWindow.CloseRequested -= OnCloseRequested;
            _teamWindow.LeaveRequested -= OnLeaveRequested;
        }
        if (_quickTeamWidget != null)
        {
            _quickTeamWidget.OpenTeamRequested -= OnOpenTeamRequested;
            _quickTeamWidget.OpenPageRequested -= OnOpenPageRequested;
        }
        _selectedEntry = null;
    }
}
