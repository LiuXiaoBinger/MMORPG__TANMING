// 完整轨道注册表、时间轴绘图和多段动画制作；轨道显隐不删除事件、不绕过游戏导出校验。
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

// The original script GUID stays on SkillEditorWindow.cs.
/// <summary>按帧制作技能的原 Y 窗口分部；完整编辑能力与本游戏导出支持范围独立，预览使用隔离副本。</summary>
public partial class SkillEditorWindow
{
    #region Timeline区域
    /// <summary>一次布局收集的轨道显示记录，保存原布局身份、事件行号和绘制尺寸，不序列化到资产。</summary>
    struct TrkInfo{ 
    /// <summary>此显示轨道使用的绘制及编辑回调定义。</summary>
    public TrackDef def; 
    /// <summary>轨道标题文字，仅用于编辑界面，翻译不得回写原序列化身份。</summary>
    public string name; 
    /// <summary>该显示轨道占用的窗口像素高度。</summary>
    public float height;
        /// <summary>原事件下标到非重叠显示行号的映射。</summary>
        public Dictionary<int,int> lanes; 
        /// <summary>当前轨道显示所需行数，至少为一。</summary>
        public int laneCount; 
        /// <summary>该显示轨道是否展开，不删除其原事件。</summary>
        public bool expanded; 
        /// <summary>原布局列表下标，负一表示仅恢复显示而未写入布局。</summary>
        public int trackListIdx; }

    // ═══════════════════════════════════════════════════════════
    //  轨道注册表 — 新增一种轨道只需在 BuildTrackDefs() 追加一条
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 轨道描述符：把「颜色/名称/添加菜单/右键菜单/绘制/属性面板/时间轴长度」全部收敛为数据。
    /// 原先这些信息散落在 9 处平行 switch 中，新增轨道需要改 9 个地方。
    /// </summary>
    class TrackDef{
        /// <summary>原轨道枚举身份，序列化整数保持不变。</summary>
        public Global.TrackType type;
        /// <summary>当前选择类别，只属于窗口，不写入资产。</summary>
        public SelType selType;
        /// <summary>当前绘图使用的显示颜色，仅影响编辑器可视化。</summary>
        public Color color;
        /// <summary>原始英文默认名，仅作为稳定内部身份，不能随个人语言写回配置。</summary>
        public string name;
        /// <summary>当前语言的轨道头文字，只供窗口绘制，不进入技能序列化。</summary>
        public string localizedName;
        /// <summary>单事件菜单显示名称，语言切换后重建。</summary>
        public string itemName;    // 单个 Event 的中文名，用于菜单文案
        /// <summary>创建轨道菜单显示名称，不作为原字段查找键。</summary>
        public string menuLabel;   // 「添加轨道」菜单文案
        /// <summary>该创建入口是否只适用于敌方作者配置。</summary>
        public bool enemyOnly;     // 仅敌人技能可添加

        /// <summary>统计轨道头显示数量的回调，空值表示不显示数量。</summary>
        public Func<int> count;    // 轨道头括号内计数（null=不显示）
        /// <summary>读取原事件列表长度的回调，用于验证选择索引。</summary>
        public Func<int> listCount;// 事件列表长度，用于选中索引边界校验
        /// <summary>读取此轨道末尾帧的回调，空值表示不贡献总长度。</summary>
        public Func<int> maxEnd;   // 对时间轴总长的贡献（null=不贡献）

        /// <summary>原事件下标到非重叠显示行号的映射。</summary>
        public Func<Dictionary<int,int>,Dictionary<int,int>> lanes; // lane 分配（null=非 lane 轨道）
        /// <summary>绘制可按行分配的原事件列表回调。</summary>
        public Action<Rect,Dictionary<int,int>,int> drawLanes;      // lane 轨道绘制
        /// <summary>不采用通用显示行的轨道绘制回调。</summary>
        public Action<Rect> drawCustom;                             // 非 lane 轨道绘制
        /// <summary>计算自定义轨道像素高度的回调。</summary>
        public Func<float> customHeight;                            // 非 lane 轨道高度

        /// <summary>播放头处添加此类原事件的菜单回调。</summary>
        public Action addEvent;        // [+] 按钮 / 菜单「添加」（null=不可添加）
        /// <summary>按选中原下标绘制对应属性面板的回调。</summary>
        public Action<int> inspector;  // 属性面板绘制（参数=selIdx）
        /// <summary>按原列表下标删除事件的回调，沿原 Undo 事务执行。</summary>
        public Action<int> removeAt;
        /// <summary>读取RO事件开始秒数；仅新RO轨道使用，帧号始终由当前配置FPS投影。</summary>
        public Func<int,float> roBegin;
        /// <summary>读取RO事件有效末尾秒数，含循环的最后触发时刻。</summary>
        public Func<int,float> roEnd;
        /// <summary>把原RO事件平移至新开始秒数，保留原秒跨度及区间长度。</summary>
        public Action<int,float> roMove;   // 删除第 idx 个事件

        /// <summary>lane 结果复用缓冲，避免每帧新建 Dictionary</summary>
        public readonly Dictionary<int,int> laneBuf=new Dictionary<int,int>();
    }

    /// <summary>包含完整创作功能的绘制/编辑回调注册表，换语言后重建。</summary>
    List<TrackDef> _trackDefs;
    /// <summary>懒加载完整轨道定义，语言变化后重新建立显示缓存。</summary>
    List<TrackDef> TrackDefs{ get{ if(_trackDefs==null) _trackDefs=BuildTrackDefs(); return _trackDefs; } }

