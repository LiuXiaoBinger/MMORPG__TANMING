// 窗口状态与生命周期。正式游戏不挂此编辑器类型；完整轨道能力保留，当前宿主导出仍按原白名单检查。
using AtkJudge;
using System;
using Ethan.ActionEditor;
using Ethan.ActionEditor.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// ACT 技能动作编辑器 — Action + TimeLine + Event/Command 架构
/// 
/// 布局：顶部工具栏 | 左侧轨道标签(Track Header)+右侧时间轴(TimeLine) | 底部属性检查器(Inspector)
/// 
/// - Action: 一个 SkillConfigSO 对应一个行为
/// - TimeLine: 由多条 Track 组成，每条 Track 承载一类 Event
/// - Event/Command: 轨道上的具体指令（攻击、特效、音效、连招窗口等）
/// </summary>
public partial class SkillEditorWindow : EditorWindow
{
    #region 常量与颜色
    /// <summary>顶部三行工具栏占用的像素高度。</summary>
    const float TOOLBAR_HEIGHT = 82f;
    /// <summary>左侧轨道标题区宽度，单位像素。</summary>
    const float TRACK_HEADER_WIDTH = 224f;
    /// <summary>普通事件每条显示行的像素高度。</summary>
    const float TRACK_HEIGHT = 30f;
    /// <summary>旧曲线绘图行高度，单位像素。</summary>
    const float TRACK_HEIGHT_CURVE = 55f;
    /// <summary>时间轴标尺行的像素高度。</summary>
    const float RULER_HEIGHT = 22f;
    /// <summary>窄布局属性面板最低像素高度。</summary>
    const float INSPECTOR_MIN_HEIGHT = 180f;
    /// <summary>时间轴与属性面板分隔条厚度，单位像素。</summary>
    const float SPLITTER_HEIGHT = 5f;
    /// <summary>当前帧竖线宽度，单位像素。</summary>
    const float PLAYHEAD_WIDTH = 2f;
    /// <summary>时间轴缩放下限，每帧四像素。</summary>
    const float MIN_PPF = 4f;
    /// <summary>时间轴缩放上限，每帧四十像素。</summary>
    const float MAX_PPF = 40f;
    /// <summary>窗口首次显示时每帧默认占十二像素。</summary>
    const float DEF_PPF = 12f;
    /// <summary>轨道添加入口的像素高度。</summary>
    const float ADD_BTN_HEIGHT = 22f;
    /// <summary>空配置显示至少六十帧，作为制作视口而非技能状态时长。</summary>
    const int MIN_TOTAL_FRAMES = 60;
    /// <summary>窗口宽度达到此像素值后使用左右布局。</summary>
    const float WIDE_LAYOUT_THRESHOLD = 1120f;
    /// <summary>宽布局属性面板最小像素宽度。</summary>
    const float INSPECTOR_MIN_WIDTH = 320f;
    /// <summary>宽布局属性面板最大像素宽度。</summary>
    const float INSPECTOR_MAX_WIDTH = 520f;

    /// <summary>动画的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_ANIM  = new Color(.30f,.60f,.90f,.85f);
    /// <summary>攻击判定的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_ATK   = new Color(.90f,.30f,.25f,.85f);
    /// <summary>粒子的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_FX    = new Color(.30f,.85f,.45f,.85f);
    /// <summary>声音的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_SND   = new Color(.25f,.70f,.85f,.85f);
    /// <summary>衔接的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_JUMP  = new Color(.95f,.75f,.20f,.85f);
    /// <summary>位移的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_MOVE  = new Color(.70f,.50f,.90f,.85f);
    /// <summary>预警的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_WARN  = new Color(1f,.50f,0f,.85f);
    /// <summary>取消的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_CANCEL= new Color(.90f,.45f,.70f,.85f);
    /// <summary>飞行物的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_PROJ  = new Color(.20f,.80f,.95f,.85f);
    /// <summary>命中表现的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_HITFX = new Color(.95f,.55f,.85f,.85f);
    /// <summary>霸体的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_SA    = new Color(1f,.65f,.15f,.85f);  // SuperArmor 霸体轨道 — 橙金色
    /// <summary>朝向的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_AM    = new Color(.45f,.95f,.65f,.85f); // AdjustMotion 朝向调整 — 绿色
    /// <summary>刀光的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_TRAIL = new Color(.60f,.85f,1f,.85f);   // Trail 刀光轨道 — 冰蓝色
    /// <summary>时间轴背景的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_BG    = new Color(.18f,.18f,.18f);
    /// <summary>奇数轨道背景的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_TRK   = new Color(.22f,.22f,.22f);
    /// <summary>偶数轨道背景的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_TRK2  = new Color(.25f,.25f,.25f);
    /// <summary>次刻度网格的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_GRID  = new Color(1,1,1,.05f);
    /// <summary>主刻度网格的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_GRIDM = new Color(1,1,1,.12f);
    /// <summary>播放头的绘制颜色，透明度仅影响编辑显示。</summary>
    static readonly Color C_HEAD  = new Color(1f,.35f,.2f,1f);
    #endregion

