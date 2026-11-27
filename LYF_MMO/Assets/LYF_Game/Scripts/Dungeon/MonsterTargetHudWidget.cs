using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 副本内当前选中怪物的目标血量：头顶条沿用 RO MMonsterHUDComponent 的选中即显示行为，
/// 固定栏沿主界面既有目标区补一条不随头部出画的读数。普通怪与首领共用一条权威生命数据，
/// 满血时同样显示；首领只放大并换醒目配色，不另建第二套控件。
/// </summary>
// 主城 CinemachineBrain 的顺序为 100，血条与名牌在最终镜头确定后投影。
[DefaultExecutionOrder(112)]
public sealed class MonsterTargetHudWidget : MonoBehaviour
{
    /// <summary>血条底边相对头部锚点再上抬的距离，单位为画布参考像素，避免贴住头顶。</summary>
    private const float BarHeadGap = 10f;

    /// <summary>普通怪固定目标条的尺寸，画布逻辑像素，明显短于首领条。</summary>
    private static readonly Vector2 NormalBarSize = new Vector2(240f, 12f);

    /// <summary>首领固定目标条的尺寸，加长加粗以区分首领与普通怪。</summary>
    private static readonly Vector2 BossBarSize = new Vector2(420f, 20f);

    /// <summary>普通怪前景色，与头顶条同一红色。</summary>
    private static readonly Color NormalFillColor = new Color(0.85f, 0.23f, 0.26f, 1f);

    /// <summary>首领前景色，偏橙的高饱和红，远看即可与普通怪区分。</summary>
    private static readonly Color BossFillColor = new Color(1f, 0.36f, 0.12f, 1f);

    /// <summary>普通怪目标条底槽色。</summary>
    private static readonly Color NormalBgColor = new Color(0.088f, 0.119f, 0.124f, 0.91f);

    /// <summary>首领目标条底槽色，压暗以抬高前景对比。</summary>
    private static readonly Color BossBgColor = new Color(0.051f, 0.043f, 0.043f, 0.95f);

    /// <summary>普通怪名称字号，单位为 TMP 点值。</summary>
    private const float NormalNameSize = 22f;

    /// <summary>首领名称字号，随目标条一起放大。</summary>
    private const float BossNameSize = 28f;

    /// <summary>名称文本框相对字号的额外高度，避免中文上下缘被文本框裁掉。</summary>
    private const float NameBoxPadding = 6f;

    /// <summary>固定目标栏整体高度，单位为画布逻辑像素，与预制体 MonsterTargetPanel 一致。</summary>
    private const float PanelHeight = 52f;

    /// <summary>普通怪名称颜色。</summary>
    private static readonly Color NormalNameColor = new Color(1f, 1f, 1f, 1f);

    /// <summary>首领名称颜色，用金色提示这是首领目标。</summary>
    private static readonly Color BossNameColor = new Color(1f, 0.85f, 0.42f, 1f);

    /// <summary>可整体收起的头顶血条内容，轴心在底边中点，尺寸与颜色由预制体保存。</summary>
    [SerializeField] private RectTransform _content;

    /// <summary>头顶条按剩余生命比例横向填充的红色前景，值域为零到一。</summary>
    [SerializeField] private Image _fill;

    /// <summary>同一画布的名牌层，提供与玩家昵称、NPC 称谓相同的头部锚点和缩放。</summary>
    [SerializeField] private RoleNameHudWidget _nameHud;

    /// <summary>主界面固定目标栏根节点，位于既有目标区，不随怪物头部出画收起。</summary>
    [SerializeField] private GameObject _mainContent;

    /// <summary>固定目标栏的目标名称，取本地怪物目录的显示名。</summary>
    [SerializeField] private TMP_Text _mainName;

    /// <summary>固定目标栏的条形容器，普通怪与首领只改这里的尺寸。</summary>
    [SerializeField] private RectTransform _mainBar;

    /// <summary>固定目标栏的底槽，首领换更深配色。</summary>
    [SerializeField] private Image _mainBg;

    /// <summary>固定目标栏的前景，按同一权威生命比例填充。</summary>
    [SerializeField] private Image _mainFill;

    /// <summary>当前名字缓存所属的目标身份，切换目标时失效。</summary>
    private ulong _namedUid;

    /// <summary>当前名字缓存所属的配置编号，对象池复用 UID 时一并校验。</summary>
    private int _namedConfigId;

    /// <summary>已解析名字，空串表示还没有目标。</summary>
    private string _cachedName = string.Empty;

    /// <summary>当前已解析的副本场景编号，用于只在换场景时重读怪物目录。</summary>
    private int _catalogSceneId;

    /// <summary>当前场景的怪物表现目录，提供本地怪物显示名。</summary>
    private ROSceneCreatureCatalog _catalog;

