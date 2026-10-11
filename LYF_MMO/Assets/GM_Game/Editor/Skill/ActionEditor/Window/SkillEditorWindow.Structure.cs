// 统一效果与运动显示布局、基础字段筛选及完整工具菜单。
using System.Collections.Generic;
using Ethan.ActionEditor;
using Ethan.ActionEditor.Editor;
using UnityEditor;
using UnityEngine;
/// <summary>按帧制作技能的原 Y 窗口分部；完整编辑能力与本游戏导出支持范围独立，预览使用隔离副本。</summary>
public partial class SkillEditorWindow
{
    /// <summary>把普通粒子和预警统一分配显示行时使用的帧区间。</summary>
    struct EffectSpan { 
    /// <summary>此共享效果条目是否来自预警原列表。</summary>
    public bool telegraph; 
    /// <summary>index：对应原列表的零起始下标；start：作者触发帧；end：仅显示分行使用的末帧，包含旁注宽度，绝不写回作者区间。</summary>
    public int index, start, end; }
    /// <summary>视觉效果及预警的统一帧区间显示快照。</summary>
    readonly List<EffectSpan> effectSpans = new List<EffectSpan>();
    /// <summary>合并效果条目下标到显示行号的缓存。</summary>
    readonly Dictionary<int,int> effectCombinedLanes = new Dictionary<int,int>();
    /// <summary>普通效果原下标到共享显示行号的映射。</summary>
    readonly Dictionary<int,int> effectVisualLanes = new Dictionary<int,int>();
    /// <summary>预警原下标到共享显示行号的映射。</summary>
    readonly Dictionary<int,int> effectTelegraphLanes = new Dictionary<int,int>();
    /// <summary>预警属性页索引，零为视觉、一为音频。</summary>
    int telegraphPage;

    /// <summary>每条共享特效显示行的像素高度，保留四行文字及下沿独立播放估算条。</summary>
    private const float EffectTimelineLaneHeight = 76f;

    /// <summary>资源自然单轮的只读保守估算；不用于正式时长、作者END或预览清理。</summary>
    private struct EffectDurationInfo
    {
        /// <summary>含活跃子系统延迟与尾粒的单轮保守模拟秒数，事件倍率在显示时统一换算。</summary>
        public float EstimatedSeconds;
        /// <summary>源层级是否包含循环粒子；实际本技能预览自动寿命模式仍强制单轮。</summary>
        public bool SourceLoops;
        /// <summary>源层级是否存在活跃粒子，全部失活或空资源不能伪造持续时间。</summary>
        public bool HasParticles;
        /// <summary>独立定时发射系统最早参数起点的秒估算，已除系统倍率，尚未除事件倍率。</summary>
        public float EstimatedStartSeconds;
        /// <summary>是否找到常量延迟与常量发射或爆发可定位的预计首发；不等于屏幕首次可见。</summary>
        public bool HasEmissionStart;
        /// <summary>曲线、距离发射或随机爆发可能更早触发，存在时不提供伪精确的首发定位。</summary>
        public bool EmissionStartUnknown;
    }

    /// <summary>按原粒子资源身份缓存单轮参数估算；项目资源变更或窗口关闭时清空。</summary>
    private readonly Dictionary<ParticleSystem, EffectDurationInfo> _effectDurationCache = new Dictionary<ParticleSystem, EffectDurationInfo>();

    /// <summary>释放资源只读估算缓存；不改原粒子、作者字段或正在播放的副本。</summary>
    private void ClearEffectDurationCache()
    {
        _effectDurationCache.Clear();
    }