    #region 编辑数据
    /// <summary>当前编辑的原作者资产；Undo 与保存均使用其真实身份。</summary>
    public SkillConfigSO configFile;
    /// <summary>作者配置的技能标识，正式归属另由宿主绑定和原表验证。</summary>
    int skillID=>configFile!=null?configFile.skillID:0;
    /// <summary>作者配置名称，仅用于编辑标签。</summary>
    public string skillName=>configFile!=null?configFile.skillName:string.Empty;
    /// <summary>作者配置说明文字，不参与伤害或施法状态。</summary>
    public string skillDescription=>configFile!=null?configFile.skillDescription:string.Empty;
    /// <summary>原技能类别整数索引，显示翻译不改变其数值。</summary>
    public int skillTypeSelectIndex=>configFile!=null?configFile.skillType:0;
    /// <summary>技能类别展示选项，顺序保持原技能类型索引，按当前语言即时读取。</summary>
    string[] skillTypeArray => new[] {SkillEditorLocalization.Text("Basic Attack", "普通攻击"),SkillEditorLocalization.Text("Active Ability", "主动技能"),SkillEditorLocalization.Text("Passive Ability", "被动技能")};
    /// <summary>包内通用冷却字段，当前游戏结算继续采用原表及服务器。</summary>
    public float skillCD=>configFile!=null?configFile.skillCD:0f;
    /// <summary>包内通用启动消耗，正式游戏费用由原技能表决定。</summary>
    public float energyCostOnActivate=>configFile!=null?configFile.energyCostOnActivate:0f;
    /// <summary>通用命中能量收益数据，实际发放由宿主负责。</summary>
    public float defaultEnergyGainOnHit=>configFile!=null?configFile.defaultEnergyGainOnHit:0f;
    /// <summary>通用角色类别，用于创作默认目标和敌方属性显示。</summary>
    public Global.SkillOwnerType ownerType=>configFile!=null?configFile.ownerType:Global.SkillOwnerType.Player;
    /// <summary>原取消优先级枚举，不能代替本游戏 RO 施法权限配置。</summary>
    public Global.CancelPriority cancelPriority=>configFile!=null?configFile.cancelPriority:Global.CancelPriority.Lv0;
    /// <summary>通用整段霸体开关，正式战斗状态需宿主接收器解释。</summary>
    public bool isSuperArmorSkill=>configFile!=null&&configFile.isSuperArmorSkill;
    /// <summary>打断优先级显示选项，整数索引保持原有等级含义。</summary>
    string[] cancelPriorityArray => new[] {SkillEditorLocalization.Text("Lv0", 
        "等级0"),SkillEditorLocalization.Text("Lv1", "等级1"),
        SkillEditorLocalization.Text("Lv2", "等级2"),
        SkillEditorLocalization.Text("Lv3", "等级3"),
        SkillEditorLocalization.Text("Lv4", "等级4"),
        SkillEditorLocalization.Text("Lv5", "等级5"),SkillEditorLocalization.Text("Lv6", "等级6"),SkillEditorLocalization.Text("Lv7", "等级7"),SkillEditorLocalization.Text("Lv8", "等级8"),SkillEditorLocalization.Text("Lv9", "等级9"),SkillEditorLocalization.Text("Uninterruptible", "不可打断")};
    /// <summary>命中强度展示选项，语言切换不改变强度枚举值。</summary>
    string[] impactLevelArray => new[] {SkillEditorLocalization.Text("Light", "轻"),SkillEditorLocalization.Text("Medium", "中"),SkillEditorLocalization.Text("Heavy", "重")};
    /// <summary>敌方攻击类别显示选项，顺序保持原普通与霸体攻击身份。</summary>
    string[] enemyAttackTypeArray => new[] {SkillEditorLocalization.Text("Normal Attack", "普通攻击"),SkillEditorLocalization.Text("Armored Attack", "霸体攻击")};
    /// <summary>伤害检测方式显示选项，单次和持续检测索引不变。</summary>
    string[] damageModeArray => new[] {SkillEditorLocalization.Text("Single Hit", "单次命中"),SkillEditorLocalization.Text("Repeated Hits", "重复命中")};
    /// <summary>碰撞形状显示选项，原球体与盒体索引不变。</summary>
    string[] skillShapeArray => new[] {SkillEditorLocalization.Text("Sphere", "球体"),SkillEditorLocalization.Text("Box", "盒体")};
    /// <summary>新建判定默认形状索引，零为球、一为盒。</summary>
    public int skillShapeSelectIndex;
    /// <summary>range1：默认球半径或盒体长度，单位模型米。；range2：默认盒体宽度，单位模型米。；offsetX：默认判定中心相对角色根的 X 偏移，单位米。；offsetY：默认判定中心相对角色根的 Y 偏移，单位米。；offsetZ：默认判定中心相对角色根的 Z 偏移，单位米。</summary>
    public float range1,range2,offsetX,offsetY,offsetZ;
    /// <summary>原技能衔接窗口列表，Jump 不等于物理跳跃。</summary>
    List<Global.Jump> jumpList=new List<Global.Jump>();
    /// <summary>原攻击判定列表，编辑器只制作数据，不结算伤害。</summary>
    List<Global.Attack> atkList=new List<Global.Attack>();
    /// <summary>原粒子与声音共享事件列表，实例引用保持原配置。</summary>
    List<Global.FxAndSound> fxAndSoundList=new List<Global.FxAndSound>();
    /// <summary>原视觉/音频预警列表，共享效果行仍保留独立数据。</summary>
    List<Global.WarningCue> warningCueList=new List<Global.WarningCue>();
    /// <summary>原取消窗口列表，实际权限由接入方读取。</summary>
    List<Global.CancelPoint> cancelList=new List<Global.CancelPoint>();
    /// <summary>原飞行物创作数据列表，预览使用隔离副本。</summary>
    List<Global.Projectile> projectileList=new List<Global.Projectile>();
    /// <summary>命中确认后的表现配置，时间轴不自动触发真实命中。</summary>
    List<Global.HitFx> hitFxList=new List<Global.HitFx>();
    /// <summary>分段霸体创作窗口，真实战斗状态需宿主实现。</summary>
    List<Global.SuperArmorSegment> superArmorList=new List<Global.SuperArmorSegment>();
    /// <summary>转向或锁定朝向的帧窗数据，不替代游戏角色控制器。</summary>
    List<Global.AdjustMotionSegment> adjustMotionList=new List<Global.AdjustMotionSegment>();
    /// <summary>刀光启停事件原列表，运行清理由接收方实现。</summary>
    List<Global.TrailToggle> trailToggleList=new List<Global.TrailToggle>();
    /// <summary>曲线或根运动位移段原列表，编辑器参考图不驱动正式移动。</summary>
    List<Global.MoveSegment> moveSegmentList=new List<Global.MoveSegment>();
    /// <summary>镜头提示原列表，Scene 预览不驱动游戏摄像机。</summary>
    List<ActionCameraCue> cameraCues=new List<ActionCameraCue>();
    /// <summary>按原条件判断的通用交互窗口，宿主提交具体交互效果。</summary>
    List<InteractionWindow> interactionWindows=new List<InteractionWindow>();

    // ── 多段动画 ──
    List<Global.AnimClipSegment> animSegments = new List<Global.AnimClipSegment>();
    // ── 动态轨道 ──
    List<Global.SkillTrack> trackList = new List<Global.SkillTrack>();
    #endregion

    #region 预览
    /// <summary>previewModel：用户选定的预览源对象，原对象与预制体不得被采样修改。；animSourceModel：提供 AnimationClip 子资源的来源模型，不等于正式角色动画绑定。</summary>
    public GameObject previewModel, animSourceModel;
    /// <summary>旧预览路径的 Animator 缓存，随模型切换更新。</summary>
    Animator previewAnim;
    /// <summary>_previewInstanceRoot：编辑器隔离角色包装根，不保存到正式场景。；_previewInstance：动画实际采样的隔离模型，停止预览时销毁。</summary>
    GameObject _previewInstanceRoot,_previewInstance;
    // ── 多FX预览实例支持 ──
    /// <summary>隔离预览粒子的实例状态，不保存到技能配置或正式场景。</summary>
    struct PreviewFxEntry
    {
        /// <summary>当前粒子克隆体，清理后不再驱动。</summary>
        public GameObject go;
        /// <summary>生成后经过的时间轴秒数，用于显式自定义清理延迟，不受粒子倍率影响。</summary>
        public float duration;
        /// <summary>有限粒子包含延迟/子发射器的最晚结束秒数；自定义/提示为参考清理秒并另加原半秒缓冲。</summary>
        public float maxLifetime;
        /// <summary>编辑粒子播放倍率，一表示实时。</summary>
        public float speed;
        /// <summary>无自定义寿命的普通技能粒子使用有限单轮及真实尾粒存活判定。</summary>
        public bool finitePlayback;
        /// <summary>当前克隆体根/子粒子快照，避免逐帧重复查询。</summary>
        public ParticleSystem[] particles;
    }
    /// <summary>此窗口拥有的粒子副本状态，关闭或循环时统一清理。</summary>
    List<PreviewFxEntry> _previewFxInstances=new List<PreviewFxEntry>();
    /// <summary>本輪已生成的原效果引用集合，用于避免同事件重复触发。</summary>
    HashSet<Global.FxAndSound> _spawnedFxIndices=new HashSet<Global.FxAndSound>();
    // ── 投掷物预览实例支持 ──
    struct PreviewProjEntry { 
    /// <summary>此预览条目的隔离对象实例，过期或关闭时销毁。</summary>
    public GameObject go; 
    /// <summary>飞行物预览世界速度向量，单位米每秒。</summary>
    public Vector3 velocity; 
    /// <summary>飞行物副本累计预览秒数。</summary>
    public float elapsed; 
    /// <summary>飞行物副本最大预览秒数，保留原清理缓冲。</summary>
    public float lifetime;
    /// <summary>飞行物副本粒子快照，随时间轴同步，暂停时不由Unity自然推进。</summary>
    public ParticleSystem[] particles; }
    /// <summary>飞行物预览副本状态，不参加正式物理或战斗结算。</summary>
    List<PreviewProjEntry> _previewProjInstances=new List<PreviewProjEntry>();
    /// <summary>动画来源资产中的实际剪辑快照，用于新增动画菜单。</summary>
    AnimationClip[] currentClips;
    /// <summary>窗口独占隐藏对象的二维声音源，仅用于编辑器声音播放，不指向正式场景组件。</summary>
    public AudioSource as1;
    #endregion

