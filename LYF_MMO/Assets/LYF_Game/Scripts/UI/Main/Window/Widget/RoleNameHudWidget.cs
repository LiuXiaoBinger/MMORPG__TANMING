using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>在主界面画布上跟随已加载的玩家角色，显示昵称和职业称谓。</summary>
[DefaultExecutionOrder(100)]
public sealed class RoleNameHudWidget : MonoBehaviour
{
    /// <summary>从头骨向上投影的世界间距，单位为米，与既有护盾提示的锚点一致。</summary>
    private const float HeadGap = 0.28f;

    /// <summary>昵称上缘与护盾下缘之间的距离，单位为画布逻辑像素。</summary>
    private const float ShieldGap = 4f;

    /// <summary>称谓与昵称之间的距离，单位为画布逻辑像素。</summary>
    private const float TitleGap = 1f;

    /// <summary>存放字体、描边和对齐样式的常驻 TMP 模板，模板本身始终隐藏。</summary>
    [SerializeField] private TMP_Text _labelTemplate;

    /// <summary>单独保存称谓字号、颜色和描边的隐藏模板。</summary>
    [SerializeField] private TMP_Text _titleTemplate;

    /// <summary>每帧复用的场景实体快照，内容由统一场景世界提供。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();

    /// <summary>按可见角色顺序复用的两行文本，最大数量取决于同时可见的玩家数量。</summary>
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

        // 屏幕覆盖画布转换坐标时使用空相机。
        Camera uiCamera = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = _canvas.worldCamera;
        }

        world.CopyEntities(_entities);
        // 已占用的标签数，同时是下一张可复用标签的下标。
        int visibleCount = 0;
        for (int index = 0; index < _entities.Count; index++)
        {
            // 怪物与 NPC 沿用各自的显示入口，只处理有玩家身份数据的角色。
            RoleEntity role = _entities[index] as RoleEntity;
            // 昵称来自角色快照，角色退出或复用时不保留上次文本。
            string nickname = role?.BaseInfo?.Nickname;
            if (string.IsNullOrWhiteSpace(nickname) || !role.Model.IsLoaded || !role.Model.Visible)
            {
                continue;
            }

            // 优先使用经过动画更新的人形头骨，其他模型使用实体身高。
            Vector3 head = role.Position + Vector3.up * role.Height;
            Animator animator = role.Model.Ator?.UnityAnimator;
            if (animator != null && animator.isHuman)
            {
                // 头骨位置已经是世界坐标。
                Transform bone = animator.GetBoneTransform(HumanBodyBones.Head);
                if (bone != null)
                {
                    head = bone.position;
                }
            }

            // 屏幕坐标为像素，z 为到摄像机的前向距离。
            Vector3 screen = worldCamera.WorldToScreenPoint(head + Vector3.up * HeadGap);
            if (screen.z <= worldCamera.nearClipPlane || !worldCamera.pixelRect.Contains(screen))
            {
                continue;
            }

            // 画布锚点为最下方文字的底边，单位为画布逻辑像素。
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, uiCamera,
                out Vector2 anchor))
            {
                continue;
            }
            ShowName(visibleCount, nickname, ResolveTitle(role.BaseInfo.JobId), anchor);
            visibleCount++;
        }
        HideUnused(visibleCount);
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
        if (jobId <= 0 || jobId >= ConstDefine.RoleJobName.Count)
        {
            return string.Empty;
        }
        return ConstDefine.RoleJobName[jobId];
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
    private void ShowName(int index, string nickname, string title, Vector2 anchor)
    {
        if (index == _labels.Count)
        {
            _labels.Add((Instantiate(_labelTemplate, _root, false), Instantiate(_titleTemplate, _root, false)));
        }
        // 缓存组中的昵称在上、称谓在下；空称谓时昵称直接使用头顶锚点。
        var labels = _labels[index];
        ShowLine(labels.Name, nickname, anchor + Vector2.up * GetTitleOffset(title));
        ShowLine(labels.Title, title, anchor);
    }

    /// <summary>更新一行文本，有内容变化时才重建 TMP 文本。</summary>
    /// <param name="label">当前缓存的昵称或称谓标签。</param>
    /// <param name="value">本帧的文本，空值隐藏。</param>
    /// <param name="anchor">这一行底边的画布坐标。</param>
    private static void ShowLine(TMP_Text label, string value, Vector2 anchor)
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
