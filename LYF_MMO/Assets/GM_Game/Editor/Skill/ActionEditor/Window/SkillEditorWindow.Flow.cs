// 流程、旧技能衔接和取消的合并创作视图；当前游戏的 RO 施法权限仍由独立规则资产负责。
using System.Collections.Generic;
using Ethan.ActionEditor;
using UnityEditor;
using UnityEngine;

/// <summary>行为流程轨道的窗口显示、选择和属性编辑；显示语言不参与流程数据保存。</summary>
public partial class SkillEditorWindow
{
    /// <summary>当前配置中的原流程节点列表，仍使用原节点引用。</summary>
    List<ActionFlowNode> flowNodes = new List<ActionFlowNode>();
    /// <summary>一项可视流程节点的屏幕布局数据，不是运行时流程事件。</summary>
    struct FlowItem
    {
        /// <summary>用于定位原列表的选择类型。</summary>
        public SelType selection;
        /// <summary>原列表中的零起始索引。</summary>
        public int index;
        /// <summary>可视窗口开始帧，零起始。</summary>
        public int start;
        /// <summary>可视窗口最后一帧，包含该帧。</summary>
        public int end;
        /// <summary>原事件拖动结束边界，保留旧分支的排他语义。</summary>
        public int dragEnd;
        /// <summary>布局行号，零起始。</summary>
        public int lane;
        /// <summary>当次语言下的轨道标签，用户资源名原样保留。</summary>
        public string label;
    }
    /// <summary>按时间排序的当前绘制快照，仅在窗口中使用。</summary>
    readonly List<FlowItem> flowItems = new List<FlowItem>();
    /// <summary>流程轨道是否展开，保持原窗口折叠生命周期。</summary>
    bool flowExpanded = true;
    /// <summary>原节点、旧衔接、旧取消及显式结束边界的总显示数量。</summary>
    int FlowCount
    {
        get
        {
            // 数量仅用于轨道标题，不改变旧数据容器。
            int count = flowNodes.Count + jumpList.Count + cancelList.Count;
            if (configFile != null && configFile.exitFrame > 0) count++;
            return count;
        }
    }