    #region Timeline状态
    /// <summary>横向缩放倍率，单位为每帧像素。</summary>
    float pixelsPerFrame=DEF_PPF;
    /// <summary>时间轴视口滚动位置，单位窗口像素。</summary>
    Vector2 timelineScrollPos;
    /// <summary>frameSelectIndex：当前播放头帧号，零起始，以当前时间轴 FPS 换算秒。；last_frameSelectIndex：上次采样帧号，负一表示新预览可触发零帧事件。</summary>
    int frameSelectIndex,last_frameSelectIndex;
    /// <summary>是否自动推进编辑预览时钟，不影响正式游戏状态。</summary>
    bool playFrame; 
    /// <summary>playTimer：预览尚未消耗的真实秒数，用于固定帧推进。；frameRate：预览每帧秒数，是 FPS 的倒数。</summary>
    float playTimer,frameRate=.033f;
    /// <summary>上次 Editor 更新的启动后秒数，用于计算真实时间差。</summary>
    double m_LastEditorTime;
    /// <summary>窄布局属性面板高度，单位窗口像素。</summary>
    float inspectorHeight=250f;
    /// <summary>宽布局属性面板宽度，单位窗口像素。</summary>
    float inspectorWidth=400f;
    /// <summary>isDraggingSplitter：上下分隔条拖拽是否正在进行。；
    /// isDraggingVerticalSplitter：左右分隔条拖拽是否正在进行。；
    /// isDraggingPlayhead：播放头拖拽状态，不建立配置 Undo。；
    /// isDraggingItem：事件帧段正在拖拽，由同一手势完成 Undo 提交。</summary>
    bool isDraggingSplitter,isDraggingVerticalSplitter,isDraggingPlayhead,isDraggingItem;
    /// <summary>本次事件拖拽开始时的选择类别。</summary>
    SelType dragType=SelType.None; 
    /// <summary>dragIdx：被拖拽事件在原列表的零起始下标。；dragOrigF：拖拽开始时的事件触发帧，供范围平移计算。；dragOrigEnd：拖拽开始时的结束帧，供维持原区间跨度。</summary>
    int dragIdx=-1,dragOrigF,dragOrigEnd;
    // Segment 拖拽
    bool isDraggingSegment; 
    /// <summary>本次拖拽动画段的原列表下标。</summary>
    int dragSegIdx=-1;
    /// <summary>动画源剪辑裁剪边缘拖拽状态。</summary>
    bool isDraggingSegEdge; 
    /// <summary>segEdgeIdx：当前被裁剪动画段的原列表下标。；segEdgeSide：裁剪侧标识，零为起始边，另一侧为结束边。</summary>
    int segEdgeIdx=-1, segEdgeSide;
    /// <summary>segOrigClipStart：拖拽前的源剪辑起始帧，单位源剪辑帧。；segOrigClipEnd：拖拽前的源剪辑结束帧，单位源剪辑帧。；segOrigTimelineStart：拖拽前的段时间轴起始帧，单位配置帧。</summary>
    int segOrigClipStart,segOrigClipEnd,segOrigTimelineStart;
    /// <summary>时间轴和属性面板共用的原选择身份，数值不写入技能资源。</summary>
    enum SelType{/* 无选择。 */ None,
        /* 攻击判定事件。 */ Attack,
        /* 粒子事件。 */ Fx,
        /* 声音事件。 */ Sound,
        /* 技能衔接窗口。 */ Jump,
        /* 预警事件。 */ Warning,
        /* 取消窗口。 */ Cancel,
        /* 技能基础属性。 */ Basic,
        /* 新判定默认形状。 */ Hitbox,
        /* 位移段。 */ Move,
        /* 敌方作者属性。 */ Enemy,
        /* 动画段。 */ AnimSegment,
        /* 飞行物。 */ Projectile,
        /* 受击表现。 */ HitFx,
        /* 分段霸体。 */ SuperArmor,
        /* 朝向调整。 */ AdjustMotion,
        /* 刀光开关。 */ TrailToggle,
        /* 第二阶段追加判定。 */ Phase2Attack,
        /* 第二阶段追加效果。 */ Phase2Fx,
        /* 交互窗口。 */ Interaction,
        /* 镜头提示。 */ Camera,
        /* 原流程节点。 */ Flow,
        /* 原退出帧。 */ FlowEnd,
        /* RO模型变色事件选择，不写入资产。 */ ChangeColor,
        /* RO技能进度条事件选择，不写入资产。 */ SkillBar,
        /* RO召唤单位事件选择，不写入资产。 */ MobUnit,
        /* ROAI事件事件选择，不写入资产。 */ AIEvent,
        /* ROBuff事件事件选择，不写入资产。 */ Buff,
        /* RORO震屏事件选择，不写入资产。 */ Shake,
        /* RO镜头后处理事件选择，不写入资产。 */ CameraPostEffect,
        /* RO镜头特效事件选择，不写入资产。 */ CameraFx,
        /* RO旋转事件选择，不写入资产。 */ Rotate
        
    }
    /// <summary>当前选择类别，只属于窗口，不写入资产。</summary>
    SelType selType=SelType.None; 
    /// <summary>当前事件在对应原列表的下标，负一表示未选中。</summary>
    int selIdx=-1;
    /// <summary>属性面板滚动缓存，切换配置时归零。</summary>
    Vector2 inspScrollPos;

    /// <summary>当前原资产的 SerializedObject 边界缓存，字段名不随语言变化。</summary>
    SerializedObject _serializedConfig;
    /// <summary>原配置验证结果，窗口只翻译显示而不改错误代码。</summary>
    readonly List<ActionValidationIssue> _validationIssues=new List<ActionValidationIssue>();
    /// <summary>_validationErrorCount：阻止预览的配置错误数量。；_validationWarningCount：不阻止预览的原配置警告数量。</summary>
    int _validationErrorCount,_validationWarningCount;
    #endregion


    #region SceneGUI
    /// <summary>isDrawAtk：旧判定可视化是否显示的窗口状态。；isRewriteAtk：当前判定是否采用事件覆盖形状。</summary>
    bool isDrawAtk,isRewriteAtk;
    /// <summary>当前待绘制的强类型判定形状接口。</summary>
    IJudgeArea _IJudgeArea; 
    /// <summary>当前 Scene 判定绘制数据包装，非真实碰撞对象。</summary>
    Judgment _judgment=new Judgment();
    /// <summary>编辑器盒体边界手柄，不创建运行碰撞体。</summary>
    BoxBoundsHandle boxHandle=new BoxBoundsHandle();
    /// <summary>编辑器球体边界手柄，不执行物理查询。</summary>
    SphereBoundsHandle sphereHandle=new SphereBoundsHandle();
    /// <summary>盒体绘制所需的尺寸与局部偏移缓存。</summary>
    BoxItem boxItem=new BoxItem(); 
    /// <summary>球体绘制所需的半径与局部偏移缓存。</summary>
    SphereItem sphereItem=new SphereItem();
    // ── 自动跟踪骨骼生成判定框 ──
    Transform _trackBoneRef;   // 选中的骨骼参考点
    /// <summary>相对预览角色根的骨骼路径缓存。</summary>
    string _trackBonePath="";  // 骨骼路径缓存
    /// <summary>属性面板骨骼偏移数据显示折叠状态。</summary>
    bool _showBoneOffsetsPreview; // Inspector中偏移数据折叠
    #endregion