    /// <param name="t">待查找的原轨道枚举身份。</param>
    /// <summary>按原轨道枚举查找绘制及编辑回调，未知轨道返回空值。</summary>
    TrackDef GetDef(Global.TrackType t){
        // 当前完整轨道注册表引用，配置加载后回调仍读取原列表。
        var defs=TrackDefs;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<defs.Count;i++) if(defs[i].type==t) return defs[i];
        return null;
    }
    /// <param name="s">待查找的原窗口选择类别。</param>
    /// <summary>按当前选择类别查找属性及删除回调，选择失效时返回空值。</summary>
    TrackDef GetDefBySel(SelType s){
        // 当前完整轨道注册表引用，配置加载后回调仍读取原列表。
        var defs=TrackDefs;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<defs.Count;i++) if(defs[i].selType==s) return defs[i];
        return null;
    }

    /// <summary>轨道注册表。所有 lambda 在调用时读取字段，因此 LoadConfig 重建列表后依然有效。</summary>
    /// <summary>建立本地显示注册表，类型和事件回调仍沿用原身份，语言变化时只重建此缓存。</summary>
    /// <returns>显示名称本地化而原英文身份保留的轨道定义列表。</returns>
    List<TrackDef> BuildTrackDefs(){
        // 帧段类事件的结束帧：endKeyNumber <= keyNumber 时视为单帧
        const int TAIL=5; // 事件末尾预留帧，保证时间轴可继续往后拖

        // 动画轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var animDef=new TrackDef{
            type=Global.TrackType.Animation, selType=SelType.AnimSegment, color=C_ANIM,
            name="Animation", localizedName=SkillEditorLocalization.Text("Animation", "动画"), itemName=SkillEditorLocalization.Text("Animation", "动画"), menuLabel=SkillEditorLocalization.Text("Animation", "动画"),
            count=()=>animSegments.Count, listCount=()=>animSegments.Count,
            maxEnd=()=>{ 
            // 当前最大有效末尾帧，空列表为零。
            int m=0; 
            // 原动画段列表中的引用，裁剪单位为源剪辑帧。
            foreach(var s in animSegments) if(ActionTiming.EndFrame(configFile,s)>m) m=ActionTiming.EndFrame(configFile,s); return m; },
            addEvent=AddAnimSegHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspAnimSeg(animSegments[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(animSegments,i) };
        animDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,animSegments,/* 当前动画段原引用，委托只读取帧范围或显示标签。 */ s=>s.startFrame,/* 当前动画段原引用，委托只读取帧范围或显示标签。 */ s=>ActionTiming.EndFrame(configFile,s)>s.startFrame?ActionTiming.EndFrame(configFile,s)-1:s.startFrame);
        animDef.drawLanes=DrawAnimTrackMultiSeg;

        // 判定轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var atkDef=new TrackDef{
            type=Global.TrackType.Attack, selType=SelType.Attack, color=C_ATK,
            name="Collision", localizedName=SkillEditorLocalization.Text("Collision", "碰撞判定"), itemName=SkillEditorLocalization.Text("Collision", "碰撞判定"), menuLabel=SkillEditorLocalization.Text("Collision", "碰撞判定"),
            count=()=>atkList.Count, listCount=()=>atkList.Count,
            maxEnd=()=>MaxEnd(atkList,/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>a.endKeyNumber)+TAIL,
            addEvent=ShowCollisionAddMenu, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspAtk(atkList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(atkList,i) };
        atkDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,atkList,/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>SegEnd(a.keyNumber,a.endKeyNumber));
        atkDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,atkList,atkDef,
            /* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a=>SegEnd(a.keyNumber,a.endKeyNumber),
            (/* 当前攻击判定原引用，委托只读取帧范围或显示标签。 */ a,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>CollisionTrackLabel(a,b,e),alwaysLabel:true);

        // 视觉效果轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var fxDef=new TrackDef{
            type=Global.TrackType.Fx, selType=SelType.Fx, color=C_FX,
            name="Effect", localizedName=SkillEditorLocalization.Text("Effect", "特效"), itemName=SkillEditorLocalization.Text("Effect", "特效"), menuLabel=SkillEditorLocalization.Text("Effect", "特效"),
            count=()=>fxAndSoundList.Count, listCount=()=>fxAndSoundList.Count,
            maxEnd=()=>MaxEnd(fxAndSoundList,/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>f.keyNumber,/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>f.endKeyNumber)+TAIL,
            addEvent=ShowEffectAddMenu, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspFx(fxAndSoundList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(fxAndSoundList,i) };
        fxDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,fxAndSoundList,/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>f.keyNumber,/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>SegEnd(f.keyNumber,f.endKeyNumber));
        fxDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,fxAndSoundList,fxDef,
            /* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>f.keyNumber,/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f=>SegEnd(f.keyNumber,f.endKeyNumber),
            (/* 当前粒子/音频事件原引用，委托只读取帧范围或显示标签。 */ f,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>EffectTrackLabel(f),filter:BuiltinTrackRegistry.IsVisibleInFxTrack,alwaysLabel:true);
        fxDef.count=()=>FxCount()+warningCueList.Count;
        fxDef.lanes=null; fxDef.drawCustom=DrawUnifiedEffectTrack; fxDef.customHeight=UnifiedEffectHeight;

        // 声音轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var sndDef=new TrackDef{
            type=Global.TrackType.Sound, selType=SelType.Sound, color=C_SND,
            name="Sound", localizedName=SkillEditorLocalization.Text("Sound", "音效"), itemName=SkillEditorLocalization.Text("Sound", "音效"), menuLabel=SkillEditorLocalization.Text("Sound", "音效"),
            count=SndCount, listCount=()=>fxAndSoundList.Count,
            maxEnd=null, // 与特效共用列表，长度已由 Fx 轨道贡献
            addEvent=AddSndHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspSnd(fxAndSoundList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(fxAndSoundList,i) };
        // 声音按真实音频长度显示波形和叠层，制作点事件的末帧语义不改变。
        sndDef.lanes=AssignSoundTimelineLanes;
        sndDef.drawLanes=DrawSoundTimeline;

        // 技能衔接轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var jumpDef=new TrackDef{
            type=Global.TrackType.Jump, selType=SelType.Jump, color=C_JUMP,
            name="Transition", localizedName=SkillEditorLocalization.Text("Transition", "技能衔接"), itemName=SkillEditorLocalization.Text("Transition", "技能衔接"), menuLabel=SkillEditorLocalization.Text("Transition", "技能衔接"),
            count=()=>jumpList.Count, listCount=()=>jumpList.Count,
            maxEnd=()=>{ 
            // 当前最大有效末尾帧，空列表为零。
            int m=0; 
            // 原技能衔接窗口引用，不表示物理跳跃。
            foreach(var j in jumpList) if(j.endKey>m) m=j.endKey; return m+TAIL; },
            addEvent=AddJumpHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspJump(jumpList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(jumpList,i) };
        jumpDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,jumpList,/* 当前技能衔接窗口原引用，委托只读取帧范围或显示标签。 */ j=>j.beginKey,/* 当前技能衔接窗口原引用，委托只读取帧范围或显示标签。 */ j=>j.endKey);
        jumpDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,jumpList,jumpDef,
            /* 当前技能衔接窗口原引用，委托只读取帧范围或显示标签。 */ j=>j.beginKey,/* 当前技能衔接窗口原引用，委托只读取帧范围或显示标签。 */ j=>j.endKey,
            (/* 当前技能衔接窗口原引用，委托只读取帧范围或显示标签。 */ j,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>j.nextSkill!=null?(j.autoTrigger?" ":"")+j.nextSkill.name:$"{b}-{e}",
            endExclusive:true,alwaysLabel:true);

        // 预警轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var warnDef=new TrackDef{
            type=Global.TrackType.Warning, selType=SelType.Warning, color=C_WARN,
            name="Telegraph", localizedName=SkillEditorLocalization.Text("Telegraph", "预警"), itemName=SkillEditorLocalization.Text("Telegraph", "预警"), menuLabel=SkillEditorLocalization.Text("Telegraph", "预警"), enemyOnly=false,
            count=()=>warningCueList.Count, listCount=()=>warningCueList.Count,
            maxEnd=()=>MaxEnd(warningCueList,/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>w.endKeyNumber)+TAIL,
            addEvent=AddWarnHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspWarn(warningCueList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(warningCueList,i) };
        warnDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,warningCueList,/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>SegEnd(w.keyNumber,w.endKeyNumber));
        warnDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,warningCueList,warnDef,
            /* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>SegEnd(w.keyNumber,w.endKeyNumber),
            (/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>TelegraphTrackLabel(w),filter:/* 当前预警事件原引用，委托只读取帧范围或显示标签。 */ w=>w!=null,alwaysLabel:true);

        // 取消轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var cancelDef=new TrackDef{
            type=Global.TrackType.Cancel, selType=SelType.Cancel, color=C_CANCEL,
            name="Cancel", localizedName=SkillEditorLocalization.Text("Cancel", "取消"), itemName=SkillEditorLocalization.Text("Cancel", "取消"), menuLabel=SkillEditorLocalization.Text("Cancel", "取消"),
            count=()=>cancelList.Count, listCount=()=>cancelList.Count,
            maxEnd=()=>MaxEnd(cancelList,/* 当前轨道绘制所需的显示行数量。 */ c=>c.keyNumber,/* 当前轨道绘制所需的显示行数量。 */ c=>c.endKeyNumber)+TAIL,
            addEvent=AddCancelHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspCancel(cancelList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(cancelList,i) };
        cancelDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,cancelList,/* 当前轨道绘制所需的显示行数量。 */ c=>c.keyNumber,/* 当前轨道绘制所需的显示行数量。 */ c=>SegEnd(c.keyNumber,c.endKeyNumber));
        cancelDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,cancelList,cancelDef,
            /* 当前取消窗口原引用，委托只读取帧范围或显示标签。 */ cp=>cp.keyNumber,/* 当前取消窗口原引用，委托只读取帧范围或显示标签。 */ cp=>SegEnd(cp.keyNumber,cp.endKeyNumber),
            (/* 当前取消窗口原引用，委托只读取帧范围或显示标签。 */ cp,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>$"≥Lv{(cp.minCancelPriority==Global.CancelPriority.Uncancellable?"∞":((int)cp.minCancelPriority).ToString())}");

        // 飞行物轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var projDef=new TrackDef{
            type=Global.TrackType.Projectile, selType=SelType.Projectile, color=C_PROJ,
            name="Projectile", localizedName=SkillEditorLocalization.Text("Projectile", "飞行物"), itemName=SkillEditorLocalization.Text("Projectile", "飞行物"), menuLabel=SkillEditorLocalization.Text("Projectile", "飞行物"),
            count=()=>projectileList.Count, listCount=()=>projectileList.Count,
            maxEnd=()=>MaxEnd(projectileList,/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>p.keyNumber,/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>p.endKeyNumber)+TAIL,
            addEvent=AddProjHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspProj(projectileList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(projectileList,i) };
        projDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,projectileList,/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>p.keyNumber,/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>SegEnd(p.keyNumber,p.endKeyNumber));
        projDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,projectileList,projDef,
            /* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>p.keyNumber,/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p=>SegEnd(p.keyNumber,p.endKeyNumber),
            (/* 当前飞行物事件原引用，委托只读取帧范围或显示标签。 */ p,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>ProjectileTrackLabel(p,b,e),alwaysLabel:true);

        // 霸体轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var saDef=new TrackDef{
            type=Global.TrackType.SuperArmor, selType=SelType.SuperArmor, color=C_SA,
            name="Armor", localizedName=SkillEditorLocalization.Text("Armor", "霸体"), itemName=SkillEditorLocalization.Text("Armor", "霸体"), menuLabel=SkillEditorLocalization.Text("Armor", "霸体"),
            count=()=>superArmorList.Count, listCount=()=>superArmorList.Count,
            maxEnd=()=>MaxEnd(superArmorList,/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>s.keyNumber,/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>s.endKeyNumber)+TAIL,
            addEvent=AddSuperArmorHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspSuperArmor(superArmorList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(superArmorList,i) };
        saDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,superArmorList,/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>s.keyNumber,/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>SegEnd(s.keyNumber,s.endKeyNumber));
        saDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,superArmorList,saDef,
            /* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>s.keyNumber,/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s=>SegEnd(s.keyNumber,s.endKeyNumber),
            (/* 当前霸体窗口原引用，委托只读取帧范围或显示标签。 */ s,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>$"{(s.armorLevel==0?"L1":s.armorLevel==1?"L2":"?")} {b}-{e}");

        // 朝向轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var amDef=new TrackDef{
            type=Global.TrackType.AdjustMotion, selType=SelType.AdjustMotion, color=C_AM,
            name="Facing", localizedName=SkillEditorLocalization.Text("Facing", "朝向"), itemName=SkillEditorLocalization.Text("Facing", "朝向"), menuLabel=SkillEditorLocalization.Text("Facing", "朝向"), enemyOnly=false,
            count=()=>adjustMotionList.Count, listCount=()=>adjustMotionList.Count,
            maxEnd=()=>MaxEnd(adjustMotionList,/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>a.endKeyNumber)+TAIL,
            addEvent=AddAdjustMotionHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspAdjustMotion(adjustMotionList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(adjustMotionList,i) };
        amDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,adjustMotionList,/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>SegEnd(a.keyNumber,a.endKeyNumber));
        amDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,adjustMotionList,amDef,
            /* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>a.keyNumber,/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a=>SegEnd(a.keyNumber,a.endKeyNumber),
            (/* 当前朝向窗口原引用，委托只读取帧范围或显示标签。 */ a,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>FacingTrackLabel(a,b,e));

        // 刀光轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var trailDef=new TrackDef{
            type=Global.TrackType.Trail, selType=SelType.TrailToggle, color=C_TRAIL,
            name="Trail", localizedName=SkillEditorLocalization.Text("Trail", "刀光"), itemName=SkillEditorLocalization.Text("Trail", "刀光"), menuLabel=SkillEditorLocalization.Text("Trail", "刀光"),
            count=()=>trailToggleList.Count, listCount=()=>trailToggleList.Count,
            maxEnd=()=>MaxEnd(trailToggleList,/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>t.keyNumber,/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>t.endKeyNumber)+TAIL,
            addEvent=AddTrailHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspTrail(trailToggleList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(trailToggleList,i) };
        trailDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,trailToggleList,/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>t.keyNumber,/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>SegEnd(t.keyNumber,t.endKeyNumber));
        trailDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,trailToggleList,trailDef,
            /* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>t.keyNumber,/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t=>SegEnd(t.keyNumber,t.endKeyNumber),
            (/* 当前刀光开关事件原引用，委托只读取帧范围或显示标签。 */ t,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>$"{b}-{e} {TrailShortLabel(t)}");

        // ── 非 lane 轨道 ──
        var hitFxDef=new TrackDef{
            type=Global.TrackType.HitFx, selType=SelType.HitFx, color=C_HITFX,
            name="Hit Effect", localizedName=SkillEditorLocalization.Text("Hit Effect", "命中特效"), itemName=SkillEditorLocalization.Text("Hit Effect", "命中特效"), menuLabel=SkillEditorLocalization.Text("Hit Effect", "命中特效"),
            count=()=>hitFxList.Count, listCount=()=>hitFxList.Count,
            drawCustom=DrawHitFxTrack, customHeight=()=>TRACK_HEIGHT*Mathf.Max(hitFxList.Count,1),
            addEvent=AddHitFxHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspHitFx(hitFxList[i]),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(hitFxList,i) };

        // 位移轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var moveDef=new TrackDef{
            type=Global.TrackType.Move, selType=SelType.Move, color=C_MOVE,
            name="Motion", localizedName=SkillEditorLocalization.Text("Motion", "运动"), itemName=SkillEditorLocalization.Text("Motion", "运动"), menuLabel=SkillEditorLocalization.Text("Motion", "运动"),
            count=()=>moveSegmentList.Count, listCount=()=>moveSegmentList.Count,
            maxEnd=()=>MaxEnd(moveSegmentList,/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>m.keyNumber,/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>SegEnd(m.keyNumber,m.endKeyNumber))+TAIL,
            addEvent=AddMoveSegmentHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>InspMoveSegment(i),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(moveSegmentList,i) };
        moveDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,moveSegmentList,/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>m.keyNumber,/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>SegEnd(m.keyNumber,m.endKeyNumber));
        moveDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,moveSegmentList,moveDef,
            /* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>m.keyNumber,/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m=>SegEnd(m.keyNumber,m.endKeyNumber),
            (/* 当前位移段原引用，委托只读取帧范围或显示标签。 */ m,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>MoveSegLabel(m,b,e));

        // 交互轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var interactionDef=new TrackDef{
            type=Global.TrackType.Interaction, selType=SelType.Interaction, color=C_CANCEL,
            name="Interaction", localizedName=SkillEditorLocalization.Text("Interaction", "交互"), itemName=SkillEditorLocalization.Text("Interaction", "交互"), menuLabel=SkillEditorLocalization.Text("Interaction", "交互"),
            count=()=>interactionWindows.Count, listCount=()=>interactionWindows.Count,
            maxEnd=()=>MaxEnd(interactionWindows,/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>w.endKeyNumber)+TAIL,
            addEvent=AddInteractionHere, inspector=DrawInteractionInspector,
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(interactionWindows,i) };
        interactionDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,interactionWindows,/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>SegEnd(w.keyNumber,w.endKeyNumber));
        interactionDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,interactionWindows,interactionDef,
            /* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>w.keyNumber,/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w=>SegEnd(w.keyNumber,w.endKeyNumber),(/* 当前交互窗口原引用，委托只读取帧范围或显示标签。 */ w,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>$"{w.id} [{b}-{e}]",alwaysLabel:true);

        moveDef.count=()=>moveSegmentList.Count+adjustMotionList.Count;
        moveDef.lanes=null;
        moveDef.drawCustom=DrawUnifiedMotionTrack;
        moveDef.customHeight=MotionTrackHeight;
        moveDef.addEvent=ShowMotionAddMenu;
        // 镜头轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var cameraDef=new TrackDef {
            type=Global.TrackType.Camera, selType=SelType.Camera, color=new Color(.55f,.65f,.95f,.9f),
            name="Camera", localizedName=SkillEditorLocalization.Text("Camera", "镜头"), itemName=SkillEditorLocalization.Text("Camera cue", "镜头事件"), menuLabel=SkillEditorLocalization.Text("Camera", "镜头"),
            count=()=>cameraCues.Count, listCount=()=>cameraCues.Count,
            maxEnd=()=>MaxEnd(cameraCues,/* 当前轨道绘制所需的显示行数量。 */ c=>c.keyNumber,/* 当前轨道绘制所需的显示行数量。 */ c=>c.endKeyNumber)+TAIL,
            addEvent=AddCameraHere, inspector=/* 当前原事件列表的零起始选中下标。 */ i=>DrawSerializedSelection("cameraCues",i),
            removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(cameraCues,i) };
        cameraDef.lanes=/* 当前原轨道复用的显示行分配字典。 */ buf=>AssignLanes(buf,cameraCues,/* 当前轨道绘制所需的显示行数量。 */ c=>c.keyNumber,/* 当前轨道绘制所需的显示行数量。 */ c=>c.endKeyNumber);
        cameraDef.drawLanes=(/* 当前轨道绘图的像素矩形。 */ r,/* 原事件下标到显示行号的映射。 */ l,/* 当前轨道绘制所需的显示行数量。 */ c)=>DrawEventTrack(r,l,c,cameraCues,cameraDef,
            /* 当前镜头提示原引用，委托只读取帧范围或显示标签。 */ cue=>cue.keyNumber,/* 当前镜头提示原引用，委托只读取帧范围或显示标签。 */ cue=>cue.endKeyNumber,(/* 当前镜头提示原引用，委托只读取帧范围或显示标签。 */ cue,/* 原事件开始帧，零起始。 */ b,/* 原事件有效末尾帧。 */ e)=>cue.label,alwaysLabel:true);
        // 流程轨道的完整绘制、创建、属性及删除回调定义，不代表游戏宿主已经接入。
        var flowDef = new TrackDef { type=Global.TrackType.Flow, selType=SelType.Flow, name="Action Flow", localizedName=SkillEditorLocalization.Text("Action Flow", "技能流程"), itemName=SkillEditorLocalization.Text("Flow node", "流程节点"), menuLabel=SkillEditorLocalization.Text("Action Flow", "技能流程"), color=new Color(.6f,.68f,.9f),
            count=()=>FlowCount,listCount=()=>flowNodes.Count,maxEnd=()=>MaxEnd(flowNodes,/* 当前原事件原引用，委托只读取帧范围或显示标签。 */ n=>n.keyNumber,/* 当前原事件原引用，委托只读取帧范围或显示标签。 */ n=>n.limitWindow?n.endKeyNumber:n.keyNumber)+TAIL,
            addEvent=ShowFlowAddMenu,inspector=DrawFlowInspector,removeAt=/* 当前原事件列表的零起始选中下标。 */ i=>RemoveEvent(flowNodes,i),drawCustom=DrawFlowTrack,customHeight=FlowTrackHeight };
        // 原轨道注册保持完整，追加RO缺项且不改旧列表或旧枚举。
        List<TrackDef> definitions = new List<TrackDef>{
            flowDef,cameraDef,animDef,atkDef,fxDef,sndDef,jumpDef,cancelDef,projDef,hitFxDef,
            moveDef,warnDef,saDef,amDef,trailDef,interactionDef };
        RegisterRoTracks(definitions);
        return definitions;
    }

    /// <summary>帧段结束帧：endKey <= beginKey 时视为单帧</summary>
    /// <param name="begin">事件区间起始帧，零起始。</param>
    /// <param name="end">当前事件区间的有效末尾帧，用于范围限制。</param>
    static int SegEnd(int begin,int end)=>end>begin?end:begin;
    /// <summary>列表中最大结束帧</summary>
    /// <param name="list">可按下标访问的原事件列表，编辑对象仍为原引用。</param>
    /// <param name="getBegin">读取原事件起始帧的强类型委托。</param>
    /// <param name="getEnd">读取原事件结束帧的强类型委托。</param>
    static int MaxEnd<T>(List<T> list,Func<T,int> getBegin,Func<T,int> getEnd){
        // 当前最大有效末尾帧，空列表为零。
        int m=0;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<list.Count;i++){ 
        // 当前原事件的有效结束帧。
        int e=SegEnd(getBegin(list[i]),getEnd(list[i])); if(e>m) m=e; }
        return m;
    }
    /// <summary>显示原特效与音频资源名称，仅本地化前缀和未指定资源提示。</summary>
    /// <param name="effect">原特效音效事件，不修改资源引用、模式或用户名称。</param>
    /// <returns>当前语言的视觉或音频轨道块标签。</returns>
    static string EffectTrackLabel(Global.FxAndSound effect)
    {
        if (effect.contentKind == Global.EffectContentKind.Legacy && effect.particleSystem != null && effect.audioClip != null)
            return SkillEditorLocalization.Text("VFX + Audio: ", "特效 + 音频：") + effect.particleSystem.name + " / " + effect.audioClip.name;
        if (EffectAuthoring.Resolve(effect) == Global.EffectContentKind.Audio)
        {
            // 音频模式的空引用只影响提示文字。
            string audioName = SkillEditorLocalization.Text("Assign clip", "指定音频剪辑");
            if (effect.audioClip != null) audioName = effect.audioClip.name;
            return SkillEditorLocalization.Text("Audio: ", "音频：") + audioName;
        }
        // 视觉模式使用原粒子资源名，不把翻译内容写回事件。
        string particleName = SkillEditorLocalization.Text("Assign effect", "指定特效");
        if (effect.particleSystem != null) particleName = effect.particleSystem.name;
        return SkillEditorLocalization.Text("VFX: ", "特效：") + particleName;
    }

    /// <summary>将作者触发帧或真实区间换算为当前预览时钟的秒数；零或倒序末帧仍表示点事件。</summary>
    /// <param name="begin">作者事件的零起始触发帧，不由粒子自然寿命推导。</param>
    /// <param name="end">作者有效结束帧；小于等于开始帧时不显示虚假的结束时间。</param>
    /// <returns>当前语言的帧与秒时间摘要，仅用于界面。</returns>
    private string EffectTimingLabel(int begin, int end)
    {
        // 与SeekPreviewFrame使用相同的配置或旧动画帧率，单位帧每秒。
        float frameRate = GetPreviewFrameRate();
        if (end > begin)
            return SkillEditorLocalization.Text($"Range F{begin}–{end} | {begin / frameRate:0.000}–{end / frameRate:0.000}s",
                $"区间 F{begin}–{end} | {begin / frameRate:0.000}–{end / frameRate:0.000}秒");
        return SkillEditorLocalization.Text($"Trigger F{begin} | {begin / frameRate:0.000}s",
            $"触发 F{begin} | {begin / frameRate:0.000}秒");
    }

    /// <summary>读取三行旁注的实际像素宽度，用于显示分行和滚动范围，不扩大事件区间。</summary>
    /// <param name="begin">作者事件零起始触发帧。</param>
    /// <param name="end">作者有效结束帧，点事件等于开始帧。</param>
    /// <param name="resourceLabel">未裁剪的原特效资源名，包含当前语言的类型前缀。</param>
    /// <param name="durationLabel">区分单轮保守估算、源循环或自定义截断的只读持续说明。</param>
    /// <returns>三行中最宽一行的像素宽度，包含左右留白。</returns>
    private float EffectLabelWidth(int begin, int end, string resourceLabel, string durationLabel)
    {
        // 当前语言时间摘要的实际文字宽度，缩放改变帧像素时文字字号仍保持可读。
        float timingWidth = TSty.Item.CalcSize(new GUIContent(EffectTimingLabel(begin, end))).x;
        // 原资源文字不通过别名缩短，完整显示所需宽度由GUI字体计算。
        float resourceWidth = TSty.Item.CalcSize(new GUIContent(resourceLabel)).x;
        // 持续说明可能包含源循环与技能单轮，按当前语言完整测量避免旁注撞邻事件。
        float durationWidth = TSty.Item.CalcSize(new GUIContent(durationLabel)).x;
        return Mathf.Max(Mathf.Max(timingWidth, resourceWidth), durationWidth) + 8f;
    }

    /// <summary>在真实事件标记旁绘制触发、预计起末、持续及资源四行文字；拖动仍命中原事件矩形。</summary>
    /// <param name="eventRect">真实点标记或作者区间的像素矩形，不能扩成粒子自然寿命。</param>
    /// <param name="begin">作者事件零起始触发帧。</param>
    /// <param name="end">作者有效结束帧，点事件等于开始帧。</param>
    /// <param name="resourceLabel">完整原特效资源名，tooltip保留同一资源身份。</param>
    /// <param name="durationLabel">当前源循环、技能单轮估算或自定义截断的持续说明。</param>
    /// <param name="effect">当前原特效事件，仅提供只读参数区间和倍率。</param>
    private void DrawEffectTimingLabel(Rect eventRect, int begin, int end, string resourceLabel, string durationLabel, Global.FxAndSound effect)
    {
        // 时间摘要完整显示在第一行，优先让使用者看到触发时点。
        string timing = EffectTimingLabel(begin, end);
        // tooltip同时给出资源与时点，窄窗口通过悬停仍能查看完整信息。
        string tooltip = resourceLabel + "\n" + timing + "\n" + EffectRangeLabel(effect) + "\n" + durationLabel + "\n" + SkillEditorLocalization.Text(
            "White line = authored trigger; gray band = estimated startup delay; green band = estimated emission/tail window; orange line = estimated end or custom cutoff. Each event plays independently and overlapping windows are not a queue. Estimates use particle parameters, not observed first/last visible pixels. Authored ranges and END are unchanged; custom cutoff excludes cleanup grace.",
            "白线为作者触发，灰段为预计启动延迟，淡绿段为预计发射及尾粒范围，橙线为估算结束或自定义截断。每个事件独立播放，重叠区间不是排队。估算来自粒子参数，不是首末可见像素的实测；作者区间和END保持，自定义截断不含清理宽限。");
        // 旁注可越出单帧八像素标记，统一显示分行已经为其实际宽度留出空间。
        float width = EffectLabelWidth(begin, end, resourceLabel, durationLabel);
        width = Mathf.Max(width, TSty.Item.CalcSize(new GUIContent(EffectRangeLabel(effect))).x + 8f);
        GUI.Label(new Rect(eventRect.x + 2f, eventRect.y, width - 4f, 14f), new GUIContent(timing, tooltip), TSty.Item);
        GUI.Label(new Rect(eventRect.x + 2f, eventRect.y + 14f, width - 4f, 14f), new GUIContent(EffectRangeLabel(effect), tooltip), TSty.Item);
        GUI.Label(new Rect(eventRect.x + 2f, eventRect.y + 28f, width - 4f, 14f), new GUIContent(durationLabel, tooltip), TSty.Item);
        GUI.Label(new Rect(eventRect.x + 2f, eventRect.y + 42f, width - 4f, 14f), new GUIContent(resourceLabel, tooltip), TSty.Item);
    }

    /// <summary>在原事件下沿画只读延迟及播放估算条；整条不可拖动，命中仍使用原作者marker。</summary>
    /// <param name="eventRect">原作者marker矩形，横坐标对应触发帧，不扩展编辑范围。</param>
    /// <param name="effect">当前特效原事件引用，资源与时间只读。</param>
    private void DrawEffectPlaybackRange(Rect eventRect, Global.FxAndSound effect)
    {
        // 显示区间的预计发射与估算尾端，未知首发仍显示整体参数范围。
        int firstFrame, tailFrame;
        if (!TryGetEffectPreviewRange(effect, out firstFrame, out tailFrame)) return;
        // 原触发点在轨道中的横坐标，平移区间保持与时间轴网格对齐。
        float origin = eventRect.x;
        // 估算尾端坐标，仅用于显示和导航；不写endKeyNumber。
        float tailX = origin + Mathf.Max(0, tailFrame - effect.keyNumber) * pixelsPerFrame;
        // 区间底部留出的八像素条带，不覆盖四行时间文字。
        float bandY = eventRect.yMax - 9f;
        // 首发未知时整条采用灰色，避免把触发当作真实首个可见时刻。
        Color rangeColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);
        EditorGUI.DrawRect(new Rect(origin, bandY, Mathf.Max(2f, tailX - origin), 7f), rangeColor);
        if (firstFrame >= 0)
        {
            // 延迟后参数发射起点，使用当前缩放绘制淡绿预计发射及尾粒范围。
            float firstX = origin + (firstFrame - effect.keyNumber) * pixelsPerFrame;
            EditorGUI.DrawRect(new Rect(firstX, bandY, Mathf.Max(2f, tailX - firstX), 7f), new Color(0.35f, 0.8f, 0.52f, 0.5f));
            EditorGUI.DrawRect(new Rect(firstX, bandY - 2f, 1f, 11f), new Color(0.35f, 0.9f, 0.55f, 0.9f));
        }
        EditorGUI.DrawRect(new Rect(origin, bandY - 2f, 2f, 11f), Color.white);
        EditorGUI.DrawRect(new Rect(tailX, bandY - 2f, 2f, 11f), new Color(1f, 0.65f, 0.2f, 0.95f));
    }

    /// <summary>碰撞块的显示标签，保留形状/响应原枚举语义，只切换文字。</summary>
    /// <param name="attack">原碰撞事件只读引用。</param>
    /// <param name="begin">开始帧，非负。</param>
    /// <param name="end">有效结束帧。</param>
    /// <returns>形状、接触响应和帧范围组成的显示文本。</returns>
    static string CollisionTrackLabel(Global.Attack attack, int begin, int end)
    {
        // 球体为原默认形状，值1表示盒体。
        string shape = SkillEditorLocalization.Text("Sphere", "球体");
        if (attack.shapeType == 1) shape = SkillEditorLocalization.Text("Box", "盒体");
        // 未知响应仍保留原枚举名字，已支持两种响应明确翻译。
        string response = attack.responseMode.ToString();
        if (attack.responseMode == Global.CollisionResponseMode.Damage) response = SkillEditorLocalization.Text("Damage", "伤害");
        else if (attack.responseMode == Global.CollisionResponseMode.Signal) response = SkillEditorLocalization.Text("Signal", "信号");
        return shape + " / " + response + " " + begin + "-" + end;
    }

    /// <summary>预警块优先展示原视觉资源名，其次音频资源名，空引用给出当前语言提示。</summary>
    /// <param name="warning">原预警事件引用，不修改资源或字段。</param>
    /// <returns>当前语言的预警前缀与原资源名称。</returns>
    static string TelegraphTrackLabel(Global.WarningCue warning)
    {
        // 未绑定资源的提示不是写入资产的名称。
        string resourceName = SkillEditorLocalization.Text("Choose effect", "选择特效");
        if (warning.warningVfx != null) resourceName = warning.warningVfx.name;
        else if (warning.warningSound != null) resourceName = warning.warningSound.name;
        return SkillEditorLocalization.Text("Telegraph: ", "预警：") + resourceName;
    }

    /// <summary>飞行物轨道块展示原预制体名与帧范围，只翻译缺资源提示和帧单位。</summary>
    /// <param name="projectile">原飞行物事件，不更改其运行行为。</param>
    /// <param name="begin">开始帧。</param>
    /// <param name="end">有效结束帧。</param>
    /// <returns>本地化显示标签。</returns>
    static string ProjectileTrackLabel(Global.Projectile projectile, int begin, int end)
    {
        // 原资源名直接用于显示，空引用时给出当前语言提示。
        string resourceName = SkillEditorLocalization.Text("Missing Prefab", "缺少预制体");
        if (projectile.prefab != null) resourceName = projectile.prefab.name;
        if (begin == end) return SkillEditorLocalization.Text($"{resourceName} F{begin}", $"{resourceName} 第{begin}帧");
        return resourceName + " " + begin + "-" + end;
    }

    /// <summary>朝向轨道显示跟踪或锁定提示，真实allowTurning仍由原事件值决定。</summary>
    /// <param name="motion">只读朝向事件。</param>
    /// <param name="begin">开始帧。</param>
    /// <param name="end">结束帧。</param>
    /// <returns>当前语言的朝向提示。</returns>
    static string FacingTrackLabel(Global.AdjustMotionSegment motion, int begin, int end)
    {
        if (motion.allowTurning) return SkillEditorLocalization.Text($"Track target {begin}-{end}", $"跟踪目标 {begin}-{end}");
        return SkillEditorLocalization.Text($"Lock facing {begin}-{end}", $"锁定朝向 {begin}-{end}");
    }

    /// <param name="t">当前原刀光开关事件。</param>
    /// <summary>展示刀光粒子或层级路径，过长路径只缩短标签，不修改保存路径。</summary>
    static string TrailShortLabel(Global.TrailToggle t){
        if(t.particle!=null) return $"{t.particle.gameObject.name}";
        if(string.IsNullOrEmpty(t.trailPath)) return SkillEditorLocalization.Text("Unassigned", "未指定");
        return t.trailPath.Length>12?".."+t.trailPath.Substring(t.trailPath.Length-10):t.trailPath;
    }

    // ── 时间轴总帧数：每次 OnGUI 重算一次，帧内复用 ──
    int _totalFramesCache=-1;

    /// <summary>合并各轨道对时间轴长度的贡献并在本次 GUI 事件缓存结果。</summary>
    int GetTotalFrames(){
        if(_totalFramesCache>=0) return _totalFramesCache;
        // 各轨道贡献的最大末尾帧，初值保留最小编辑长度。
        int max=MIN_TOTAL_FRAMES;
        // 当前完整轨道注册表引用，配置加载后回调仍读取原列表。
        var defs=TrackDefs;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<defs.Count;i++){
            if(defs[i].maxEnd==null) continue;
            // 当前原事件的有效结束帧。
            int e=defs[i].maxEnd(); if(e>max) max=e;
        }
        return _totalFramesCache=max;
    }
    /// <param name="t">待读取显示颜色的原轨道类别。</param>
    /// <summary>读取轨道注册颜色，未知类型使用灰色，颜色只表示轨道类别。</summary>
    Color GetTrackColor(Global.TrackType t){ 
    // 当前轨道的注册定义，存在时读取其显示颜色。
    var d=GetDef(t); return d!=null?d.color:Color.gray; }
    /// <summary>取得稳定英文默认轨道名，保持旧默认身份与显示语言独立。</summary>
    /// <param name="t">原始轨道类型，不改变其序列化值。</param>
    /// <returns>原英文默认名，未知类型沿用稳定Track标记。</returns>
    string GetTrackDefaultName(Global.TrackType t)
    {
        // 原类型对应的轨道注册记录，不生成或编辑任何布局数据。
        TrackDef definition = GetDef(t);
        if (definition != null) return definition.name;
        return "Track";
    }

    /// <summary>取得当前语言的轨道标题，仅用于窗口和对话框显示。</summary>
    /// <param name="type">原始轨道类型。</param>
    /// <returns>本地化轨道标题；未知类型使用普通轨道文字。</returns>
    string GetTrackDisplayName(Global.TrackType type)
    {
        // 注册表缓存会在语言变化时重新建立。
        TrackDef definition = GetDef(type);
        if (definition != null) return definition.localizedName;
        return SkillEditorLocalization.Text("Track", "轨道");
    }

    /// <summary>只转换分组展示文字，原Presentation/Mechanics键继续参与轨道分类判断。</summary>
    /// <param name="category">BuiltinTrackRegistry给出的固定分组标识。</param>
    /// <returns>当前语言的分组文字，未知标识原样显示。</returns>
    static string TrackCategoryLabel(string category)
    {
        if (category == "Presentation") return SkillEditorLocalization.Text("Presentation", "表现");
        if (category == "Mechanics") return SkillEditorLocalization.Text("Mechanics", "机制");
        return category;
    }

    /// <summary>统计共享效果列表中可在视觉轨道显示的事件，保持旧双引用兼容规则。</summary>
    int FxCount(){
        // 筛选后属于粒子轨道的数量，旧双引用仍保留视觉入口。
        int count=0;
        // 共享列表的原下标不因显示分类改变。
        for(int index=0;index<fxAndSoundList.Count;index++)
            if(BuiltinTrackRegistry.IsVisibleInFxTrack(fxAndSoundList[index]))count++;
        return count;
    }
    /// <summary>统计共享效果列表中声音轨道可见项，保留旧双引用事件的兼容行为。</summary>
    int SndCount(){
        // 筛选后属于音效轨道的数量，空音效占位仍可编辑。
        int count=0;
        // 共享列表的零起始下标，保留原对象引用。
        for(int index=0;index<fxAndSoundList.Count;index++)
            if(BuiltinTrackRegistry.IsVisibleInSoundTrack(fxAndSoundList[index]))count++;
        return count;
    }

    /// <summary>本次时间轴布局的显示记录缓存，原事件数据不复制。</summary>
    readonly List<TrkInfo> _tracks=new List<TrkInfo>();

    /// <summary>缺失布局但有事件轨道的个人折叠状态，不回写原布局。</summary>
    readonly HashSet<Global.TrackType> collapsedRecoveredTracks=new HashSet<Global.TrackType>();

    /// <summary>按稳定分组键建立本次GUI的显示轨道，用户自定义标题原样保留，不回写技能布局。</summary>
    /// <returns>窗口复用的轨道显示缓存，语言切换后根据当前文案重建。</returns>
    List<TrkInfo> BuildTracks()
    {
        _tracks.Clear();
        // 一次绘制中已显示的规范轨道类型，防止别名生成重复行。
        HashSet<Global.TrackType> seen = new HashSet<Global.TrackType>();
        // 分类判断始终使用原标识；翻译只发生在绘制与菜单层。
        string[] categories = { "Presentation", "Mechanics" };
        // 当前已本地化的注册表快照，只改显示缓存，不改资产引用。
        List<TrackDef> definitions = TrackDefs;
        // 分组的零起始下标，保持原表现优先、机制随后顺序。
        for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
        {
            // 本分组稳定分类键。
            string category = categories[categoryIndex];
            // 当前正式布局的零起始轨道下标。
            for (int trackIndex = 0; trackIndex < trackList.Count; trackIndex++)
            {
                // 布局原对象只读，语言切换不会给它赋值。
                Global.SkillTrack track = trackList[trackIndex];
                // 原轨道别名对应的规范类型，值与原运行注册表一致。
                Global.TrackType canonical = BuiltinTrackRegistry.Canonical(track.type);
                if (BuiltinTrackRegistry.GetCategory(canonical) != category || !seen.Add(canonical)) continue;
                // 本轨道显示与绘制信息，不是序列化数据。
                TrackDef definition = GetDef(canonical);
                if (definition == null) continue;
                // 窗口显示标题，用户配置过的标题不参与翻译。
                string displayName = definition.localizedName;
                if (!string.IsNullOrEmpty(track.displayName)) displayName = track.displayName;
                _tracks.Add(CreateTrackInfo(definition, displayName, track.expanded, trackIndex));
            }
            // 缺失布局的活动事件只恢复可见行，不修改正式trackList。
            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                // 当前候选轨道定义，仍对应原类型和事件集合。
                TrackDef definition = definitions[definitionIndex];
                if (BuiltinTrackRegistry.Canonical(definition.type) != definition.type || seen.Contains(definition.type) ||
                    BuiltinTrackRegistry.GetCategory(definition.type) != category || definition.count == null || definition.count() == 0) continue;
                seen.Add(definition.type);
                _tracks.Add(CreateTrackInfo(definition, definition.localizedName, !collapsedRecoveredTracks.Contains(definition.type), -1));
            }
        }
        return _tracks;
    }

    /// <summary>计算一行轨道的lane与显示高度，所有结果只存在窗口缓存。</summary>
    /// <param name="definition">规范轨道注册定义，绑定原事件引用。</param>
    /// <param name="displayName">已选择的当前语言标题或用户原自定义标题。</param>
    /// <param name="expanded">原布局或恢复行的展开状态。</param>
    /// <param name="layoutIndex">正式布局下标，负一表示仅显示的恢复行。</param>
    /// <returns>仅供绘制使用的轨道信息，不写回SkillConfigSO。</returns>
    TrkInfo CreateTrackInfo(TrackDef definition, string displayName, bool expanded, int layoutIndex)
    {
        // 事件的lane分配缓存，无lane轨道保持空值。
        Dictionary<int, int> lanes = null;
        // lane至少保留一行，高度单位为窗口像素。
        int laneCount = 1;
        if (definition.lanes != null)
        {
            lanes = definition.lanes(definition.laneBuf);
            // 字典值快照允许统一按索引读取，而不修改事件集合。
            List<int> laneValues = new List<int>(lanes.Values);
            // lane快照的零起始下标。
            for (int laneIndex = 0; laneIndex < laneValues.Count; laneIndex++) laneCount = Mathf.Max(laneCount, laneValues[laneIndex] + 1);
        }
        // 折叠行的内容高度为零，外层保持原最小轨道头高度。
        float height = 0f;
        if (expanded)
        {
            if (definition.lanes != null) height = TRACK_HEIGHT * laneCount;
            else height = definition.customHeight();
        }
        return new TrkInfo { def = definition, name = displayName, expanded = expanded, trackListIdx = layoutIndex, lanes = lanes, laneCount = laneCount, height = height };
    }

    /// <param name="track">原配置显示布局对象，显隐不删除事件。</param>
    /// <summary>只切换轨道的显示展开状态；原事件数据继续保留，不等于关闭运行功能。</summary>
    void ToggleTrackView(TrkInfo track) {
        if(track.trackListIdx>=0) { PushUndo(); trackList[track.trackListIdx].expanded=!track.expanded; }
        else if(track.expanded) collapsedRecoveredTracks.Add(track.def.type);
        else collapsedRecoveredTracks.Remove(track.def.type);
    }

    /// <summary>显示活动事件恢复行的操作菜单，只有明确点击添加才修改布局。</summary>
    /// <param name="def">恢复行对应的原轨道定义。</param>
    void ShowRecoveredTrackMenu(TrackDef def) {
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        if(def.addEvent!=null) menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Add {def.itemName} at playhead", $"在播放头添加{def.itemName}")),false,()=>def.addEvent());
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Add track to layout", "添加到轨道布局")),false,()=>{
            if(trackList.Exists(/* 当前原轨道布局条目。 */ t=>BuiltinTrackRegistry.Canonical(t.type)==def.type)) return;
            PushUndo(); trackList.Add(new Global.SkillTrack{type=def.type,expanded=true});
        });
        menu.ShowAsContext();
    }

    /// <summary>当前行分配时各行已占用的结束帧缓存。</summary>
    readonly List<int> _laneEnds=new List<int>();

    /// <summary>分配 lane 索引，使重叠的条目分布在不同行。结果写入复用缓冲，避免每帧分配。</summary>
    /// <param name="buf">供原轨道复用的显示行分配字典。</param>
    /// <param name="list">可按下标访问的原事件列表，编辑对象仍为原引用。</param>
    /// <param name="getBegin">读取原事件起始帧的强类型委托。</param>
    /// <param name="getEnd">读取原事件结束帧的强类型委托。</param>
    /// <param name="filter">判断当前原事件是否属于此显示轨道的强类型委托。</param>
    Dictionary<int,int> AssignLanes<T>(Dictionary<int,int> buf,List<T> list,
        Func<T,int> getBegin,Func<T,int> getEnd,Func<T,bool> filter=null){
        buf.Clear(); _laneEnds.Clear();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<list.Count;i++){
            if(filter!=null&&!filter(list[i])) continue;
            // b：当前原事件开始帧。；e：当前原事件的有效结束帧。
            int b=getBegin(list[i]),e=getEnd(list[i]);
            // 本次事件已分配行号，负一表示尚未找到空行。
            int assigned=-1;
            // 当前通用绘图回调的事件下标到行号映射。
            for(int l=0;l<_laneEnds.Count;l++){
                if(_laneEnds[l]<b){ assigned=l; _laneEnds[l]=e; break; }
            }
            if(assigned<0){ assigned=_laneEnds.Count; _laneEnds.Add(e); }
            buf[i]=assigned;
        }
        return buf;
    }

    /// <param name="area">输入或绘图允许的窗口像素区域。</param>
    /// <summary>布局轨道头、标尺、网格和事件块，统一滚动偏移并分派时间轴输入。</summary>
    void DrawTimelineArea(Rect area){
        EditorGUI.DrawRect(area,C_BG);
        // 当前轨道合并后的时间轴总帧数。
        int totalF=GetTotalFrames();
        // 播放头可导航的纯预览尾部，不扩展作者事件可编辑范围或正式结束帧。
        int previewF=Mathf.Max(totalF,GetPreviewEndFrame());
        // 扣除属性面板与分隔条后的时间轴像素宽度。
        float tlW=Mathf.Max(Mathf.Max(previewF,SoundTimelineDisplayEnd()),EffectTimelineDisplayEnd())*pixelsPerFrame+200f;
        // 当前重绘使用的显示轨道缓存，事件仍指向原列表。
        var tracks=BuildTracks();
        // 标尺、轨道和添加入口合计的滚动内容高度，单位像素。
        float trkH=RULER_HEIGHT+ADD_BTN_HEIGHT+52; 
        // 显示轨道快照的零起始下标，用于累计原轨道高度。
        for(int trackIndex=0;trackIndex<tracks.Count;trackIndex++) trkH+=Mathf.Max(tracks[trackIndex].height,30);

        // 左侧轨道头
        Rect hdr=new Rect(area.x,area.y,TRACK_HEADER_WIDTH,area.height);
        DrawTrackHeaders(hdr,tracks);

        // 右侧内容
        Rect cnt=new Rect(area.x+TRACK_HEADER_WIDTH,area.y,area.width-TRACK_HEADER_WIDTH,area.height);
        GUI.BeginClip(cnt);
        // cw：时间轴当前内容视口宽度，单位像素。；ch：时间轴当前内容视口高度，单位像素。
        float cw=cnt.width,ch=cnt.height;
        timelineScrollPos=GUI.BeginScrollView(new Rect(0,0,cw,ch),timelineScrollPos,
            new Rect(0,0,tlW,Mathf.Max(trkH,ch-16)));

        DrawRuler(new Rect(0,0,tlW,RULER_HEIGHT),previewF);
        // 当前纵向绘制位置，单位窗口像素。
        float y=RULER_HEIGHT; 
        // 上一行的稳定轨道分组键，用于分组标题切换。
        string previousLayer=null;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<tracks.Count;i++){
            // 本次布局中的显示轨道记录。
            var t=tracks[i]; 
            // 当前显示轨道裁剪后的像素高度，至少三十像素。
            float h=Mathf.Max(t.height,30);
            // 当前行的稳定轨道分组键，翻译仅用于标签。
            string layer=BuiltinTrackRegistry.GetCategory(t.def.type);
            if(layer!=previousLayer){ EditorGUI.DrawRect(new Rect(0,y,tlW,26),new Color(.13f,.14f,.16f)); y+=26; previousLayer=layer; }
            // 当前绘制元素的窗口像素矩形。
            Rect r=new Rect(0,y,tlW,h);
            // 交替背景只影响显示，保持当前轨道原排序和点击区域。
            Color rowColor=C_TRK2;
            if(i%2==0) rowColor=C_TRK;
            EditorGUI.DrawRect(r,rowColor);
            DrawGrid(r,previewF);
            if(t.expanded&&t.height>0){
                if(t.def.lanes!=null) t.def.drawLanes(r,t.lanes,t.laneCount);
                else t.def.drawCustom?.Invoke(r);
            }
            y+=h;
        }
        // 正式技能末帧仍画原结束线，纯预览尾部不会替换它。
        float endX=GetPreviewContentEndFrame()*pixelsPerFrame;
        EditorGUI.DrawRect(new Rect(endX-1,0,2,y),new Color(1f,.65f,.15f,.75f));
        GUI.Label(new Rect(Mathf.Max(0,endX-58),2,56,16),SkillEditorLocalization.Text($"END {GetPreviewContentEndFrame()}", $"结束 {GetPreviewContentEndFrame()}"),EditorStyles.miniLabel);
        DrawPlayhead(new Rect(0,0,tlW,y),previewF);
        HandleTLInput(new Rect(0,0,tlW,y),totalF);
        GUI.EndScrollView();
        GUI.EndClip();
    }

    /// <summary>绘制分组和轨道标题、布局按钮；自定义名称已由BuildTracks原样保留。</summary>
    /// <param name="area">左侧轨道头的窗口像素区域。</param>
    /// <param name="tracks">本次GUI的原类型与当前语言显示信息。</param>
    void DrawTrackHeaders(Rect area,List<TrkInfo> tracks){
        EditorGUI.DrawRect(area,new Color(.16f,.16f,.16f));
        // 标尺头部的窗口像素矩形。
        Rect rh=new Rect(area.x,area.y,area.width,RULER_HEIGHT);
        EditorGUI.DrawRect(rh,new Color(.14f,.14f,.14f));
        GUI.Label(new Rect(rh.x+6,rh.y+3,rh.width-12,16),configFile!=null?configFile.name:"---",EditorStyles.miniLabel);
        // 当前纵向绘制位置，单位窗口像素。
        float y=area.y+RULER_HEIGHT-timelineScrollPos.y; 
        // 上一行的稳定轨道分组键，用于分组标题切换。
        string previousLayer=null;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<tracks.Count;i++){
            // 当前轨道头对应的显示记录。
            var t=tracks[i]; 
            // 当前轨道头裁剪后的像素高度，至少三十像素。
            float h=Mathf.Max(t.height,30);
            // 当前行的稳定轨道分组键，翻译仅用于标签。
            string layer=BuiltinTrackRegistry.GetCategory(t.def.type);
            if(layer!=previousLayer){
                EditorGUI.DrawRect(new Rect(area.x,y,area.width,26),new Color(.13f,.14f,.16f));
                GUI.Label(new Rect(area.x+12,y+5,area.width-24,18),TrackCategoryLabel(layer),EditorStyles.boldLabel);
                y+=26; previousLayer=layer;
            }
            // 当前轨道标题行的像素矩形。
            Rect hr=new Rect(area.x,y,area.width,h);
            if(hr.yMax>=area.y&&hr.y<=area.yMax){
                EditorGUI.DrawRect(hr,i%2==0?new Color(.19f,.19f,.19f):new Color(.21f,.21f,.21f));
                // 左侧颜色条
                EditorGUI.DrawRect(new Rect(hr.x,hr.y,4,hr.height),t.def.color);
                // 轨道名称 (为按钮留出空间)
                float btnZoneW=92; // [+] 和 [▼] 两个按钮的总宽度
                GUI.Label(new Rect(hr.x+8,hr.y+3,hr.width-btnZoneW-8,16),t.name,TSty.TrkLbl);
                // ── [+] 添加 Event 按钮（醒目，仅非位移轨道显示）──
                if(t.def.addEvent!=null){
                    // 当前轨道添加按钮的像素点击范围。
                    Rect addBtnR=new Rect(hr.xMax-92,hr.y+5,40,20);
                    // 用轨道颜色作为按钮高亮背景
                    Color btnBg=new Color(t.def.color.r,t.def.color.g,t.def.color.b,0.35f);
                    EditorGUI.DrawRect(addBtnR,btnBg);
                    if(GUI.Button(addBtnR,SkillEditorLocalization.Text("Add", "添加"),EditorStyles.miniButton)){
                        t.def.addEvent();
                    }
                }
                // ── [▼/▶] 展开/折叠按钮 ──
                int ti=t.trackListIdx;
                // 展开按钮只反映当前轨道状态，语言不修改此状态。
                string expandLabel = SkillEditorLocalization.Text("Show", "显示");
                if (t.expanded) expandLabel = SkillEditorLocalization.Text("Hide", "隐藏");
                if(GUI.Button(new Rect(hr.xMax-48,hr.y+5,44,20),expandLabel,EditorStyles.miniButton)){
                    ToggleTrackView(t);
                }
                // 右键上下文菜单
                if(Event.current.type==EventType.ContextClick&&hr.Contains(Event.current.mousePosition)){
                    if(t.trackListIdx>=0) ShowTrackContextMenu(t.trackListIdx); else ShowRecoveredTrackMenu(t.def); Event.current.Use(); }
            }
            y+=h;
        }
        // ── 底部：添加轨道按钮 ──
        Rect addRect=new Rect(area.x,y,area.width,ADD_BTN_HEIGHT);
        if(addRect.yMax>=area.y&&addRect.y<=area.yMax){
            EditorGUI.DrawRect(addRect,new Color(.17f,.17f,.17f));
            if(GUI.Button(new Rect(addRect.x+4,addRect.y+2,addRect.width-8,addRect.height-4),SkillEditorLocalization.Text("Add track", "添加轨道"),EditorStyles.miniButton))
                ShowAddTrackMenu();
            if(Event.current.type==EventType.ContextClick&&addRect.Contains(Event.current.mousePosition)){
                ShowAddTrackMenu(); Event.current.Use(); }
        }
        EditorGUI.DrawRect(new Rect(area.xMax-1,area.y,1,area.height),new Color(0,0,0,.5f));
    }

    /// <summary>显示当前语言的轨道菜单，事件类型与点击后的序列化值不受语言影响。</summary>
    void ShowAddTrackMenu(){
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        // 原布局已有规范轨道类型快照，避免重复添加。
        var existing=new HashSet<Global.TrackType>(); 
        // 当前原轨道布局条目，读取规范类型以避免重复创建。
        foreach(var t in trackList) existing.Add(BuiltinTrackRegistry.Canonical(t.type));
        // 当前轨道完整绘制及编辑回调定义。
        foreach(var def in TrackDefs){
            if(BuiltinTrackRegistry.Canonical(def.type)!=def.type) continue;
            // 原轨道枚举身份，序列化整数保持不变。
            var type=def.type; 
            // 本次本地化显示文字，原资源名称与字段路径保持。
            var label=GetTrackMenuPath(def);
            if(existing.Contains(type)) menu.AddDisabledItem(new GUIContent(SkillEditorLocalization.Text($"{label} (already added)", $"{label}（已添加）")));
            else menu.AddItem(new GUIContent(label),false,()=>{
                PushUndo(); trackList.Add(new Global.SkillTrack{type=type,expanded=true}); });
        }
        menu.ShowAsContext();
    }

    /// <summary>组合仅供菜单显示的本地化分类和轨道名，固定分类判断键不变。</summary>
    /// <param name="def">原类型和当前语言菜单显示名。</param>
    /// <returns>当前语言菜单层级路径，不作为资源或逻辑查找键。</returns>
    static string GetTrackMenuPath(TrackDef def){
        return $"{TrackCategoryLabel(BuiltinTrackRegistry.GetCategory(def.type))}/{def.menuLabel}";
    }

    /// <summary>展示轨道布局和选中事件的本地化操作，原按钮回调行为保持。</summary>
    /// <param name="trackListIdx">正式trackList中原轨道的零起始下标。</param>
    void ShowTrackContextMenu(int trackListIdx){
        if(trackListIdx<0||trackListIdx>=trackList.Count) return;
        // 当前原轨道布局项引用。
        var trk=trackList[trackListIdx]; 
        // 原轨道布局的零起始下标，用于明确菜单调整。
        int ti=trackListIdx;
        // 当前轨道完整绘制及编辑回调定义。
        var def=GetDef(BuiltinTrackRegistry.Canonical(trk.type));
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        if(def!=null){
            if(def.addEvent!=null)
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Add {def.itemName} at playhead", $"在播放头添加{def.itemName}")),false,()=>def.addEvent());
            // 当前选择是否匹配此轨道且下标有效。
            bool hasSel=selType==def.selType&&selIdx>=0&&def.listCount!=null&&selIdx<def.listCount();
            if(hasSel){
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Copy selected {def.itemName}", $"复制选中的{def.itemName}")),false,CopySelected);
                if(def.removeAt!=null){
                    // 菜单创建时捕获的原选择下标。
                    int si=selIdx; 
                    // 当前轨道删除事件的原回调引用。
                    var rm=def.removeAt;
                    menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Remove selected {def.itemName}", $"移除选中的{def.itemName}")),false,()=>{
                        rm(si); selType=SelType.None; selIdx=-1; });
                }
            }
            if(CanPasteToTrack(trk.type))
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Paste {GetClipboardLabel()} at frame {frameSelectIndex}", $"在第{frameSelectIndex}帧粘贴{GetClipboardLabel()}")),false,PasteClipboard);
        }
        menu.AddSeparator("");
        if(ti>0) menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Move track up", "上移轨道")),false,()=>{ PushUndo(); 
        // 交换布局顺序时暂存的原轨道对象引用。
        var tmp=trackList[ti]; trackList[ti]=trackList[ti-1]; trackList[ti-1]=tmp; });
        else menu.AddDisabledItem(new GUIContent(SkillEditorLocalization.Text("Move track up", "上移轨道")));
        if(ti<trackList.Count-1) menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Move track down", "下移轨道")),false,()=>{ PushUndo(); 
        // 交换布局顺序时暂存的原轨道对象引用。
        var tmp=trackList[ti]; trackList[ti]=trackList[ti+1]; trackList[ti+1]=tmp; });
        else menu.AddDisabledItem(new GUIContent(SkillEditorLocalization.Text("Move track down", "下移轨道")));
        menu.AddSeparator("");
        if(def!=null && def.count!=null && def.count()>0) {
            menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Collapse track", "折叠轨道")),false,()=>{PushUndo();
                // 轨道列表可按索引访问，折叠仍修改原布局对象。
                for (int trackIndex = 0; trackIndex < trackList.Count; trackIndex++)
                    if (BuiltinTrackRegistry.Canonical(trackList[trackIndex].type) == def.type) trackList[trackIndex].expanded = false;});
            menu.ShowAsContext(); return;
        }
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Hide track", "隐藏轨道")),false,()=>{
            if(EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Hide track", "隐藏轨道"),SkillEditorLocalization.Text($"Hide the {GetTrackDisplayName(trk.type)} track?\nEvents are preserved and remain active at runtime. Add the track again to edit them.", $"隐藏{GetTrackDisplayName(trk.type)}轨道？\n事件会保留且在运行时继续生效。再次添加轨道即可编辑。"),SkillEditorLocalization.Text("Remove", "移除"),SkillEditorLocalization.Text("Cancel", "取消"))){
                PushUndo();
                if(BuiltinTrackRegistry.Canonical(trk.type)==Global.TrackType.Fx) trackList.RemoveAll(/* 当前原轨道布局条目。 */ t=>BuiltinTrackRegistry.Canonical(t.type)==Global.TrackType.Fx);
                else trackList.RemoveAt(ti);
            }
        });
        menu.ShowAsContext();
    }
    #endregion

    #region 标尺与网格
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <param name="totalF">当前轨道合并后的时间轴总帧数。</param>
    /// <summary>根据横向缩放绘制帧标尺及主次刻度，标签显示零起始配置帧号。</summary>
    void DrawRuler(Rect r,int totalF){
        EditorGUI.DrawRect(r,new Color(.15f,.15f,.15f));
        // 标尺次刻度间隔帧数，随每帧像素选择。
        int iv=pixelsPerFrame<6?10:pixelsPerFrame<12?5:pixelsPerFrame<20?2:1;
        // 当前零起始时间轴帧，用于采样或区间检测。
        for(int f=0;f<=totalF;f++){
            // 当前横向绘制位置，单位窗口像素。
            float x=f*pixelsPerFrame; if(x>r.width) break;
            // 当前帧是否为五个次刻度组成的主刻度。
            bool maj=f%(iv*5)==0; 
            // 当前帧是否为次刻度。
            bool mid=f%iv==0;
            if(maj){ EditorGUI.DrawRect(new Rect(r.x+x,r.yMax-12,1,12),new Color(1,1,1,.5f));
                // 主刻度的原整数帧号显示文字。
                string lb=f.ToString();
                GUI.Label(new Rect(r.x+x+2,r.y+1,50,r.height),lb,TSty.Ruler); }
            else if(mid) EditorGUI.DrawRect(new Rect(r.x+x,r.yMax-6,1,6),new Color(1,1,1,.25f));
        }
        EditorGUI.DrawRect(new Rect(r.x,r.yMax-1,r.width,1),new Color(0,0,0,.6f));
    }
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <param name="totalF">当前轨道合并后的时间轴总帧数。</param>
    /// <summary>根据每帧像素和主刻度步长绘制时间轴网格，不改变事件帧号。</summary>
    void DrawGrid(Rect r,int totalF){
        // 标尺次刻度间隔帧数，随每帧像素选择。
        int iv=pixelsPerFrame<6?10:pixelsPerFrame<12?5:pixelsPerFrame<20?2:1;
        // 当前零起始时间轴帧，用于采样或区间检测。
        for(int f=0;f<=totalF;f+=iv){
            // 当前横向绘制位置，单位窗口像素。
            float x=f*pixelsPerFrame; if(x>r.width) break;
            EditorGUI.DrawRect(new Rect(r.x+x,r.y,1,r.height),f%(iv*5)==0?C_GRIDM:C_GRID);
        }
    }
    /// <param name="area">输入或绘图允许的窗口像素区域。</param>
    /// <param name="totalF">当前轨道合并后的时间轴总帧数。</param>
    /// <summary>以选中帧乘每帧像素绘制播放头及顶部指示，不修改选择帧。</summary>
    void DrawPlayhead(Rect area,int totalF){
        // 当前横向绘制位置，单位窗口像素。
        float x=frameSelectIndex*pixelsPerFrame;
        EditorGUI.DrawRect(new Rect(x-1,0,PLAYHEAD_WIDTH,area.height),C_HEAD);
        EditorGUI.DrawRect(new Rect(x-5,0,10,8),C_HEAD);
    }
    #endregion

    #region 轨道绘制
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <param name="lanes">原事件下标到非重叠显示行号的映射。</param>
    /// <param name="laneCount">当前轨道实际显示行数，至少一行。</param>
    /// <summary>按配置帧范围绘制动画段、融合区和裁剪手柄，并处理剪辑拖入入口。</summary>
    void DrawAnimTrackMultiSeg(Rect r,Dictionary<int,int> lanes,int laneCount){
        // 当前轨道每行实际像素高度。
        float laneH=laneCount>0?r.height/laneCount:r.height;
        // 菜单创建时捕获的原选择下标。
        for(int si=0;si<animSegments.Count;si++){
            // 当前原动画段引用，裁剪参数使用源剪辑帧。
            var seg=animSegments[si]; if(seg.clip==null) continue;
            // 当前事件占用的零起始显示行。
            int lane=lanes.ContainsKey(si)?lanes[si]:0;
            // 源动画剪辑完整长度的整数帧数。
            int totalClipF=Mathf.Max(1,(int)(seg.clip.length*seg.clip.frameRate));
            // 限制到完整源剪辑范围内的起始帧。
            int cs=Mathf.Clamp(seg.clipStartFrame,0,totalClipF);
            // 限制到完整源剪辑范围内的结束帧。
            int ce=seg.clipEndFrame>0?Mathf.Clamp(seg.clipEndFrame,cs,totalClipF):totalClipF;
            // 当前粒子持续时间加最长尾粒寿命，单位秒。
            int dur=ActionTiming.Duration(configFile,seg); 
            // xS：动画段开始帧对应的横向像素位置。；xE：动画段有效末尾对应的横向像素位置。
            float xS=seg.startFrame*pixelsPerFrame, xE=(seg.startFrame+dur)*pixelsPerFrame;
            // 当前动画段块的像素矩形。
            Rect cr=new Rect(r.x+xS,r.y+lane*laneH+2,xE-xS,laneH-4);
            // 当前绘制块是否为选中项，仅影响高亮。
            bool sel=selType==SelType.AnimSegment&&selIdx==si;
            EditorGUI.DrawRect(cr,sel?Color.Lerp(C_ANIM,Color.white,.25f):C_ANIM);
            if(sel) DrawOutline(cr,Color.white);

            // ── 绘制融合区域可视化 ──
            if(seg.blendInFrames>0){
                // 混合帧数乘缩放得到的像素宽度。
                float blendPx=seg.blendInFrames*pixelsPerFrame;
                blendPx=Mathf.Min(blendPx,cr.width); // 不超过片段本身宽度
                // 融合渐变区域（左侧渐变色块）
                Rect blendRect=new Rect(cr.x,cr.y,blendPx,cr.height);
                // 绘制多条渐变条纹模拟渐变效果
                Color blendColor=new Color(0.2f,0.8f,1f,0.35f);
                // 混合区渐变条带数量，至少三条。
                int stripes=Mathf.Max(3,Mathf.FloorToInt(blendPx/2f));
                // 当前混合渐变条带序号，零起始。
                for(int s=0;s<stripes;s++){
                    // 当前渐变条带归一化进度，范围零到一。
                    float t=(float)s/stripes;
                    // 当前渐变条带的透明度，不影响实际混合权重。
                    float alpha=Mathf.Lerp(0.45f,0.05f,t);
                    // 当前渐变条带的横向像素位置。
                    float sx=blendRect.x+t*blendRect.width;
                    // 渐变条带的像素宽度，留一像素防缝隙。
                    float sw=blendRect.width/stripes+1f;
                    EditorGUI.DrawRect(new Rect(sx,blendRect.y,sw,blendRect.height),new Color(0.2f,0.8f,1f,alpha));
                }
                // 融合区域右边界分割线
                EditorGUI.DrawRect(new Rect(cr.x+blendPx-1f,cr.y,2f,cr.height),new Color(0.2f,0.8f,1f,0.7f));
                // 融合帧数标签
                GUI.Label(new Rect(cr.x+2,cr.y+cr.height-13,blendPx-4,12),SkillEditorLocalization.Text($"{seg.blendInFrames}f", $"{seg.blendInFrames}帧"),EditorStyles.miniLabel);
            }

            // 动画原名保留，仅帧区间和秒单位按语言展示。
            string lb=SkillEditorLocalization.Text($"{seg.clip.name} ({cs}-{ce}, {(ActionTiming.SourceEnd(seg)-ActionTiming.SourceStart(seg)):F2}s)", $"{seg.clip.name} ({cs}-{ce}, {(ActionTiming.SourceEnd(seg)-ActionTiming.SourceStart(seg)):F2}秒)");
            GUI.Label(new Rect(cr.x+4,cr.y+1,cr.width-8,cr.height),lb,TSty.Clip);
            // 边缘手柄
            const float EW=6f;
            // le：源动画开始裁剪边的像素点击范围。；re：源动画末尾裁剪边的像素点击范围。
            Rect le=new Rect(cr.x-EW/2,cr.y,EW,cr.height), re=new Rect(cr.xMax-EW/2,cr.y,EW,cr.height);
            EditorGUI.DrawRect(new Rect(cr.x,cr.y,2,cr.height),new Color(1,1,1,.4f));
            EditorGUI.DrawRect(new Rect(cr.xMax-2,cr.y,2,cr.height),new Color(1,1,1,.4f));
            EditorGUIUtility.AddCursorRect(le,MouseCursor.SplitResizeLeftRight);
            EditorGUIUtility.AddCursorRect(re,MouseCursor.SplitResizeLeftRight);
            // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0){
                if(le.Contains(e.mousePosition)){ isDraggingSegEdge=true; segEdgeIdx=si; segEdgeSide=0;
                    segOrigClipStart=cs; segOrigClipEnd=ce; segOrigTimelineStart=seg.startFrame; e.Use(); }
                else if(re.Contains(e.mousePosition)){ isDraggingSegEdge=true; segEdgeIdx=si; segEdgeSide=1;
                    segOrigClipStart=cs; segOrigClipEnd=ce; segOrigTimelineStart=seg.startFrame; e.Use(); }
                else if(cr.Contains(e.mousePosition)){
                    selType=SelType.AnimSegment; selIdx=si;
                    isDraggingSegment=true; _segmentGrabFrame=(e.mousePosition.x-r.x)/pixelsPerFrame-seg.startFrame; dragSegIdx=si; segOrigTimelineStart=seg.startFrame; e.Use(); }
            }
            if(e.type==EventType.ContextClick&&cr.Contains(e.mousePosition)){
                // 当前原事件列表的零起始下标。
                int idx=si; 
                // 本次鼠标操作的上下文菜单，点击回调才修改配置。
                var menu=new GenericMenu();
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Copy", "复制")),false,()=>{ selType=SelType.AnimSegment; selIdx=idx; CopySelected(); });
                if(HasClipboard&&_clipboardType==SelType.AnimSegment) menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Paste {GetClipboardLabel()} at frame {frameSelectIndex}", $"在第{frameSelectIndex}帧粘贴{GetClipboardLabel()}")),false,PasteClipboard);
                else menu.AddDisabledItem(new GUIContent(SkillEditorLocalization.Text("Paste", "粘贴")));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Remove clip [{seg.clip.name}]", $"移除剪辑 [{seg.clip.name}]")),false,()=>{ RemoveEvent(animSegments,idx); if(selType==SelType.AnimSegment){selType=SelType.None;selIdx=-1;} });
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Reset source trim", "重置源动画裁剪")),false,()=>{ PushUndo(); animSegments[idx].clipStartFrame=0; animSegments[idx].clipEndFrame=0; });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Blend over 5 frames", "以5帧混合")),false,()=>{ AutoSetBlendFrames(idx,5); });
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Blend over 8 frames", "以8帧混合")),false,()=>{ AutoSetBlendFrames(idx,8); });
                menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Clear blend", "清除混合")),false,()=>{ PushUndo(); animSegments[idx].blendInFrames=0; });
                menu.ShowAsContext(); e.Use(); }
        }
        if(animSegments.Count==0) GUI.Label(new Rect(r.x+10,r.y+4,300,16),SkillEditorLocalization.Text("Use Add or drop an AnimationClip here.", "点击添加或把动画剪辑拖到此处。"),TSty.Item);
        HandleAnimClipDrop(r);
    }
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <summary>把拖入的动画资源解析为剪辑段并加入原动画列表，保留 Undo 事务。</summary>
    void HandleAnimClipDrop(Rect r){
        // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
        var e=Event.current;
        if((e.type==EventType.DragUpdated||e.type==EventType.DragPerform)&&r.Contains(e.mousePosition)){
            // 当前拖入资源集合是否包含可用 AnimationClip。
            bool has=false; 
            // 当前正在检查重叠的其他原事件引用。
            foreach(var o in DragAndDrop.objectReferences) if(o is AnimationClip){has=true;break;}
            if(has){ DragAndDrop.visualMode=DragAndDropVisualMode.Copy;
                if(e.type==EventType.DragPerform){ DragAndDrop.AcceptDrag();
                    PushUndo();
                    // 拖入首段剪辑的非负时间轴起始帧。
                    int df=Mathf.Max(0,Mathf.RoundToInt((e.mousePosition.x-r.x)/pixelsPerFrame));
                    // 原动画段列表中的引用，裁剪单位为源剪辑帧。
                    foreach(var o in DragAndDrop.objectReferences){ if(o is AnimationClip /* 真实动画剪辑子资源引用，不修改源资源。 */ c){
                        // 当前原动画段引用，裁剪参数使用源剪辑帧。
                        var seg=new Global.AnimClipSegment{clip=c,startFrame=df}; animSegments.Add(seg); df+=ActionTiming.Duration(configFile,seg); } }
                } e.Use(); }
        }
    }

    /// <summary>为指定索引的 segment 设置融合帧数（仅对排序后非第一段的 segment 有效）</summary>
    /// <param name="idx">当前原事件列表的零起始下标。</param>
    /// <param name="blendFrames">要求设置的混合帧数，单位时间轴帧。</param>
    void AutoSetBlendFrames(int idx,int blendFrames){
        if(idx<0||idx>=animSegments.Count) return;
        PushUndo();
        // 当前原动画段引用，裁剪参数使用源剪辑帧。
        var seg=animSegments[idx];
        // 当前段按开始帧排序的零起始位置。
        int sortedIdx=GetSortedSegIndex(seg);
        if(sortedIdx<=0){ seg.blendInFrames=0; return; } // 第一段不融合
        // 本段时长与二十帧上限中的较小值。
        int maxBlend=Mathf.Min(ActionTiming.Duration(configFile,seg),20);
        seg.blendInFrames=Mathf.Clamp(blendFrames,0,maxBlend);
    }

    /// <summary>
    /// 一键自动优化所有动画片段的拼接：
    /// 1. 按 startFrame 排序
    /// 2. 自动消除片段间的间隙（前一段结束帧=后一段起始帧）
    /// 3. 为非第一段自动设置融合帧数
    /// </summary>
    /// <param name="defaultBlendFrames">自动排列时使用的默认混合帧数，默认五帧。</param>
    void AutoOptimizeAllSegments(int defaultBlendFrames=5){
        if(animSegments.Count<2) return;
        PushUndo();
        // 按 startFrame 排序
        animSegments.Sort((/* 比较的第一个原动画段，按起始帧排序。 */ a,/* 比较的第二个原动画段，按起始帧排序。 */ b)=>a.startFrame.CompareTo(b.startFrame));
        // 消除间隙 + 设置融合
        for(int i=1;i<animSegments.Count;i++){
            // 排序后的前一个原动画段，提供衔接结束帧及融合长度限制。
            var prev=animSegments[i-1]; 
            // 当前待排列的原动画段，按前段结束帧更新开始帧和融合帧数。
            var cur=animSegments[i];
            // 让后一段紧贴前一段（消除间隙或重叠）
            int prevEnd=prev.startFrame+ActionTiming.Duration(configFile,prev);
            if(cur.startFrame!=prevEnd) cur.startFrame=prevEnd;
            // 融合上限取当前段与前段各半时长的较小值，再限制为至少一帧。
            int maxBlend=Mathf.Min(ActionTiming.Duration(configFile,cur)/2,ActionTiming.Duration(configFile,prev)/2);
            maxBlend=Mathf.Max(maxBlend,1);
            cur.blendInFrames=Mathf.Clamp(defaultBlendFrames,0,maxBlend);
        }
        // 第一段不融合
        animSegments[0].blendInFrames=0;
    }

    /// <summary>优化拼接弹出菜单</summary>
    /// <summary>显示动画片段排列与混合帧操作，语言不改变原混合参数或用户剪辑。</summary>
    void ShowOptimizeMenu(){
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Arrange / Short blend (3 frames)", "排列 / 短混合（3帧）")),false,()=>{ AutoOptimizeAllSegments(3); Repaint(); });
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Arrange / Medium blend (5 frames)", "排列 / 中混合（5帧）")),false,()=>{ AutoOptimizeAllSegments(5); Repaint(); });
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Arrange / Long blend (8 frames)", "排列 / 长混合（8帧）")),false,()=>{ AutoOptimizeAllSegments(8); Repaint(); });
        menu.AddSeparator("");
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Close gaps without blending", "衔接间隙，不使用混合")),false,()=>{ AutoOptimizeAllSegments(0); Repaint(); });
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Clear all blends", "清除全部混合")),false,()=>{ PushUndo();
            // 原片段引用按下标清除混合帧，未改变菜单行为。
            for (int segmentIndex = 0; segmentIndex < animSegments.Count; segmentIndex++) animSegments[segmentIndex].blendInFrames = 0;
            Repaint(); });
        menu.ShowAsContext();
    }

    // ═══════════════════════════════════════════════════════════
    //  通用事件轨道绘制 — 替代原先 10 个几乎完全相同的 DrawXxxTrackMultiLane
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 绘制一条 lane 式事件轨道：矩形块 + 标签 + 选中/拖拽/右键菜单。
    /// </summary>
    /// <param name="getLabel">(item, begin, end) → 块内文字</param>
    /// <param name="filter">仅绘制满足条件的条目（特效/音效共用一个列表时用到）</param>
    /// <param name="endExclusive">true=结束帧不占格（连招窗口），false=结束帧占满一格</param>
    /// <param name="alwaysLabel">true=无论缩放都画标签</param>
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <param name="lanes">原事件下标到非重叠显示行号的映射。</param>
    /// <param name="laneCount">当前轨道实际显示行数，至少一行。</param>
    /// <param name="list">可按下标访问的原事件列表，编辑对象仍为原引用。</param>
    /// <param name="def">当前轨道完整绘制及编辑回调定义。</param>
    /// <param name="getBegin">读取原事件起始帧的强类型委托。</param>
    /// <param name="getEnd">读取原事件结束帧的强类型委托。</param>
    void DrawEventTrack<T>(Rect r,Dictionary<int,int> lanes,int laneCount,
        List<T> list,TrackDef def,
        Func<T,int> getBegin,Func<T,int> getEnd,Func<T,int,int,string> getLabel,
        Func<T,bool> filter=null,bool endExclusive=false,bool alwaysLabel=false){
        // 当前轨道每行实际像素高度。
        float laneH=r.height;
        if(laneCount>0) laneH=r.height/laneCount;
        // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
        var e=Event.current;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<list.Count;i++){
            // 当前原轨道事件引用，委托读取帧范围；编辑仍修改原列表对象。
            var item=list[i];
            if(filter!=null&&!filter(item)) continue;
            // 当前事件占用的零起始显示行。
            int lane; if(!lanes.TryGetValue(i,out lane)) lane=0;
            // begin：事件区间起始帧，零起始。；end：当前事件区间的有效末尾帧，用于范围限制。
            int begin=getBegin(item),end=getEnd(item);
            // xs：事件开始帧对应的横向像素位置。；xe：事件区间末尾对应的横向像素位置。
            float xs=begin*pixelsPerFrame, xe=(end+1)*pixelsPerFrame;
            if(endExclusive) xe=end*pixelsPerFrame;
            // 当前事件块的像素矩形，最少留八像素可选范围。
            Rect ir=new Rect(r.x+xs,r.y+lane*laneH+2,Mathf.Max(xe-xs,8),laneH-4);
            // 当前绘制块是否为选中项，仅影响高亮。
            bool sel=selType==def.selType&&selIdx==i;
            // 选中状态仅调整原事件块的颜色，不改变标记与拖动命中宽度。
            Color eventColor=def.color;
            if(sel) eventColor=Color.Lerp(def.color,Color.white,.3f);
            EditorGUI.DrawRect(ir,eventColor);
            if(sel) DrawOutline(ir,Color.white);
            if(def.type==Global.TrackType.Fx)
            {
                DrawEffectPlaybackRange(ir,fxAndSoundList[i]);
                DrawEffectTimingLabel(ir,begin,end,getLabel(item,begin,end),EffectDurationLabel(fxAndSoundList[i]),fxAndSoundList[i]);
            }
            else if(alwaysLabel||pixelsPerFrame>6)
                GUI.Label(new Rect(ir.x+2,ir.y,ir.width-4,14),getLabel(item,begin,end),TSty.Item);
            if(e.type==EventType.MouseDown&&e.button==0&&ir.Contains(e.mousePosition)){
                selType=def.selType; selIdx=i;
                SeekPreviewFrame(begin);
                StartDrag(def.selType,i,begin,end); e.Use(); }
            if(e.type==EventType.ContextClick&&ir.Contains(e.mousePosition)){
                ShowEventContextMenu(def,i,begin); e.Use(); }
        }
    }

    /// <summary>时间轴上单个 Event 的右键菜单（复制 / 粘贴 / 删除）。</summary>
    /// <param name="def">当前轨道完整绘制及编辑回调定义。</param>
    /// <param name="idx">当前原事件列表的零起始下标。</param>
    /// <param name="frame">要采样的零起始配置时间轴帧。</param>
    /// <param name="extraLabel">事件右键删除提示的额外原标签，可为空。</param>
    void ShowEventContextMenu(TrackDef def,int idx,int frame,string extraLabel=null){
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Copy", "复制")),false,()=>{ selType=def.selType; selIdx=idx; CopySelected(); });
        if(CanPasteToTrack(def.type))
            menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Paste {GetClipboardLabel()} at frame {frameSelectIndex}", $"在第{frameSelectIndex}帧粘贴{GetClipboardLabel()}")),false,PasteClipboard);
        else menu.AddDisabledItem(new GUIContent(SkillEditorLocalization.Text("Paste", "粘贴")));
        menu.AddSeparator("");
        // 删除菜单中显示的原事件名称或帧号。
        string tail=extraLabel??SkillEditorLocalization.Text($"(frame {frame})", $"（第{frame}帧）");
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text($"Remove {def.itemName} {tail}", $"移除{def.itemName} {tail}")),false,()=>{
            def.removeAt(idx); selType=SelType.None; selIdx=-1; });
        menu.ShowAsContext();
    }

    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <summary>按命中表现列表顺序绘制可选择的数据块，命中时机不由此轨道自动触发。</summary>
    void DrawHitFxTrack(Rect r){
        // 命中表现每行的像素高度。
        float itemH=TRACK_HEIGHT;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<hitFxList.Count;i++){
            // 当前原受击表现配置，不触发正式命中。
            var hf=hitFxList[i];
            // 当前事件块的像素矩形，最少留八像素可选范围。
            Rect ir=new Rect(r.x+4,r.y+i*itemH+2,r.width-8,itemH-4);
            // 当前绘制块是否为选中项，仅影响高亮。
            bool sel=selType==SelType.HitFx&&selIdx==i;
            EditorGUI.DrawRect(ir,sel?Color.Lerp(C_HITFX,Color.white,.3f):C_HITFX);
            if(sel) DrawOutline(ir,Color.white);
            // 标签保留原攻击索引，默认覆盖所有攻击。
            string atkLabel = SkillEditorLocalization.Text("All attacks", "全部攻击");
            if (hf.attackIndex >= 0) atkLabel = SkillEditorLocalization.Text($"ATK[{hf.attackIndex}]", $"攻击[{hf.attackIndex}]");
            // 原资源名称保持不翻译，仅空引用提示切换语言。
            string vfxLabel = SkillEditorLocalization.Text("No VFX", "无特效");
            if (hf.hitVfx != null) vfxLabel = hf.hitVfx.name;
            // 原命中声音的简短显示标记。
            string sndLabel=hf.hitSound!=null?"":"";
            GUI.Label(new Rect(ir.x+4,ir.y+1,ir.width-8,14),$"{atkLabel} {vfxLabel} {sndLabel}",TSty.Item);
            // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0&&ir.Contains(e.mousePosition)){
                selType=SelType.HitFx;selIdx=i; e.Use(); }
            if(e.type==EventType.ContextClick&&ir.Contains(e.mousePosition)){
                ShowEventContextMenu(GetDefBySel(SelType.HitFx),i,0,$"[{atkLabel}]"); e.Use(); }
        }
    }


    // 位移曲线峰值缓存：曲线未变时不重复 100 次 Evaluate
    float _moveCurvePeak=-1f; 
    /// <summary>旧位移曲线绘图缓存版本，曲线改变后刷新峰值。</summary>
    int _moveCurveHash;

    /// <param name="cv">旧整段位移曲线的原引用，只采样绘图。</param>
    /// <summary>缓存位移曲线采样峰值供绘图纵向缩放，曲线变化后重新计算。</summary>
    float GetMoveCurvePeak(AnimationCurve cv){
        // 曲线绘图缓存校验值，键数组变化后重新计算峰值。
        int hash=cv.keys.Length;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<cv.keys.Length;i++){
            // 当前内层遍历的零起始下标。
            var k=cv.keys[i];
            hash=hash*31+k.time.GetHashCode();
            hash=hash*31+k.value.GetHashCode();
        }
        if(_moveCurvePeak>0f&&hash==_moveCurveHash) return _moveCurvePeak;
        // 时间轴内的鼠标横坐标，单位像素。
        float mx=.01f; 
        // 曲线采样的归一化进度，范围零到一。
        for(float t=0;t<=1;t+=.01f) mx=Mathf.Max(mx,Mathf.Abs(cv.Evaluate(t)));
        _moveCurveHash=hash; _moveCurvePeak=mx;
        return mx;
    }

    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <summary>把旧整段位移曲线采样为时间轴折线；此可视化不推动角色根位置。</summary>
    void DrawMoveTrack(Rect r){
        if(configFile==null||configFile.moveCurve==null) return;
        // 当前轨道合并后的时间轴总帧数。
        int totalF=GetTotalFrames();
        // 旧整段位移曲线的原引用，只采样绘图。
        var cv=configFile.moveCurve; if(cv.keys.Length<2) return;
        // 时间轴内的鼠标横坐标，单位像素。
        float mx=GetMoveCurvePeak(cv);
        // 旧曲线显示零值所在的纵向像素位置。
        float zy=r.y+r.height*.5f;
        EditorGUI.DrawRect(new Rect(r.x,zy,totalF*pixelsPerFrame,1),new Color(1,1,1,.1f));
        // 当前原动画段引用，裁剪参数使用源剪辑帧。
        int seg=Mathf.Min(totalF*3,400); 
        // 上一个位移曲线采样点的窗口绘图坐标，用于连接折线。
        Vector3 prev=Vector3.zero;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<=seg;i++){
            // 当前原流程节点引用，帧范围由此节点定义。
            float n=(float)i/seg; 
            // 已经转换到绘制空间的顶点数组。
            float v=cv.Evaluate(n);
            // 当前横向绘制位置，单位窗口像素。
            float x=r.x+n*totalF*pixelsPerFrame;
            // 当前纵向绘制位置，单位窗口像素。
            float y=zy-(v/mx)*(r.height*.4f);
            // 旧曲线当前采样值对应的绘图点。
            var pt=new Vector3(x,y);
            if(i>0){ Handles.color=C_MOVE; Handles.DrawLine(prev,pt); }
            prev=pt;
        }
        GUI.Label(new Rect(r.x+4,r.y+2,140,14),SkillEditorLocalization.Text($"Distance: {configFile.totalMoveDistance:F1}", $"距离：{configFile.totalMoveDistance:F1}"),TSty.Item);
        if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&r.Contains(Event.current.mousePosition)){
            selType=SelType.Move;selIdx=-1; Event.current.Use(); }
    }
    #endregion

    #region 分割线
    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <summary>处理窄布局上下分隔条拖拽，限制属性面板像素高度并触发重绘。</summary>
    void DrawSplitter(Rect r){
        EditorGUI.DrawRect(r,new Color(.1f,.1f,.1f));
        EditorGUI.DrawRect(new Rect(r.x,r.y+r.height/2-.5f,r.width,1),new Color(.4f,.4f,.4f));
        EditorGUIUtility.AddCursorRect(r,MouseCursor.SplitResizeUpDown);
        if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)){ isDraggingSplitter=true; Event.current.Use(); }
        if(isDraggingSplitter){
            if(Event.current.type==EventType.MouseDrag){ inspectorHeight-=Event.current.delta.y;
                inspectorHeight=Mathf.Clamp(inspectorHeight,INSPECTOR_MIN_HEIGHT,position.height*.6f); Repaint(); Event.current.Use(); }
            if(Event.current.type==EventType.MouseUp){ isDraggingSplitter=false; Event.current.Use(); }
        }
    }

    /// <param name="r">当前绘制元素的窗口像素矩形。</param>
    /// <summary>绘制并处理宽布局的左右分隔拖拽，只调整窗口属性面板宽度。</summary>
    void DrawVerticalSplitter(Rect r){
        EditorGUI.DrawRect(r,new Color(.1f,.1f,.1f));
        EditorGUI.DrawRect(new Rect(r.x+r.width/2-.5f,r.y,1,r.height),new Color(.4f,.4f,.4f));
        EditorGUIUtility.AddCursorRect(r,MouseCursor.ResizeHorizontal);
        if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)){ isDraggingVerticalSplitter=true; Event.current.Use(); }
        if(isDraggingVerticalSplitter){
            if(Event.current.type==EventType.MouseDrag){
                inspectorWidth-=Event.current.delta.x;
                inspectorWidth=Mathf.Clamp(inspectorWidth,INSPECTOR_MIN_WIDTH,INSPECTOR_MAX_WIDTH);
                Repaint(); Event.current.Use();
            }
            if(Event.current.type==EventType.MouseUp){ isDraggingVerticalSplitter=false; Event.current.Use(); }
        }
    }
    #endregion
}
