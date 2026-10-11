// 攻击判定制作、骨骼绑定及属性页。只产出创作数据；正式命中、伤害和反馈需要游戏接入。
using Ethan.ActionEditor;
using UnityEditor;
using UnityEngine;

/// <summary>技能窗口的双语属性与原编辑操作适配，不改运行时或资产字段身份。</summary>
public partial class SkillEditorWindow
{
    /// <summary>原判定属性页索引，语言只改变其显示标题。</summary>
    enum CollisionPage
    {
        /// <summary>形状与场景定位页，索引为 0。</summary>
        /* 形状参数页。 */ Shape,
        /// <summary>伤害或信号响应页，索引为 1。</summary>
        /* 接触响应页。 */ Response,
        /// <summary>特效、音效与冲击反馈页，索引为 2。</summary>
        /* 命中反馈页。 */ Feedback
    }
    /// <summary>原 Scene 几何手柄模式，显示语言不改变模式索引。</summary>
    enum CollisionHandleMode
    {
        /// <summary>修改位置或当前骨骼采样偏移。</summary>
        /* 位移段。 */ Move,
        /// <summary>修改球半径或盒体完整尺寸。</summary>
        /* 尺寸编辑手柄。 */ Resize,
        /// <summary>修改盒体旋转。</summary>
        /* 旋转编辑手柄。 */ Rotate
    }
    /// <summary>当前判定页，仅属于窗口导航状态。</summary>
    CollisionPage collisionPage;
    /// <summary>当前场景手柄模式，原默认值为尺寸编辑。</summary>
    CollisionHandleMode collisionHandleMode = CollisionHandleMode.Resize;
    /// <summary>反馈页内的视觉／音效／冲击索引。</summary>
    int collisionFeedbackPage;
    /// <summary>是否显示原 Scene 编辑手柄，默认开启。</summary>
    bool editCollisionInScene = true;

    /// <summary>验证骨骼属于原预览角色层级并返回真实相对路径。</summary>
    /// <param name="bone">待绑定或复制路径的真实骨骼引用。</param>
    /// <param name="path">输出真实相对骨骼路径，用户层级名原样保留。</param>
    bool TryGetBonePath(Transform bone, out string path)
    {
        path = null;
        if (bone == null || previewModel == null) return false;
        // 用于聚焦或路径计算的原角色根节点。
        var root = previewModel.transform;
        if (bone != root && !bone.IsChildOf(root))
        {
            // 原有效预览角色，不新建或替换预览对象。
            var effective = GetEffectivePreviewModel();
            if (effective == null) return false;
            root = effective.transform;
            if (bone != root && !bone.IsChildOf(root)) return false;
        }
        path = AnimationUtility.CalculateTransformPath(bone, root);
        return true;
    }

    /// <summary>通过原创作事务绑定原骨骼路径，不修改路径名称。</summary>
    /// <param name="collision">配置列表中的原攻击判定引用。</param>
    /// <param name="bone">待绑定或复制路径的真实骨骼引用。</param>
    void BindCollisionBone(Global.Attack collision, Transform bone)
    {
        if (collision == null || !TryGetBonePath(bone, out var /* 原资产相对路径或骨骼层级路径，不翻译其字符串身份。 */ path)) return;
        RunAuthoringOperation(SkillEditorLocalization.Text("Bind Action Bone", "绑定行为骨骼"), () => {
            _trackBoneRef = bone;
            collision.trackBonePath = path;
        });
    }

    /// <summary>检查已打开编辑器的预览根节点是否一致，再复制真实骨骼路径。</summary>
    /// <param name="command">Unity 原层级菜单上下文，属于编辑器适配边界。</param>
    [MenuItem("GameObject/ACT Action Editor/Copy Bone Path", false, 49)]
    static void CopySelectedBonePath(MenuCommand command)
    {
        // 菜单上下文对应的原场景骨骼对象，用于计算相对路径。
        var selected = command.context as GameObject;
        // 当前上下文对象优先，否则读取层级选择；不改变实际骨骼身份。
        Transform bone = Selection.activeTransform;
        if (selected != null) bone = selected.transform;
        // 已经确认的真实骨骼路径，多个窗口不一致时拒绝复制。
        string result = null;
        // 活动窗口快照可按索引访问，仍保留各窗口原引用。
        SkillEditorWindow[] editors = Resources.FindObjectsOfTypeAll<SkillEditorWindow>();
        // 遍历原可索引集合的零起始位置，仍访问原对象引用。
        for (int editorIndex = 0; editorIndex < editors.Length; editorIndex++)
        {
            // 当前窗口用于确认预览根节点路径的一致性。
            SkillEditorWindow editor = editors[editorIndex];
            if (!editor.TryGetBonePath(bone, out var /* 原资产相对路径或骨骼层级路径，不翻译其字符串身份。 */ path)) continue;
            if (result != null && result != path)
            {
                EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Copy Bone Path", "复制骨骼路径"), SkillEditorLocalization.Text("Multiple action editors use different Preview actor roots. Copy the path from the intended editor's Tracking bone controls.", "多个编辑器使用不同的预览角色根节点；请在目标编辑器的跟踪骨骼控件中复制路径。"), SkillEditorLocalization.Text("OK", "确定"));
                return;
            }
            result = path;
        }
        if (result == null)
        {
            EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Copy Bone Path", "复制骨骼路径"), SkillEditorLocalization.Text("Open ACT Action Editor, assign Preview actor, then select a bone inside that actor's hierarchy.", "打开 ACT 技能编辑器并指定预览角色，再选择该角色层级内的骨骼。"), SkillEditorLocalization.Text("OK", "确定"));
            return;
        }
        EditorGUIUtility.systemCopyBuffer = result;
    }

