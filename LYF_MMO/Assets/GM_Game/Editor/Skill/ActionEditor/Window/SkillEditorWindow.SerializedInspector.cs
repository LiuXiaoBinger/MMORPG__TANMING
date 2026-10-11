// 双语序列化属性面板与创作工具，翻译仅用于标签，原字段名和序列化身份保持。
using System;
using Ethan.ActionEditor.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>技能窗口的双语属性与原编辑操作适配，不改运行时或资产字段身份。</summary>
public partial class SkillEditorWindow
{
    /// <summary>旧效果字段页签的窗口内显示缓存，关闭窗口失效，不存入配置。</summary>
    readonly System.Collections.Generic.Dictionary<string, Global.EffectContentKind> legacyEffectViews =
        new System.Collections.Generic.Dictionary<string, Global.EffectContentKind>();

    /// <summary>读取旧效果的当前字段视图，缓存只属于窗口而不改变效果内容类型。</summary>
    /// <param name="listName">原序列化列表字段名，语言切换保持原拼写。</param>
    /// <param name="index">原列表的零起始索引。</param>
    Global.EffectContentKind EffectView(string listName, int index)
    {
        // 保留所选旧特效对象引用，页签仅改变字段视图。
        Global.FxAndSound effect;
        if (listName == "fxList") effect = configFile.fxList[index];
        else effect = configFile.phase2FxList[index];
        // 仅区分配置、列表和索引的旧效果视图缓存键。
        string key = configFile.GetInstanceID() + ":" + listName + ":" + index;
        // 已有显示视图只来自窗口缓存，查询本身不修改技能字段。
        if (effect.contentKind == Global.EffectContentKind.Legacy && legacyEffectViews.TryGetValue(key, out var /* 旧效果当前个人字段视图，不替换原 contentKind。 */ view)) return view;
        return EffectAuthoring.Resolve(effect);
    }