    /// <summary>
    /// 每帧按当前选中怪物读取权威生命：头顶条跟随投影，固定栏留在目标区。
    /// </summary>
    private void LateUpdate()
    {
        // 本帧本地玩家选中的怪物，已包含存活、可见和仍在世界中的判定。
        MonsterEntity monster = ResolveTarget();
        if (monster == null)
        {
            HideHead();
            HideMain();
            return;
        }
        // 服务端下发的生命上限；未初始化时不用猜测值画条。
        int maxHp = monster.AttrComp.MaxHp;
        if (maxHp <= 0)
        {
            HideHead();
            HideMain();
            return;
        }
        // 当前生命占上限的比例，与服务端结算值同源，不做客户端推算。
        float ratio = Mathf.Clamp01((float)monster.AttrComp.CurrentHp / maxHp);
        RefreshHead(monster, ratio);
        RefreshMain(monster, ratio);
    }

    /// <summary>主界面隐藏或副本卸载时收起两条血条，恢复显示后由下一帧重新判定。</summary>
    private void OnDisable()
    {
        HideHead();
        HideMain();
    }

    /// <summary>按最终游戏镜头把头顶条放到怪物头上，出画或没有相机时只收头顶条。</summary>
    /// <param name="monster">本帧仍有效的选中怪物。</param>
    /// <param name="ratio">同一权威生命比例。</param>
    private void RefreshHead(MonsterEntity monster, float ratio)
    {
        // 预制体引用缺失时跳过头顶条，不影响固定目标栏。
        if (_content == null || _fill == null || _nameHud == null)
        {
            return;
        }
        // 本帧绘制世界 HUD 的最终游戏镜头。
        Camera worldCamera = Camera.main;
        // 头部锚点为画布坐标，scale 为该怪物本帧的投影比例。
        Vector2 anchor;
        // 该怪物本帧的投影比例，血条尺寸与偏移一起缩放。
        float scale;
        if (worldCamera == null || !_nameHud.TryGetHeadAnchor(monster, worldCamera, out anchor, out scale))
        {
            HideHead();
            return;
        }
        SetFillRatio(_fill, ratio);
        _content.localScale = Vector3.one * scale;
        _content.anchoredPosition = anchor + Vector2.up * (BarHeadGap * scale);
        SetVisible(_content.gameObject, true);
    }

    /// <summary>刷新主界面固定目标栏：名称、尺寸与配色按普通怪/首领切换，比例与头顶条同源。</summary>
    /// <param name="monster">本帧仍有效的选中怪物。</param>
    /// <param name="ratio">同一权威生命比例。</param>
    private void RefreshMain(MonsterEntity monster, float ratio)
    {
        if (_mainContent == null)
        {
            return;
        }
        // 首领档位来自副本出生配置写入的 Rank，普通怪与精英保持普通档。
        bool isBoss = false;
        if (monster.Rank == MonsterRank.Boss || monster.Rank == MonsterRank.Mvp)
        {
            isBoss = true;
        }
        // 档位只影响外观，每帧按当前目标重算，改选后不需要额外的状态同步。
        ApplyStyle(isBoss);
        if (_mainFill != null)
        {
            SetFillRatio(_mainFill, ratio);
        }
        if (_mainName != null)
        {
            // 名字只随目标身份变化重读目录，避免每帧查表。
            string displayName = ResolveName(monster);
            if (!string.Equals(_mainName.text, displayName))
            {
                _mainName.text = displayName;
            }
        }
        SetVisible(_mainContent, true);
    }

    /// <summary>按普通怪或首领套用固定目标栏的尺寸、配色和名称字号，不触碰生命比例。</summary>
    /// <param name="isBoss">当前目标是否首领档，true 时加长加粗并换金色名称。</param>
    private void ApplyStyle(bool isBoss)
    {
        // 本档目标条的宽度和高度，单位画布逻辑像素。
        float barWidth = NormalBarSize.x;
        float barHeight = NormalBarSize.y;
        // 本档名称字号与文本框高度，文本框高度始终包住一行中文。
        float nameSize = NormalNameSize;
        if (isBoss)
        {
            barWidth = BossBarSize.x;
            barHeight = BossBarSize.y;
            nameSize = BossNameSize;
        }
        if (_mainBar != null)
        {
            _mainBar.sizeDelta = new Vector2(barWidth, barHeight);
        }
        if (_mainBg != null)
        {
            if (isBoss)
            {
                _mainBg.color = BossBgColor;
            }
            else
            {
                _mainBg.color = NormalBgColor;
            }
        }
        if (_mainFill != null)
        {
            if (isBoss)
            {
                _mainFill.color = BossFillColor;
            }
            else
            {
                _mainFill.color = NormalFillColor;
            }
        }
        if (_mainName == null)
        {
            return;
        }
        _mainName.fontSize = nameSize;
        if (isBoss)
        {
            _mainName.color = BossNameColor;
        }
        else
        {
            _mainName.color = NormalNameColor;
        }
        // 名称文本框底边贴在目标条上方，放大字号时同步抬高，保持上缘不超过面板。
        float nameHeight = nameSize + NameBoxPadding;
        _mainName.rectTransform.sizeDelta = new Vector2(barWidth, nameHeight);
        _mainName.rectTransform.anchoredPosition = new Vector2(0f, PanelHeight - nameHeight);
    }

