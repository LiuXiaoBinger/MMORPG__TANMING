using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>副本伤害的 HUD 网格，沿用 RO 的普通攻击、玩家受伤和暴击数字表现。</summary>
public sealed class MBattleHudWordMgr : MaskableGraphic
{
    /// <summary>一次真实命中的绘制数据，数字在命中时确定，离场或经过寿命后释放。</summary>
    private struct Draw
    {
        /// <summary>受击实体在当前 SceneWorld 中的唯一编号，用来错开同时命中同一实体的数字。</summary>
        public ulong entityUid;
        /// <summary>命中时受击者头顶的世界坐标，单位米，后续随相机重新投影。</summary>
        public Vector3 originWorldPos;
        /// <summary>攻击者指向受击者的屏幕单位方向的水平分量。</summary>
        public float originDirX;
        /// <summary>服务端实际扣血点数转成的十进制数字。</summary>
        public string context;
        /// <summary>所有字形按原 Sprite 宽度和边框排布后的总宽，单位为画布参考像素。</summary>
        public float width;
        /// <summary>数字开始播放的游戏时间，单位秒。</summary>
        public float startTime;
    }

    /// <summary>RO HudWordMinInterval，同一受击者连续两次普通跳字的最小起播间隔，单位秒。</summary>
    private const float HudWordMinInterval = 0.068f;

    /// <summary>RO FontType.Normal 按字符 0 到 9 排列的十张 Sprite。</summary>
    [SerializeField] private Sprite[] fontSprites;
    /// <summary>玩家普通受伤使用的 Hurt_Hit 子网格，绑定 RO 红色渐变和原运动曲线。</summary>
    [SerializeField] private MBattleHudWordMgr playerHitWord;
    /// <summary>玩家暴击受伤使用的 Hurt_Crit 子网格，绑定 RO 黄色渐变和原运动曲线。</summary>
    [SerializeField] private MBattleHudWordMgr criticalHitWord;
    /// <summary>当前 RO 数字样式的显示总时长，单位秒。</summary>
    [SerializeField] private float _lifeTime;
    /// <summary>按攻击方向横向漂移的 RO 速度，乘以 100 后为画布参考像素每秒。</summary>
    [SerializeField] private float _speed;
    /// <summary>原数字缩放曲线的整体倍率。</summary>
    [SerializeField] private float _scaleSize;
    /// <summary>当前样式先上浮再回落的高度曲线，保留 RO 的关键帧和切线。</summary>
    [SerializeField] private AnimationCurve _yCurve;
    /// <summary>当前 RO 伤害数字由大缩小的缩放曲线。</summary>
    [SerializeField] private AnimationCurve _scaleCurve;
    /// <summary>当前 RO 数字的顶点颜色和透明度渐变，分别承载普通白字、受伤红字或暴击黄字。</summary>
    [SerializeField] private Gradient _colorGradient;
    /// <summary>本场景尚未播完的数字，按收到命中的顺序提交给同一画布网格。</summary>
    private readonly List<Draw> drawList = new List<Draw>();

    /// <summary>CanvasRenderer 使用已提取材质绑定的 Fonts.png 采样各数字的 UV。</summary>
    public override Texture mainTexture { get { return material.mainTexture; } }

    /// <summary>按受击者身份和服务端暴击标记选择 RO 数字表现，怪物仍使用原普通攻击网格。</summary>
    /// <param name="target">已经绑定模型的受击实体，致死一击也使用它的头顶位置。</param>
    /// <param name="firerPosition">本次命中时服务端记录的施害者世界坐标，单位米。</param>
    /// <param name="damage">服务端本次实际结算的正整数伤害，不在客户端重新计算。</param>
    /// <param name="critical">服务端暴击结果；仅用于选择玩家受伤的数字样式。</param>
    public void AddDraw(MEntity target, Vector3 firerPosition, int damage, bool critical = false)
    {
        if (damage <= 0) return;
        if (target is RoleEntity)
        {
            if (critical) criticalHitWord.AddDrawValue(target, firerPosition, damage);
            else playerHitWord.AddDrawValue(target, firerPosition, damage);
            return;
        }
        AddDrawValue(target, firerPosition, damage);
    }