    /// <summary>显示盒体与球体菜单，回调仍使用原形状数值。</summary>
    void ShowCollisionAddMenu()
    {
        // 当次 GUI 上下文菜单，标签不写入资产。
        var menu = new GenericMenu();
        menu.AddItem(SkillEditorLocalization.Content("Box", "盒体"), false, () => AddCollision(1));
        menu.AddItem(SkillEditorLocalization.Content("Sphere", "球体"), false, () => AddCollision(0));
        menu.ShowAsContext();
    }

    /// <summary>在当前帧创建原默认判定，默认数据名称保持英文原值。</summary>
    /// <param name="shape">原判定形状值，0 为球体，1 为盒体。</param>
    void AddCollision(int shape)
    {
        // 新数据默认名称沿用原英文；切换语言不能改变持久化默认值。
        string defaultName = "Sphere";
        if (shape == 1) defaultName = "Box";
        PushUndo();
        atkList.Add(new Global.Attack { keyNumber = frameSelectIndex, endKeyNumber = frameSelectIndex + 5,
            isReWrite = true, shapeType = shape, parameter1 = 1f, parameter2 = 1f,
            boxHeight = 2f, offset = new Vector3(0, 1, 1), autoAim = false,
            collisionName = defaultName });
        selType = SelType.Attack; selIdx = atkList.Count - 1; collisionPage = CollisionPage.Shape;
        SceneView.RepaintAll();
    }

    /// <summary>当前原攻击判定引用，包含基础或第二阶段列表，非法选择返回空。</summary>
    Global.Attack SelectedCollision
    {
        get
        {
            if (configFile == null || selIdx < 0) return null;
            if (selType == SelType.Attack && selIdx < atkList.Count) return atkList[selIdx];
            if (selType == SelType.Phase2Attack && selIdx < configFile.phase2AttackList.Count) return configFile.phase2AttackList[selIdx];
            return null;
        }
    }

    /// <summary>按原字段名访问判定或流程嵌套属性，交由统一创作绘制入口。</summary>
    /// <param name="item">原嵌套判定或流程节点的序列化游标。</param>
    /// <param name="names">原字段名称顺序数组，不作为显示文案。</param>
    void CollisionFields(SerializedProperty item, params string[] names)
    {
        // 字段名称按原顺序定位，不翻译 propertyPath 或领域属性。
        for (int index = 0; index < names.Length; index++)
        {
            // 当前原字段缓存，嵌套显示仍走统一创作绘制链。
            SerializedProperty p = item.FindPropertyRelative(names[index]);
            if (p != null) DrawAuthoringProperty(p);
        }
    }

