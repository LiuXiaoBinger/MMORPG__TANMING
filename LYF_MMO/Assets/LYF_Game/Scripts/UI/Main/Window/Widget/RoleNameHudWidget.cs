using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>在主界面画布上跟随已加载的玩家角色，显示其真实昵称。</summary>
[DefaultExecutionOrder(100)]
public sealed class RoleNameHudWidget : MonoBehaviour
{
    /// <summary>从头骨向上投影的世界间距，单位为米，与既有护盾提示的锚点一致。</summary>
    private const float HeadGap = 0.28f;

    /// <summary>护盾上缘与昵称之间的距离，单位为画布逻辑像素。</summary>
    private const float ShieldGap = 4f;

    /// <summary>本地玩家的昵称颜色，用暖白与其他玩家区分。</summary>
    private static readonly Color LocalNameColor = new Color32(255, 239, 194, 255);

    /// <summary>场景中其他玩家的昵称颜色。</summary>
    private static readonly Color OtherNameColor = new Color32(220, 242, 238, 255);

    /// <summary>存放字体、描边和对齐样式的常驻 TMP 模板，模板本身始终隐藏。</summary>
    [SerializeField] private TMP_Text _labelTemplate;

    /// <summary>本地角色护盾的显示区域，仅在护盾出现时给昵称让出它的高度。</summary>
    [SerializeField] private RectTransform _shieldContent;

    /// <summary>每帧复用的场景实体快照，内容由统一场景世界提供。</summary>
    private readonly List<MEntity> _entities = new List<MEntity>();

    /// <summary>按可见角色顺序复用的昵称文本，最大数量取决于同时可见的玩家数量。</summary>
    private readonly List<TMP_Text> _labels = new List<TMP_Text>();

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

            // 昵称底边在头顶锚点上方，护盾出现时移到护盾上缘。
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, uiCamera,
                out Vector2 anchor))
            {
                continue;
            }
            // 只有本地角色沿用现有头顶护盾区域。
            bool isLocal = role == world.LocalRole;
            if (isLocal && _shieldContent.gameObject.activeInHierarchy)
            {
                anchor.y += _shieldContent.rect.height + ShieldGap;
            }
            ShowName(visibleCount, nickname, anchor, isLocal);
            visibleCount++;
        }
        HideUnused(visibleCount);
    }

    /// <summary>取一个可复用文本，刷新内容和位置后显示。</summary>
    /// <param name="index">本帧可见名牌在缓存列表中的下标。</param>
    /// <param name="nickname">服务器角色快照中的原始昵称。</param>
    /// <param name="anchor">相对名牌层中心的画布坐标。</param>
    /// <param name="isLocal">当前昵称是否属于本地操控角色。</param>
    private void ShowName(int index, string nickname, Vector2 anchor, bool isLocal)
    {
        if (index == _labels.Count)
        {
            _labels.Add(Instantiate(_labelTemplate, _root, false));
        }
        // 按下标重复使用实例，仅在昵称或角色身份改变时刷新网格样式。
        TMP_Text label = _labels[index];
        if (label.text != nickname)
        {
            label.SetText(nickname);
        }
        // 文本颜色随当前占用的角色更新。
        Color color = OtherNameColor;
        if (isLocal)
        {
            color = LocalNameColor;
        }
        if (label.color != color)
        {
            label.color = color;
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
            if (_labels[index].gameObject.activeSelf)
            {
                _labels[index].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>主界面隐藏时立即清收名牌，恢复时由场景世界重新提供数据。</summary>
    private void OnDisable()
    {
        _entities.Clear();
        HideUnused(0);
    }
}
