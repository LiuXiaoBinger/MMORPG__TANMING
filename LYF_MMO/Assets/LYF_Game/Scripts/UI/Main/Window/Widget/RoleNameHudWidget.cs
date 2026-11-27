using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>在主界面画布上跟随玩家和已配置称谓的主城 NPC，显示名字及身份。</summary>
// 主城 CinemachineBrain 的顺序为 100，名牌和护盾都在最终镜头确定后投影。
[DefaultExecutionOrder(110)]
public sealed class RoleNameHudWidget : MonoBehaviour
{
    /// <summary>四行信息共用的底边距头骨的距离，单位为米，近景下称谓仍高于头部。</summary>
    private const float HeadGap = 0.62f;

    /// <summary>NPC 名牌底边高出站立碰撞体顶端的距离，单位为米。</summary>
    private const float NpcHeadGap = 0.14f;

    /// <summary>角色投影达到约六分之一参考屏高时使用预制体原字号，单位为画布逻辑像素。</summary>
    private const float FullSizeBodyHeight = 180f;

    /// <summary>昵称上缘与护盾下缘之间的距离，单位为画布逻辑像素。</summary>
    private const float ShieldGap = 1f;

    /// <summary>称谓与昵称之间的距离，单位为画布逻辑像素。</summary>
    private const float TitleGap = 1f;

    /// <summary>按职业编号预先加上尖括号，避免名牌跟随时逐帧拼接称谓；jobName 为职业配置中的原名。</summary>
    private static readonly List<string> JobTitles = ConstDefine.RoleJobName.ConvertAll(jobName => "<" + jobName + ">");

    /// <summary>存放字体、描边和对齐样式的常驻 TMP 模板，模板本身始终隐藏。</summary>
    [SerializeField] private TMP_Text _labelTemplate;

    /// <summary>单独保存称谓字号、颜色和描边的隐藏模板。</summary>
    [SerializeField] private TMP_Text _titleTemplate;

    /// <summary>每帧复用的场景实体快照，内容由统一场景世界提供。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();

    /// <summary>按可见角色顺序复用的两行文本，数量取决于同时可见的玩家和 NPC。</summary>
    private readonly List<(TMP_Text Name, TMP_Text Title)> _labels =
        new List<(TMP_Text Name, TMP_Text Title)>();

    /// <summary>名牌层使用的主界面画布。</summary>
    private Canvas _canvas;

    /// <summary>覆盖画布的名牌层，以中心为锚点换算昵称位置。</summary>
    private RectTransform _root;