    /// <summary>显示判定形状、响应和反馈三页，页签索引与原功能保持一致。</summary>
    /// <param name="listName">原序列化列表字段名，语言切换保持原拼写。</param>
    /// <param name="index">原列表的零起始索引。</param>
    void DrawCollisionInspector(string listName, int index)
    {
        if (_serializedConfig == null) return;
        _serializedConfig.Update();
        // 当前原配置列表的序列化缓存。
        var list = _serializedConfig.FindProperty(listName);
        if (list == null || index < 0 || index >= list.arraySize) return;
        // 当前原列表元素的属性游标。
        var item = list.GetArrayElementAtIndex(index);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SkillEditorLocalization.Text("Collision", "攻击判定"), EditorStyles.boldLabel);
        // 用户是否明确点击移除；语言重绘不会触发。
        bool remove = GUILayout.Button(SkillEditorLocalization.Text("Remove", "移除"), GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        if (remove) { list.DeleteArrayElementAtIndex(index); ApplyInspectorProperties(); selType = SelType.None; selIdx = -1; return; }
        CollisionFields(item, "collisionName", "keyNumber", "endKeyNumber");
        collisionPage = (CollisionPage)GUILayout.Toolbar((int)collisionPage, new[] { SkillEditorLocalization.Text("Shape", "形状"), SkillEditorLocalization.Text("Response", "响应"), SkillEditorLocalization.Text("Feedback", "反馈") });
        EditorGUILayout.Space(6);
        switch (collisionPage)
        {
            case CollisionPage.Shape:
                // 原形状属性，0 为球体，1 为盒体。
                var shape = item.FindPropertyRelative("shapeType");
                EditorGUI.BeginChangeCheck();
                // 用户选择后的原形状索引。
                int nextShape = EditorGUILayout.Popup(SkillEditorLocalization.Text("Shape", "形状"), shape.intValue, new[] { SkillEditorLocalization.Text("Sphere", "球体"), SkillEditorLocalization.Text("Box", "盒体") });
                if (EditorGUI.EndChangeCheck()) shape.intValue = nextShape;
                if (shape.intValue == 0) {
                    // 球体半径的原数值属性，单位为米。
                    var radius = item.FindPropertyRelative("parameter1");
                    EditorGUI.BeginChangeCheck(); 
                    // 用户输入的球体半径，单位角色局部米。
                    float value = EditorGUILayout.FloatField(SkillEditorLocalization.Text("Radius", "半径（米）"), radius.floatValue);
                    if (EditorGUI.EndChangeCheck()) radius.floatValue = value;
                } else {
                    // 盒体完整 X 尺寸原属性，配套 Y/Z 属性单位均为米。
                    var x = item.FindPropertyRelative("parameter1"); 
                    // 当前纵向绘制位置，单位窗口像素。
                    var y = item.FindPropertyRelative("boxHeight"); 
                    // 盒体宽度的原参数属性，单位模型米。
                    var z = item.FindPropertyRelative("parameter2");
                    EditorGUI.BeginChangeCheck(); 
                    // 用户输入的盒体三轴尺寸，单位角色局部米。
                    var size = EditorGUILayout.Vector3Field(SkillEditorLocalization.Text("Size", "尺寸（米）"), new Vector3(x.floatValue, y.floatValue, z.floatValue));
                    if (EditorGUI.EndChangeCheck()) { x.floatValue = size.x; y.floatValue = size.y; z.floatValue = size.z; }
                    CollisionFields(item, "collisionRotation");
                }
                CollisionFields(item, "useBoneTracking");
                if (item.FindPropertyRelative("useBoneTracking").boolValue) CollisionFields(item, "trackBonePath", "boneOffsets");
                else CollisionFields(item, "offset");
                EditorGUILayout.Space(6);
                editCollisionInScene = EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Edit in Scene", "在 Scene 中编辑"), editCollisionInScene);
                collisionHandleMode = (CollisionHandleMode)GUILayout.Toolbar((int)collisionHandleMode, new[] { SkillEditorLocalization.Text("Move", "移动"), SkillEditorLocalization.Text("Resize", "缩放尺寸"), SkillEditorLocalization.Text("Rotate", "旋转") });
                if (GUILayout.Button(SkillEditorLocalization.Text("Frame collision", "聚焦攻击判定"))) FrameSelectedCollision();
                if (GUILayout.Button(SkillEditorLocalization.Text("Go to start frame", "跳到开始帧"))) { frameSelectIndex = item.FindPropertyRelative("keyNumber").intValue; SceneView.RepaintAll(); }
                if (previewModel == null) EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Assign a Preview actor to position the volume on your model. Without one, the volume is shown at the world origin.", "指定预览角色以便把判定体定位在模型上；未指定时显示在世界原点。"), MessageType.None);
                if (item.FindPropertyRelative("useBoneTracking").boolValue)
                    EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Move edits the current sampled offset inside the collision window. Resize and Rotate affect the whole volume.", "移动只编辑判定窗口内当前帧的采样偏移；缩放尺寸和旋转作用于整个判定体。"), MessageType.None);
                break;
            case CollisionPage.Response:
                CollisionFields(item, "responseMode", "damageMode");
                if (item.FindPropertyRelative("damageMode").enumValueIndex != 0) CollisionFields(item, "tickInterval");
                if (item.FindPropertyRelative("responseMode").enumValueIndex == (int)Global.CollisionResponseMode.Signal) {
                    CollisionFields(item, "contactSignal");
                    EditorGUILayout.HelpBox(SkillEditorLocalization.Text("The host sends confirmed contacts to IActionCollisionReceiver. Signal responses do not apply damage.", "宿主向 IActionCollisionReceiver 提交确认接触；信号响应不结算伤害。"), MessageType.None);
                } else {
                    CollisionFields(item, "damageRatio", "knockbackForce", "impactLevel", "toughnessDamage", "unblockable", "unparryable", "interactionTag");
                    showAdvanced = EditorGUILayout.ToggleLeft(SkillEditorLocalization.Text("Show advanced properties", "显示高级属性"), showAdvanced);
                    if (showAdvanced) CollisionFields(item, "autoAim", "energyDeltaOnHit", "enemyAttackType", "requestSlowPromptOnHit", "slowPromptRequiredEnergy", "requestGlowPromptOnHit");
                }
                break;
            case CollisionPage.Feedback:
                // 原判定是否只有信号响应，决定是否提供冲击页。
                bool signalOnly = item.FindPropertyRelative("responseMode").enumValueIndex == (int)Global.CollisionResponseMode.Signal;
                // 信号判定只有视觉／音效两页，普通判定保留冲击页。
                int maximumFeedbackPage = 2;
                // 视觉、声音及冲击页签的本地化显示数组。
                string[] feedbackLabels = { SkillEditorLocalization.Text("Visual", "视觉"), SkillEditorLocalization.Text("Audio", "音效"), SkillEditorLocalization.Text("Impact", "冲击") };
                if (signalOnly) { maximumFeedbackPage = 1; feedbackLabels = new[] { SkillEditorLocalization.Text("Visual", "视觉"), SkillEditorLocalization.Text("Audio", "音效") }; }
                collisionFeedbackPage = GUILayout.Toolbar(Mathf.Clamp(collisionFeedbackPage, 0, maximumFeedbackPage), feedbackLabels);
                if (collisionFeedbackPage == 0) {
                    CollisionFields(item, "hitVfx");
                    if (item.FindPropertyRelative("hitVfx").objectReferenceValue != null)
                        CollisionFields(item, "hitVfxOffset", "hitVfxRotation", "hitVfxScale", "hitVfxPlaybackSpeed", "hitVfxFollowTarget", "hitVfxUseAttackerRotation");
                } else if (collisionFeedbackPage == 1) {
                    CollisionFields(item, "hitSound");
                    if (item.FindPropertyRelative("hitSound").objectReferenceValue != null) {
                        CollisionFields(item, "hitSoundVolume", "hitSoundRandomize");
                        if (item.FindPropertyRelative("hitSoundRandomize").boolValue) CollisionFields(item, "hitSoundPitchVariation", "hitSoundVolumeVariation");
                    }
                } else {
                    CollisionFields(item, "enableHitPause"); if (item.FindPropertyRelative("enableHitPause").boolValue) CollisionFields(item, "hitPauseDuration");
                    CollisionFields(item, "enableCameraShake"); if (item.FindPropertyRelative("enableCameraShake").boolValue) CollisionFields(item, "cameraShakeDuration", "cameraShakeAmplitude", "cameraShakeFrequency");
                    CollisionFields(item, "enableChromaticAberration"); if (item.FindPropertyRelative("enableChromaticAberration").boolValue) CollisionFields(item, "chromaticAberrationDuration", "chromaticAberrationIntensity");
                }
                EditorGUILayout.HelpBox(SkillEditorLocalization.Text("Feedback runs on a confirmed contact or accepted damage result, through the host receiver.", "反馈通过宿主接收器，在确认接触或接受伤害结果后播放。"), MessageType.None);
                break;
        }
        ApplyInspectorProperties();
        if (collisionPage == CollisionPage.Shape && SelectedCollision?.useBoneTracking == true) DrawSelectedAuthoringTools(listName, index);
    }

    /// <summary>按原世界几何中心与尺寸聚焦场景视图。</summary>
    void FrameSelectedCollision()
    {
        // 窗口当前原攻击判定引用。
        var collision = SelectedCollision; if (collision == null) return;
        // 原有效预览角色，未指定时按世界原点处理。
        var actor = GetEffectivePreviewModel();
        // 无预览角色时保持原世界原点几何。
        Transform actorTransform = null;
        if (actor != null) actorTransform = actor.transform;
        // 原判定几何的世界中心，单位为米。
        var center = ActionCollisionGeometry.Center(collision, actorTransform, frameSelectIndex);
        // 聚焦尺寸使用原球直径或盒体对角线长度，单位为米。
        float size = ActionCollisionGeometry.BoxSize(collision).magnitude;
        if (collision.shapeType == 0) size = collision.parameter1 * 2;
        // 原活动 Scene 视图或原窗口创建接口返回的视图。
        var scene = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        scene.Frame(new Bounds(center, Vector3.one * Mathf.Max(size, 1f)), false);
        scene.Repaint();
    }
}