    /// <summary>显示未迁移配置的双语字段，禁用编辑并丢弃任何绘制缓存副作用。</summary>
    void DrawReadOnlyConfig()
    {
        if (_serializedConfig == null) return;
        inspScrollPos = EditorGUILayout.BeginScrollView(inspScrollPos);
        _serializedConfig.Update();
        // 进入面板前的原缩进级别，绘制结束后恢复。
        int previousIndent = EditorGUI.indentLevel;
        // 原序列化字段游标，不修改其技术名称。
        var property = _serializedConfig.GetIterator();
        // 是否进入当前展开容器，保持原只读遍历方式。
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            EditorGUI.indentLevel = property.depth;
            // 属性是否为具有可见子字段的原容器。
            bool container = property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren;
            if (container)
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, SkillEditorPropertyLabels.Content(property), true);
            else
                using (new EditorGUI.DisabledScope(true)) SkillEditorPropertyLabels.DrawLeaf(property, SkillEditorPropertyLabels.Content(property), null, true);
            enterChildren = container && property.isExpanded;
        }
        EditorGUI.indentLevel = previousIndent;
        // 丢弃绘制器可能产生的缓存副作用，此只读入口不提交旧资产。
        _serializedConfig.Update();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>按指定原字段顺序绘制，并沿原事务链提交用户真实修改。</summary>
    /// <param name="fields">需要绘制的原序列化字段名数组，按顺序访问。</param>
    void DrawSerializedFields(params string[] fields)
    {
        if (_serializedConfig == null) return;
        _serializedConfig.Update();
        // 按字段顺序访问序列化缓存，不复制或替换技能领域对象。
        for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
        {
            // 当前原字段名保持英文序列化身份。
            string name = fields[fieldIndex];
            // 原序列化字段游标，不修改其技术名称。
            var property = _serializedConfig.FindProperty(name);
            if (property != null) DrawAuthoringProperty(property);
        }
        ApplyInspectorProperties();
    }

    /// <summary>绘制原选择事件的嵌套属性、旧效果视图和预览工具。</summary>
    /// <param name="listName">原序列化列表字段名，语言切换保持原拼写。</param>
    /// <param name="index">原列表的零起始索引。</param>
    void DrawSerializedSelection(string listName, int index)
    {
        if (listName == "warningCueList") { DrawTelegraphInspector(index); return; }
        if (listName == "flowNodes") { DrawFlowInspector(index); return; }
        if (listName == "jumpList" || listName == "cancelList") { DrawLegacyFlowInspector(listName,index); return; }
        if (listName == "attackList" || listName == "phase2AttackList") { DrawCollisionInspector(listName, index); return; }
        if (_serializedConfig == null) return;
        _serializedConfig.Update();
        // 当前原配置列表的序列化缓存。
        var list = _serializedConfig.FindProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize)
        {
            if (listName == "animSegments") DrawSerializedFields("skillClip");
            else EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Select an event first.", "请先选择事件。"), MessageType.None);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{SelectionTitle(listName)} {index+1}", EditorStyles.boldLabel);
        // 用户是否明确点击移除；语言重绘不会触发。
        bool remove = GUILayout.Button(new GUIContent(SkillEditorLocalization.Text("Remove", "移除"), SkillEditorLocalization.Text("Remove selected event (Undo supported)", "移除选中事件（支持撤销）")), GUILayout.Width(52));
        EditorGUILayout.EndHorizontal();
        if (remove)
        {
            list.DeleteArrayElementAtIndex(index);
            ApplyInspectorProperties();
            selIdx = -1;
            selType = SelType.None;
            return;
        }

        showAdvanced=EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Show advanced properties", "显示高级属性"),showAdvanced);
        if(listName=="adjustMotionList") EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Allow tracking while anticipating a strike. Lock facing while feet are planted. Locks win overlaps; gaps hold facing when windows are authored.", "出招准备阶段可允许追踪目标；双脚固定时锁定朝向。重叠时锁定优先；配置了朝向窗口时，空档保持朝向。"),MessageType.None);
        if(listName=="cameraCues") EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Played by an ActionCameraCue receiver. The bundled ActionCameraDriver needs a dedicated camera pivot or lens. Camera playback is not simulated in Scene preview.", "由 ActionCameraCue 接收器播放。内置 ActionCameraDriver 需要独立镜头枢轴或镜头；Scene 预览不模拟镜头播放。"),MessageType.None);
        // 当前列表或容器是否使用共享特效／音效视图规则。
        bool isEffect = listName == "fxList" || listName == "phase2FxList";
        // 旧特效字段视图与原 contentKind 分离，不因换语言写回数据。
        Global.EffectContentKind effectView = Global.EffectContentKind.Legacy;
        if (isEffect) effectView = EffectView(listName, index);
        // 当前原列表元素的属性游标。
        var item = list.GetArrayElementAtIndex(index);
        if (isEffect)
        {
            // 原事件是否为兼容类型，页签只切换显示。
            bool legacy = item.FindPropertyRelative("contentKind").enumValueIndex == 0;
            if (legacy)
            {
                // 音效视图索引为 1，粒子视图索引为 0，索引不随语言改变。
                int currentTab = 0;
                if (effectView == Global.EffectContentKind.Audio) currentTab = 1;
                // 当前显示页索引，0 为粒子，1 为音效。
                int tab = GUILayout.Toolbar(currentTab, new[] { SkillEditorLocalization.Text("Visual Effect", "粒子特效"), SkillEditorLocalization.Text("Audio", "音效") });
                if (tab == 1) effectView = Global.EffectContentKind.Audio;
                else effectView = Global.EffectContentKind.VisualEffect;
                legacyEffectViews[configFile.GetInstanceID() + ":" + listName + ":" + index] = effectView;
                EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Legacy event: tabs only change the property view. Existing visual and audio references are preserved and both can play.", "旧事件：页签只切换字段视图，现有特效和音效引用均保留，两者都能播放。"), MessageType.None);
            }
            else
            {
                if (effectView == Global.EffectContentKind.Audio) EditorGUILayout.LabelField(SkillEditorLocalization.Text("Audio", "音效"), EditorStyles.boldLabel);
                else EditorGUILayout.LabelField(SkillEditorLocalization.Text("Visual Effect", "粒子特效"), EditorStyles.boldLabel);
            }
        }
        // 当前容器的序列化结束标记，防止递归进入相邻字段。
        var end = item.GetEndProperty();
        // 子属性游标副本，不改变上层遍历位置。
        var child = item.Copy();
        if (child.NextVisible(true))
            do
            {
                if (SerializedProperty.EqualContents(child, end)) break;
                // 当前字段的显隐规则沿用原特效视图或基础字段规则。
                bool visible;
                if (isEffect) visible = EffectAuthoring.IsFieldVisible(effectView, child.name, showAdvanced);
                else visible = showAdvanced || IsEssentialField(listName, child.name);
                if (visible) DrawAuthoringProperty(child);
            } while (child.NextVisible(false));
        ApplyInspectorProperties();
        if (isEffect && effectView != Global.EffectContentKind.Audio)
        {
            // 应用字段编辑后重新取得原事件引用，秒数与轨道同帧更新。
            Global.FxAndSound selectedEffect = null;
            if (listName == "fxList" && index < configFile.fxList.Count) selectedEffect = configFile.fxList[index];
            else if (listName == "phase2FxList" && index < configFile.phase2FxList.Count) selectedEffect = configFile.phase2FxList[index];
            if (selectedEffect != null)
            {
                EditorGUILayout.HelpBox(EffectTimingLabel(selectedEffect.keyNumber, SegEnd(selectedEffect.keyNumber, selectedEffect.endKeyNumber)) + "\n" + EffectRangeLabel(selectedEffect) + "\n" + EffectDurationLabel(selectedEffect) + "\n" +
                    SkillEditorLocalization.Text("Each event plays at its own trigger without waiting for earlier effects. Emission may begin after the prefab's internal delay. The white line marks trigger, gray marks estimated delay, green marks estimated emission/tail and orange marks estimated end. Parameter estimates are not observed visibility; random/curve/distance emission may have no reliable start point.",
                        "每个事件按自身触发帧独立播放，不等待前一个特效。发射可能受预制体内部延迟影响：白线为触发，灰段为预计延迟，淡绿段为预计发射及尾粒，橙线为估算结束。参数估算不是可见效果实测；随机、曲线或距离发射可能无法可靠定位首发。"), MessageType.None);
                DrawSelectedEffectTimingTools(selectedEffect);
            }
        }
        if(showAdvanced || listName=="fxList" || listName=="phase2FxList") DrawSelectedAuthoringTools(listName, index);
    }

    /// <summary>提供三种只读暂停定位，不改变触发帧、结束帧、Undo或配置脏状态。</summary>
    /// <param name="effect">属性页当前原特效事件，导航点按当前FPS重新计算。</param>
    private void DrawSelectedEffectTimingTools(Global.FxAndSound effect)
    {
        // 当前参数范围的导航点；负首帧表示资源不支持可靠的参数首发估算。
        int firstFrame, tailFrame;
        // 空或全部失活的资源仍能定位作者触发，但不能伪造首发和尾端。
        bool hasRange = TryGetEffectPreviewRange(effect, out firstFrame, out tailFrame);
        using (new EditorGUI.DisabledScope(!IsPreviewAllowed))
        {
            if (GUILayout.Button(SkillEditorLocalization.Text("Pause at trigger", "暂停定位到触发")))
            {
                SeekPreviewFrame(effect.keyNumber);
                SampleAnim();
            }
            using (new EditorGUI.DisabledScope(!hasRange || firstFrame < 0))
            {
                if (GUILayout.Button(SkillEditorLocalization.Text("Pause at estimated emission start", "暂停定位到预计发射")))
                {
                    SeekPreviewFrame(firstFrame);
                    SampleAnim();
                }
            }
            using (new EditorGUI.DisabledScope(!hasRange))
            {
                if (GUILayout.Button(SkillEditorLocalization.Text("Pause at estimated end / cutoff", "暂停定位到估算结束／截断")))
                {
                    SeekPreviewFrame(tailFrame);
                    SampleAnim();
                }
            }
        }
    }

    /// <summary>只在原属性缓存确有编辑时提交撤销、刷新绑定和重新校验。</summary>
    bool ApplyInspectorProperties()
    {
        if (_serializedConfig == null || configFile == null) return false;
        if (ActionConfigMigrationService.NeedsMigration(configFile))
        {
            _serializedConfig.Update();
            return false;
        }
        // 原生 ApplyModifiedProperties 负责撤销记录；导航、折叠与换语言不提交数据或标脏。
        if (!_serializedConfig.ApplyModifiedProperties()) return false;
        ActionEditorTransaction.MarkChanged(configFile);
        RefreshConfigBindings();
        InvalidatePreviewFrame();
        _totalFramesCache = -1;
        RefreshValidation();
        Repaint();
        SceneView.RepaintAll();
        return true;
    }

    /// <summary>递归显示原属性；保留原创作数值策略，语言数据不参与资产写入。</summary>
    /// <param name="property">当前 Unity 原属性游标，真实路径和值不翻译。</param>
    void DrawAuthoringProperty(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Generic && property.type == nameof(Global.Attack))
        {
            // 从原判定属性路径匹配得到的列表与索引。
            var match = System.Text.RegularExpressions.Regex.Match(property.propertyPath, @"^(attackList|phase2AttackList)\.Array\.data\[(\d+)\]$");
            if (match.Success) {
                if (GUILayout.Button(SkillEditorLocalization.Text("Edit collision ", "编辑攻击判定 ") + (int.Parse(match.Groups[2].Value) + 1))) {
                    if (match.Groups[1].Value == "attackList") selType = SelType.Attack;
                    else selType = SelType.Phase2Attack;
                    selIdx = int.Parse(match.Groups[2].Value); collisionPage = CollisionPage.Shape;
                }
                return;
            }
        }
        // 标签与提示只读取本地化显示数据，propertyPath 和属性值保持原样。
        GUIContent label = SkillEditorPropertyLabels.Content(property);
        // 保留原创作面板的数值策略：绕过 Range/Min 自动规范化，由校验报告非法值。
        if (property.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.BeginChangeCheck();
            // 用户输入的原数值，保持原单位和校验策略。
            float value = EditorGUILayout.FloatField(label, property.floatValue);
            if (EditorGUI.EndChangeCheck()) property.floatValue = value;
            return;
        }
        if (property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.BeginChangeCheck();
            // 用户输入的原数值，保持原单位和校验策略。
            int value = EditorGUILayout.IntField(label, property.intValue);
            if (EditorGUI.EndChangeCheck()) property.intValue = value;
            return;
        }
        if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
        {
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);
            if (!property.isExpanded) return;
            if (property.isArray)
            {
                EditorGUI.BeginChangeCheck();
                // 用户输入的元素数量或判定尺寸，沿原字段类型保存。
                int size = EditorGUILayout.IntField(SkillEditorLocalization.Text("Count", "数量"), property.arraySize);
                if (EditorGUI.EndChangeCheck())
                {
                    property.arraySize = Mathf.Max(0, size);
                    return; // 调整数组后不再遍历旧元素游标。
                }
                EditorGUI.indentLevel++;
                // 遍历原可索引集合的零起始位置，仍访问原对象引用。
                for (int i = 0; i < property.arraySize; i++)
                    DrawAuthoringProperty(property.GetArrayElementAtIndex(i));
                EditorGUI.indentLevel--;
                return;
            }
            // 当前列表或容器是否使用共享特效／音效视图规则。
            bool isEffect = property.type == nameof(Global.FxAndSound);
            // 当前效果字段视图的原枚举值。
            var kind = Global.EffectContentKind.Legacy;
            if (isEffect)
            {
                kind = (Global.EffectContentKind)property.FindPropertyRelative("contentKind").intValue;
                if (kind == Global.EffectContentKind.Legacy)
                {
                    // 只用于窗口旧效果字段视图的缓存键。
                    string viewKey = configFile.GetInstanceID() + ":" + property.propertyPath;
                    if (!legacyEffectViews.TryGetValue(viewKey, out kind))
                        {
                        if (property.FindPropertyRelative("audioClip").objectReferenceValue != null && property.FindPropertyRelative("particleSystem").objectReferenceValue == null) kind = Global.EffectContentKind.Audio;
                        else kind = Global.EffectContentKind.VisualEffect;
                    }
                    // 页签状态属于窗口显示缓存，既不修改双引用也不修改旧事件类型。
                    int currentTab = 0;
                    if (kind == Global.EffectContentKind.Audio) currentTab = 1;
                    // 本次选择的效果展示页，不写入原事件类型。
                    int chosenTab = GUILayout.Toolbar(currentTab, new[] { SkillEditorLocalization.Text("Visual Effect", "粒子特效"), SkillEditorLocalization.Text("Audio", "音效") });
                    if (chosenTab == 1) kind = Global.EffectContentKind.Audio;
                    else kind = Global.EffectContentKind.VisualEffect;
                    legacyEffectViews[viewKey] = kind;
                    EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Legacy event: tabs preserve both references. Both can play.", "旧事件：页签保留两种引用，特效与音效都能播放。"), MessageType.None);
                }
                else
                {
                    if (kind == Global.EffectContentKind.Audio) EditorGUILayout.LabelField(SkillEditorLocalization.Text("Audio", "音效"));
                    else EditorGUILayout.LabelField(SkillEditorLocalization.Text("Visual Effect", "粒子特效"));
                }
            }
            // 当前容器的序列化结束标记，防止递归进入相邻字段。
            var end = property.GetEndProperty();
            // 子属性游标副本，不改变上层遍历位置。
            var child = property.Copy();
            EditorGUI.indentLevel++;
            if (child.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(child, end)) break;
                    if (!isEffect || EffectAuthoring.IsFieldVisible(kind, child.name, showAdvanced)) DrawAuthoringProperty(child);
                } while (child.NextVisible(false));
            EditorGUI.indentLevel--;
            return;
        }
        SkillEditorPropertyLabels.DrawLeaf(property, label, () => ApplyInspectorProperties());
    }

    /// <summary>执行用户明确选择的创作操作，沿原撤销与验证刷新链保存结果。</summary>
    /// <param name="name">原创作操作的撤销显示名称。</param>
    /// <param name="operation">用户明确操作后执行的原编辑函数。</param>
    void RunAuthoringOperation(string name, Action operation)
    {
        if (configFile == null || ActionConfigMigrationService.NeedsMigration(configFile)) return;
        Undo.RecordObject(configFile, name);
        operation();
        ActionEditorTransaction.MarkChanged(configFile);
        _serializedConfig?.Update();
        RefreshConfigBindings();
        RefreshValidation();
        SceneView.RepaintAll();
    }

    /// <summary>显示骨骼采样和表现预览操作，预览仍使用原选中事件引用。</summary>
    /// <param name="listName">原序列化列表字段名，语言切换保持原拼写。</param>
    /// <param name="index">原列表的零起始索引。</param>
    void DrawSelectedAuthoringTools(string listName, int index)
    {
        EditorGUILayout.Space();
        if (listName == "attackList" || listName == "phase2AttackList")
        {
            // 当前判定仍是配置列表中的原引用。
            Global.Attack attack;
            if (listName == "attackList") attack = configFile.attackList[index];
            else attack = configFile.phase2AttackList[index];
            _trackBoneRef = EditorGUILayout.ObjectField(SkillEditorLocalization.Text("Tracking bone", "跟踪骨骼"), _trackBoneRef, typeof(Transform), true) as Transform;
            using (new EditorGUI.DisabledScope(!TryGetBonePath(Selection.activeTransform, out _)))
            {
                if (GUILayout.Button(SkillEditorLocalization.Text("Use selected bone", "使用选中骨骼"))) BindCollisionBone(attack, Selection.activeTransform);
            }
            using (new EditorGUI.DisabledScope(!TryGetBonePath(_trackBoneRef, out _)))
            {
                if (GUILayout.Button(SkillEditorLocalization.Text("Bind bone path", "绑定骨骼路径"))) BindCollisionBone(attack, _trackBoneRef);
                if (GUILayout.Button(SkillEditorLocalization.Text("Copy bone path", "复制骨骼路径")) && TryGetBonePath(_trackBoneRef, out var /* 选中跟踪骨骼相对预览角色根的层级路径，供复制。 */ path)) EditorGUIUtility.systemCopyBuffer = path;
                if (GUILayout.Button(SkillEditorLocalization.Text("Sample bone path", "采样骨骼路径"))) RunAuthoringOperation(SkillEditorLocalization.Text("Sample Action Bone", "采样行为骨骼"), () => SampleBoneTrackingOffsets(attack));
                if (GUILayout.Button(SkillEditorLocalization.Text("Update current offset", "更新当前帧偏移"))) RunAuthoringOperation(SkillEditorLocalization.Text("Update Action Bone Offset", "更新行为骨骼偏移"), () => UpdateSingleFrameBoneOffset(attack, frameSelectIndex));
            }
            EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Drag a bone from the Preview actor hierarchy into Tracking bone, or select it in Hierarchy and use Use selected bone. Paths are relative to Preview actor.", "把预览角色层级中的骨骼拖到跟踪骨骼字段，或在 Hierarchy 选中后点击使用选中骨骼。路径相对于预览角色。"), MessageType.None);
            if (GUILayout.Button(SkillEditorLocalization.Text("Clear bone offsets", "清除骨骼偏移"))) RunAuthoringOperation(SkillEditorLocalization.Text("Clear Action Bone Offsets", "清除行为骨骼偏移"), () => {
                attack.boneOffsets?.Clear(); attack.useBoneTracking = false;
            });
            if (GUILayout.Button(SkillEditorLocalization.Text("Focus hit shape", "聚焦攻击判定"))) {
                // 用于聚焦或路径计算的原角色根节点。
                var root = GetEffectivePreviewModel();
                if (root != null) FocusSceneViewOn(root.transform.TransformPoint(attack.GetOffsetAtFrame(frameSelectIndex)), 2f);
            }
        }
        using (new EditorGUI.DisabledScope(!IsPreviewAllowed))
        {
            if (listName == "fxList" || listName == "phase2FxList")
            {
                // 原粒子／音效引用仅用于预览，不生成或替换正式事件。
                Global.FxAndSound fx;
                if (listName == "fxList") fx = configFile.fxList[index];
                else fx = configFile.phase2FxList[index];
                // 旧效果当前显示页，不替换原 contentKind。
                var view = EffectView(listName, index);
                // 按钮说明按当前字段视图显示，实际预览类型保持原枚举。
                string previewLabel;
                if (view == Global.EffectContentKind.Audio) previewLabel = SkillEditorLocalization.Text("Preview from audio trigger", "从音效触发帧试听");
                else previewLabel = SkillEditorLocalization.Text("Preview from effect trigger", "从特效触发帧预览");
                if (GUILayout.Button(previewLabel)) {
                    // 从该事件触发帧开始试听完整时间轴，统一暂停/定位，不额外生成一份声音或粒子。
                    SeekPreviewFrame(fx.keyNumber);
                    SampleAnim();
                    SetPreviewPlaying(true);
                }
            }
            if (listName == "projectileList" && GUILayout.Button(SkillEditorLocalization.Text("Preview projectile", "预览飞行物"))){
                SetPreviewPlaying(true);
                SpawnPreviewProjectile(configFile.projectileList[index]);
            }
            if (listName == "jumpList" && GUILayout.Button(SkillEditorLocalization.Text("Preview transition cue", "预览跳转提示"))){
                SetPreviewPlaying(true);
                SpawnPreviewDerivePrompt(configFile.jumpList[index]);
            }
        }
        if (GUILayout.Button(SkillEditorLocalization.Text("Stop effect preview", "停止特效预览"))) StopPreviewSession();
    }
}