    [MenuItem("Tools/ACT Action Editor/Open Editor")]
    /// <summary>通过保留的固定菜单身份打开技能窗口，显示标题使用个人语言。</summary>
    static void Open()
    {
        // 已打开窗口直接复用，不创建第二份技能数据或预览角色业务实体。
        SkillEditorWindow window = GetWindow<SkillEditorWindow>();
        window.minSize = new Vector2(900, 600);
        window.UpdateLocalizedTitle();
        window.Show();
    }

    /// <summary>窗口启用时绑定编辑器回调并恢复个人语言，仅原生命周期负责预览初始化。</summary>
    void OnEnable(){
        SkillEditorLocalization.Changed += OnEditorLanguageChanged;
        EditorApplication.projectChanged+=ClearSoundWaveforms;
        OnEditorLanguageChanged();
        EnsurePreviewAudioSource();
        SceneView.duringSceneGui+=OnSceneGUICallback;
        m_LastEditorTime=EditorApplication.timeSinceStartup;
        EditorApplication.update+=OnEditorUpdate;
        EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        Undo.undoRedoPerformed+=OnUnityUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload+=StopPreviewSession;
        if(!Application.isPlaying&&!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        EnsureSceneLabelStyles();
        // ★ 域重载后从 SO 重新加载数据，防止空列表覆盖有效配置
        if(configFile!=null) LoadConfig();
    }
    /// <summary>窗口关闭时解除语言通知和原预览回调，避免静态事件持有关闭窗口。</summary>
    void OnDisable(){
        SkillEditorLocalization.Changed -= OnEditorLanguageChanged;
        EditorApplication.projectChanged-=ClearSoundWaveforms;
        ClearSoundWaveforms();
        _editGesture.Complete();
        CommitDeferredChanges();
        SceneView.duringSceneGui-=OnSceneGUICallback;
        EditorApplication.update-=OnEditorUpdate;
        EditorApplication.playModeStateChanged-=OnPlayModeChanged;
        Undo.undoRedoPerformed-=OnUnityUndoRedo;
        AssemblyReloadEvents.beforeAssemblyReload-=StopPreviewSession;
        CleanPreviewFx(); DestroyPreviewInstance(); DestroyPreviewAudioSource();
        if(AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
    }
    /// <summary>更新标题和本地轨道显示缓存，不加载配置、提交编辑、产生Undo或重置预览时钟。</summary>
    void OnEditorLanguageChanged()
    {
        UpdateLocalizedTitle();
        _trackDefs = null;
        _totalFramesCache = -1;
        Repaint();
        SceneView.RepaintAll();
    }

    /// <summary>同步窗口可见标题，兼容宿主GetWindow传入固定英文标题的原入口。</summary>
    void UpdateLocalizedTitle()
    {
        // 比较当前显示标题，只有改变时替换GUI内容，不重建任何预览对象。
        string localizedTitle = SkillEditorLocalization.Text("ACT Action Editor", 
            "ACT 技能编辑器");
        if (titleContent.text != localizedTitle) titleContent = new GUIContent(localizedTitle);
    }

    /// <param name="state">Unity 进入或退出编辑/运行模式的生命周期通知。</param>
    /// <summary>进入运行模式前停止隔离预览，返回编辑模式后重载配置避免空缓存写回。</summary>
    void OnPlayModeChanged(PlayModeStateChange state){
        switch(state){
            case PlayModeStateChange.ExitingEditMode:
                // 即将进入 PlayMode：停止预览，避免与运行时动画冲突
                StopPreviewSession();
                CleanOrphanedPreviewFx(); // 兜底清理域重载后丢失引用的残留 FX
                if(AnimationMode.InAnimationMode()) 
                    AnimationMode.StopAnimationMode();
                break;
            case PlayModeStateChange.EnteredEditMode:
                // 回到编辑模式：清理可能在 Play 模式中残留的预览 FX
                CleanOrphanedPreviewFx();
                if(!AnimationMode.InAnimationMode()) 
                    AnimationMode.StartAnimationMode();
                // ★ Play Mode 域重载后重新加载数据，防止空列表覆盖
                if(configFile!=null) LoadConfig();
                break;
        }
    }
    /// <summary>编辑模式且原配置没有错误时才允许生成隔离预览。</summary>
    bool IsPreviewAllowed=>!Application.isPlaying&&_validationErrorCount==0;

    /// <summary>重新加载 Undo 后的原配置并恢复有效选择帧，清除未完成拖拽状态。</summary>
    void OnUnityUndoRedo(){
        // Undo 重载前的原选择类别，用于重载后恢复导航。
        var selectedType=selType;
        // Undo 重载前原列表选择下标，恢复时重新做边界裁剪。
        int selectedIndex=selIdx;
        // Undo 重载前播放头帧号，重载后限制到新范围。
        int selectedFrame=frameSelectIndex;
        isDraggingItem=isDraggingSegment=isDraggingSegEdge=false;
        if(configFile!=null) LoadConfig();
        selType=selectedType;
        // 当前轨道完整绘制及编辑回调定义。
        var def=GetDefBySel(selType);
        selIdx=def?.listCount!=null?Mathf.Clamp(selectedIndex,-1,def.listCount()-1):selectedIndex;
        frameSelectIndex=Mathf.Clamp(selectedFrame,0,GetTotalFrames());
        RefreshValidation();
        Repaint();
        SceneView.RepaintAll();
    }

    #region 主GUI
    /// <summary>返回工具栏下方的窗口内容矩形，高度限制为非负值，单位窗口像素。</summary>
    Rect GetContentRect() => new Rect(0, TOOLBAR_HEIGHT, position.width, Mathf.Max(0, position.height - TOOLBAR_HEIGHT));
    /// <summary>按当前语言绘制窗口；原技能编辑、时间轴交互与预览采样顺序保持。</summary>
    void OnGUI(){
        UpdateLocalizedTitle();
        _totalFramesCache=-1; // 每次重绘重算一次总帧数，帧内复用
        DrawToolbar();
        if(configFile==null){ using(new GUILayout.AreaScope(GetContentRect())){
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(SkillEditorLocalization.
                Text("Create a new action or select an existing asset to begin.", 
                    "新建技能或选择已有技能资源开始编辑。"),MessageType.None);
            GUILayout.FlexibleSpace(); } return; }
        if(ActionConfigMigrationService.NeedsMigration(configFile)){
            using(new GUILayout.AreaScope(GetContentRect())){
            EditorGUILayout.HelpBox(SkillEditorLocalization.
                Text($"{configFile.name} uses a legacy schema and is read-only. " +
                     $"Choose Tools > Upgrade schema with backup to edit it.", 
                    $"{configFile.name} 使用旧结构，当前只读。选择工具 > 备份并升级结构后编辑。"),MessageType.None);
            EditorGUILayout.ObjectField(SkillEditorLocalization.
                Text("Action asset", "技能资源"),configFile,
                typeof(SkillConfigSO),false);
            DrawReadOnlyConfig();
            }
            return;
        }
        PrepareUndoForCurrentEvent();
        // tb：工具栏像素高度，用于确定内容区上边界。；total：窗口总高度扣除工具栏后的内容高度，单位窗口像素。
        float tb=TOOLBAR_HEIGHT, total=position.height-tb;
        if(position.width>=WIDE_LAYOUT_THRESHOLD){
            // 宽布局属性面板的裁剪后像素宽度。
            float insW=Mathf.Clamp(inspectorWidth,INSPECTOR_MIN_WIDTH,INSPECTOR_MAX_WIDTH);
            // 扣除属性面板与分隔条后的时间轴像素宽度。
            float tlW=position.width-insW-SPLITTER_HEIGHT;
            DrawTimelineArea(new Rect(0,tb,tlW,total));
            DrawVerticalSplitter(new Rect(tlW,tb,SPLITTER_HEIGHT,total));
            DrawInspectorPanel(new Rect(tlW+SPLITTER_HEIGHT,tb,insW,total));
        }else{
            // 窄布局属性面板的裁剪后像素高度。
            float insH=Mathf.Clamp(inspectorHeight,INSPECTOR_MIN_HEIGHT,total*.6f);
            // 扣除属性面板与分隔条后的时间轴像素高度。
            float tlH=total-insH-SPLITTER_HEIGHT;
            DrawTimelineArea(new Rect(0,tb,position.width,tlH));
            DrawSplitter(new Rect(0,tb+tlH,position.width,SPLITTER_HEIGHT));
            DrawInspectorPanel(new Rect(0,tb+tlH+SPLITTER_HEIGHT,position.width,insH));
        }
        HandleGlobalInput();
        // 动画采样只在 Repaint 执行一次，避免 Layout/Repaint 双事件重复采样
        if(Event.current.type==EventType.Repaint) SampleAnim();
    }
    #endregion














    #region 辅助
    /// <summary>
    /// 计算拖入的 Transform 相对于角色根节点的路径（用于 firePointPath 自动填充）。
    /// 优先查找预览模型的层级；如果拖入的物体不是预览模型的子物体，则尝试向上遍历
    /// 到 Animator 根节点来构建路径。
    /// </summary>
    /// <param name="target">待计算挂点相对路径的原目标变换。</param>
    string ComputeRelativePath(Transform target){
        if(target==null) return null;
        // 优先检查是否属于预览模型的层级
        var eff=GetEffectivePreviewModel();
        // 实际隔离预览模型的变换根，作为相对挂点路径起点。
        Transform root=eff!=null?eff.transform:null;
        if(root!=null&&target.IsChildOf(root)){
            return BuildPathFromRoot(root,target);
        }
        // 不属于预览模型 → 向上找 Animator 根节点（适用于直接从场景拖入的情况）
        Transform walk=target;
        while(walk.parent!=null){
            if(walk.parent.GetComponent<Animator>()!=null){
                return BuildPathFromRoot(walk.parent,target);
            }
            walk=walk.parent;
        }
        // 最后尝试从场景根到 target 的完整父级链（root 是最顶层非 target 的父级）
        if(target.parent!=null){
            // 找到最顶层有 Animator 的节点，或者退回到 target 的直接父级
            Transform animRoot=target;
            while(animRoot.parent!=null) animRoot=animRoot.parent;
            if(animRoot!=target) return BuildPathFromRoot(animRoot,target);
        }
        return null;
    }
    /// <param name="root">相对路径的指定根，追溯到此节点后停止。</param>
    /// <param name="target">待追溯到指定根的目标节点，不修改其父级。</param>
    /// <summary>沿父级链构造相对骨骼路径，目标不属于指定根时返回空值。</summary>
    string BuildPathFromRoot(Transform root,Transform target){
        if(target==root) return string.Empty;
        // 从目标向根收集的节点名称列表，反转后形成相对路径。
        var parts=new List<string>();
        // 从目标向指定根逐级追溯的父节点游标，不修改层级。
        Transform cur=target;
        while(cur!=null&&cur!=root){ parts.Add(cur.name); cur=cur.parent; }
        if(cur!=root) return null; // target 不是 root 的子物体
        parts.Reverse();
        return string.Join("/",parts);
    }
    /// <summary>销毁当前窗口记录的粒子和飞行物副本，清空本轮已触发事件集合。</summary>
    void CleanPreviewFx(){ 
        // 反向销毁窗口拥有的粒子副本，不操作原预制体。
        for(int index=_previewFxInstances.Count-1;index>=0;index--)
            if(_previewFxInstances[index].go!=null) DestroyImmediate(_previewFxInstances[index].go);
        _previewFxInstances.Clear(); _spawnedFxIndices.Clear(); CleanPreviewProj();
    }
    /// <summary>
    /// 兜底清理：查找场景中所有带 EditorPreviewFxTag 的残留预览 FX 并销毁。
    /// 用于域重载后 _previewFxInstances 列表被清空但 DontSave 对象仍残留的情况。
    /// </summary>
    void CleanOrphanedPreviewFx(){
        // 域重载后残留预览标记组件快照，销毁不会遍历正在变化的场景列表。
        var orphans=Resources.FindObjectsOfTypeAll<EditorPreviewFxTag>();
        // 隔离预览标记引用，用于只清理编辑副本。
        foreach(var tag in orphans){
            if(tag!=null&&tag.gameObject!=null) DestroyImmediate(tag.gameObject);
        }
    }
    /// <summary>从动画来源资源枚举真实 AnimationClip 子资产，不创建或替换原剪辑。</summary>
    void LoadClipsFromAsset(){
        if(animSourceModel==null) return; 
        // 动画来源资源的实际 AssetDatabase 相对路径。
        var ap=AssetDatabase.GetAssetPath(animSourceModel);
        // 该资产的 Unity 子资源快照，只筛选实际剪辑。
        var objs=AssetDatabase.LoadAllAssetsAtPath(ap); 
        // 筛选得到的原 AnimationClip 引用列表。
        var cl=new List<AnimationClip>();
        // 当前正在检查重叠的其他原事件引用。
        foreach(var o in objs) if(o is AnimationClip /* 真实动画剪辑子资源引用，不修改源资源。 */ c) cl.Add(c); currentClips=cl.ToArray();
    }
    /// <param name="prevFrame">上一次采样的时间轴帧，负一允许触发零帧事件。</param>
    /// <param name="curFrame">本次采样帧，与上帧组成前开后闭触发区间。</param>
    /// <summary>播放本次跨帧经过的全部效果事件，按原对象引用去重，允许同帧多特效。</summary>
    void PlayFxAndSound(int prevFrame,int curFrame){
        // 遍历所有 FX，(prevFrame, curFrame] 范围内的全部触发（支持同帧多个 FX）
        for(int i=0;i<fxAndSoundList.Count;i++){
            // 原共享粒子/音频列表中的事件引用。
            var f=fxAndSoundList[i];
            if(f==null) continue;
            if(f.particleSystem==null&&f.audioClip==null) continue;
            // 事件区间起始帧，零起始。
            int begin=f.keyNumber;
            // 当前事件区间的有效末尾帧，用于范围限制。
            int end=f.endKeyNumber>f.keyNumber?f.endKeyNumber:f.keyNumber;
            // 检查 (prevFrame, curFrame] 是否与 [begin, end] 有交集
            if(curFrame<begin||prevFrame>=end) continue;
            // 已触发过的不重复创建
            if(_spawnedFxIndices.Contains(f)) continue;
            _spawnedFxIndices.Add(f);
            if(f.particleSystem!=null) SpawnPreviewFx(f);
            // 音效由SynchronizePreviewAudio按绝对帧独立定位，暂停和倒拖不会遗留PlayOneShot。
        }
    }

    /// <summary>在编辑器预览中按角色挂点生成粒子副本，并交给 FxUpdate 增量模拟。</summary>
    /// <param name="t">当前时间轴触发的粒子事件，偏移与旋转相对所选挂点。</param>
    void SpawnPreviewFx(Global.FxAndSound t){
        // 使用预览副本的挂点；无效路径必须报错跳过，避免错误地显示在角色根节点。
        GameObject actor=null;
        if(previewModel!=null){
            EnsurePreviewInstance();
            actor=GetEffectivePreviewModel();
        }
        if(t.useWorldSpace && !string.IsNullOrWhiteSpace(t.socketPath)){
            Debug.LogError(SkillEditorLocalization.Text("World-space effects cannot also use a socket: ", "技能编辑器世界坐标特效不能同时设置挂点：")+t.socketPath);
            return;
        }
        // 根据宿主位置模式计算世界姿态，Scene 标记与真正生成使用同一实现。
        Transform anchor;
        // 解算后的世界位置，单位米。
        Vector3 position;
        // 按事件定位规则解析的粒子世界四元数，用于隔离克隆。
        Quaternion rotation;
        if(!TryResolvePreviewFxPose(actor,t,true,out anchor,out position,out rotation)) return;
        // 粒子引用对应的原预制体，只读用于创建副本。
        var srcGo=t.particleSystem.gameObject;
        // 本次预览创建的粒子克隆，不保存到正式场景。
        var fxGo=Instantiate(srcGo);
        if(fxGo==null) return;
        fxGo.hideFlags=HideFlags.DontSave|HideFlags.HideInHierarchy;
        // 添加标识组件，用于在域重载后查找并清理残留的预览 FX
        fxGo.AddComponent<EditorPreviewFxTag>().hideFlags=HideFlags.DontSave|HideFlags.HideInInspector;

        // ── 设置位置和旋转 ──
        fxGo.transform.position=position;
        fxGo.transform.rotation=rotation;
        if(anchor!=null && t.followCharacter) fxGo.transform.parent=anchor;

        // ── 设置缩放 ──
        fxGo.transform.localScale=t.scale;

        // ── 播放速率 & 计算最大存活时间 ──
        float spd=Mathf.Max(t.playbackSpeed,0.01f);
        // 此粒子克隆预估最长存活秒数。
        float maxLife=0f;
        // 粒子数组包含根粒子和所有子粒子，最长尾迹决定预览实例的清理时间。
        ParticleSystem[] particles=InitializePreviewParticles(fxGo);
        // 当前单轮粒子寿命查询的路径集合，防止子发射器循环引用递归不止。
        HashSet<ParticleSystem> visited=new HashSet<ParticleSystem>();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<particles.Length;i++){
            // 当前克隆体的原粒子组件引用，仅副本被模拟。
            ParticleSystem p=particles[i];
            // 普通技能粒子与宿主运行保持单轮，只有克隆体被修改，原预制体不变。
            if(t.customLifetime<=0f){
                // 当前克隆粒子的主模块，只在副本上关闭循环。
                ParticleSystem.MainModule main=p.main;
                main.loop=false;
            }
            // 只有活跃层级参与最长寿命，不激活或等待原失活节点。
            if(!p.gameObject.activeInHierarchy) continue;
            // 延迟、发射和尾粒含系统自身倍率；事件倍率在这里统一换算一次。
            float dur=GetPreviewParticleDuration(p,visited)/spd;
            if(dur>maxLife) maxLife=dur;
            p.Simulate(0,false,true,false);
            p.Pause(false);
        }
        if(maxLife<=0f) maxLife=5f;
        if(t.customLifetime>0f) maxLife=t.customLifetime;
        _previewFxInstances.Add(new PreviewFxEntry{go=fxGo,duration=0f,maxLifetime=maxLife,speed=spd,finitePlayback=t.customLifetime<=0f,particles=particles});
    }

    /// <summary>在编辑器预览中生成派生提示特效：复用 PreviewFxEntry 系统驱动 Simulate</summary>
    /// <param name="j">技能衔接窗口中的提示粒子配置。</param>
    void SpawnPreviewDerivePrompt(Global.Jump j){
        if(j.derivePromptVfx==null) return;
        // 粒子引用对应的原预制体，只读用于创建副本。
        var srcGo=j.derivePromptVfx.gameObject;
        // 本次预览创建的粒子克隆，不保存到正式场景。
        var fxGo=Instantiate(srcGo);
        if(fxGo==null) return;
        fxGo.hideFlags=HideFlags.DontSave|HideFlags.HideInHierarchy;
        fxGo.AddComponent<EditorPreviewFxTag>().hideFlags=HideFlags.DontSave|HideFlags.HideInInspector;

        // ── 设置位置 ──
        if(previewModel!=null){
            EnsurePreviewInstance(); 
            // 动画实际采样的隔离模型副本。
            var eff=GetEffectivePreviewModel();
            if(eff!=null){
                fxGo.transform.position=eff.transform.TransformPoint(j.derivePromptOffset);
                fxGo.transform.rotation=eff.transform.rotation;
                if(j.derivePromptFollow) fxGo.transform.parent=eff.transform;
            }
        }

        // ── 设置缩放 ──
        if(j.derivePromptScale!=Vector3.one&&j.derivePromptScale!=Vector3.zero)
            fxGo.transform.localScale=j.derivePromptScale;

        // ── 计算最大存活时间 ──
        float maxLife=0f;
        // 派生提示的粒子快照，与普通技能特效共用暂停初始化。
        ParticleSystem[] particles=InitializePreviewParticles(fxGo);
        // 提示粒子的零起始组件下标。
        for(int index=0;index<particles.Length;index++){
            // 仅预览副本的当前粒子组件。
            ParticleSystem p=particles[index];
            // 当前粒子持续时间加最长尾粒寿命，单位秒。
            float dur=p.main.duration+p.main.startLifetime.constantMax;
            if(dur>maxLife) maxLife=dur;
            p.Simulate(0,false,true,false);
        }
        if(maxLife<=0f) maxLife=5f;
        _previewFxInstances.Add(new PreviewFxEntry{go=fxGo,duration=0f,maxLifetime=maxLife,speed=1f,particles=particles});
    }

    /// <param name="f">当前零起始时间轴帧，用于采样或区间检测。</param>
    /// <summary>按当前帧寻找首个覆盖该帧的攻击区间，未命中任何区间返回负一。</summary>
    int FindKeyIndex(int f){ 
    // 当前可索引集合的零起始遍历下标，访问原对象引用。
    for(int i=0;i<atkList.Count;i++){ 
    // 原攻击判定列表中的事件引用。
    var a=atkList[i]; 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=a.endKeyNumber>a.keyNumber?a.endKeyNumber:a.keyNumber; if(f>=a.keyNumber&&f<=end) return i; } return -1; }
    /// <summary>收集当前帧所有活跃（在开始帧~结束帧范围内）的攻击索引</summary>
    /// <param name="f">当前零起始时间轴帧，用于采样或区间检测。</param>
    List<int> FindActiveAttacks(int f){
        // 当前帧覆盖的原攻击事件下标列表，每次查询重新创建。
        var result=new List<int>();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<atkList.Count;i++){
            // 原攻击判定列表中的事件引用。
            var a=atkList[i]; 
            // 当前事件区间的有效末尾帧，用于范围限制。
            int end=a.endKeyNumber>a.keyNumber?a.endKeyNumber:a.keyNumber;
            if(f>=a.keyNumber&&f<=end) result.Add(i);
        }
        return result;
    }
    /// <param name="i">当前可索引集合的零起始遍历下标，访问原对象引用。</param>
    /// <summary>检查攻击列表的零起始索引，避免选择失效时访问越界。</summary>
    bool CheckKey(int i)=>i>=0&&i<atkList.Count;
    /// <param name="i">当前可索引集合的零起始遍历下标，访问原对象引用。</param>
    /// <summary>确认索引有效且此攻击覆盖默认判定形状，供 Scene 判定绘制选择数据。</summary>
    bool CheckKeyRewrite(int i)=>CheckKey(i)&&atkList[i].isReWrite;
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <param name="c">选中边框的显示颜色。</param>
    /// <param name="t">选中边框像素厚度，默认一像素。</param>
    /// <summary>在给定像素矩形四边绘制选中边框，厚度使用窗口像素。</summary>
    void DrawOutline(Rect r,Color c,float t=1){
        EditorGUI.DrawRect(new Rect(r.x,r.y,r.width,t),c);
        EditorGUI.DrawRect(new Rect(r.x,r.yMax-t,r.width,t),c);
        EditorGUI.DrawRect(new Rect(r.x,r.y,t,r.height),c);
        EditorGUI.DrawRect(new Rect(r.xMax-t,r.y,t,r.height),c);
    }
    #endregion
}