    /// <summary>向已经选定样式的网格追加一次伤害，子网格不再次进行样式分流。</summary>
    /// <param name="target">命中时仍存在于实体世界的受击者。</param>
    /// <param name="firerPosition">施害者世界位置，单位米。</param>
    /// <param name="damage">本次需要显示的服务端伤害点数。</param>
    private void AddDrawValue(MEntity target, Vector3 firerPosition, int damage)
    {
        // 世界相机用于确定该观察者眼里的受击方向。
        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        // 原点与方向来自同一次受击；队友使用自己的相机投影。
        Vector3 origin = target.Position;
        Vector3 hurtDirection = origin - firerPosition;
        Vector2 screenDirection = worldCamera.WorldToScreenPoint(origin + hurtDirection) - worldCamera.WorldToScreenPoint(origin);
        // 数字与排版宽度在收到通知时确定，后续帧只计算投影和轨迹。
        string context = damage.ToString(CultureInfo.InvariantCulture);
        float width = 0f;
        for (int index = 0; index < context.Length; index++)
        {
            // 当前数字在 RO Normal 字形表中的原 Sprite。
            Sprite sprite = fontSprites[context[index] - '0'];
            width += sprite.rect.width - sprite.border.x - sprite.border.z;
        }

        // 同一帧可能收到多次结算，从该受击者上一条数字起播时刻向后错开。
        float startTime = Time.time;
        for (int index = drawList.Count - 1; index >= 0; index--)
        {
            if (drawList[index].entityUid != target.UID) continue;
            startTime = Mathf.Max(startTime, drawList[index].startTime + HudWordMinInterval);
            break;
        }

        drawList.Add(new Draw {
            entityUid = target.UID,
            originWorldPos = GetHeadPosition(target),
            originDirX = screenDirection.normalized.x,
            context = context,
            width = width,
            startTime = startTime
        });
        SetVerticesDirty();
    }

    /// <summary>在命中帧取模型可见蒙皮的最高点，悬浮怪的碰撞高度可能低于它的头顶。</summary>
    /// <param name="target">受击实体；角色使用实体身高，怪物使用原准备数据中的蒙皮高度。</param>
    private static Vector3 GetHeadPosition(MEntity target)
    {
        // 没有启用蒙皮的模型沿用实体高度，位置以世界米计。
        Vector3 head = target.Position + Vector3.up * target.Height;
        if (target is RoleEntity) return head;
        // 复用模型准备数据里的蒙皮列表，无需在命中时重新遍历节点或烘焙网格。
        SkinnedMeshRenderer[] skins = target.Model.GameObject.NativeObject.GetComponent<MModelPrepareData>().SkinnedMeshRenderers;
        for (int index = 0; index < skins.Length; index++)
        {
            // 只以当前启用的部件确定这次跳字的世界锚点。
            SkinnedMeshRenderer skin = skins[index];
            if (!skin.enabled || !skin.gameObject.activeInHierarchy) continue;
            Bounds bounds = skin.bounds;
            if (bounds.max.y <= head.y) continue;
            head = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }
        return head;
    }

    /// <summary>相机移动时也要重投影，最后一条过期时提交空网格。</summary>
    private void LateUpdate()
    {
        if (drawList.Count == 0) return;
        // 同一帧对所有数字采样相同的游戏时间，单位秒。
        float now = Time.time;
        for (int index = drawList.Count - 1; index >= 0; index--)
        {
            if (now - drawList[index].startTime >= _lifeTime) drawList.RemoveAt(index);
        }
        SetVerticesDirty();
    }