    /// <summary>取得主画布和用于世界点投影的名牌层。</summary>
    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _root = (RectTransform)transform;
    }

    /// <summary>角色与镜头更新完成后，投影头顶位置并复用可见昵称。</summary>
    private void LateUpdate()
    {
        // 场景初始化与清理期间可能暂时没有世界或游戏相机。
        SceneWorldSystem world = Global.Instance?.SceneWorld;
        Camera worldCamera = Camera.main;
        if (world == null || worldCamera == null)
        {
            HideUnused(0);
            return;
        }

        world.CopyEntities(_entities);
        // 已占用的标签数，同时是下一张可复用标签的下标。
        int visibleCount = 0;
        for (int index = 0; index < _entities.Count; index++)
        {
            // 场景快照同时包含玩家、NPC 和其他实体，只有前两者使用这套两行名牌。
            MEntity entity = _entities[index];
            // 第一行显示玩家昵称或出生表里的 NPC 名字。
            string nickname;
            // 第二行显示玩家职业或 NPC 配置的身份称谓。
            string title;
            if (entity is RoleEntity role && role.BaseInfo != null)
            {
                nickname = role.BaseInfo.Nickname;
                title = ResolveTitle(role.BaseInfo.JobId);
            }
            else if (entity is NpcEntity npc && !string.IsNullOrEmpty(npc.NameplateTitle))
            {
                nickname = npc.Name;
                title = npc.NameplateTitle;
            }
            else continue;

            if (string.IsNullOrWhiteSpace(nickname) || !entity.Model.IsLoaded || !entity.Model.Visible) continue;
            // 两行共用最终游戏镜头的屏幕锚点和随距离变化的字号倍率。
            if (!TryProjectHead(entity, worldCamera, out Vector2 anchor, out float scale)) continue;
            ShowName(visibleCount, nickname, title, anchor, scale);
            visibleCount++;
        }
        HideUnused(visibleCount);
    }

    /// <summary>把头骨位置投影到主画布，供名牌、护盾和怪物血条共用同一条垂直中线。</summary>
    /// <param name="entity">当前需要绘制头顶 HUD 的实体，玩家、NPC 或怪物都可以。</param>
    /// <param name="worldCamera">本帧观察该实体的场景相机。</param>
    /// <param name="anchor">实体头部上方按距离留出间距后的画布坐标。</param>
    /// <param name="scale">按实体屏幕高度计算的等比缩放，与名牌使用同一公式。</param>
    /// <returns>锚点位于该相机可见范围内时为真。</returns>
    public bool TryGetHeadAnchor(MEntity entity, Camera worldCamera, out Vector2 anchor, out float scale)
    {
        return TryProjectHead(entity, worldCamera, out anchor, out scale);
    }

    /// <summary>用最终游戏相机投影名牌，玩家跟随头骨，NPC 使用按模型标定的站立高度。</summary>
    /// <param name="entity">正在显示名字的场景实体。</param>
    /// <param name="worldCamera">当前绘制主城的世界相机。</param>
    /// <param name="anchor">名牌底边在主界面画布内的位置。</param>
    /// <param name="scale">按模型在屏幕上的高度计算的文字缩放比例。</param>
    private bool TryProjectHead(MEntity entity, Camera worldCamera, out Vector2 anchor, out float scale)
    {
        anchor = Vector2.zero;
        scale = 0f;
        // 非人形或尚未载入骨架的模型使用既有实体身高。
        Vector3 head = entity.Position + Vector3.up * entity.Height;
        // 已完成动画更新的人形骨架提供这帧的头部世界位置。
        Animator animator = entity.Model.Ator?.UnityAnimator;
        // NPC 从含帽饰的模型顶端留小间距，玩家继续使用角色头骨的间距。
        float gap = HeadGap;
        if (entity is NpcEntity) gap = NpcHeadGap;
        else if (animator != null && animator.isHuman)
        {
            // 不同职业的头骨随动作上下移动，四行信息一起跟随。
            Transform bone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (bone != null) head = bone.position;
        }
        // 屏幕纵横坐标以像素计，深度与近裁剪面使用相同单位。
        Vector3 screen = worldCamera.WorldToScreenPoint(head + Vector3.up * gap);
        if (screen.z <= worldCamera.nearClipPlane || !worldCamera.pixelRect.Contains(screen)) return false;
        // 覆盖画布传空相机，相机画布用自身 UI 相机来换算屏幕坐标。
        Camera uiCamera = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCamera = _canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, uiCamera, out anchor)) return false;

        // 投影矩阵给出距镜头一米处每米对应的屏幕像素；正交相机的倍率与深度无关。
        float pixelsPerMeter = worldCamera.pixelHeight * worldCamera.projectionMatrix.m11 * 0.5f;
        if (!worldCamera.orthographic)
        {
            // 角色站立中心的世界坐标；待机动作只移动名牌锚点。
            Vector3 bodyCenter = entity.Position + Vector3.up * (entity.Height * 0.5f);
            // 相机前向上量得的中心深度，单位为米，与透视投影分母一致。
            float depth = Vector3.Dot(bodyCenter - worldCamera.transform.position, worldCamera.transform.forward);
            pixelsPerMeter /= Mathf.Max(depth, worldCamera.nearClipPlane);
        }
        // 俯视时站立高度会缩短，正上方仍能看到角色的横截面直径。
        float visibleHeight = Mathf.Max(entity.Height * Mathf.Abs(worldCamera.transform.up.y), entity.Radius * 2f);
        scale = Mathf.Min(1f, visibleHeight * pixelsPerMeter / (_canvas.scaleFactor * FullSizeBodyHeight));
        return true;
    }

    /// <summary>提供护盾下缘需要让出的两行文字高度。</summary>
    /// <param name="role">当前护盾所属的场景角色。</param>
    /// <returns>相对头顶锚点向上的画布逻辑像素。</returns>
    public float GetShieldOffset(RoleEntity role)
    {
        if (string.IsNullOrWhiteSpace(role.BaseInfo?.Nickname))
        {
            return 0f;
        }
        return _labelTemplate.rectTransform.rect.height + GetTitleOffset(ResolveTitle(role.BaseInfo.JobId)) +
            ShieldGap;
    }

    /// <summary>沿用角色资料的职业映射，未知职业不显示称谓。</summary>
    /// <param name="jobId">角色快照中的职业编号。</param>
    /// <returns>职业名，缺少配置时为空。</returns>
    private static string ResolveTitle(int jobId)
    {
        if (jobId <= 0 || jobId >= JobTitles.Count)
        {
            return string.Empty;
        }
        return JobTitles[jobId];
    }

    /// <summary>计算称谓下方占用的高度及两行间距。</summary>
    /// <param name="title">实际要显示的职业称谓，空文本不留空行。</param>
    /// <returns>昵称底边相对称谓底边的画布逻辑像素。</returns>
    private float GetTitleOffset(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return 0f;
        }
        return _titleTemplate.rectTransform.rect.height + TitleGap;
    }

    /// <summary>取一组可复用文本，刷新昵称与称谓的位置及内容。</summary>
    /// <param name="index">本帧可见名牌在缓存列表中的下标。</param>
    /// <param name="nickname">服务器角色快照中的原始昵称。</param>
    /// <param name="title">由当前职业取得的显示称谓。</param>
    /// <param name="anchor">最下方文字相对名牌层中心的画布坐标。</param>
    /// <param name="scale">当前角色的屏幕缩放比例，两行文字及其间距一起缩放。</param>
    private void ShowName(int index, string nickname, string title, Vector2 anchor, float scale)
    {
        if (index == _labels.Count)
        {
            _labels.Add((Instantiate(_labelTemplate, _root, false), Instantiate(_titleTemplate, _root, false)));
        }
        // 缓存组中的昵称在上、称谓在下；空称谓时昵称直接使用头顶锚点。
        var labels = _labels[index];
        ShowLine(labels.Name, nickname, anchor + Vector2.up * (GetTitleOffset(title) * scale), scale);
        ShowLine(labels.Title, title, anchor, scale);
    }

    /// <summary>更新一行文本，有内容变化时才重建 TMP 文本。</summary>
    /// <param name="label">当前缓存的昵称或称谓标签。</param>
    /// <param name="value">本帧的文本，空值隐藏。</param>
    /// <param name="anchor">这一行底边的画布坐标。</param>
    /// <param name="scale">相对模板的等比缩放，按本帧角色投影更新。</param>
    private static void ShowLine(TMP_Text label, string value, Vector2 anchor, float scale)
    {
        if (string.IsNullOrEmpty(value))
        {
            label.gameObject.SetActive(false);
            return;
        }
        if (label.text != value)
        {
            label.SetText(value);
        }
        label.rectTransform.localScale = Vector3.one * scale;
        label.rectTransform.anchoredPosition = anchor;
        if (!label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(true);
        }
    }

    /// <summary>收起离场或出屏角色留下的缓存标签。</summary>
    /// <param name="count">本帧继续显示的标签数量。</param>
    private void HideUnused(int count)
    {
        for (int index = count; index < _labels.Count; index++)
        {
            _labels[index].Name.gameObject.SetActive(false);
            _labels[index].Title.gameObject.SetActive(false);
        }
    }

    /// <summary>主界面隐藏时立即清收名牌，恢复时由场景世界重新提供数据。</summary>
    private void OnDisable()
    {
        _entities.Clear();
        HideUnused(0);
    }
}