#region 样式
/// <summary>窗口通用显示样式缓存，按需创建且不序列化到技能资产。</summary>
static class TSty{
    /// <summary>_tl：轨道标题样式懒加载缓存，只用于绘制，不保存进技能配置。；_rl：帧标尺样式懒加载缓存，只用于绘制，不保存进技能配置。；_cl：动画剪辑块样式懒加载缓存，只用于绘制，不保存进技能配置。；_il：事件块样式懒加载缓存，只用于绘制，不保存进技能配置。；_ba：播放按钮样式懒加载缓存，只用于绘制，不保存进技能配置。；_ab：添加按钮样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    static GUIStyle _tl,_rl,_cl,_il,_ba,_ab;
    /// <summary>轨道标题样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    public static GUIStyle TrkLbl{ get{ if(_tl==null){ _tl=new GUIStyle(EditorStyles.label); _tl.fontSize=11; _tl.normal.textColor=new Color(.85f,.85f,.85f); _tl.fontStyle=FontStyle.Bold; } return _tl; } }
    /// <summary>帧标尺样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    public static GUIStyle Ruler{ get{ if(_rl==null){ _rl=new GUIStyle(EditorStyles.miniLabel); _rl.fontSize=9; _rl.normal.textColor=new Color(.7f,.7f,.7f); } return _rl; } }
    /// <summary>动画剪辑块样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    public static GUIStyle Clip{ get{ if(_cl==null){ _cl=new GUIStyle(EditorStyles.miniLabel); _cl.fontSize=10; _cl.normal.textColor=Color.white; _cl.fontStyle=FontStyle.Bold; _cl.alignment=TextAnchor.MiddleLeft; _cl.clipping=TextClipping.Clip; } return _cl; } }
    /// <summary>事件块样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    public static GUIStyle Item{ get{ if(_il==null){ _il=new GUIStyle(EditorStyles.miniLabel); _il.fontSize=9; _il.normal.textColor=new Color(1,1,1,.9f); _il.clipping=TextClipping.Clip; } return _il; } }
    /// <summary>播放按钮样式懒加载缓存，只用于绘制，不保存进技能配置。</summary>
    public static GUIStyle BtnAct{ get{ if(_ba==null){ _ba=new GUIStyle(EditorStyles.miniButton); _ba.normal.textColor=new Color(1f,.35f,.2f); _ba.fontStyle=FontStyle.Bold; } return _ba; } }
    /// <summary>轨道头部 [+] 添加 Event 按钮样式 — 白色粗体，居中</summary>
    public static GUIStyle AddBtn{ get{ if(_ab==null){ _ab=new GUIStyle(EditorStyles.miniButton); _ab.fontSize=13; _ab.fontStyle=FontStyle.Bold; _ab.normal.textColor=new Color(1f,1f,1f,.95f); _ab.alignment=TextAnchor.MiddleCenter; _ab.padding=new RectOffset(0,0,0,2); } return _ab; } }
}
#endregion