    /// <summary>显示三种原流程类型；Branch 指行为分支而非物理跳跃。</summary>
    /// <param name="kind">原流程类型枚举。</param>
    /// <returns>当前语言标签。</returns>
    static string FlowKindLabel(ActionFlowKind kind)
    {
        if (kind == ActionFlowKind.AllowExit) return SkillEditorLocalization.Text("Allow Exit", "允许退出");
        if (kind == ActionFlowKind.Branch) return SkillEditorLocalization.Text("Branch", "行为分支");
        return SkillEditorLocalization.Text("Complete", "完成行为");
    }
    /// <summary>显示添加菜单，菜单回调仍传原枚举值。</summary>
    void ShowFlowAddMenu()
    {
        // 当次创建的显示菜单，不持久化标签。
        GenericMenu menu = new GenericMenu();
        // 反射仅获取声明枚举元数据，非业务载荷。
        ActionFlowKind[] kinds = (ActionFlowKind[])System.Enum.GetValues(typeof(ActionFlowKind));
        // 当前原列表元素的零起始下标。
        for (int index = 0; index < kinds.Length; index++)
        {
            // 回调捕获原枚举值，不捕获可变化的显示索引。
            ActionFlowKind captured = kinds[index];
            menu.AddItem(new GUIContent(FlowKindLabel(captured)), false, () => AddFlowNode(captured));
        }
        menu.ShowAsContext();
    }
    /// <summary>在播放头插入原类型流程节点，保持原默认帧范围及撤销行为。</summary>
    /// <param name="kind">用户明确选择的流程类型。</param>
    void AddFlowNode(ActionFlowKind kind)
    {
        PushUndo();
        flowNodes.Add(new ActionFlowNode { kind = kind, keyNumber = frameSelectIndex, endKeyNumber = frameSelectIndex + 10 });
        // 检查原轨道类型，语言不影响布局身份。
        bool hasTrack = trackList.Exists(/* 原布局中检查是否存在流程轨道的条目。 */ t => Ethan.ActionEditor.Editor.BuiltinTrackRegistry.Canonical(t.type) == Global.TrackType.Flow);
        if (!hasTrack) trackList.Add(new Global.SkillTrack { type = Global.TrackType.Flow, expanded = true });
        selType = SelType.Flow; selIdx = flowNodes.Count - 1;
    }
    /// <summary>计算流程轨道多行布局；保持新旧结束帧的原包含／排除规则。</summary>
    /// <returns>按原行高计算的像素高度。</returns>
    float FlowTrackHeight()
    {
        flowItems.Clear();
        // 当前行为的运行时结束帧，仅用于无上限窗口的可视长度。
        int end = 0;
        if (configFile != null) end = configFile.RuntimeEndFrame;
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for (int i = 0; i < flowNodes.Count; i++)
        {
            // 原流程节点引用，不复制领域数据。
            ActionFlowNode n = flowNodes[i]; if (n == null) continue;
            // 最后可视帧沿用流程类型和窗口约束。
            int last = Mathf.Max(n.keyNumber, end);
            if (n.kind == ActionFlowKind.Complete) last = n.keyNumber;
            else if (n.limitWindow) last = n.endKeyNumber;
            // 拖动终点保持原属性语义。
            int dragEnd = n.keyNumber;
            if (n.limitWindow && n.kind != ActionFlowKind.Complete) dragEnd = n.endKeyNumber;
            // 固定框架标签本地化，实际后续资源名和自定义分类不翻译。
            string label = FlowKindLabel(n.kind);
            if (n.kind == ActionFlowKind.Branch)
            {
                label += " / ";
                if (n.nextAction != null) label += n.nextAction.name;
                else label += SkillEditorLocalization.Text("Choose action", "选择行为");
            }
            else if (n.kind == ActionFlowKind.AllowExit && !string.IsNullOrEmpty(n.category)) label += " / " + CommandCategoryLabel(n.category);
            flowItems.Add(new FlowItem { selection = SelType.Flow, index = i, start = n.keyNumber, end = last, dragEnd = dragEnd, label = label });
        }
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for (int i = 0; i < jumpList.Count; i++)
        {
            // 旧行为分支仍保留 endKey 排他边界，非物理跳跃轨道。
            Global.Jump n = jumpList[i]; if (n == null) continue;
            // 资源名称为用户内容，只有空引用提示翻译。
            string label = FlowKindLabel(ActionFlowKind.Branch) + " / ";
            if (n.nextSkill != null) label += n.nextSkill.name;
            else label += SkillEditorLocalization.Text("Choose action", "选择行为");
            flowItems.Add(new FlowItem { selection = SelType.Jump, index = i, start = n.beginKey, end = n.endKey - 1, dragEnd = n.endKey, label = label });
        }
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for (int i = 0; i < cancelList.Count; i++)
        {
            // 原取消窗口不修改最低优先级及开放结束规则。
            Global.CancelPoint n = cancelList[i]; if (n == null) continue;
            // 开放窗口仍画至原动作结束帧。
            int last = Mathf.Max(n.keyNumber, end);
            // 仅明确有结束边界时拖动结束边界。
            int dragEnd = n.keyNumber;
            if (n.endKeyNumber > n.keyNumber) { last = n.endKeyNumber; dragEnd = n.endKeyNumber; }
            flowItems.Add(new FlowItem { selection = SelType.Cancel, index = i, start = n.keyNumber, end = last, dragEnd = dragEnd,
                label = SkillEditorLocalization.Text("Allow Exit / At least ", "允许退出／最低 ") + SkillEditorPropertyLabels.EnumText(n.minCancelPriority.ToString()) });
        }
        if (configFile != null && configFile.exitFrame > 0) flowItems.Add(new FlowItem { selection = SelType.FlowEnd, index = 0,
            start = configFile.exitFrame, end = configFile.exitFrame, dragEnd = configFile.exitFrame, label = SkillEditorLocalization.Text("Complete / Exit frame", "完成行为／结束帧") });
        flowItems.Sort((/* 比较的第一个显示条目，保持原类别及开始帧。 */ a,/* 比较的第二个显示条目，保持原类别及开始帧。 */ b) => {
            // 原排序首先按时间，再按选择类型，最后按原索引。
            int time = a.start.CompareTo(b.start); if (time != 0) return time;
            // 类型排序不依赖翻译后的标签。
            int type = a.selection.CompareTo(b.selection); if (type != 0) return type;
            return a.index.CompareTo(b.index);
        });
        // 每行的已占用可视结束帧，仅用于布局。
        List<int> ends = new List<int>();
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for (int i = 0; i < flowItems.Count; i++)
        {
            // 可视布局结构的值副本不修改领域节点。
            FlowItem item = flowItems[i];
            // 寻找不与当前标签重叠的第一行。
            int lane = 0;
            while (lane < ends.Count && ends[lane] >= item.start) lane++;
            // 原标签宽度折算为占用帧数。
            int labelEnd = item.start + Mathf.CeilToInt(190 / Mathf.Max(1, pixelsPerFrame));
            if (lane == ends.Count) ends.Add(0);
            ends[lane] = Mathf.Max(item.end, labelEnd); item.lane = lane; flowItems[i] = item;
        }
        return Mathf.Max(1, ends.Count) * TRACK_HEIGHT;
    }
    /// <summary>绘制流程轨道与原导航／拖动菜单，换语言只重建显示布局。</summary>
    /// <param name="rect">窗口内轨道像素矩形。</param>
    void DrawFlowTrack(Rect rect)
    {
        FlowTrackHeight();
        // 当前原列表元素的零起始下标。
        for (int index = 0; index < flowItems.Count; index++)
        {
            // 原节点对应的当前布局项。
            FlowItem item = flowItems[index];
            // 标签左边缘和所属行的像素位置。
            float x = rect.x + item.start * pixelsPerFrame, y = rect.y + item.lane * TRACK_HEIGHT + 3;
            // 最后一帧右边缘，保持包含末帧绘制。
            float end = rect.x + (item.end + 1) * pixelsPerFrame;
            // 当前布局项是否为窗口选择项。
            bool selected = selType == item.selection && selIdx == item.index;
            // 原选择配色，不参与资产数据。
            Color color = new Color(.60f,.68f,.90f);
            if (selected) color = new Color(.85f,.88f,1);
            EditorGUI.DrawRect(new Rect(x,y,4,TRACK_HEIGHT-6),color);
            if (item.end > item.start) EditorGUI.DrawRect(new Rect(x+4,y+TRACK_HEIGHT-10,Mathf.Max(0,end-x-4),2),new Color(color.r,color.g,color.b,.45f));
            // 原点击范围，以像素为单位。
            Rect hit = new Rect(x,y,Mathf.Max(170,Mathf.Min(350,end-x)),TRACK_HEIGHT-6);
            GUI.Label(new Rect(x+7,y,Mathf.Max(163,hit.width-7),18),item.label,EditorStyles.miniLabel);
            if (selected) DrawOutline(hit,color);
            // 当前 IMGUI 事件，仅用户输入会启动编辑手势。
            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && hit.Contains(ev.mousePosition))
            { selType=item.selection; selIdx=item.index; frameSelectIndex=item.start; StartDrag(item.selection,item.index,item.start,item.dragEnd); ev.Use(); }
            if (ev.type == EventType.ContextClick && hit.Contains(ev.mousePosition))
            {
                selType=item.selection; selIdx=item.index;
                // 菜单标签动态显示，命令仍绑定原编辑函数。
                GenericMenu menu = new GenericMenu();
                if (item.selection != SelType.FlowEnd) menu.AddItem(SkillEditorLocalization.Content("Copy", "复制"), false, CopySelected);
                menu.AddItem(SkillEditorLocalization.Content("Remove", "移除"), false, DelSel); menu.ShowAsContext(); ev.Use();
            }
        }
    }
    /// <summary>绘制流程属性，分类显示与实际分类标识严格分离。</summary>
    /// <param name="index">原流程节点列表零起始索引。</param>
    void DrawFlowInspector(int index)
    {
        if (_serializedConfig == null || index < 0 || index >= flowNodes.Count) return;
        _serializedConfig.Update();
        // 原流程列表缓存。
        SerializedProperty list = _serializedConfig.FindProperty("flowNodes");
        // 原节点属性游标，不替换节点引用。
        SerializedProperty item = list.GetArrayElementAtIndex(index);
        // 直接读取原真实枚举值，显示不会写回枚举。
        ActionFlowKind kind = (ActionFlowKind)item.FindPropertyRelative("kind").intValue;
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Action Flow / ", "行为流程／") + FlowKindLabel(kind), EditorStyles.boldLabel);
        if (GUILayout.Button(SkillEditorLocalization.Text("Remove node", "移除节点")))
        { list.DeleteArrayElementAtIndex(index); ApplyInspectorProperties(); selType=SelType.None; selIdx=-1; return; }
        CollisionFields(item, "keyNumber");
        if (kind != ActionFlowKind.Complete)
        {
            CollisionFields(item, "limitWindow");
            if (item.FindPropertyRelative("limitWindow").boolValue) CollisionFields(item, "endKeyNumber");
            else EditorGUILayout.LabelField(SkillEditorLocalization.Text("Active from this frame until the action ends.", "从此帧开始持续到行为结束。"), EditorStyles.wordWrappedMiniLabel);
        }
        if (kind == ActionFlowKind.AllowExit)
        {
            // 原分类 ID 缓存；以下固定 ID 永远使用原英文字符串。
            SerializedProperty category = item.FindPropertyRelative("category");
            // 与宿主请求匹配的原分类标识，空字符串表示任意分类。
            string[] categories = { "", "Attack", "Shoot", "Special", "Dodge", "Jump", "Movement" };
            // 只用于当前语言菜单，与 categories 原索引一一对应。
            string[] display = new string[categories.Length + 1];
            // 固定类别数组的零起始遍历下标。
            for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++) display[categoryIndex] = CommandCategoryLabel(categories[categoryIndex]);
            display[7] = SkillEditorLocalization.Text("Custom", "自定义");
            // 未知分类仍为自定义项，实际文本保持不变。
            int selected = System.Array.IndexOf(categories, category.stringValue); if (selected < 0) selected = 7;
            EditorGUI.BeginChangeCheck();
            // 菜单显示索引，语言重绘本身不构成数据编辑。
            int chosen = EditorGUILayout.Popup(SkillEditorLocalization.Text("Allowed category", "允许分类"), selected, display);
            if (EditorGUI.EndChangeCheck())
            {
                if (chosen < 7) category.stringValue = categories[chosen];
                else category.stringValue = "Custom";
            }
            if (chosen == 7) DrawAuthoringProperty(category);
            CollisionFields(item, "priorityRule");
            if (item.FindPropertyRelative("priorityRule").intValue == (int)ActionFlowPriority.AtLeast) CollisionFields(item, "minimumPriority");
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Allows an eligible action request. Without a request, the current action continues.", "允许符合条件的行为请求；没有请求时继续当前行为。"), MessageType.None);
        }
        else if (kind == ActionFlowKind.Branch)
        {
            CollisionFields(item, "nextAction", "automatic");
            if (!item.FindPropertyRelative("automatic").boolValue) CollisionFields(item, "command");
        }
        else EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Completes the skill before this frame's gameplay events. Input or AI can select the next action. Animation transitions use the host's blending policy.", "在此帧的玩法事件前结束技能。输入或 AI 可选择后续行为；动画衔接采用宿主混合策略。"), MessageType.None);
        if (kind != ActionFlowKind.Complete)
        {
            showAdvanced = EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Show conditions", "显示条件"), showAdvanced);
            if (showAdvanced) CollisionFields(item, "conditions");
        }
        ApplyInspectorProperties();
    }
    /// <summary>显示旧衔接／取消数据，保持旧帧边界与旧资源字段。</summary>
    /// <param name="listName">原序列化列表名称，不翻译。</param>
    /// <param name="index">原列表零起始索引。</param>
    void DrawLegacyFlowInspector(string listName, int index)
    {
        _serializedConfig.Update();
        // 旧节点原属性游标。
        SerializedProperty item = _serializedConfig.FindProperty(listName).GetArrayElementAtIndex(index);
        // Jump 原轨道表示行为分支。
        bool branch = listName == "jumpList";
        // 仅用于选择显示标签。
        ActionFlowKind kind = ActionFlowKind.AllowExit;
        if (branch) kind = ActionFlowKind.Branch;
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Action Flow / ", "行为流程／") + FlowKindLabel(kind), EditorStyles.boldLabel);
        if (branch) CollisionFields(item,"beginKey","endKey","nextSkill","autoTrigger","triggerCommandId");
        else CollisionFields(item,"keyNumber","endKeyNumber","minCancelPriority");
        showAdvanced = EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Show advanced properties", "显示高级属性"), showAdvanced);
        if (showAdvanced)
        {
            if (branch) CollisionFields(item,"triggerKey","requiredEnergy","energyCost","fadeDuration","requestSlowPromptOnJump","derivePromptVfx","derivePromptSound");
            else CollisionFields(item,"fadeDuration");
        }
        if (branch) EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Existing branch: the end frame is exclusive. Project resource and cue settings retain their original behavior.", "旧行为分支不包含结束帧，保留原资源与提示设置的行为。"), MessageType.None);
        else EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Existing exit rule: priority uses an inclusive minimum. End <= start remains open until completion.", "旧退出规则包含最低优先级；结束帧不大于开始帧时持续到行为完成。"), MessageType.None);
        ApplyInspectorProperties();
        if (GUILayout.Button(SkillEditorLocalization.Text("Remove node", "移除节点"))) DelSel();
    }
    /// <summary>显示旧结束帧，继续保留宿主原帧末结束次序。</summary>
    void DrawLegacyFlowEnd()
    {
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Action Flow / Complete", "行为流程／完成行为"), EditorStyles.boldLabel);
        DrawSerializedFields("exitFrame");
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Existing exit frame retains the host's original end-of-frame ordering. New Complete nodes stop before gameplay events.", "旧结束帧仍采用宿主原帧末顺序；新完成行为节点在玩法事件前停止。"), MessageType.None);
        if (GUILayout.Button(SkillEditorLocalization.Text("Remove node", "移除节点"))) DelSel();
    }
    /// <summary>显示固定输入分类；未知自定义 ID 原样展示。</summary>
    /// <param name="category">原分类 ID，Jump 是输入分类而不是轨道物理位移。</param>
    /// <returns>当前语言固定分类标签或原自定义 ID。</returns>
    static string CommandCategoryLabel(string category)
    {
        switch (category)
        {
            case "": return SkillEditorLocalization.Text("Any", "任意");
            case "Attack": return SkillEditorLocalization.Text("Attack", "攻击");
            case "Shoot": return SkillEditorLocalization.Text("Shoot", "射击");
            case "Special": return SkillEditorLocalization.Text("Special", "特殊技能");
            case "Dodge": return SkillEditorLocalization.Text("Dodge", "闪避");
            case "Jump": return SkillEditorLocalization.Text("Jump", "跳跃输入");
            case "Movement": return SkillEditorLocalization.Text("Movement", "移动输入");
            default: return category;
        }
    }
}