    /// <summary>按实际生成源根内的activeSelf祖先链判断活跃；Prefab资产未入场景不使用activeInHierarchy。</summary>
    /// <param name="particle">源资源中的候选粒子组件，只读其节点及祖先的激活配置。</param>
    /// <param name="sourceRoot">本技能Instantiate的particleSystem.gameObject根，原资源外祖先不随副本生成。</param>
    /// <returns>候选位于源根内部且整条路径均激活时为真；源根本身失活仍为假。</returns>
    private bool IsEffectParticleActive(ParticleSystem particle, Transform sourceRoot)
    {
        if (particle == null || sourceRoot == null) return false;
        // 从候选向生成根逐级核对activeSelf，避免Prefab资产上下文误报整层失活。
        Transform current = particle.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) return false;
            if (current == sourceRoot) return true;
            current = current.parent;
        }
        return false;
    }

    /// <summary>首次读取原活跃粒子参数后缓存，复用预览保守算法而不逐GUI递归层级。</summary>
    /// <param name="particle">原粒子资源根，仅读取模块与子系统，不实例化或Simulate。</param>
    /// <returns>未应用事件倍率和自定义截断的单轮保守估算及源循环标记。</returns>
    private EffectDurationInfo ReadEffectDuration(ParticleSystem particle)
    {
        // 资源身份对应的缓存值，空资源保持未指定状态。
        EffectDurationInfo info;
        if (particle == null) return new EffectDurationInfo();
        if (_effectDurationCache.TryGetValue(particle, out info)) return info;
        // 最大缓存数量限制频繁切换配置产生的窗口引用积累。
        if (_effectDurationCache.Count >= 64) _effectDurationCache.Clear();
        info = new EffectDurationInfo();
        // 与现有预览范围相同，只查活跃子节点，不激活源预制体中的失活部分。
        ParticleSystem[] particles = particle.GetComponentsInChildren<ParticleSystem>(true);
        // 与SpawnPreviewFx实际Instantiate相同的源根，子节点之外的资源祖先不参与副本活跃判断。
        Transform sourceRoot = particle.transform;
        // 每次资源读取复用路径集合，避免子发射器相互引用递归不止。
        HashSet<ParticleSystem> visited = new HashSet<ParticleSystem>();
        // 失活祖先下的子发射器可能自身activeSelf为真，先将这些节点放入只读拒绝集合。
        // 完整子粒子快照的零起始下标，只检查相对生成根的激活路径。
        for (int inactiveIndex = 0; inactiveIndex < particles.Length; inactiveIndex++)
        {
            if (!IsEffectParticleActive(particles[inactiveIndex], sourceRoot)) visited.Add(particles[inactiveIndex]);
        }
        // 原粒子数组的零起始下标，资源参数只读且不修改循环标记。
        for (int index = 0; index < particles.Length; index++)
        {
            // 当前活跃粒子的原组件引用，用于单轮参数估算。
            ParticleSystem current = particles[index];
            if (!IsEffectParticleActive(current, sourceRoot)) continue;
            info.HasParticles = true;
            if (current.main.loop) info.SourceLoops = true;
            info.EstimatedSeconds = Mathf.Max(info.EstimatedSeconds, GetPreviewParticleDuration(current, visited));
            // 当前独立粒子在自身时钟中的预计首发秒数；特殊发射条件不伪造精准起点。
            float emissionStart;
            // 该系统是否存在尚不能仅由常量参数定位的发射条件。
            bool unknown;
            if (TryReadEffectEmissionStart(current, out emissionStart, out unknown))
            {
                if (!info.HasEmissionStart || emissionStart < info.EstimatedStartSeconds) info.EstimatedStartSeconds = emissionStart;
                info.HasEmissionStart = true;
            }
            info.EmissionStartUnknown |= unknown;
        }
        _effectDurationCache.Add(particle, info);
        return info;
    }

    /// <summary>读取单常量或上下界相同的双常量；真正随机范围和曲线不能当作可定位的固定参数。</summary>
    /// <param name="curve">原粒子的时间或发射数量曲线，仅检查模式与常量上下界。</param>
    /// <param name="value">可确定的固定参数值；单位由调用处区分秒、每秒粒子数或每次爆发数量。</param>
    /// <returns>原参数具有唯一固定值时为真。</returns>
    private bool TryReadEffectConstant(ParticleSystem.MinMaxCurve curve, out float value)
    {
        value = 0f;
        if (curve.mode == ParticleSystemCurveMode.Constant) { value = curve.constant; return true; }
        if (curve.mode == ParticleSystemCurveMode.TwoConstants && curve.constantMin == curve.constantMax)
        {
            value = curve.constantMax;
            return true;
        }
        return false;
    }

    /// <summary>只读定时发射参数，区分粒子生成触发与延迟后首发；随机/曲线和距离发射不能保证首发时刻。</summary>
    /// <param name="particle">活跃源系统，不模拟、不修改发射或寿命。</param>
    /// <param name="seconds">常量延迟加最早定时爆发或连续发射起点，单位时间轴秒（未应用事件倍率）。</param>
    /// <param name="unknown">存在依赖曲线、距离或随机概率的发射条件时为真。</param>
    /// <returns>找到定时发射的常量起点时为真，仍只是参数估算而非渲染可见测量。</returns>
    private bool TryReadEffectEmissionStart(ParticleSystem particle, out float seconds, out bool unknown)
    {
        seconds = 0f;
        unknown = false;
        // 系统原发射模块，关闭发射的容器不能冒充立即可见的粒子。
        ParticleSystem.EmissionModule emission = particle.emission;
        if (!emission.enabled) return false;
        // 系统启动延迟仅在常量模式下有可定位的参数起点。
        ParticleSystem.MainModule main = particle.main;
        // 固定启动延迟秒数，双常量同值与单常量等价。
        float delay;
        if (!TryReadEffectConstant(main.startDelay, out delay)) { unknown = true; return false; }
        // 当前系统最早定时发射点，正无穷表示还没有定时贡献。
        float earliest = float.PositiveInfinity;
        // 每秒发射数量，只在固定参数下推导启动发射边界。
        float timeRate;
        if (TryReadEffectConstant(emission.rateOverTime, out timeRate))
        {
            if (timeRate > 0f) earliest = 0f;
        }
        else unknown = true;
        // 按位移发射不能从静态时间参数定位，明确保留未知首发状态。
        float distanceRate;
        if (!TryReadEffectConstant(emission.rateOverDistance, out distanceRate) || distanceRate > 0f) unknown = true;
        // 原定时爆发的零起始下标，不改变次数、随机概率或资源数据。
        for (int index = 0; index < emission.burstCount; index++)
        {
            // 当前原爆发参数；非必然或随机数量不声称可以精确定位首发。
            ParticleSystem.Burst burst = emission.GetBurst(index);
            if (burst.probability <= 0f) continue;
            // 固定爆发粒子数量，上下界相同的双常量也表示无随机数量。
            float burstCount;
            if (!TryReadEffectConstant(burst.count, out burstCount) || burst.probability < 1f) { unknown = true; continue; }
            if (burstCount > 0f) earliest = Mathf.Min(earliest, burst.time);
        }
        if (float.IsPositiveInfinity(earliest)) return false;
        seconds = (Mathf.Max(0f, delay) + Mathf.Max(0f, earliest)) / Mathf.Max(0.01f, main.simulationSpeed);
        return true;
    }

    /// <summary>将独立特效参数投影成绝对时间轴帧，只用于显示和暂停导航，不修改作者结束帧。</summary>
    /// <param name="effect">原特效事件，事件倍率与自定义秒数即时读取。</param>
    /// <param name="firstFrame">预计首发帧，向上量化；无法可靠定位时为负一。</param>
    /// <param name="tailFrame">估算尾端或自定义截断的向上量化帧，空资源返回触发帧。</param>
    /// <returns>源包含活跃粒子且有可显示持续范围时为真。</returns>
    private bool TryGetEffectPreviewRange(Global.FxAndSound effect, out int firstFrame, out int tailFrame)
    {
        firstFrame = -1;
        tailFrame = 0;
        if (effect == null) return false;
        tailFrame = effect.keyNumber;
        // 原资源的只读估算缓存，倍率变化不需要重扫粒子层级。
        EffectDurationInfo info = ReadEffectDuration(effect.particleSystem);
        if (!info.HasParticles) return false;
        // 事件模拟倍率一次作用于粒子时间，自定义寿命使用时间轴秒不随倍率变化。
        float speed = Mathf.Max(0.01f, effect.playbackSpeed);
        // 当前帧率，全部导航点量化到同一制作时钟。
        float fps = GetPreviewFrameRate();
        // 参数尾端不包含副本清理半秒宽限，也不当作实际可见尾端。
        float duration = info.EstimatedSeconds / speed;
        if (effect.customLifetime > 0f) duration = effect.customLifetime;
        tailFrame = effect.keyNumber + Mathf.CeilToInt(duration * fps);
        if (info.HasEmissionStart && !info.EmissionStartUnknown)
        {
            firstFrame = effect.keyNumber + Mathf.CeilToInt(info.EstimatedStartSeconds / speed * fps);
            if (firstFrame > tailFrame) firstFrame = -1;
        }
        return true;
    }

    /// <summary>显示独立特效的绝对预计首发与估算尾端，明确参数估算不能代表首个可见像素。</summary>
    /// <param name="effect">原特效事件，仅用于只读帧秒换算。</param>
    /// <returns>当前语言的预计起末帧秒摘要，未知首发不输出伪精确时刻。</returns>
    private string EffectRangeLabel(Global.FxAndSound effect)
    {
        // 绝对预计首发和尾端帧，均按当前FPS向上量化。
        int firstFrame, tailFrame;
        if (!TryGetEffectPreviewRange(effect, out firstFrame, out tailFrame)) return SkillEditorLocalization.Text("Preview range: unavailable", "预览范围：无活跃粒子");
        // 绝对帧转秒仍沿现有预览时钟，不能使用粒子原生采样率。
        float fps = GetPreviewFrameRate();
        // 自定义寿命是截断标记，自动寿命则只能声称保守估算尾端。
        string tailLabel = SkillEditorLocalization.Text("estimated end", "估算结束");
        if (effect.customLifetime > 0f) tailLabel = SkillEditorLocalization.Text("custom cutoff", "自定义截断");
        if (firstFrame < 0)
            return SkillEditorLocalization.Text($"Emission start uncertain | {tailLabel} F{tailFrame} ({tailFrame / fps:0.000}s)",
                $"首发起点不确定 | {tailLabel} F{tailFrame}（{tailFrame / fps:0.000}秒）");
        return SkillEditorLocalization.Text($"Estimated emission F{firstFrame} ({firstFrame / fps:0.000}s) → {tailLabel} F{tailFrame} ({tailFrame / fps:0.000}s)",
            $"预计发射 F{firstFrame}（{firstFrame / fps:0.000}秒）→ {tailLabel} F{tailFrame}（{tailFrame / fps:0.000}秒）");
    }

    /// <summary>区分技能单轮估算、源循环和自定义截断，不能将作者结束帧当作粒子自然时长。</summary>
    /// <param name="effect">原特效事件，当前速度及自定义寿命即时读取，不修改作者数据。</param>
    /// <returns>当前语言的持续时间说明；空或失活资源给出明确状态。</returns>
    private string EffectDurationLabel(Global.FxAndSound effect)
    {
        if (effect == null || effect.particleSystem == null)
            return SkillEditorLocalization.Text("Duration: no effect assigned", "持续：未指定特效");
        // 缓存仅存资源参数，用户修改倍率或自定义秒数后显示立即更新。
        EffectDurationInfo info = ReadEffectDuration(effect.particleSystem);
        if (!info.HasParticles) return SkillEditorLocalization.Text("Duration: no active particles", "持续：无活跃粒子");
        if (effect.customLifetime > 0f)
        {
            if (info.SourceLoops)
                return SkillEditorLocalization.Text($"Source loops; custom cutoff: {effect.customLifetime:0.000}s", $"资源循环；自定义截断：{effect.customLifetime:0.000}秒");
            return SkillEditorLocalization.Text($"Custom cutoff: {effect.customLifetime:0.000}s", $"自定义截断：{effect.customLifetime:0.000}秒");
        }
        // 与技能预览相同，只在最外层应用事件倍率一次，系统simulationSpeed已由复用算法换算。
        float seconds = info.EstimatedSeconds / Mathf.Max(0.01f, effect.playbackSpeed);
        if (info.SourceLoops)
            return SkillEditorLocalization.Text($"Source loops; skill single pass estimate: ~{seconds:0.000}s (preview parameters)",
                $"资源循环；技能单轮持续估算：约{seconds:0.000}秒（预览保守参数）");
        return SkillEditorLocalization.Text($"Single pass duration estimate: ~{seconds:0.000}s (preview parameters)", $"单轮持续估算：约{seconds:0.000}秒（预览保守参数）");
    }

    /// <summary>为特效旁注保留纯显示帧跨度，真实区间和拖动跨度仍从作者事件读取。</summary>
    /// <param name="effect">原特效事件，只读取开始帧、有效结束帧和显示文字。</param>
    /// <returns>包含文字及六像素间距的显示末帧，不参与GetTotalFrames。</returns>
    private int EffectLabelDisplayEnd(Global.FxAndSound effect)
    {
        // 作者真实有效结束帧，零或倒序末帧按原点事件处理。
        int end = SegEnd(effect.keyNumber, effect.endKeyNumber);
        // 完整四行旁注的像素宽度，按当前缩放换算为纯显示帧数。
        float width = EffectLabelWidth(effect.keyNumber, end, EffectTrackLabel(effect), EffectDurationLabel(effect));
        width = Mathf.Max(width, TSty.Item.CalcSize(new GUIContent(EffectRangeLabel(effect))).x + 8f);
        // 参数播放区间参与显示分行，相邻特效即使文本不相撞，真实重叠区间也不会叠在同一行。
        int firstFrame, tailFrame;
        TryGetEffectPreviewRange(effect, out firstFrame, out tailFrame);
        return Mathf.Max(Mathf.Max(end + 1, tailFrame + 1), effect.keyNumber + Mathf.CeilToInt((width + 6f) / Mathf.Max(0.01f, pixelsPerFrame)));
    }

    /// <summary>保证最后一个特效完整旁注及估算区间能横向滚动看到，纯显示尾段不移动作者END。</summary>
    /// <returns>展开特效行的最大旁注末帧，轨道隐藏时返回零。</returns>
    private int EffectTimelineDisplayEnd()
    {
        // 当前制作轨道快照，只检查特效展开状态，不改变布局资产。
        List<TrkInfo> tracks = BuildTracks();
        // 当前显示轨道的零起始下标，找到展开特效后只读已有显示区间。
        for (int index = 0; index < tracks.Count; index++)
        {
            if (tracks[index].def.type != Global.TrackType.Fx || !tracks[index].expanded) continue;
            // 特效和预警共享显示分行后的最大末尾帧，含旁注所需宽度。
            int maximum = 0;
            // 纯显示快照的零起始下标，保持原事件引用与作者帧不变。
            for (int spanIndex = 0; spanIndex < effectSpans.Count; spanIndex++) maximum = Mathf.Max(maximum, effectSpans[spanIndex].end);
            return maximum;
        }
        return 0;
    }

    /// <summary>把粒子和预警区间统一分配到不重叠行，计算共享特效轨道显示高度。</summary>
    float UnifiedEffectHeight()
    {
        effectSpans.Clear(); effectVisualLanes.Clear(); effectTelegraphLanes.Clear();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<fxAndSoundList.Count;i++) { 
        // 原共享粒子/音频列表中的事件引用。
        var f=fxAndSoundList[i]; if(BuiltinTrackRegistry.IsVisibleInFxTrack(f))
            effectSpans.Add(new EffectSpan {index=i,start=f.keyNumber,end=EffectLabelDisplayEnd(f)}); }
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<warningCueList.Count;i++) { 
        // 当前原预警事件，读取帧范围参与共享效果行分配。
        var w=warningCueList[i]; if(w!=null)
            effectSpans.Add(new EffectSpan {telegraph=true,index=i,start=w.keyNumber,end=SegEnd(w.keyNumber,w.endKeyNumber)}); }
        effectSpans.Sort((/* 比较的第一个显示条目，保持原类别及开始帧。 */ a,/* 比较的第二个显示条目，保持原类别及开始帧。 */ b)=>a.start.CompareTo(b.start));
        AssignLanes(effectCombinedLanes,effectSpans,/* 待读取起始或末尾帧的合并效果显示区间。 */ s=>s.start,/* 待读取起始或末尾帧的合并效果显示区间。 */ s=>s.end);
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<effectSpans.Count;i++) {
            // 原区间的帧跨度，平移时保持这一长度。
            var span=effectSpans[i];
            if (span.telegraph) effectTelegraphLanes[span.index]=effectCombinedLanes[i];
            else effectVisualLanes[span.index]=effectCombinedLanes[i];
        }
        return EffectTimelineLaneHeight*LaneCount(effectCombinedLanes);
    }
    /// <param name="rect">本次绘制的窗口像素范围，不写入配置。</param>
    /// <summary>把普通效果和预警共享到同一组视觉行，原两列表和类型保持独立。</summary>
    void DrawUnifiedEffectTrack(Rect rect)
    {
        UnifiedEffectHeight(); 
        // 共享效果轨道需要的非重叠显示行数。
        int count=LaneCount(effectCombinedLanes);
        GetDef(Global.TrackType.Fx).drawLanes(rect,effectVisualLanes,count);
        GetDef(Global.TrackType.Warning).drawLanes(rect,effectTelegraphLanes,count);
    }
    /// <summary>显示预警事件的本地化视觉/音频属性页，原序列化字段身份保持。</summary>
    /// <param name="index">预警原列表中的零起始下标。</param>
    void DrawTelegraphInspector(int index)
    {
        if(_serializedConfig==null || index<0 || index>=warningCueList.Count) return;
        _serializedConfig.Update();
        // 原预警集合与本次选中元素，不使用翻译标签查找字段。
        var list=_serializedConfig.FindProperty("warningCueList");
        // 选中事件的原序列化属性。
        var item=list.GetArrayElementAtIndex(index);
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Effect / Telegraph", "特效 / 预警"),EditorStyles.boldLabel);
        if(GUILayout.Button(SkillEditorLocalization.Text("Remove", "移除"))) { list.DeleteArrayElementAtIndex(index); ApplyInspectorProperties(); selType=SelType.None; selIdx=-1; return; }
        CollisionFields(item,"keyNumber","endKeyNumber");
        telegraphPage=GUILayout.Toolbar(telegraphPage,new[]{SkillEditorLocalization.Text("Visual", "视觉"),SkillEditorLocalization.Text("Audio", "音频")});
        if(telegraphPage==0) {
            EditorGUILayout.PropertyField(item.FindPropertyRelative("warningVfx"),new GUIContent(SkillEditorLocalization.Text("Visual effect", "视觉特效")));
            if(item.FindPropertyRelative("warningVfx").objectReferenceValue!=null) CollisionFields(item,"vfxOffset","vfxRotation","vfxScale");
        } else EditorGUILayout.PropertyField(item.FindPropertyRelative("warningSound"),new GUIContent(SkillEditorLocalization.Text("Audio clip", "音频剪辑")));
        ApplyInspectorProperties();
    }
    /// <summary>位移原事件下标到显示行号的映射。</summary>
    readonly Dictionary<int,int> motionLanes=new Dictionary<int,int>();
    /// <summary>朝向原事件下标到显示行号的映射。</summary>
    readonly Dictionary<int,int> facingLanes=new Dictionary<int,int>();
    /// <summary>showAdvanced：属性面板是否显示完整高级创作字段，不改变配置内容。；showValidation：是否展开原配置验证结果，不触发资产保存。</summary>
    bool showAdvanced,showValidation;
    /// <summary>将已知原序列化列表名映射为属性页标题，不改列表键。</summary>
    /// <param name="list">原事件集合的序列化字段名。</param>
    /// <returns>当前语言的属性页标题。</returns>
    static string SelectionTitle(string list) => list switch {
        "animSegments"=>SkillEditorLocalization.Text("Animation", "动画"), "fxList"=>SkillEditorLocalization.Text("Effect", "特效"), "cameraCues"=>SkillEditorLocalization.Text("Camera", "镜头"), "moveSegmentList"=>SkillEditorLocalization.Text("Motion / Displacement", "运动 / 位移"),
        "adjustMotionList"=>SkillEditorLocalization.Text("Motion / Facing", "运动 / 朝向"), "attackList"=>SkillEditorLocalization.Text("Collision", "碰撞判定"), "jumpList"=>SkillEditorLocalization.Text("Transition", "技能衔接"), "interactionWindows"=>SkillEditorLocalization.Text("Interaction", "交互"),
        "warningCueList"=>SkillEditorLocalization.Text("Effect / Telegraph", "特效 / 预警"), "cancelList"=>SkillEditorLocalization.Text("Cancel", "取消"), "hitFxList"=>SkillEditorLocalization.Text("Hit Effect", "命中特效"), "trailToggleList"=>SkillEditorLocalization.Text("Trail", "刀光"), _=>ObjectNames.NicifyVariableName(list) };
    /// <summary>显示字段标题，已知字段使用具体中文单位说明，其余由统一标签表处理。</summary>
    /// <param name="name">原字段名，只作为显示映射查找键。</param>
    /// <param name="fallback">第三方默认显示标题，不修改其数据路径。</param>
    /// <returns>当前语言标签。</returns>
    static string FieldLabel(string name,string fallback) => name switch {
        "keyNumber"=>SkillEditorLocalization.Text("Start frame", "开始帧"), "endKeyNumber"=>SkillEditorLocalization.Text("End frame", "结束帧"), "beginKey"=>SkillEditorLocalization.Text("Start frame", "开始帧"), "endKey"=>SkillEditorLocalization.Text("End frame (exclusive)", "结束帧（不含）"),
        "skillName"=>SkillEditorLocalization.Text("Action name", "技能名称"), "skillDescription"=>SkillEditorLocalization.Text("Description", "说明"), "ownerType"=>SkillEditorLocalization.Text("Actor profile", "角色配置"), "exitFrame"=>SkillEditorLocalization.Text("Exit frame (0 = auto)", "退出帧（0为自动）"),
        "skillCD"=>SkillEditorLocalization.Text("Cooldown", "冷却"), "particleSystem"=>SkillEditorLocalization.Text("Visual effect", "视觉特效"), "socketPath"=>SkillEditorLocalization.Text("Socket path", "挂点路径"), "placementKind"=>SkillEditorLocalization.Text("Placement kind", "定位方式"), "forwardDistance"=>SkillEditorLocalization.Text("Forward distance (m)", "前方距离（米）"), "audioClip"=>SkillEditorLocalization.Text("Audio clip", "音频剪辑"), "followCharacter"=>SkillEditorLocalization.Text("Follow actor", "跟随角色"),
        "allowTurning"=>SkillEditorLocalization.Text("Allow target tracking", "允许跟踪目标"), "targetKind"=>SkillEditorLocalization.Text("Tracking target", "跟踪目标"), "rotationSpeed"=>SkillEditorLocalization.Text("Turn speed (deg/s)", "转向速度（度/秒）"),
        "maxDistance"=>SkillEditorLocalization.Text("Distance limit", "距离上限"), "space"=>SkillEditorLocalization.Text("Direction", "方向"), "isReWrite"=>SkillEditorLocalization.Text("Override hit shape", "覆盖命中形状"), "collisionName"=>SkillEditorLocalization.Text("Name", "名称"), "collisionRotation"=>SkillEditorLocalization.Text("Rotation", "旋转"), "responseMode"=>SkillEditorLocalization.Text("On contact", "接触响应"), "contactSignal"=>SkillEditorLocalization.Text("Signal", "信号"), "damageMode"=>SkillEditorLocalization.Text("Detection", "检测方式"), "damageRatio"=>SkillEditorLocalization.Text("Damage multiplier", "伤害倍率"), "parameter1"=>SkillEditorLocalization.Text("Radius / length", "半径 / 长度"), "parameter2"=>SkillEditorLocalization.Text("Width", "宽度"), _=>SkillEditorPropertyLabels.Label(name,fallback) };
    /// <param name="list">可按下标访问的原事件列表，编辑对象仍为原引用。</param>
    /// <param name="field">当前待检查的原字段名，不使用翻译字符串作为筛选键。</param>
    /// <summary>按原字段名决定基础属性页的可见项，高级页可查看其余完整创作字段。</summary>
    static bool IsEssentialField(string list,string field){
        // 当前基础视图允许的原字段名称集合。
        string fields=list switch {
            "attackList" or "phase2AttackList"=>"keyNumber endKeyNumber damageMode tickInterval damageRatio impactLevel isReWrite shapeType parameter1 parameter2 offset unblockable unparryable interactionTag",
            "fxList" or "phase2FxList"=>"keyNumber particleSystem socketPath placementKind forwardDistance audioClip offset rotation scale followCharacter baseVolume customLifetime",
            "moveSegmentList"=>"keyNumber endKeyNumber driveMode rootMotionScale space target resolveDirectionPerFrame curve maxDistance alignFacing",
            "animSegments"=>"clip startFrame clipStartFrame clipEndFrame blendInFrames",
            "jumpList"=>"beginKey endKey nextSkill triggerCommandId triggerKey autoTrigger requiredEnergy energyCost fadeDuration",
            "projectileList"=>"keyNumber endKeyNumber prefab firePoint spawnOffset speed lifetime damageRatio autoAim shotCount spreadAngle",
            _=>null };
        return fields==null || (" "+fields+" ").Contains(" "+field+" ");
    }
    /// <param name="lanes">原事件下标到非重叠显示行号的映射。</param>
    /// <summary>从事件行号求轨道需要的显示行数，空集合至少留一行作为编辑入口。</summary>
    int LaneCount(Dictionary<int,int> lanes){
        // 当前最大行号加一，空集合保留一行。
        int count=1;
        // 字典行号快照用于按索引读取，不改变原事件或缓存。
        List<int> values=new List<int>(lanes.Values);
        // 显示行号快照的零起始下标。
        for(int index=0;index<values.Count;index++)count=Mathf.Max(count,values[index]+1);
        return count;
    }
    /// <summary>分别分配位移与朝向行号，计算二者合并显示的轨道像素高度。</summary>
    float MotionTrackHeight(){
        AssignLanes(motionLanes,moveSegmentList,/* 当前原位移段引用。 */ m=>m.keyNumber,/* 当前原位移段引用。 */ m=>SegEnd(m.keyNumber,m.endKeyNumber));
        AssignLanes(facingLanes,adjustMotionList,/* 当前原位移段引用。 */ m=>m.keyNumber,/* 当前原位移段引用。 */ m=>SegEnd(m.keyNumber,m.endKeyNumber));
        return 36+TRACK_HEIGHT*(LaneCount(motionLanes)+LaneCount(facingLanes));
    }
    /// <summary>绘制位移与朝向子轨道的当前语言说明。</summary>
    /// <param name="rect">运动轨道在时间轴内的像素区域。</param>
    void DrawUnifiedMotionTrack(Rect rect){
        // 运动子轨道的当前绘制纵坐标，单位为窗口像素。
        float y=rect.y;
        GUI.Label(new Rect(rect.x+6,y+1,160,16),SkillEditorLocalization.Text("Displacement", "位移"),EditorStyles.miniLabel);y+=18;
        // 原事件分配的位移与朝向lane数量，语言只影响说明文字。
        int movementCount=LaneCount(motionLanes),turnCount=LaneCount(facingLanes);
        GetDef(Global.TrackType.Move).drawLanes(new Rect(rect.x,y,rect.width,TRACK_HEIGHT*movementCount),motionLanes,movementCount);
        y+=TRACK_HEIGHT*movementCount;
        GUI.Label(new Rect(rect.x+6,y+1,260,16),SkillEditorLocalization.Text("Facing / target tracking", "朝向 / 目标跟踪"),EditorStyles.miniLabel);y+=18;
        GetDef(Global.TrackType.AdjustMotion).drawLanes(new Rect(rect.x,y,rect.width,TRACK_HEIGHT*turnCount),facingLanes,turnCount);
    }
    /// <summary>显示本地化运动事件菜单，点击后的原事件类型和参数不变。</summary>
    void ShowMotionAddMenu(){
        // 菜单只承载展示文字，点击仍执行原业务回调。
        var menu=new GenericMenu();
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Displacement", "位移")),false,AddMoveSegmentHere);
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Allow target tracking", "允许跟踪目标")),false,()=>AddFacingWindow(true));
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Lock facing / planted feet", "锁定朝向 / 固定站姿")),false,()=>AddFacingWindow(false));menu.ShowAsContext();
    }
    /// <param name="allowed">新朝向窗口是否允许跟踪目标，假表示锁定朝向。</param>
    /// <summary>创建允许跟踪或锁定朝向的窗口，依据角色类别选择默认目标。</summary>
    void AddFacingWindow(bool allowed){PushUndo();adjustMotionList.Add(new Global.AdjustMotionSegment {
        keyNumber=frameSelectIndex,endKeyNumber=frameSelectIndex+10,allowTurning=allowed,
        targetKind=ownerType==Global.SkillOwnerType.Enemy?Global.MoveTargetKind.Player:Global.MoveTargetKind.LockedTarget });
        selType=SelType.AdjustMotion;selIdx=adjustMotionList.Count-1;
    }
    /// <summary>在播放头创建十五帧镜头提示并选中；Scene 预览不驱动正式镜头。</summary>
    void AddCameraHere(){PushUndo();cameraCues.Add(new ActionCameraCue {keyNumber=frameSelectIndex,endKeyNumber=frameSelectIndex+15});selType=SelType.Camera;selIdx=cameraCues.Count-1;}
    /// <summary>显示本地化技能工具菜单，备份和迁移仍使用原明确操作入口。</summary>
    void ShowActionTools(){
        // 菜单只承载展示文字，点击仍执行原业务回调。
        var menu=new GenericMenu();
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Show advanced properties", "显示高级属性")),showAdvanced,()=>showAdvanced=!showAdvanced);
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Actor settings", "角色设置")),false,()=>{selType=SelType.Enemy;selIdx=-1;});
        menu.AddSeparator("");menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Restore backup", "恢复备份")),false,RestoreCurrentConfig);
        if(configFile!=null&&ActionConfigMigrationService.NeedsMigration(configFile))menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Upgrade schema with backup", "备份并升级结构")),false,MigrateCurrentConfig);
        if(animSegments.Count>=2)menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Arrange animation clips", "排列动画剪辑")),false,ShowOptimizeMenu);
        menu.ShowAsContext();
    }
}
