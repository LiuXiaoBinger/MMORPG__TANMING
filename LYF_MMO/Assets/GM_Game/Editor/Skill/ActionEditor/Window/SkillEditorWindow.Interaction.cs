// 时间轴输入、剪贴板及完整事件创建入口；Unity 资源引用在 JSON 边界单独保存。
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
    #region 剪贴板（Clip 复制粘贴，static 支持跨 Config 文件）
    /// <summary>剪贴板数据：保存复制的 Event/Clip 的 JSON + 类型信息</summary>
    static string _clipboardJson;
    /// <summary>静态剪贴板对应的原事件选择类型。</summary>
    static SelType _clipboardType = SelType.None;
    /// <summary>剪贴板中保存的引用对象（UnityEngine.Object 无法 JSON 序列化，需单独保存）</summary>
    static Dictionary<string, UnityEngine.Object> _clipboardRefs = new Dictionary<string, UnityEngine.Object>();

    /// <summary>剪贴板数值快照和选择类型均有效时允许复制粘贴入口。</summary>
    bool HasClipboard => !string.IsNullOrEmpty(_clipboardJson) && _clipboardType != SelType.None;

    /// <summary>复制当前选中的 Event/Clip 到剪贴板</summary>
    void CopySelected(){
        if(selType==SelType.None||selIdx<0) return;
        _clipboardRefs.Clear();
        _clipboardType=selType;
        if (TryCopyRoSelected()) return;
        switch(selType){
            case SelType.Flow: if(selIdx<flowNodes.Count) { 
            // 当前选中的原流程节点，数值写入剪贴板 JSON，资源引用另存。
            var node=flowNodes[selIdx]; _clipboardJson=JsonUtility.ToJson(node); if(node.nextAction!=null) _clipboardRefs["nextAction"]=node.nextAction; if(node.conditions!=null) 
            // 当前可索引集合的零起始遍历下标，访问原对象引用。
            for(int i=0;i<node.conditions.Count;i++) _clipboardRefs["flowCondition"+i]=node.conditions[i]; } break;
            case SelType.Camera: if(selIdx<cameraCues.Count) _clipboardJson=JsonUtility.ToJson(cameraCues[selIdx]); break;
            case SelType.Interaction:
                if(selIdx<interactionWindows.Count){
                    // 当前选中的原交互窗口，字段写入 JSON 快照。
                    var w=interactionWindows[selIdx]; _clipboardJson=JsonUtility.ToJson(w);
                    // 当前可索引集合的零起始遍历下标，访问原对象引用。
                    for(int i=0;i<w.conditions.Count;i++) _clipboardRefs["condition"+i]=w.conditions[i];
                } break;
            case SelType.Attack:
                if(selIdx<atkList.Count){ 
                // 原攻击判定列表中的事件引用。
                var a=atkList[selIdx]; SaveAtkRefs(a); _clipboardJson=JsonUtility.ToJson(a); } break;
            case SelType.Fx:
                if(selIdx<fxAndSoundList.Count){ 
                // 原共享粒子/音频列表中的事件引用。
                var f=fxAndSoundList[selIdx]; SaveFxRefs(f); _clipboardJson=JsonUtility.ToJson(f); } break;
            case SelType.Sound:
                if(selIdx<fxAndSoundList.Count){ 
                // 原共享粒子/音频列表中的事件引用。
                var f=fxAndSoundList[selIdx]; SaveFxRefs(f); _clipboardJson=JsonUtility.ToJson(f); } break;
            case SelType.Jump:
                if(selIdx<jumpList.Count){ 
                // 原技能衔接窗口引用，不表示物理跳跃。
                var j=jumpList[selIdx]; if(j.nextSkill!=null) _clipboardRefs["nextSkill"]=j.nextSkill; _clipboardJson=JsonUtility.ToJson(j); } break;
            case SelType.Warning:
                if(selIdx<warningCueList.Count){ 
                // 当前选中的原预警事件，数值与资源引用分别缓存。
                var w=warningCueList[selIdx]; SaveWarnRefs(w); _clipboardJson=JsonUtility.ToJson(w); } break;
            case SelType.Cancel:
                if(selIdx<cancelList.Count) _clipboardJson=JsonUtility.ToJson(cancelList[selIdx]); break;
            case SelType.Projectile:
                if(selIdx<projectileList.Count){ 
                // 原飞行物事件引用，预览使用隔离克隆。
                var p=projectileList[selIdx]; SaveProjRefs(p); _clipboardJson=JsonUtility.ToJson(p); } break;
            case SelType.HitFx:
                if(selIdx<hitFxList.Count){ 
                // 当前原受击表现引用，数值写入 JSON 快照。
                var h=hitFxList[selIdx]; SaveHitFxRefs(h); _clipboardJson=JsonUtility.ToJson(h); } break;
            case SelType.SuperArmor:
                if(selIdx<superArmorList.Count) _clipboardJson=JsonUtility.ToJson(superArmorList[selIdx]); break;
            case SelType.AdjustMotion:
                if(selIdx<adjustMotionList.Count) _clipboardJson=JsonUtility.ToJson(adjustMotionList[selIdx]); break;
            case SelType.TrailToggle:
                if(selIdx<trailToggleList.Count){ 
                // 当前原刀光开关事件引用。
                var t=trailToggleList[selIdx]; if(t.particle!=null) _clipboardRefs["particle"]=t.particle; _clipboardJson=JsonUtility.ToJson(t); } break;
            case SelType.AnimSegment:
                if(selIdx<animSegments.Count){ 
                // 当前原动画段引用。
                var s=animSegments[selIdx]; if(s.clip!=null) _clipboardRefs["clip"]=s.clip; _clipboardJson=JsonUtility.ToJson(s); } break;
            case SelType.Move:
                if(selIdx<moveSegmentList.Count) _clipboardJson=JsonUtility.ToJson(moveSegmentList[selIdx]); break;
            default: return;
        }
    }
    /// <param name="a">当前原攻击判定引用，不结算伤害。</param>
    /// <summary>单独缓存攻击表现引用，避免 JsonUtility 丢失 Unity 资源引用。</summary>
    void SaveAtkRefs(Global.Attack a){
        if(a.hitVfx!=null) _clipboardRefs["hitVfx"]=a.hitVfx;
        if(a.hitSound!=null) _clipboardRefs["hitSound"]=a.hitSound;
    }
    /// <param name="f">当前原效果事件，保留粒子与声音引用。</param>
    /// <summary>单独缓存粒子和音频资源引用，跨配置粘贴仍使用原资源。</summary>
    void SaveFxRefs(Global.FxAndSound f){
        if(f.particleSystem!=null) _clipboardRefs["particleSystem"]=f.particleSystem;
        if(f.audioClip!=null) _clipboardRefs["audioClip"]=f.audioClip;
    }
    /// <param name="w">被复制的原预警事件，缓存其粒子和音效资源引用。</param>
    /// <summary>缓存预警视觉及声音引用，独立于 JSON 数值快照保存。</summary>
    void SaveWarnRefs(Global.WarningCue w){
        if(w.warningSound!=null) _clipboardRefs["warningSound"]=w.warningSound;
        if(w.warningVfx!=null) _clipboardRefs["warningVfx"]=w.warningVfx;
    }
    /// <param name="p">当前原飞行物配置，制作资源和飞行参数。</param>
    /// <summary>缓存飞行物及其派生表现资源，复制粘贴不复制或改写原预制体。</summary>
    void SaveProjRefs(Global.Projectile p){
        if(p.prefab!=null) _clipboardRefs["prefab"]=p.prefab;
        if(p.muzzleVfx!=null) _clipboardRefs["muzzleVfx"]=p.muzzleVfx;
        if(p.fireSound!=null) _clipboardRefs["fireSound"]=p.fireSound;
        if(p.impactVfx!=null) _clipboardRefs["impactVfx"]=p.impactVfx;
        if(p.impactSound!=null) _clipboardRefs["impactSound"]=p.impactSound;
    }
    /// <param name="h">当前原受击表现数据，缓存其粒子及音频引用。</param>
    /// <summary>缓存受击粒子和声音引用，JSON 仅保存其可序列化数值字段。</summary>
    void SaveHitFxRefs(Global.HitFx h){
        if(h.hitVfx!=null) _clipboardRefs["hitVfx"]=h.hitVfx;
        if(h.hitSound!=null) _clipboardRefs["hitSound"]=h.hitSound;
    }

    /// <summary>粘贴剪贴板中的 Event/Clip 到当前帧位置</summary>
    void PasteClipboard(){
        if(!HasClipboard) return;
        PushUndo();
        // 本次操作锁定的零起始播放头帧号。
        int f=frameSelectIndex;
        if (TryPasteRoClipboard(f)) { Repaint(); return; }
        switch(_clipboardType){
            case SelType.Flow:{ 
            // 从剪贴板 JSON 创建的流程节点副本，迁移帧号后追加到原列表。
            var node=JsonUtility.FromJson<ActionFlowNode>(_clipboardJson); 
            // 原区间的帧跨度，平移时保持这一长度。
            int span=node.endKeyNumber-node.keyNumber; node.keyNumber=f; node.endKeyNumber=f+Mathf.Max(0,span); if(_clipboardRefs.TryGetValue("nextAction",out var /* 剪贴板保存的后续技能资产引用，恢复到新流程节点。 */ target)) node.nextAction=target as SkillConfigSO; if(node.conditions!=null) 
            // 当前可索引集合的零起始遍历下标，访问原对象引用。
            for(int i=0;i<node.conditions.Count;i++) if(_clipboardRefs.TryGetValue("flowCondition"+i,out var /* 原条件资产引用，不在编辑窗口提交游戏资源消耗。 */ condition)) node.conditions[i]=condition as ActionFlowCondition; flowNodes.Add(node); selType=SelType.Flow; selIdx=flowNodes.Count-1; break; }
            case SelType.Camera:{ 
            // 原镜头提示事件引用，不驱动正式摄像机。
            var cue=JsonUtility.FromJson<ActionCameraCue>(_clipboardJson); 
            // 原区间的帧跨度，平移时保持这一长度。
            int span=cue.endKeyNumber-cue.keyNumber; cue.keyNumber=f; cue.endKeyNumber=f+Mathf.Max(0,span); cameraCues.Add(cue); selType=SelType.Camera; selIdx=cameraCues.Count-1; break; }
            case SelType.Interaction:{
                // 从 JSON 创建的交互窗口副本，平移帧区间后追加。
                var w=JsonUtility.FromJson<InteractionWindow>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=w.endKeyNumber-w.keyNumber; w.keyNumber=f; w.endKeyNumber=f+Mathf.Max(0,span);
                w.id=UniqueWindowId(w.id);
                // 当前可索引集合的零起始遍历下标，访问原对象引用。
                for(int i=0;i<w.conditions.Count;i++)
                    if(_clipboardRefs.TryGetValue("condition"+i,out var /* 原条件资产引用，不在编辑窗口提交游戏资源消耗。 */ condition)) w.conditions[i]=condition as InteractionCondition;
                interactionWindows.Add(w); selType=SelType.Interaction; selIdx=interactionWindows.Count-1;
                break;}
            case SelType.Attack:{
                // 当前原攻击判定引用，不结算伤害。
                var a=JsonUtility.FromJson<Global.Attack>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=a.endKeyNumber-a.keyNumber; a.keyNumber=f; a.endKeyNumber=f+Mathf.Max(span,0);
                RestoreAtkRefs(a); atkList.Add(a); selType=SelType.Attack; selIdx=atkList.Count-1;
                break;}
            case SelType.Fx:{
                // 当前原粒子/声音事件引用，预览只实例化其资源。
                var fx=JsonUtility.FromJson<Global.FxAndSound>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=fx.endKeyNumber-fx.keyNumber; fx.keyNumber=f; fx.endKeyNumber=f+Mathf.Max(span,0);
                RestoreFxRefs(fx); fxAndSoundList.Add(fx); selType=SelType.Fx; selIdx=fxAndSoundList.Count-1;
                break;}
            case SelType.Sound:{
                // 当前原粒子/声音事件引用，预览只实例化其资源。
                var fx=JsonUtility.FromJson<Global.FxAndSound>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=fx.endKeyNumber-fx.keyNumber; fx.keyNumber=f; fx.endKeyNumber=f+Mathf.Max(span,0);
                RestoreFxRefs(fx); fxAndSoundList.Add(fx); selType=SelType.Sound; selIdx=fxAndSoundList.Count-1;
                break;}
            case SelType.Jump:{
                // 从静态剪贴板反序列化的技能衔接副本。
                var j=JsonUtility.FromJson<Global.Jump>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=j.endKey-j.beginKey; j.beginKey=f; j.endKey=f+Mathf.Max(span,0);
                if(_clipboardRefs.ContainsKey("nextSkill")) j.nextSkill=_clipboardRefs["nextSkill"] as SkillConfigSO;
                jumpList.Add(j); selType=SelType.Jump; selIdx=jumpList.Count-1;
                break;}
            case SelType.Warning:{
                // 从 JSON 创建的预警事件副本，恢复资源引用后追加。
                var w=JsonUtility.FromJson<Global.WarningCue>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=w.endKeyNumber-w.keyNumber; w.keyNumber=f; w.endKeyNumber=f+Mathf.Max(span,0);
                RestoreWarnRefs(w); warningCueList.Add(w); selType=SelType.Warning; selIdx=warningCueList.Count-1;
                break;}
            case SelType.Cancel:{
                // 当前原取消点帧窗数据。
                var cp=JsonUtility.FromJson<Global.CancelPoint>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=cp.endKeyNumber-cp.keyNumber; cp.keyNumber=f; cp.endKeyNumber=f+Mathf.Max(span,0);
                cancelList.Add(cp); selType=SelType.Cancel; selIdx=cancelList.Count-1;
                break;}
            case SelType.Projectile:{
                // 从静态剪贴板反序列化的飞行物副本。
                var p=JsonUtility.FromJson<Global.Projectile>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=p.endKeyNumber-p.keyNumber; p.keyNumber=f; p.endKeyNumber=f+Mathf.Max(span,0);
                RestoreProjRefs(p); projectileList.Add(p); selType=SelType.Projectile; selIdx=projectileList.Count-1;
                break;}
            case SelType.HitFx:{
                // 从 JSON 读取的受击表现副本。
                var h=JsonUtility.FromJson<Global.HitFx>(_clipboardJson);
                RestoreHitFxRefs(h); hitFxList.Add(h); selType=SelType.HitFx; selIdx=hitFxList.Count-1;
                break;}
            case SelType.SuperArmor:{
                // 当前原分段霸体数据，不执行游戏战斗状态。
                var sa=JsonUtility.FromJson<Global.SuperArmorSegment>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=sa.endKeyNumber-sa.keyNumber; sa.keyNumber=f; sa.endKeyNumber=f+Mathf.Max(span,0);
                superArmorList.Add(sa); selType=SelType.SuperArmor; selIdx=superArmorList.Count-1;
                break;}
            case SelType.AdjustMotion:{
                // 当前原朝向调整窗口数据。
                var am=JsonUtility.FromJson<Global.AdjustMotionSegment>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=am.endKeyNumber-am.keyNumber; am.keyNumber=f; am.endKeyNumber=f+Mathf.Max(span,0);
                adjustMotionList.Add(am); selType=SelType.AdjustMotion; selIdx=adjustMotionList.Count-1;
                break;}
            case SelType.TrailToggle:{
                // 当前原刀光开关数据。
                var tt=JsonUtility.FromJson<Global.TrailToggle>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=tt.endKeyNumber-tt.keyNumber; tt.keyNumber=f; tt.endKeyNumber=f+Mathf.Max(span,0);
                if(_clipboardRefs.ContainsKey("particle")) tt.particle=_clipboardRefs["particle"] as ParticleSystem;
                trailToggleList.Add(tt); selType=SelType.TrailToggle; selIdx=trailToggleList.Count-1;
                break;}
            case SelType.AnimSegment:{
                // 从静态剪贴板反序列化的动画段副本。
                var s=JsonUtility.FromJson<Global.AnimClipSegment>(_clipboardJson);
                s.startFrame=f;
                if(_clipboardRefs.ContainsKey("clip")) s.clip=_clipboardRefs["clip"] as AnimationClip;
                animSegments.Add(s); selType=SelType.AnimSegment; selIdx=animSegments.Count-1;
                break;}
            case SelType.Move:{
                // 从静态剪贴板读取的位移事件副本，不替换原事件。
                var move=JsonUtility.FromJson<Global.MoveSegment>(_clipboardJson);
                // 原区间的帧跨度，平移时保持这一长度。
                int span=move.endKeyNumber-move.keyNumber;
                move.keyNumber=f;
                move.endKeyNumber=f+Mathf.Max(span,0);
                moveSegmentList.Add(move); selType=SelType.Move; selIdx=moveSegmentList.Count-1;
                break;}
        }
        Repaint();
    }
    /// <param name="a">当前原攻击判定引用，不结算伤害。</param>
    /// <summary>把 JSON 无法保留的攻击特效和音效引用从剪贴板快照绑定回副本。</summary>
    void RestoreAtkRefs(Global.Attack a){
        if(_clipboardRefs.ContainsKey("hitVfx")) a.hitVfx=_clipboardRefs["hitVfx"] as ParticleSystem;
        if(_clipboardRefs.ContainsKey("hitSound")) a.hitSound=_clipboardRefs["hitSound"] as AudioClip;
    }
    /// <param name="f">当前原效果事件，保留粒子与声音引用。</param>
    /// <summary>把粒子及音频资源引用绑定回反序列化效果副本，不改原剪贴板资产。</summary>
    void RestoreFxRefs(Global.FxAndSound f){
        if(_clipboardRefs.ContainsKey("particleSystem")) f.particleSystem=_clipboardRefs["particleSystem"] as ParticleSystem;
        if(_clipboardRefs.ContainsKey("audioClip")) f.audioClip=_clipboardRefs["audioClip"] as AudioClip;
    }
    /// <param name="w">粘贴生成的预警事件副本，恢复剪贴板中保留的资源引用。</param>
    /// <summary>恢复预警的视觉与声音引用，旧双资源配置仍完整保留。</summary>
    void RestoreWarnRefs(Global.WarningCue w){
        if(_clipboardRefs.ContainsKey("warningSound")) w.warningSound=_clipboardRefs["warningSound"] as AudioClip;
        if(_clipboardRefs.ContainsKey("warningVfx")) w.warningVfx=_clipboardRefs["warningVfx"] as ParticleSystem;
    }
    /// <param name="p">当前原飞行物配置，制作资源和飞行参数。</param>
    /// <summary>恢复飞行物预制体、枪口、命中及声音引用，确保 JSON 粘贴不会丢失资产身份。</summary>
    void RestoreProjRefs(Global.Projectile p){
        if(_clipboardRefs.ContainsKey("prefab")) p.prefab=_clipboardRefs["prefab"] as GameObject;
        if(_clipboardRefs.ContainsKey("muzzleVfx")) p.muzzleVfx=_clipboardRefs["muzzleVfx"] as ParticleSystem;
        if(_clipboardRefs.ContainsKey("fireSound")) p.fireSound=_clipboardRefs["fireSound"] as AudioClip;
        if(_clipboardRefs.ContainsKey("impactVfx")) p.impactVfx=_clipboardRefs["impactVfx"] as ParticleSystem;
        if(_clipboardRefs.ContainsKey("impactSound")) p.impactSound=_clipboardRefs["impactSound"] as AudioClip;
    }
    /// <param name="h">待恢复受击粒子与音频引用的粘贴副本。</param>
    /// <summary>恢复受击表现中的粒子及声音资源引用，引用身份与复制前保持。</summary>
    void RestoreHitFxRefs(Global.HitFx h){
        if(_clipboardRefs.ContainsKey("hitVfx")) h.hitVfx=_clipboardRefs["hitVfx"] as ParticleSystem;
        if(_clipboardRefs.ContainsKey("hitSound")) h.hitSound=_clipboardRefs["hitSound"] as AudioClip;
    }

    /// <summary>获取剪贴板中数据的类型描述（用于菜单显示）</summary>
    string GetClipboardLabel(){
        // 当前轨道完整绘制及编辑回调定义。
        var def=GetDefBySel(_clipboardType);
        if (def != null) return def.itemName;
        return SkillEditorLocalization.Text("Event", "事件");
    }

    /// <summary>检查剪贴板类型是否可以粘贴到指定轨道</summary>
    /// <param name="trackType">待检查的原轨道类型，别名解析后决定粘贴兼容性。</param>
    bool CanPasteToTrack(Global.TrackType trackType){
        if(!HasClipboard) return false;
        // RO后处理只有一个配置，已有值时不允许粘贴覆盖或伪造第二项。
        if (trackType == Global.TrackType.CameraPostEffect && RoPostEffectCount() > 0) return false;
        if(BuiltinTrackRegistry.Canonical(trackType)==Global.TrackType.Flow) return _clipboardType==SelType.Flow || _clipboardType==SelType.Jump || _clipboardType==SelType.Cancel;
        // 当前轨道完整绘制及编辑回调定义。
        var def=GetDef(trackType);
        // 位移曲线轨道没有可粘贴的 Event
        return def!=null&&def.removeAt!=null&&(def.selType==_clipboardType ||
            (BuiltinTrackRegistry.Canonical(trackType)==Global.TrackType.Fx &&
             (_clipboardType==SelType.Fx || _clipboardType==SelType.Warning)));
    }
    #endregion

    #region 交互
    /// <summary>动画段拖拽点相对段起始的帧偏移。</summary>
    float _segmentGrabFrame;
    /// <summary>鼠标抓取点相对事件触发帧的偏移，单位时间轴帧；拖声音波形内部时触发点不会跳到鼠标下。</summary>
    float _eventGrabFrame;
    /// <param name="area">输入或绘图允许的窗口像素区域。</param>
    /// <param name="totalF">当前轨道合并后的时间轴总帧数。</param>
    /// <param name="input">显式传入的 GUI 输入事件，空值沿用 Event.current。</param>
    /// <summary>处理播放头、事件和动画段拖拽，按手势统一记录 Undo 并在释放时提交。</summary>
    void HandleTLInput(Rect area,int totalF,Event input=null){
        // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
        var e=input??Event.current;
        if(e==null) return;
        // 标尺允许拖动播放头的像素矩形。
        Rect ruler=new Rect(area.x,area.y,area.width,RULER_HEIGHT);
        if(e.type==EventType.MouseDown&&e.button==0&&ruler.Contains(e.mousePosition)){ isDraggingPlayhead=true; SetFrame(e.mousePosition.x,totalF); e.Use(); }
        if(isDraggingPlayhead){
            if(e.type==EventType.MouseDrag){ SetFrame(e.mousePosition.x,totalF); e.Use(); }
            if(e.type==EventType.MouseUp){ isDraggingPlayhead=false; e.Use(); }
        }
        // Segment 整体拖拽
        if(isDraggingSegment&&e.type==EventType.MouseDrag){
            _editGesture.Record(configFile,SkillEditorLocalization.Text("Move Animation Segment", "移动动画片段"));
            if(dragSegIdx>=0&&dragSegIdx<animSegments.Count){
                // 由鼠标横坐标换算的新帧号，先限制到可编辑范围。
                int nf=Mathf.Max(0,Mathf.RoundToInt(e.mousePosition.x/pixelsPerFrame-_segmentGrabFrame));
                animSegments[dragSegIdx].startFrame=nf; Repaint(); e.Use(); }
        }
        if(isDraggingSegment&&e.type==EventType.MouseUp){ CommitGuiChanges(); _editGesture.Complete(); isDraggingSegment=false; e.Use(); }
        // Segment 边缘拖拽（裁剪）
        if(isDraggingSegEdge&&e.type==EventType.MouseDrag){
            _editGesture.Record(configFile,SkillEditorLocalization.Text("Trim Animation Segment", "裁剪动画片段"));
            if(segEdgeIdx>=0&&segEdgeIdx<animSegments.Count){
                // 当前原动画段引用，裁剪参数使用源剪辑帧。
                var seg=animSegments[segEdgeIdx]; if(seg.clip!=null){
                    // 源动画剪辑完整长度的整数帧数。
                    int totalClipF=Mathf.Max(1,(int)(seg.clip.length*seg.clip.frameRate));
                    // 当前鼠标位置对应的非负时间轴整数帧。
                    int mf=Mathf.Max(0,Mathf.RoundToInt(e.mousePosition.x/pixelsPerFrame));
                    // 源剪辑 FPS 与时间轴 FPS 的比值，用于裁剪帧换算。
                    float ratio=configFile!=null && configFile.UsesExplicitTiming ? ActionTiming.SourceRate(seg)/ActionTiming.FrameRate(configFile) : 1f;
                    if(segEdgeSide==0){ // 左：裁剪起始
                        // 拖拽相对初始位置的帧差，源帧与时间轴帧通过倍率换算。
                        int delta=Mathf.RoundToInt((mf-segOrigTimelineStart)*ratio);
                        // 裁剪后源剪辑开始帧，限制在完整源长度内。
                        int newCS=Mathf.Clamp(segOrigClipStart+delta,0,segOrigClipEnd-1);
                        seg.clipStartFrame=newCS; seg.startFrame=Mathf.Max(0,segOrigTimelineStart+Mathf.RoundToInt((newCS-segOrigClipStart)/ratio));
                    }else{ // 右：裁剪结束
                        // 当前鼠标位置相对段起点的源剪辑帧数。
                        int frameInTL=Mathf.RoundToInt((mf-seg.startFrame)*ratio);
                        seg.clipEndFrame=Mathf.Clamp(seg.clipStartFrame+frameInTL,seg.clipStartFrame+1,totalClipF);
                    } Repaint(); e.Use(); }
            }
        }
        if(isDraggingSegEdge&&e.type==EventType.MouseUp){ CommitGuiChanges(); _editGesture.Complete(); isDraggingSegEdge=false; e.Use(); }
        if(isDraggingItem&&e.type==EventType.MouseDrag){
            _editGesture.Record(configFile,SkillEditorLocalization.Text("Move Action Event", "移动技能事件"));
            // 由鼠标横坐标换算的新帧号，先限制到可编辑范围。
            int nf=Mathf.Clamp(Mathf.RoundToInt(e.mousePosition.x/pixelsPerFrame-_eventGrabFrame),0,totalF);
            // 原区间的帧跨度，平移时保持这一长度。
            int span=dragOrigEnd-dragOrigF;
            // 裁剪后的新区间起始帧，避免超出时间轴末尾。
            int nb=Mathf.Clamp(nf,0,totalF-Mathf.Max(span,0));
            // RO轨道按原始秒跨度平移，不用整数帧跨度重建End，避免亚帧时长丢失。
            TryMoveRoEvent(dragType, dragIdx, nf);
            switch(dragType){
                case SelType.FlowEnd: if(configFile!=null) configFile.exitFrame=Mathf.Max(1,nb); break;
                case SelType.Flow: if(dragIdx>=0&&dragIdx<flowNodes.Count){ flowNodes[dragIdx].keyNumber=nb; if(flowNodes[dragIdx].limitWindow) flowNodes[dragIdx].endKeyNumber=nb+Mathf.Max(0,span); } break;
                case SelType.Camera: if(dragIdx>=0&&dragIdx<cameraCues.Count){cameraCues[dragIdx].keyNumber=nb;cameraCues[dragIdx].endKeyNumber=nb+Mathf.Max(0,span);} break;
                case SelType.Interaction: if(dragIdx>=0&&dragIdx<interactionWindows.Count){
                    interactionWindows[dragIdx].keyNumber=nb; interactionWindows[dragIdx].endKeyNumber=nb+Mathf.Max(0,span); } break;
                case SelType.Attack: if(dragIdx>=0&&dragIdx<atkList.Count){ atkList[dragIdx].keyNumber=nb;
                    atkList[dragIdx].endKeyNumber=MovedEventEnd(atkList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Fx: if(dragIdx>=0&&dragIdx<fxAndSoundList.Count){ fxAndSoundList[dragIdx].keyNumber=nb;
                    fxAndSoundList[dragIdx].endKeyNumber=MovedEventEnd(fxAndSoundList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Jump: if(dragIdx>=0&&dragIdx<jumpList.Count){
                    jumpList[dragIdx].beginKey=nb; jumpList[dragIdx].endKey=nb+span; } break;
                case SelType.Warning: if(dragIdx>=0&&dragIdx<warningCueList.Count){ warningCueList[dragIdx].keyNumber=nb;
                    warningCueList[dragIdx].endKeyNumber=MovedEventEnd(warningCueList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Sound: if(dragIdx>=0&&dragIdx<fxAndSoundList.Count){ fxAndSoundList[dragIdx].keyNumber=nb;
                    fxAndSoundList[dragIdx].endKeyNumber=MovedEventEnd(fxAndSoundList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Cancel: if(dragIdx>=0&&dragIdx<cancelList.Count){ cancelList[dragIdx].keyNumber=nb;
                    cancelList[dragIdx].endKeyNumber=MovedEventEnd(cancelList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Projectile: if(dragIdx>=0&&dragIdx<projectileList.Count){ projectileList[dragIdx].keyNumber=nb;
                    projectileList[dragIdx].endKeyNumber=MovedEventEnd(projectileList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.SuperArmor: if(dragIdx>=0&&dragIdx<superArmorList.Count){ superArmorList[dragIdx].keyNumber=nb;
                    superArmorList[dragIdx].endKeyNumber=MovedEventEnd(superArmorList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.AdjustMotion: if(dragIdx>=0&&dragIdx<adjustMotionList.Count){ adjustMotionList[dragIdx].keyNumber=nb;
                    adjustMotionList[dragIdx].endKeyNumber=MovedEventEnd(adjustMotionList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.TrailToggle: if(dragIdx>=0&&dragIdx<trailToggleList.Count){ trailToggleList[dragIdx].keyNumber=nb;
                    trailToggleList[dragIdx].endKeyNumber=MovedEventEnd(trailToggleList[dragIdx].endKeyNumber,nb,span); } break;
                case SelType.Move: if(dragIdx>=0&&dragIdx<moveSegmentList.Count){ moveSegmentList[dragIdx].keyNumber=nb;
                    moveSegmentList[dragIdx].endKeyNumber=MovedEventEnd(moveSegmentList[dragIdx].endKeyNumber,nb,span); } break;
            }
            InvalidatePreviewFrame(); Repaint(); e.Use();
        }
        if(isDraggingItem&&e.type==EventType.MouseUp){ CommitGuiChanges(); _editGesture.Complete(); isDraggingItem=false; e.Use(); }
        if(e.type==EventType.ScrollWheel&&area.Contains(e.mousePosition)&&e.control){
            pixelsPerFrame=Mathf.Clamp(pixelsPerFrame-e.delta.y*.5f,MIN_PPF,MAX_PPF); e.Use(); }
        if(e.type==EventType.MouseDown&&e.button==0&&!isDraggingPlayhead&&!isDraggingItem&&!isDraggingSegment&&!isDraggingSegEdge&&e.mousePosition.y>RULER_HEIGHT)
            SetFrame(e.mousePosition.x,totalF);
    }
    /// <param name="input">显式传入的 GUI 输入事件，空值沿用 Event.current。</param>
    /// <summary>处理预览、逐帧、撤销及剪贴板快捷键；文本编辑期间不抢占按键。</summary>
    void HandleGlobalInput(Event input=null){
        // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
        var e=input??Event.current;
        if(e==null || EditorGUIUtility.editingTextField) return;
        // Ctrl+Z：撤销   Ctrl+Shift+Z / Ctrl+Y：重做
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Z && (e.control||e.command)){
            if(e.shift) RedoLatest(); else UndoLatest(); e.Use(); return;
        }
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Y && (e.control||e.command)){
            RedoLatest(); e.Use(); return;
        }
        // Ctrl+C：复制选中的 clip
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.C && (e.control||e.command)){
            CopySelected(); e.Use(); return;
        }
        // Ctrl+V：粘贴剪贴板内容到当前播放头位置
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.V && (e.control||e.command)){
            PasteClipboard(); e.Use(); return;
        }
        // Ctrl/Cmd+S is intentionally left entirely to Unity.
        if(e.type!=EventType.KeyDown || e.shift || e.alt || e.control || e.command) return;
        // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
        int tf=GetTotalFrames();
        switch(e.keyCode){
            case KeyCode.Space:
                if(_validationErrorCount==0) SetPreviewPlaying(!playFrame);
                else ShowNotification(new GUIContent(SkillEditorLocalization.Text("Fix validation errors before previewing.", "修复校验错误后才能预览。")));
                e.Use(); break;
            case KeyCode.LeftArrow: SeekPreviewFrame(frameSelectIndex-1); e.Use(); break;
            case KeyCode.RightArrow: SeekPreviewFrame(frameSelectIndex+1); e.Use(); break;
            case KeyCode.Home: SeekPreviewFrame(0); e.Use(); break;
            case KeyCode.End: SeekPreviewFrame(Mathf.Max(tf,GetPreviewEndFrame())); e.Use(); break;
            case KeyCode.Delete: DelSel(); e.Use(); break;
        }
    }
    /// <param name="mx">时间轴内的鼠标横坐标，单位像素。</param>
    /// <param name="tf">当前时间轴可见帧上限，供输入与创建窗口裁剪。</param>
    /// <summary>把鼠标横坐标换算为零起始帧，允许预览尾部导航；作者事件的编辑范围保持原值。</summary>
    void SetFrame(float mx,int tf){ SeekPreviewFrame(Mathf.Clamp(Mathf.RoundToInt(mx/pixelsPerFrame),0,Mathf.Max(tf,GetPreviewEndFrame()))); }
    /// <param name="t">本次拖拽锁定的原选择类别。</param>
    /// <param name="i">当前可索引集合的零起始遍历下标，访问原对象引用。</param>
    /// <param name="f">当前零起始时间轴帧，用于采样或区间检测。</param>
    /// <param name="end">当前事件区间的有效末尾帧，用于范围限制。</param>
    /// <summary>保存拖拽开始时的事件类别、下标和帧范围，后续鼠标事件使用同一编辑手势。</summary>
    void StartDrag(SelType t,int i,int f,int end=0){
        SetPreviewPlaying(false);
        isDraggingItem=true; dragType=t; dragIdx=i; dragOrigF=f; dragOrigEnd=end;
        _eventGrabFrame=0f;
        if(Event.current!=null) _eventGrabFrame=Event.current.mousePosition.x/pixelsPerFrame-f;
    }
    /// <summary>保持拖前点或区间语义；正数点末帧随起点平移，零或负数单次哨兵保留，避免左拖点事件生成区间。</summary>
    /// <param name="serializedEnd">当前原事件保存的结束帧；小于等于零表示未设置或旧单次哨兵。</param>
    /// <param name="newBegin">裁剪后的新起始帧，零起始且非负。</param>
    /// <param name="originalSpan">拖拽开始时的有效帧跨度；零为点事件，正数为包含末帧的区间。</param>
    /// <returns>保留单次语义或原正数跨度的结束帧。</returns>
    static int MovedEventEnd(int serializedEnd,int newBegin,int originalSpan)
    {
        if(originalSpan>0) return newBegin+originalSpan;
        if(serializedEnd<=0) return serializedEnd;
        return newBegin;
    }
    /// <param name="list">可按下标访问的原事件列表，编辑对象仍为原引用。</param>
    /// <param name="idx">当前原事件列表的零起始下标。</param>
    /// <summary>记录原资产撤销点后按零起始下标移除一个事件，不销毁其引用的动画或粒子资源。</summary>
    void RemoveEvent<T>(List<T> list,int idx){ PushUndo(); list.RemoveAt(idx); }
    /// <summary>按轨道注册的删除回调移除当前选中事件；原结束帧选择可还原自动结束。</summary>
    void DelSel(){
        if(selType==SelType.FlowEnd && configFile!=null) { PushUndo(); configFile.exitFrame=0; }
        // 原实现漏了「取消点 / 受击特效」两类，改为走注册表后全部覆盖
        var def=GetDefBySel(selType);
        if(def!=null&&def.removeAt!=null&&def.listCount!=null&&selIdx>=0&&selIdx<def.listCount())
            def.removeAt(selIdx);
        selType=SelType.None; selIdx=-1;
    }
    #endregion

    #region 添加项
    /// <summary>在播放头加入指定剪辑的新动画段，保留源剪辑帧率和原作者列表引用。</summary>
    void AddAnimSegHere(){
        if(currentClips!=null&&currentClips.Length>0){
            // 本次鼠标操作的上下文菜单，点击回调才修改配置。
            var menu=new GenericMenu();
            // 原动画段列表中的引用，裁剪单位为源剪辑帧。
            foreach(var c in currentClips){ if(c==null)continue; 
            // 菜单闭包捕获的当前真实剪辑引用。
            var cc=c;
                menu.AddItem(new GUIContent(cc.name),false,()=>{
                    PushUndo();
                    animSegments.Add(new Global.AnimClipSegment{clip=cc,startFrame=frameSelectIndex});
                    selType=SelType.AnimSegment; selIdx=animSegments.Count-1; }); }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Empty clip (assign animation later)", "空剪辑（稍后指定动画）")),false,()=>{
                PushUndo();
                animSegments.Add(new Global.AnimClipSegment{startFrame=frameSelectIndex});
                selType=SelType.AnimSegment; selIdx=animSegments.Count-1; });
            menu.ShowAsContext();
        }else{
            PushUndo();
            animSegments.Add(new Global.AnimClipSegment{startFrame=frameSelectIndex});
            selType=SelType.AnimSegment; selIdx=animSegments.Count-1;
        }
    }
    /// <summary>按默认形状在播放头创建攻击判定事件；仅创作数据，不在编辑器中结算伤害。</summary>
    void AddAtkHere(){ PushUndo(); 
    // 本次操作锁定的零起始播放头帧号。
    var a=new Global.Attack{keyNumber=frameSelectIndex,shapeType=skillShapeSelectIndex,parameter1=range1,parameter2=range2,
        offset=new Vector3(offsetX,offsetY,offsetZ),isReWrite=true}; atkList.Add(a); selType=SelType.Attack; selIdx=atkList.Count-1; }
    /// <summary>显示视觉、声音和预警创建入口，共享效果轨道不合并其原数据类型。</summary>
    void ShowEffectAddMenu(){
        // 本次鼠标操作的上下文菜单，点击回调才修改配置。
        var menu=new GenericMenu();
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Visual Effect", "视觉特效")),false,AddFxHere);
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Audio", "音频")),false,AddSndHere);
        menu.AddItem(new GUIContent(SkillEditorLocalization.Text("Telegraph", "预警")),false,AddWarnHere);
        menu.ShowAsContext();
    }
    /// <summary>在播放头添加视觉粒子事件并选中，粒子引用可在属性面板继续指定。</summary>
    void AddFxHere(){ PushUndo(); fxAndSoundList.Add(new Global.FxAndSound{keyNumber=frameSelectIndex,contentKind=Global.EffectContentKind.VisualEffect}); selType=SelType.Fx; selIdx=fxAndSoundList.Count-1; }
    /// <summary>在共享效果列表添加音频类型事件，保持音频与视觉类型的原序列化身份。</summary>
    void AddSndHere(){ PushUndo(); fxAndSoundList.Add(new Global.FxAndSound{keyNumber=frameSelectIndex,contentKind=Global.EffectContentKind.Audio}); selType=SelType.Sound; selIdx=fxAndSoundList.Count-1; }
    /// <summary>在播放头创建十帧技能衔接窗口；Jump 数据表示行为跳转而非物理跳跃。</summary>
    void AddJumpHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=Mathf.Min(frameSelectIndex+10,tf);
        jumpList.Add(new Global.Jump{beginKey=frameSelectIndex,endKey=end}); selType=SelType.Jump; selIdx=jumpList.Count-1; }
    /// <summary>添加视觉或声音预警事件，后续可在共享特效轨道编辑其资源与区间。</summary>
    void AddWarnHere(){ PushUndo(); warningCueList.Add(new Global.WarningCue{keyNumber=frameSelectIndex}); selType=SelType.Warning; selIdx=warningCueList.Count-1; }
    /// <summary>从播放头到时间轴末尾创建取消窗口，供接入方解释取消权限。</summary>
    void AddCancelHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); cancelList.Add(new Global.CancelPoint{keyNumber=frameSelectIndex,endKeyNumber=tf}); selType=SelType.Cancel; selIdx=cancelList.Count-1; }
    /// <summary>创建待指定预制体的飞行物事件，供编辑预览和宿主飞行物接收器使用。</summary>
    void AddProjHere(){ PushUndo(); projectileList.Add(new Global.Projectile{keyNumber=frameSelectIndex}); selType=SelType.Projectile; selIdx=projectileList.Count-1; }
    /// <summary>新增供命中确认后读取的受击表现数据，不自动产生伤害或命中特效。</summary>
    void AddHitFxHere(){ PushUndo(); hitFxList.Add(new Global.HitFx()); selType=SelType.HitFx; selIdx=hitFxList.Count-1; }
    /// <summary>从播放头创建默认十五帧霸体窗；正式战斗状态由宿主解释此窗口。</summary>
    void AddSuperArmorHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=Mathf.Min(frameSelectIndex+15,tf);
        superArmorList.Add(new Global.SuperArmorSegment{keyNumber=frameSelectIndex,endKeyNumber=end}); selType=SelType.SuperArmor; selIdx=superArmorList.Count-1; }
    /// <summary>在播放头新增朝向调整帧窗，记录默认结束帧并选中新窗口；实际转向需宿主接入。</summary>
    void AddAdjustMotionHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=Mathf.Min(frameSelectIndex+20,tf);
        adjustMotionList.Add(new Global.AdjustMotionSegment{keyNumber=frameSelectIndex,endKeyNumber=end}); selType=SelType.AdjustMotion; selIdx=adjustMotionList.Count-1; }
    /// <summary>在播放头添加刀光开关事件，运行接收器负责启停与中断清理。</summary>
    void AddTrailHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=Mathf.Min(frameSelectIndex+15,tf);
        trailToggleList.Add(new Global.TrailToggle{keyNumber=frameSelectIndex,endKeyNumber=end}); selType=SelType.TrailToggle; selIdx=trailToggleList.Count-1; }

    /// <summary>在播放头创建位移帧段供曲线或根运动制作；正式角色移动需独立后端接入。</summary>
    void AddMoveSegmentHere(){ PushUndo(); 
    // 当前时间轴可见帧上限，供输入与创建窗口裁剪。
    int tf=GetTotalFrames(); 
    // 当前事件区间的有效末尾帧，用于范围限制。
    int end=Mathf.Min(frameSelectIndex+15,tf);
        moveSegmentList.Add(new Global.MoveSegment{keyNumber=frameSelectIndex,endKeyNumber=end}); selType=SelType.Move; selIdx=moveSegmentList.Count-1; }
    #endregion
}