    /// <summary>将世界锚点投到已有 HUD 画布，再按 RO 时间、缩放和颜色曲线排布字形。</summary>
    /// <param name="vertices">Unity 画布提供的顶点缓冲，每次重建时只写本帧可见的数字。</param>
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        if (drawList.Count == 0) return;
        // 两台相机分别用于世界投影和画布局部坐标换算。
        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        Camera uiCamera = canvas.worldCamera;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) uiCamera = null;
        // RO MotionPath 的采样终点取高度曲线最后一帧与 1 的较小值。
        float curveEnd = Mathf.Min(1f, _yCurve[_yCurve.length - 1].time);
        // 将首帧字形的下沿对齐可见头顶，原曲线的相对升降幅度保持不变。
        float startHeight = _yCurve.Evaluate(0f);
        float halfHeight = fontSprites[0].rect.height * _scaleCurve.Evaluate(0f) * _scaleSize * 0.5f;
        for (int index = 0; index < drawList.Count; index++)
        {
            // 正在显示的单次命中及它目前投影到的屏幕像素坐标。
            Draw draw = drawList[index];
            // 距离这次跳字起播的游戏时间，排在同目标前一击后的数字到点再绘制。
            float elapsed = Time.time - draw.startTime;
            if (elapsed < 0f) continue;
            Vector3 screen = worldCamera.WorldToScreenPoint(draw.originWorldPos);
            if (screen.z <= 0f) continue;
            // 图层局部坐标以参考像素计，随分辨率和相机位置重新换算。
            Vector2 anchor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, uiCamera, out anchor)) continue;
            // 原模板的 X 曲线恒为零，水平偏移完全来自受击方向和速度。
            float weight = elapsed * curveEnd / _lifeTime;
            anchor.x += draw.originDirX * _speed * 100f * elapsed;
            anchor.y += halfHeight + (_yCurve.Evaluate(weight) - startHeight) * 100f;
            AddLetterDraw(vertices, draw, anchor, _scaleCurve.Evaluate(weight) * _scaleSize, _colorGradient.Evaluate(weight) * color);
        }
    }

    /// <summary>按 RO AddLetterDraw 的字宽和内部 UV 从左向右画出整次伤害，并整体居中。</summary>
    /// <param name="vertices">本次跳字共用的 UI 网格顶点缓冲。</param>
    /// <param name="draw">包含十进制数字和总宽度的命中记录。</param>
    /// <param name="anchor">当前轨迹采样后的画布局部坐标。</param>
    /// <param name="scale">当前曲线求出的字形倍率。</param>
    /// <param name="tint">当前渐变求出的顶点颜色和透明度。</param>
    private void AddLetterDraw(VertexHelper vertices, Draw draw, Vector2 anchor, float scale, Color32 tint)
    {
        // 游标位于整串数字的左边缘，单位为缩放前的画布参考像素。
        float cursor = -draw.width * 0.5f;
        for (int index = 0; index < draw.context.Length; index++)
        {
            // 字形矩形、边框、UV 均来自已经导入的同一张图集。
            Sprite sprite = fontSprites[draw.context[index] - '0'];
            Rect rect = sprite.rect;
            Vector4 uv = DataUtility.GetInnerUV(sprite);
            // 居中的四边形边界，单位为画布局部像素。
            float left = anchor.x + (cursor - sprite.border.x) * scale;
            float right = left + rect.width * scale;
            float bottom = anchor.y - rect.height * scale * 0.5f;
            float top = bottom + rect.height * scale;
            // 该字形的首个顶点索引，两组三角形沿用 Unity UI 的顶点顺序。
            int first = vertices.currentVertCount;
            vertices.AddVert(new Vector3(left, bottom), tint, new Vector2(uv.x, uv.y));
            vertices.AddVert(new Vector3(left, top), tint, new Vector2(uv.x, uv.w));
            vertices.AddVert(new Vector3(right, top), tint, new Vector2(uv.z, uv.w));
            vertices.AddVert(new Vector3(right, bottom), tint, new Vector2(uv.z, uv.y));
            vertices.AddTriangle(first, first + 1, first + 2);
            vertices.AddTriangle(first + 2, first + 3, first);
            cursor += rect.width - sprite.border.x - sprite.border.z;
        }
    }
}