#region 辅助绘图类
namespace AtkJudge{
    /// <summary>仅供 Scene 判定参考的强类型形状赋值接口，不进行战斗碰撞结算。</summary>
    public interface IJudgeArea{ 
    /// <param name="s1">球半径或盒体 X 尺寸，单位模型米。</param>
    /// <param name="s2">盒体 Y 尺寸，球形绘制不使用此分量。</param>
    /// <param name="s3">盒体 Z 尺寸，球形绘制不使用此分量。</param>
    /// <param name="ox">形状中心相对角色根的 X 米数。</param>
    /// <param name="oy">形状中心相对角色根的 Y 米数。</param>
    /// <param name="oz">形状中心相对角色根的 Z 米数。</param>
    
    /// <summary>把判定尺寸和局部偏移写入绘制数据；形状不使用的尺寸参数保留原接口。</summary>
    void SetValue(float s1,float s2,float s3,float ox,float oy,float oz); }
    /// <summary>保存盒体 Scene 绘制尺寸及偏移的临时形状数据。</summary>
    public class BoxItem:IJudgeArea{ 
    /// <summary>offset：形状中心相对角色根的三轴偏移，单位米。；size：盒体三轴尺寸，单位模型米。</summary>
    public Vector3 offset,size;
        /// <param name="s1">球半径或盒体 X 尺寸，单位模型米。</param>
        /// <param name="s2">盒体 Y 尺寸，球形绘制不使用此分量。</param>
        /// <param name="s3">盒体 Z 尺寸，球形绘制不使用此分量。</param>
        /// <param name="ox">形状中心相对角色根的 X 米数。</param>
        /// <param name="oy">形状中心相对角色根的 Y 米数。</param>
        /// <param name="oz">形状中心相对角色根的 Z 米数。</param>
        /// <summary>把判定尺寸和局部偏移写入绘制数据；形状不使用的尺寸参数保留原接口。</summary>
        public void SetValue(float s1,float s2,float s3,float ox,float oy,float oz){ size=new Vector3(s1,s2,s3); offset=new Vector3(ox,oy,oz); } }
    /// <summary>保存球体 Scene 绘制半径及偏移的临时形状数据。</summary>
    public class SphereItem:IJudgeArea{ 
    /// <summary>形状中心相对角色根的三轴偏移，单位米。</summary>
    public Vector3 offset; 
    /// <summary>球形判定半径，单位模型米。</summary>
    public float radius;
        /// <param name="s1">球半径或盒体 X 尺寸，单位模型米。</param>
        /// <param name="s2">盒体 Y 尺寸，球形绘制不使用此分量。</param>
        /// <param name="s3">盒体 Z 尺寸，球形绘制不使用此分量。</param>
        /// <param name="ox">形状中心相对角色根的 X 米数。</param>
        /// <param name="oy">形状中心相对角色根的 Y 米数。</param>
        /// <param name="oz">形状中心相对角色根的 Z 米数。</param>
        /// <summary>把判定尺寸和局部偏移写入绘制数据；形状不使用的尺寸参数保留原接口。</summary>
        public void SetValue(float s1,float s2,float s3,float ox,float oy,float oz){ radius=s1; offset=new Vector3(ox,oy,oz); } }
}
/// <summary>持有当前 Scene 判定绘制形状，不属于运行角色组件。</summary>
public class Judgment{ 
/// <summary>当前绘制形状的强类型接口，不承担真实战斗判定。</summary>
public AtkJudge.IJudgeArea value; }
/// <summary>把通用形状绘制转接到 Unity Scene Handles 的编辑器后端。</summary>
public class HandlesDrawTool:DrawTool{
    /// <summary>Scene Handles 绘制后端的共享实例。</summary>
    public static HandlesDrawTool H=new HandlesDrawTool();
    /// <summary>当前绘图使用的显示颜色，仅影响编辑器可视化。</summary>
    public override Color color{ get=>Handles.color; set=>Handles.color=value; }
    /// <param name="s">线段起点的世界米数。</param>
    /// <param name="e">线段终点的世界米数。</param>
    /// <summary>绘制两个世界坐标点之间的 Scene 线段，具体后端由 Handles 实现。</summary>
    public override void DrawLine(Vector3 s,Vector3 e)=>Handles.DrawLine(s,e);
    /// <param name="v">已经转换到绘制空间的顶点数组。</param>
    /// <summary>用 Scene Handles 填充凸多边形，顶点须已转换到当前绘制坐标。</summary>
    protected override void FillPolygon(Vector3[] v)=>Handles.DrawAAConvexPolygon(v);
}
/// <summary>提供线框、填充及颜色恢复的通用形状绘制基类。</summary>
public abstract class DrawTool{
    /// <summary>局部绘制颜色栈为空时使用的默认白色。</summary>
    public static Color colorDefault=Color.white;
    /// <summary>当前绘图使用的显示颜色，仅影响编辑器可视化。</summary>
    public virtual Color color{get;set;} 
    /// <summary>是否填充判定表面，不影响碰撞计算。</summary>
    public bool isFill=false;
    /// <summary>绘图前保存颜色的后进先出栈，绘图结束按顺序恢复。</summary>
    Stack<Color> _stack=new Stack<Color>();
    /// <param name="s">线段起点的世界米数。</param>
    /// <param name="e">线段终点的世界米数。</param>
    /// <summary>绘制两个世界坐标点之间的 Scene 线段，具体后端由 Handles 实现。</summary>
    public abstract void DrawLine(Vector3 s,Vector3 e);
    /// <param name="v">已经转换到绘制空间的顶点数组。</param>
    /// <summary>用 Scene Handles 填充凸多边形，顶点须已转换到当前绘制坐标。</summary>
    protected abstract void FillPolygon(Vector3[] v);
    /// <param name="c">本次局部绘图使用的新颜色。</param>
    /// <summary>保存当前绘制颜色后切换新颜色，结束局部绘图后由 PopColor 还原。</summary>
    public void PushColor(Color c){_stack.Push(color);color=c;}
    /// <summary>恢复绘制颜色栈顶；栈空时回到默认白色，防止后续 Scene 绘图继承错误颜色。</summary>
    public void PopColor(){color=_stack.Count>0?_stack.Pop():colorDefault;}
    /// <param name="size">待绘制盒体的三轴完整尺寸，单位局部米。</param>
    /// <param name="m">当前原位移段引用。</param>
    /// <summary>按盒体六个表面绘制边线，填充开关仅影响 Scene 可视化。</summary>
    public void DrawBox(Vector3 size,Matrix4x4 m){
        // 当前盒体变换后的八个绘图顶点。
        Vector3[] p=MathTool.CalcBoxVertex(size,m); 
        // 当前原事件列表的零起始下标。
        int[] idx=MathTool.GetBoxSurfaceIndex();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<6;i++){ 
        // 当前盒面按固定绕序取出的四个世界顶点。
        Vector3[] poly={p[idx[i*4]],p[idx[i*4+1]],p[idx[i*4+2]],p[idx[i*4+3]]};
            if(isFill) FillPolygon(poly); 
            // 盒体当前表面的零起始顶点边线下标，范围零到三。
            for(int k=0;k<4;k++) DrawLine(poly[k],poly[(k+1)%4]); } }
    /// <param name="r">参考球体半径，单位模型米。</param>
    /// <param name="m">当前原位移段引用。</param>
    /// <summary>在三个互相垂直的矩阵平面绘制圆线，形成球形判定参考。</summary>
    public void DrawSphere(float r,Matrix4x4 m){ DrawCircle(r,m); DrawCircle(r,m*Matrix4x4.Rotate(Quaternion.Euler(90,0,0)));
        DrawCircle(r,m*Matrix4x4.Rotate(Quaternion.Euler(0,90,0))); }
    /// <param name="r">参考圆半径，单位模型米。</param>
    /// <param name="m">当前原位移段引用。</param>
    /// <summary>在指定矩阵平面绘制三十边近似圆，半径以模型单位表示。</summary>
    void DrawCircle(float r,Matrix4x4 m){ 
    // 圆线近似的固定三十段数量。
    int sides=30; 
    // 已经转换到绘制空间的顶点数组。
    Vector3[] v=new Vector3[sides];
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<sides;i++){ 
        // 当前圆周采样角，单位弧度。
        float rad=(i*Mathf.PI*2)/sides; v[i]=m.MultiplyPoint(new Vector3(Mathf.Cos(rad)*r,Mathf.Sin(rad)*r,0)); }
        if(isFill) FillPolygon(v); 
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<sides;i++) DrawLine(v[i],v[(i+1)%sides]); }
}
/// <summary>构造判定绘图顶点和表面绕序，不执行真实物理运算。</summary>
public static class MathTool{
    /// <summary>返回八顶点盒体六个面的固定绕序索引，供线框和填充绘制复用。</summary>
    public static int[] GetBoxSurfaceIndex()=>new int[]{0,1,2,3, 4,5,6,7, 2,6,5,3, 0,4,7,1, 1,7,6,2, 0,3,5,4};
    /// <param name="s">盒体完整三轴尺寸，单位模型米。</param>
    /// <param name="m">当前原位移段引用。</param>
    /// <summary>把局部盒体半尺寸生成八个顶点，再用目标矩阵变换到绘制坐标。</summary>
    public static Vector3[] CalcBoxVertex(Vector3 s,Matrix4x4 m){
        // 盒体三轴半尺寸，单位模型米。
        Vector3 h=s/2; 
        // 盒体八个顶点，矩阵转换前为局部米数。
        Vector3[] p={new Vector3(h.x,h.y,h.z),new Vector3(h.x,h.y,-h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,h.y,h.z),
            new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z)};
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<8;i++) p[i]=m.MultiplyPoint(p[i]); return p; }
}
/// <summary>保留原事件列表的选中及普通样式身份。</summary>
public static class GUIStyles{ 
/// <summary>item_selected：旧事件列表选中态样式，不保存到配置。；item_normal：旧事件列表普通态样式，不保存到配置。</summary>
public static GUIStyle item_selected="MeTransitionSelectHead",item_normal="MeTransitionSelect"; }
#endregion

/// <summary>
/// 空标识组件 — 挂在编辑器预览 FX 实例上，用于在域重载后查找并清理残留对象。
/// 不在 Inspector 中显示，不会保存到场景。
/// </summary>
[AddComponentMenu("")]  // 不出现在 Add Component 菜单
public class EditorPreviewFxTag : MonoBehaviour { }
