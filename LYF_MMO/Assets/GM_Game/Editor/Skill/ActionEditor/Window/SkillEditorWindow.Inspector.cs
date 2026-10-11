// 完整创作属性面板与验证定位；可编辑字段不代表当前游戏已经导出并消费所有能力。
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

/// <summary>技能窗口的属性面板显示；保留原窗口脚本 GUID 与事件选择行为。</summary>
public partial class SkillEditorWindow
{
    #region 属性检查器
    /// <summary>在窗口给定像素区域绘制原选择项的属性，空选择显示操作说明。</summary>
    /// <param name="r">当前属性面板的窗口像素矩形。</param>
    void DrawInspectorPanel(Rect r){
        EditorGUI.DrawRect(r,new Color(.21f,.21f,.21f));
        GUILayout.BeginArea(new Rect(r.x+12,r.y+10,r.width-24,Mathf.Max(1,r.height-20)));
        inspScrollPos=EditorGUILayout.BeginScrollView(inspScrollPos);
        EditorGUIUtility.labelWidth=160;
        DrawValidationSummary();
        switch(selType){
            case SelType.FlowEnd: DrawLegacyFlowEnd(); break;
            case SelType.Basic: InspBasic(); break;
            case SelType.Hitbox: InspHitbox(); break;
            case SelType.Enemy: InspEnemy(); break;
            case SelType.Phase2Attack: DrawSerializedSelection("phase2AttackList",selIdx); break;
            case SelType.Phase2Fx: DrawSerializedSelection("phase2FxList",selIdx); break;
            default:{
                // 事件类属性面板统一由轨道注册表分派
                var def=GetDefBySel(selType);
                if(def!=null&&def.inspector!=null){
                    if(def.listCount==null) def.inspector(-1);                  // Move 等无列表轨道
                    else if(selIdx>=0&&selIdx<def.listCount()) def.inspector(selIdx);
                }else{
                    EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Select a clip to edit its properties. Use Add on a track to insert at the current frame.\n\nPresentation controls what you see and hear. Mechanics controls what the action does. Both share one timeline.\n\nSpace: preview   Left / Right: step   Delete: remove\nCtrl+Z / Y: undo / redo   Ctrl+C / V: copy / paste", "选择片段编辑属性；点击轨道上的添加，在当前帧插入事件。\n\n表现控制动画、特效和音效；机制控制行为逻辑，共用同一条时间轴。\n\n空格：预览  左／右：逐帧  Delete：移除\nCtrl+Z／Y：撤销／重做  Ctrl+C／V：复制／粘贴"),MessageType.None);
                }
                break;}
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    /// <summary>以当前语言显示原校验结果，错误码、计数与定位信息保持不变。</summary>
    void DrawValidationSummary(){
        if(!showValidation && _validationErrorCount==0) return;
        if(_validationIssues.Count==0){
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("No validation issues.", "未发现校验问题。"),MessageType.None);
            return;
        }
        EditorGUILayout.LabelField(SkillEditorLocalization.Text($"Validation: {_validationErrorCount} errors / {_validationWarningCount} warnings", $"校验：{_validationErrorCount} 项错误／{_validationWarningCount} 项警告"),EditorStyles.boldLabel);
        // 本次绘制的原校验结果数量。
        int visible=_validationIssues.Count;
        // 遍历原可索引集合的零起始位置，仍访问原对象引用。
        for(int i=0;i<visible;i++){
            // 当前原校验结果，显示翻译不修改其定位数据。
            var issue=_validationIssues[i];
            EditorGUILayout.BeginHorizontal();
            // 保持原校验提示无图标布局，等级计数仍由原验证结果提供。
            MessageType type = MessageType.None;
            EditorGUILayout.HelpBox($"[{issue.Code}] {SkillEditorPropertyLabels.ValidationMessage(issue.Message)}",type);
            if(issue.EventIndex>=0&&GUILayout.Button(SkillEditorLocalization.Text("Locate", "定位"),GUILayout.Width(54),GUILayout.Height(34))) LocateValidationIssue(issue);
            EditorGUILayout.EndHorizontal();
        }
        if(_validationIssues.Count>visible) EditorGUILayout.LabelField(SkillEditorLocalization.Text($"{_validationIssues.Count-visible} additional issues", $"另有 {_validationIssues.Count-visible} 项问题"),EditorStyles.miniLabel);
        EditorGUILayout.Space(4);
    }

    /// <summary>按原轨道标识定位原事件，并把时间轴滚动到其开始帧。</summary>
    /// <param name="issue">原验证结果，保持错误码、轨道名与零起始事件索引。</param>
    void LocateValidationIssue(ActionValidationIssue issue){
        if (TryLocateRoIssue(issue)) return;
        selIdx=issue.EventIndex;
        switch(issue.Track){
            case "Flow": selType=SelType.Flow; break;
            case "Camera": selType=SelType.Camera; break;
            case "Animation": selType=SelType.AnimSegment; break;
            case "Attack": selType=SelType.Attack; break;
            case "Phase2Attack": selType=SelType.Phase2Attack; break;
            case "FxAndSound": selType=SelType.Fx; break;
            case "Phase2Fx": selType=SelType.Phase2Fx; break;
            case "HitFx": selType=SelType.HitFx; break;
            case "Jump": selType=SelType.Jump; break;
            case "CancelPoint": selType=SelType.Cancel; break;
            case "Projectile": selType=SelType.Projectile; break;
            case "WarningCue": selType=SelType.Warning; break;
            case "SuperArmor": selType=SelType.SuperArmor; break;
            case "AdjustMotion": selType=SelType.AdjustMotion; break;
            case "Trail": selType=SelType.TrailToggle; break;
            case "Move": selType=SelType.Move; break;
            case "Interaction": selType=SelType.Interaction; break;
            default: selType=SelType.Basic; selIdx=-1; break;
        }
        // 验证结果对应的真实序列化列表名。
        string listName=BuiltinTrackRegistry.GetValidationListProperty(issue.Track);
        if(listName!=null&&_serializedConfig!=null){
            _serializedConfig.Update();
            // 当前原配置列表的序列化缓存。
            var list=_serializedConfig.FindProperty(listName);
            if(list!=null&&selIdx>=0&&selIdx<list.arraySize){
                // 当前原列表元素的属性游标。
                var item=list.GetArrayElementAtIndex(selIdx);
                // 原事件开始帧属性，用于播放头导航。
                var start=item.FindPropertyRelative("keyNumber")??item.FindPropertyRelative("beginKey")??item.FindPropertyRelative("startFrame");
                if(start!=null){
                    frameSelectIndex=Mathf.Max(0,start.intValue);
                    timelineScrollPos.x=Mathf.Max(0,frameSelectIndex*pixelsPerFrame-120f);
                }
            }
        }
        Repaint();
    }

    /// <summary>绘制行为基础字段与原计时迁移入口，语言切换不执行迁移。</summary>
    void InspBasic(){
        if(configFile.UsesExplicitTiming) DrawSerializedFields("timelineFrameRate");
        else {
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Legacy timing uses the first clip FPS. Migration preserves event frame numbers; recheck mixed-rate clips afterward.", "旧计时采用首个动画剪辑的帧率。迁移保留事件帧号；迁移后请复查混合帧率剪辑。"),MessageType.None);
            if(GUILayout.Button(SkillEditorLocalization.Text("Migrate to explicit timing", "迁移为明确帧率")) && EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Migrate timeline", "迁移时间轴"),SkillEditorLocalization.Text("A backup will be created. Mixed-rate clip lengths and overlaps may change; review the timeline afterward.", "将创建备份。混合帧率剪辑的长度和重叠可能变化，迁移后请检查时间轴。"),SkillEditorLocalization.Text("Back up and migrate", "备份并迁移"),SkillEditorLocalization.Text("Cancel", "取消"))) {
                // 迁移的备份路径和错误文本来自原服务，只有明确按钮确认才调用。
                if(ActionTimingMigration.Migrate(configFile,true,out var /* 迁移服务创建的备份资产路径，供用户后续恢复原稿。 */ backup,out var /* 原迁移服务返回的错误详情，不作为界面逻辑标识。 */ error)) LoadConfig();
                else Debug.LogError(error,configFile);
            }
        }
        DrawSerializedFields("phases");
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Action", "行为"),EditorStyles.boldLabel);
        DrawSerializedFields("skillName","skillDescription","ownerType","exitFrame");
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Mechanics", "机制"),EditorStyles.boldLabel);
        DrawSerializedFields("skillCD","cancelPriority","energyCostOnActivate");
        showAdvanced=EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Show advanced properties", "显示高级属性"),showAdvanced);
        if(showAdvanced) DrawSerializedFields("skillID","skillType","defaultEnergyGainOnHit","moveCancelFrame","phase2RangeMultiplier","phase2AttackList","phase2FxList","moveCurve","totalMoveDistance");
    }
    /// <summary>绘制新建攻击使用的默认判定参数，形状选项索引保持原值。</summary>
    void InspHitbox(){
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Default hit shape", "默认攻击判定形状"),EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Defaults for newly created attack events.", "用于新建攻击事件的默认判定参数。"),MessageType.None);
        skillShapeSelectIndex=EditorGUILayout.Popup(SkillEditorLocalization.Text("Shape", "形状"),skillShapeSelectIndex,new[] { SkillEditorLocalization.Text("Sphere", "球体"), SkillEditorLocalization.Text("Box", "盒体") });
        range1=EditorGUILayout.FloatField(SkillEditorLocalization.Text("Radius / length", "半径／长度（米）"),range1);
        range2=EditorGUILayout.FloatField(SkillEditorLocalization.Text("Width", "宽度（米）"),range2);
        // 当前原向量输入或被检查的片段引用，保持原分量或引用语义。
        var o=EditorGUILayout.Vector3Field(SkillEditorLocalization.Text("Offset", "偏移（米）"),new Vector3(offsetX,offsetY,offsetZ));
        offsetX=o.x; offsetY=o.y; offsetZ=o.z;
    }
    /// <summary>把当前动画片段选择分派到序列化创作面板。</summary>
    /// <param name="seg">原配置中的动画或位移片段引用，不重新复制领域对象。</param>
    void InspAnimSeg(Global.AnimClipSegment seg){
        DrawSerializedSelection("animSegments",selIdx);
    }
    /// <summary>统计开始帧更早的其他片段数量，返回当前片段的原时间排序索引。</summary>
    /// <param name="seg">原配置中的动画或位移片段引用，不重新复制领域对象。</param>
    int GetSortedSegIndex(Global.AnimClipSegment seg){
        // 按开始帧计数得到的零起始排序索引。
        int idx=0;
        // 遍历原可索引集合的零起始位置，仍访问原对象引用。
        for(int i=0;i<animSegments.Count;i++){
            if(animSegments[i]==seg) continue;
            if(animSegments[i].startFrame<seg.startFrame) idx++;
        }
        return idx;
    }
    /// <summary>把当前基础攻击判定选择分派到序列化创作面板。</summary>
    /// <param name="a">当前攻击判定原引用；实际编辑定位仍用窗口选择索引。</param>
    void InspAtk(Global.Attack a){
        DrawSerializedSelection("attackList",selIdx);
    }
    /// <summary>把当前特效选择分派到序列化创作面板。</summary>
    /// <param name="f">当前效果原引用，粒子与音效共用原事件列表。</param>
    void InspFx(Global.FxAndSound f){
        DrawSerializedSelection("fxList",selIdx);
    }
    /// <summary>把当前音效选择分派到共享效果序列化面板。</summary>
    /// <param name="f">当前效果原引用，粒子与音效共用原事件列表。</param>
    void InspSnd(Global.FxAndSound f){
        DrawSerializedSelection("fxList",selIdx);
        if(selType!=SelType.Sound || selIdx<0 || f==null) return;
        DrawSoundAlignmentInspector(f);
        if(GUILayout.Button(SkillEditorLocalization.Text("Align trigger to playhead", "触发点对齐播放头"))) AlignSoundTrigger(f,frameSelectIndex);
        if(TryGetSoundAlignmentSeconds(f,out float /* 当前选点或主波峰相对音频开始的秒数，不是运行时裁剪偏移。 */ peakSeconds)){
            if(GUILayout.Button(SkillEditorLocalization.Text("Align selected peak to playhead", "选点／主波峰对齐播放头")))
                AlignSoundTrigger(f,frameSelectIndex-Mathf.RoundToInt(peakSeconds*GetPreviewFrameRate()));
        }
    }
    /// <summary>将原声音事件移动到指定触发帧，保留原单次哨兵或区间跨度，使用原Undo事务。</summary>
    /// <param name="effect">当前被选中的原声音事件引用，资源和音量保持。</param>
    /// <param name="triggerFrame">对齐计算得到的零起始触发帧，超出可制作范围时拒绝而不伪造对齐。</param>
    void AlignSoundTrigger(Global.FxAndSound effect,int triggerFrame){
        if(effect==null || triggerFrame<0 || triggerFrame>GetTotalFrames()){
            ShowNotification(new GUIContent(SkillEditorLocalization.Text("The aligned trigger is outside the timeline. Move the playhead first.", "对齐后的触发帧超出时间轴，请先移动播放头。")));
            return;
        }
        // 原区间跨度；结束帧为零、负数或等于开始帧时均保持单次语义。
        int span=Mathf.Max(0,effect.endKeyNumber-effect.keyNumber);
        PushUndo();
        effect.endKeyNumber=MovedEventEnd(effect.endKeyNumber,triggerFrame,span);
        effect.keyNumber=triggerFrame;
        CommitGuiChanges();
        InvalidatePreviewFrame();
        Repaint();
    }
    /// <summary>把当前命中表现选择分派到序列化创作面板。</summary>
    /// <param name="hf">当前命中表现原引用。</param>
    void InspHitFx(Global.HitFx hf){
        DrawSerializedSelection("hitFxList",selIdx);
    }
    /// <summary>绘制行为分支的语义输入菜单，显示名称与持久化命令 ID 分离。</summary>
    /// <param name="j">原行为分支引用，Jump 轨道不表示物理位移。</param>
    void InspJump(Global.Jump j){
        // 提示只翻译框架文案，语义输入命令 ID 保持原字符串。
        string commandHint;
        if (string.IsNullOrEmpty(j.triggerCommandId)) commandHint = SkillEditorLocalization.Text("Legacy key mode. The host can map inputs to semantic commands.", "旧按键模式，由宿主将输入映射到语义命令。");
        else commandHint = SkillEditorLocalization.Text("Command: ", "命令：") + j.triggerCommandId + SkillEditorLocalization.Text(". The legacy trigger key is ignored.", "。此时忽略旧触发按键。");
        EditorGUILayout.HelpBox(commandHint, MessageType.None);
        // 持久化命令标识和菜单文案分别保存，Jump 命令可表示物理跳跃输入。
        string[] options = { "", "Attack", "Shoot", "Special", "Dodge", "Jump" };
        // 当前语言菜单，与原标识数组索引一一对应。
        string[] displayOptions = { SkillEditorLocalization.Text("Legacy key / Custom", "旧按键／自定义"), SkillEditorLocalization.Text("Attack", "攻击"), SkillEditorLocalization.Text("Shoot", "射击"), SkillEditorLocalization.Text("Special", "特殊技能"), SkillEditorLocalization.Text("Dodge", "闪避"), SkillEditorLocalization.Text("Jump", "跳跃输入") };
        // 原命令在固定标识数组中的位置；自定义命令仍保持原文本。
        int selected = System.Array.IndexOf(options, j.triggerCommandId);
        // 用户选择后的显示索引，仅实际变更时写回原英文标识。
        int next = EditorGUILayout.Popup(SkillEditorLocalization.Text("Input command", "输入命令"), Mathf.Max(0, selected), displayOptions);
        if (next != Mathf.Max(0, selected)) RunAuthoringOperation(SkillEditorLocalization.Text("Select transition input", "选择跳转输入"), () => j.triggerCommandId = options[next]);
        DrawSerializedSelection("jumpList",selIdx);
    }

    // ── 常用触发按键快捷下拉框 ──────────────────────────────────
    // 根据设计文档 §1.2 输入映射，将项目中常用的按键置顶，其余 KeyCode 折叠在 "Other keys" 子菜单中。
    /// <summary>原常用按键顺序及其真实键值，绘制时动态生成语言标签。</summary>
    static readonly (string label, KeyCode key)[] _commonTriggerKeys = {
        ("Left mouse (LMB)",   KeyCode.Mouse0),
        ("Right mouse (RMB)",   KeyCode.Mouse1),
        ("Space",            KeyCode.Space),
        ("E",                KeyCode.E),
        ("LeftShift",        KeyCode.LeftShift),
        ("Middle mouse",          KeyCode.Mouse2),
    };
    /// <summary>保持原常用按键及完整按键列表功能，标签按绘制时语言生成。</summary>
    /// <param name="label">调用方指定的当前语言菜单标签。</param>
    /// <param name="current">原已选择按键值，不作为显示数组索引。</param>
    KeyCode TriggerKeyPopup(string label, KeyCode current){
        // 构建显示名称列表：常用 + 分隔 + 当前值（若不在常用列表中）
        var names = new List<string>();
        // 与显示菜单同序的原按键值集合。
        var keys  = new List<KeyCode>();
        // 原按键所在的显示索引，负数表示未匹配常用列表。
        int sel = -1;
        // 遍历原可索引集合的零起始位置，仍访问原对象引用。
        for(int i=0;i<_commonTriggerKeys.Length;i++){
            names.Add(CommonTriggerKeyLabel(_commonTriggerKeys[i].key));
            keys.Add(_commonTriggerKeys[i].key);
            if(_commonTriggerKeys[i].key==current) sel=i;
        }
        // 如果当前值不在常用列表中，追加一个条目显示它
        if(sel<0){
            names.Add(SkillEditorLocalization.Text("Current: ", "当前：") + SkillEditorPropertyLabels.EnumText(current.ToString()));
            keys.Add(current);
            sel=names.Count-1;
        }
        // 追加分隔线和 "Other..." 选项
        names.Add(""); // 分隔线占位（EditorGUI.Popup 不支持真分隔线，用空串）
        keys.Add(current); // 占位，选中时保持不变
        // 完整按键入口在菜单中的索引。
        int otherIdx=names.Count;
        names.Add(SkillEditorLocalization.Text("Other keys...", "其他按键…"));
        keys.Add(current);

        // 用户本次选择的显示索引，不直接作为真实 KeyCode。
        int newSel=EditorGUILayout.Popup(label,sel,names.ToArray());
        if(newSel==otherIdx){
            // 弹出完整 KeyCode 枚举选择
            // 元数据枚举值与显示标签按同序排列，保持非连续 KeyCode 的真实值。
            KeyCode[] allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            // 完整按键菜单文案不写入资产。
            string[] allLabels = new string[allKeys.Length];
            // 真实按键值所在的菜单索引。
            int currentIndex = 0;
            // 遍历原可索引集合的零起始位置，仍访问原对象引用。
            for (int index = 0; index < allKeys.Length; index++) { allLabels[index] = SkillEditorPropertyLabels.EnumText(allKeys[index].ToString()); if (allKeys[index] == current) currentIndex = index; }
            return allKeys[EditorGUILayout.Popup(" ", currentIndex, allLabels)];
        }
        if(newSel>=0&&newSel<keys.Count) return keys[newSel];
        return current;
    }

    /// <summary>把当前预警表现选择分派到序列化创作面板。</summary>
    /// <param name="c">当前预警表现原引用。</param>
    void InspWarn(Global.WarningCue c){
        DrawSerializedSelection("warningCueList",selIdx);
    }
    /// <summary>把当前取消窗口选择分派到序列化创作面板。</summary>
    /// <param name="cp">当前取消窗口原引用。</param>
    void InspCancel(Global.CancelPoint cp){
        DrawSerializedSelection("cancelList",selIdx);
    }
    /// <summary>把当前飞行物选择分派到序列化创作面板。</summary>
    /// <param name="p">当前飞行物原引用。</param>
    void InspProj(Global.Projectile p){
        DrawSerializedSelection("projectileList",selIdx);
    }
    /// <summary>显示片段帧范围、原驱动方式与方向参数，不修改位移数据。</summary>
    /// <param name="m">原位移片段引用，距离单位为米。</param>
    /// <param name="b">轨道标签使用的开始帧，零起始。</param>
    /// <param name="e">轨道标签使用的包含式结束帧。</param>
    static string MoveSegLabel(Global.MoveSegment m,int b,int e){
        // 驱动方式只改变轨道显示，不改原枚举或位移参数。
        string mode = SkillEditorPropertyLabels.EnumText(m.driveMode.ToString());
        // 当前语言的方向与距离轨道说明，距离单位为米。
        string dir="";
        if(m.driveMode==Global.MoveDriveMode.Program){
            dir=m.space switch{
                Global.MoveSpace.Forward=>SkillEditorPropertyLabels.EnumText("Forward"),Global.MoveSpace.TowardTarget=>SkillEditorPropertyLabels.EnumText("TowardTarget"),
                Global.MoveSpace.AwayFromTarget=>SkillEditorPropertyLabels.EnumText("AwayFromTarget"),Global.MoveSpace.WorldDirection=>SkillEditorPropertyLabels.EnumText("WorldDirection"),
                Global.MoveSpace.LocalDirection=>SkillEditorPropertyLabels.EnumText("LocalDirection"),Global.MoveSpace.InputDirection=>SkillEditorPropertyLabels.EnumText("InputDirection"),_=>""};
            if(m.origin==Global.MoveOrigin.Target) dir+=SkillEditorLocalization.Text(" / converge", "／汇聚到目标");
            dir=$" {dir} {m.maxDistance:F1}m";
        }else{
            dir=$" ×{m.rootMotionScale:F1}";
        }
        return $"{b}-{e} [{mode}]{dir}";
    }

    /// <summary>绘制旧整行为累计位移曲线及配套距离。</summary>
    void InspMove(){
        DrawSerializedFields("moveCurve","totalMoveDistance");
    }

    /// <summary>把当前位移片段选择分派到序列化创作面板。</summary>
    /// <param name="idx">原片段列表中的零起始索引。</param>
    void InspMoveSegment(int idx){
        DrawSerializedSelection("moveSegmentList",selIdx);
    }

    /// <summary>判断程序位移与原动画根运动片段是否具有包含式帧范围重叠。</summary>
    /// <param name="seg">原配置中的动画或位移片段引用，不重新复制领域对象。</param>
    bool AnyAnimationOverlap(Global.MoveSegment seg){
        if(seg.driveMode==Global.MoveDriveMode.Animation) return false;
        // 当前片段的开始帧，配套结束帧采用原包含式范围。
        int b=seg.keyNumber,e=SegEnd(seg.keyNumber,seg.endKeyNumber);
        // 使用可索引原列表，比较的仍是同一个位移片段引用。
        for (int index = 0; index < moveSegmentList.Count; index++){
            // 当前被检查的原片段。
            Global.MoveSegment o = moveSegmentList[index];
            if(ReferenceEquals(o,seg)) continue;
            if(o.driveMode!=Global.MoveDriveMode.Animation) continue;
            // 被比较原片段的开始帧，配套结束帧采用原包含式范围。
            int ob=o.keyNumber,oe=SegEnd(o.keyNumber,o.endKeyNumber);
            if(b<=oe&&ob<=e) return true;
        }
        return false;
    }
    /// <summary>判断当前程序位移方向是否依赖目标位置。</summary>
    /// <param name="seg">原配置中的动画或位移片段引用，不重新复制领域对象。</param>
    bool DependsOnTarget(Global.MoveSegment seg){
        return seg.space==Global.MoveSpace.TowardTarget||seg.space==Global.MoveSpace.AwayFromTarget;
    }

    /// <summary>绘制原角色类型、整行为霸体与第二阶段范围参数。</summary>
    void InspEnemy(){
        DrawSerializedFields("ownerType","isSuperArmorSkill","phase2RangeMultiplier");
    }
    /// <summary>把当前霸体窗口选择分派到序列化创作面板。</summary>
    /// <param name="sa">原霸体窗口引用。</param>
    void InspSuperArmor(Global.SuperArmorSegment sa){
        DrawSerializedSelection("superArmorList",selIdx);
    }
    /// <summary>把当前朝向窗口选择分派到序列化创作面板。</summary>
    /// <param name="am">原朝向窗口引用。</param>
    void InspAdjustMotion(Global.AdjustMotionSegment am){
        DrawSerializedSelection("adjustMotionList",selIdx);
    }
    /// <summary>把当前拖尾窗口选择分派到序列化创作面板。</summary>
    /// <param name="tt">原拖尾窗口引用。</param>
    void InspTrail(Global.TrailToggle tt){
        DrawSerializedSelection("trailToggleList",selIdx);
    }
    /// <summary>按真实层级名称拼接相对于根节点的路径，用户节点名不翻译。</summary>
    /// <param name="root">真实层级路径的根节点。</param>
    /// <param name="child">需要计算相对路径的原子节点。</param>
    static string GetRelativePath(Transform root,Transform child){
        if(child==root) return "";
        // 真实层级名称分段列表，不翻译用户节点名称。
        var parts=new System.Collections.Generic.List<string>();
        // 向根节点逐级导航的真实层级引用。
        Transform cur=child;
        while(cur!=null&&cur!=root){ parts.Add(cur.name); cur=cur.parent; }
        parts.Reverse();
        return string.Join("/",parts);
    }

    /// <summary>按绘制时语言显示常用按键，不缓存语言相关字符串。</summary>
    /// <param name="key">原按键值，保持输入绑定语义。</param>
    /// <returns>对应按键的当前语言标签。</returns>
    static string CommonTriggerKeyLabel(KeyCode key)
    {
        if (key == KeyCode.Mouse0) return SkillEditorLocalization.Text("Left mouse (LMB)", "鼠标左键（LMB）");
        if (key == KeyCode.Mouse1) return SkillEditorLocalization.Text("Right mouse (RMB)", "鼠标右键（RMB）");
        if (key == KeyCode.Mouse2) return SkillEditorLocalization.Text("Middle mouse", "鼠标中键");
        return SkillEditorPropertyLabels.EnumText(key.ToString());
    }

    #endregion
}