    /// <summary>取本工程已有的本地怪物显示名，缺失时才退回配置编号。</summary>
    /// <param name="monster">当前选中怪物。</param>
    /// <returns>可直接显示的目标名称。</returns>
    private string ResolveName(MonsterEntity monster)
    {
        if (_namedUid == monster.UID && _namedConfigId == monster.ConfigId && _cachedName.Length > 0)
        {
            return _cachedName;
        }
        _namedUid = monster.UID;
        _namedConfigId = monster.ConfigId;
        _cachedName = LookupDisplayName(monster.ConfigId);
        return _cachedName;
    }

    /// <summary>按副本场景登记读取怪物表现目录，取与出生配置同源的显示名。</summary>
    /// <param name="configId">怪物出生使用的原怪物配置编号。</param>
    /// <returns>本地目录中的显示名，目录或条目缺失时使用配置编号。</returns>
    private string LookupDisplayName(int configId)
    {
        // 当前副本的场景编号；不在副本内时为零，不读取任何场景目录。
        int sceneId = 0;
        if (TeamData.Instance != null)
        {
            sceneId = TeamData.Instance.DungeonSceneId;
        }
        // 场景编号只在换副本时变化，目录也只在该场景重读一次。
        if (sceneId != _catalogSceneId)
        {
            _catalogSceneId = sceneId;
            _catalog = null;
            // 当前场景的资源登记，未迁入的编号返回空。
            DungeonSceneResource resource = null;
            if (sceneId > 0)
            {
                resource = DungeonSceneResource.Load(sceneId);
            }
            if (resource != null && !string.IsNullOrEmpty(resource.creatureResource))
            {
                _catalog = ROSceneCreatureCatalog.Load(resource.creatureResource);
            }
        }
        // 与出生配置同编号的目录条目，可能尚为空的不可见机制实体也有条目。
        ROSceneCreatureEntity creature = null;
        if (_catalog != null)
        {
            creature = _catalog.FindEntity(configId);
        }
        if (creature != null && !string.IsNullOrEmpty(creature.displayName))
        {
            return creature.displayName;
        }
        return "怪物 " + configId;
    }

    /// <summary>
    /// 取本地玩家当前选中的怪物；非怪物目标、目标失效或已死亡时返回空。
    /// </summary>
    /// <returns>仍需显示血条的目标怪物，无目标时为空。</returns>
    private static MonsterEntity ResolveTarget()
    {
        // 场景世界与本地角色都来自全局运行环境，不在血条内缓存。
        if (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return null;
        }
        // 当前本地玩家，选中状态保存在它身上。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if (player == null)
        {
            return null;
        }
        // 只有怪物使用本血条，玩家或 NPC 目标保持原有名牌显示。
        MonsterEntity monster = player.Target as MonsterEntity;
        if (monster == null)
        {
            return null;
        }
        // 沿用统一选敌判定：死亡、离开世界或模型不可见都不再显示。
        if (!MSkillTargetDefine.IsValidTarget(monster))
        {
            return null;
        }
        return monster;
    }

    /// <summary>把头顶条整体收起到不显示状态，不清除目标本身。</summary>
    private void HideHead()
    {
        if (_content != null)
        {
            SetVisible(_content.gameObject, false);
        }
    }

    /// <summary>把固定目标栏收起到不显示状态；目标失效、取消选择或离开副本都会走到这里。</summary>
    private void HideMain()
    {
        if (_mainContent != null)
        {
            SetVisible(_mainContent, false);
        }
        // 缓存随目标一起失效，下次选中重新取名字。
        _namedUid = 0UL;
        _namedConfigId = 0;
        _cachedName = string.Empty;
    }

    /// <summary>按目标状态切换节点显隐，已处于目标状态时不写入，避免每帧触发重建。</summary>
    /// <param name="target">要切换的血条根节点或内容节点，允许为空。</param>
    /// <param name="visible">true 显示，false 收起。</param>
    private static void SetVisible(GameObject target, bool visible)
    {
        if (target == null || target.activeSelf == visible)
        {
            return;
        }
        target.SetActive(visible);
    }

    /// <summary>
    /// 按生命比例设置条形的可见宽度：与主角血条一致，用锚点宽度收放而不是精灵填充，
    /// 因为项目血条精灵是九宫格边框图，填充会把比例画到透明边上而失真。
    /// </summary>
    /// <param name="fill">目标条或头顶条的前景图片。</param>
    /// <param name="ratio">同一权威生命比例，零到一。</param>
    private static void SetFillRatio(Image fill, float ratio)
    {
        if (fill == null)
        {
            return;
        }
        // 当前已应用的宽度比例，与目标一致时不重写布局。
        float clamped = Mathf.Clamp01(ratio);
        RectTransform rect = fill.rectTransform;
        Vector2 anchorMax = rect.anchorMax;
        if (Mathf.Approximately(anchorMax.x, clamped))
        {
            return;
        }
        rect.anchorMax = new Vector2(clamped, anchorMax.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
