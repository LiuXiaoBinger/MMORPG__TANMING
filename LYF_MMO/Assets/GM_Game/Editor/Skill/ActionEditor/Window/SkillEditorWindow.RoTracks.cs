// RO新增轨道的完整编辑链；原秒数据仅在绘制和拖帧边界换算，不接管正式战斗执行。
using System;
using System.Collections.Generic;
using Ethan.ActionEditor;
using UnityEditor;
using UnityEngine;

/// <summary>技能窗口的RO轨道扩展，复用原时间轴、事务、剪贴板和保存生命周期。</summary>
public partial class SkillEditorWindow
{
    /// <summary>RO事件末尾追加的编辑余量，单位时间轴帧，与原轨道的五帧余量一致。</summary>
    const int RoTrackTailFrames = 5;
    /// <summary>注册RO原数据中的缺失制作类别，已有Y动画、位移、声音和判定保持原列表。</summary>
    /// <param name="definitions">当前语言的窗口注册表，追加定义不修改用户配置布局。</param>
    void RegisterRoTracks(List<TrackDef> definitions)
    {
        // 模型变色读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.ChangeColor, SelType.ChangeColor, "Color Change", "模型变色", "ChangeColorList",
            ()=>{ if (configFile == null || configFile.ChangeColorList == null) return 0; return configFile.ChangeColorList.Count; },
            /* ChangeColorList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.ChangeColorList[index] == null) return 0f; return configFile.ChangeColorList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.ChangeColorList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoChangeColor(index, seconds),
            AddRoChangeColor, RemoveRoChangeColor));
        // 技能进度条读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.SkillBar, SelType.SkillBar, "Skill Bar", "技能进度条", "SkillBarList",
            ()=>{ if (configFile == null || configFile.SkillBarList == null) return 0; return configFile.SkillBarList.Count; },
            /* SkillBarList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.SkillBarList[index] == null) return 0f; return configFile.SkillBarList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.SkillBarList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoSkillBar(index, seconds),
            AddRoSkillBar, RemoveRoSkillBar));
        // 召唤单位读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.MobUnit, SelType.MobUnit, "Summon Unit", "召唤单位", "MobUnitList",
            ()=>{ if (configFile == null || configFile.MobUnitList == null) return 0; return configFile.MobUnitList.Count; },
            /* MobUnitList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.MobUnitList[index] == null) return 0f; return configFile.MobUnitList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.MobUnitList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoMobUnit(index, seconds),
            AddRoMobUnit, RemoveRoMobUnit));
        // AI事件读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.AIEvent, SelType.AIEvent, "AI Event", "AI事件", "AIEventList",
            ()=>{ if (configFile == null || configFile.AIEventList == null) return 0; return configFile.AIEventList.Count; },
            /* AIEventList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.AIEventList[index] == null) return 0f; return configFile.AIEventList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.AIEventList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoAIEvent(index, seconds),
            AddRoAIEvent, RemoveRoAIEvent));
        // Buff事件读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.Buff, SelType.Buff, "Buff Event", "Buff事件", "BuffList",
            ()=>{ if (configFile == null || configFile.BuffList == null) return 0; return configFile.BuffList.Count; },
            /* BuffList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.BuffList[index] == null) return 0f; return configFile.BuffList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.BuffList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoBuff(index, seconds),
            AddRoBuff, RemoveRoBuff));
        // RO震屏读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.Shake, SelType.Shake, "RO Camera Shake", "RO震屏", "ShakeList",
            ()=>{ if (configFile == null || configFile.ShakeList == null) return 0; return configFile.ShakeList.Count; },
            /* ShakeList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.ShakeList[index] == null) return 0f; return configFile.ShakeList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.ShakeList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoShake(index, seconds),
            AddRoShake, RemoveRoShake));
        // 镜头后处理读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.CameraPostEffect, SelType.CameraPostEffect, "Camera Post Effect", "镜头后处理", "CameraPostEffect",
            ()=>RoPostEffectCount(),
            /* 原事件列表下标，单对象后处理只允许零。 */ index=>configFile.CameraPostEffect.At,
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.CameraPostEffect),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoCameraPostEffect(index, seconds),
            AddRoCameraPostEffect, RemoveRoCameraPostEffect));
        // 镜头特效读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.CameraFx, SelType.CameraFx, "Camera Effect", "镜头特效", "CameraFxData",
            ()=>{ if (configFile == null || configFile.CameraFxData == null) return 0; return configFile.CameraFxData.Count; },
            /* CameraFxData原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.CameraFxData[index] == null) return 0f; return configFile.CameraFxData[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.CameraFxData[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoCameraFx(index, seconds),
            AddRoCameraFx, RemoveRoCameraFx));
        // 旋转读取当前资产真实引用，Undo重建序列化对象后不持有旧列表。
        definitions.Add(CreateRoTrack(Global.TrackType.Rotate, SelType.Rotate, "Rotation", "旋转", "RotateList",
            ()=>{ if (configFile == null || configFile.RotateList == null) return 0; return configFile.RotateList.Count; },
            /* RotateList原列表下标，空项由校验报告，绘图暂置零秒。 */ index=>{ if (configFile.RotateList[index] == null) return 0f; return configFile.RotateList[index].At; },
            /* 原事件列表下标，使用原数据的秒末尾语义。 */ index=>MSkillTrackTiming.EndSeconds(configFile.RotateList[index]),
            (/* 原事件下标。 */ index, /* 新开始秒数，非负。 */ seconds)=>MoveRoRotate(index, seconds),
            AddRoRotate, RemoveRoRotate));
    }

    /// <summary>用强类型数据回调建立轨道；回调只访问注册时明确的RO领域类型，不通过反射修改业务数据。</summary>
    /// <param name="type">追加的稳定轨道枚举，旧数值保持。</param>
    /// <param name="selection">窗口内选择身份，不序列化。</param>
    /// <param name="english">英文标题，不作为数据字段。</param>
    /// <param name="chinese">中文标题，不回写配置。</param>
    /// <param name="field">真实序列化字段名，保存重开与属性编辑使用同一字段。</param>
    /// <param name="count">读取当前原列表数量或可空单对象数量。</param>
    /// <param name="begin">按原下标读取开始秒数。</param>
    /// <param name="end">按原下标读取最后触发或结束秒数。</param>
    /// <param name="move">平移原事件秒时刻，保留区间长度。</param>
    /// <param name="add">在当前播放头创建有实际领域字段的事件。</param>
    /// <param name="remove">删除原事件并保留撤销能力。</param>
    TrackDef CreateRoTrack(Global.TrackType type, SelType selection, string english, string chinese, string field,
        Func<int> count, Func<int,float> begin, Func<int,float> end, Action<int,float> move, Action add, Action<int> remove)
    {
        // 当前语言的显示名只属于本次窗口注册，不作为资源身份。
        string title = SkillEditorLocalization.Text(english, chinese);
        // 回调保存字段语义；每次执行都读取configFile的当前原对象。
        TrackDef definition = new TrackDef
        {
            type = type, selType = selection, name = english, localizedName = title,
            itemName = title, menuLabel = title, color = new Color(.65f,.55f,.85f,.85f),
            count = count, listCount = count, roBegin = begin, roEnd = end, roMove = move,
            addEvent = add, removeAt = remove,
            inspector = /* 原事件列表下标。 */ index=>DrawRoSelection(field, index),
            customHeight = ()=>RoTrackHeight(count, begin, end),
            maxEnd = ()=>RoTrackEnd(count, end)
        };
        definition.drawCustom = /* 当前轨道像素区域。 */ area=>DrawRoTrack(area, definition);
        return definition;
    }

    /// <summary>将RO秒时间映射成非重叠绘图行，索引仅用于UI导航，原事件对象不复制。</summary>
    /// <param name="count">当前真实事件数回调。</param>
    /// <param name="begin">读取原开始秒数。</param>
    /// <param name="end">读取原有效结束秒数。</param>
    /// <returns>事件下标到显示行的映射。</returns>
    Dictionary<int,int> RoLanes(Func<int> count, Func<int,float> begin, Func<int,float> end)
    {
        // 显示索引快照只保存整数，不承担领域事件载荷。
        List<int> indices = new List<int>();
        // 原事件数量在此次布局固定，保证各回调索引一致。
        int total = count();
        // 正向收集原索引，不改变原列表顺序。
        for (int index = 0; index < total; index++) indices.Add(index);
        return AssignLanes(new Dictionary<int,int>(), indices,
            /* 原列表的零起始下标。 */ index=>MSkillTrackTiming.ToFrame(configFile, begin(index)),
            /* 原列表的零起始下标。 */ index=>MSkillTrackTiming.ToFrame(configFile, end(index)));
    }

    /// <summary>根据重叠事件数提供轨道像素高度，空轨道仍保留可操作行。</summary>
    /// <param name="count">原事件数回调。</param>
    /// <param name="begin">开始秒数回调。</param>
    /// <param name="end">结束秒数回调。</param>
    float RoTrackHeight(Func<int> count, Func<int,float> begin, Func<int,float> end)
    {
        // 本次绘制的下标到显示行映射，不改原数据。
        Dictionary<int,int> lanes = RoLanes(count, begin, end);
        // 最少保留一行，行号从零开始。
        int rows = 1;
        // 映射按事件下标访问，避免枚举字典的顺序依赖。
        for (int index = 0; index < count(); index++) rows = Mathf.Max(rows, lanes[index]+1);
        return rows * TRACK_HEIGHT;
    }

    /// <summary>让RO事件的最后触发时刻参与编辑时间轴长度，尾部保留原窗口余量。</summary>
    /// <param name="count">当前事件数。</param>
    /// <param name="end">当前有效结束秒数。</param>
    int RoTrackEnd(Func<int> count, Func<int,float> end)
    {
        // 当前轨道最大末尾帧，空轨道贡献零。
        int lastFrame = 0;
        // 遍历原下标读取事件末尾，不修改数据。
        for (int index = 0; index < count(); index++) lastFrame = Mathf.Max(lastFrame, MSkillTrackTiming.ToFrame(configFile, end(index)));
        if (lastFrame > int.MaxValue-RoTrackTailFrames) return int.MaxValue;
        return lastFrame + RoTrackTailFrames;
    }

    /// <summary>绘制RO事件块并接入原选择、拖帧与复制删除菜单；未接入游戏运行不影响制作保存。</summary>
    /// <param name="area">当前轨道的窗口像素矩形。</param>
    /// <param name="definition">含具体RO数据回调的轨道注册定义。</param>
    void DrawRoTrack(Rect area, TrackDef definition)
    {
        // 本帧的原下标到显示行映射。
        Dictionary<int,int> lanes = RoLanes(definition.count, definition.roBegin, definition.roEnd);
        // 当前输入事件只在命中本事件块后消费。
        Event input = Event.current;
        // 按原事件零起始下标访问对象，绘图行由重叠区间映射决定。
        for (int index = 0; index < definition.count(); index++)
        {
            // 原秒数投影到当前FPS，帧数不回写资产。
            int begin = MSkillTrackTiming.ToFrame(configFile, definition.roBegin(index));
            // 最后有效帧至少与开始帧一致，点事件仍可选。
            int end = Mathf.Max(begin, MSkillTrackTiming.ToFrame(configFile, definition.roEnd(index)));
            // 点事件留最少八像素，范围事件包括末帧。
            Rect block = new Rect(area.x+begin*pixelsPerFrame, area.y+lanes[index]*TRACK_HEIGHT+2,
                Mathf.Max(8, (end-(float)begin+1)*pixelsPerFrame), TRACK_HEIGHT-4);
            // 非选择项用轨道颜色，选择项由白色描边表示。
            EditorGUI.DrawRect(block, definition.color);
            if (selType == definition.selType && selIdx == index) DrawOutline(block, Color.white);
            GUI.Label(block, definition.itemName+" "+(index+1), TSty.Item);
            if (input.type == EventType.MouseDown && input.button == 0 && block.Contains(input.mousePosition))
            {
                selType = definition.selType; selIdx = index; frameSelectIndex = begin;
                StartDrag(definition.selType, index, begin, end); input.Use();
            }
            if (input.type == EventType.ContextClick && block.Contains(input.mousePosition))
            {
                ShowEventContextMenu(definition, index, begin); input.Use();
            }
        }
    }

    /// <summary>按RO原字段编辑事件并通过SerializedObject提交Undo；后处理原配置保持单对象。</summary>
    /// <param name="field">真实列表或单对象字段名，不使用显示标题反查。</param>
    /// <param name="index">原事件下标，后处理为零。</param>
    void DrawRoSelection(string field, int index)
    {
        if (_serializedConfig == null) return;
        _serializedConfig.Update();
        // 指向当前原资产字段的序列化游标。
        SerializedProperty container = _serializedConfig.FindProperty(field);
        if (container == null) return;
        // 列表元素与可空单对象都在当前资产中直接编辑。
        SerializedProperty item = container;
        if (field != "CameraPostEffect")
        {
            if (index < 0 || index >= container.arraySize) return;
            item = container.GetArrayElementAtIndex(index);
        }
        EditorGUILayout.HelpBox(SkillEditorLocalization.Text(
            "RO authoring data: times are seconds. These events can be saved; game execution and Scene preview need host integration.",
            "RO制作数据：时间单位为秒，可编辑并保存；游戏执行和Scene预览需要后续宿主接入。"), MessageType.None);
        if (GUILayout.Button(SkillEditorLocalization.Text("Remove event", "移除事件")))
        {
            // 当前选择的具体轨道删除回调会记录原资产撤销。
            TrackDef definition = GetDefBySel(selType);
            if (definition != null) definition.removeAt(index);
            selType = SelType.None; selIdx = -1; return;
        }
        // 子属性游标独立于容器导航，防止越过本事件。
        SerializedProperty child = item.Copy();
        // 本事件的序列化结束标记。
        SerializedProperty stop = item.GetEndProperty();
        if (child.NextVisible(true))
        {
            do
            {
                if (SerializedProperty.EqualContents(child, stop)) break;
                SkillEditorRoTrackLabels.Draw(child, ()=>ApplyInspectorProperties());
            } while (child.NextVisible(false));
        }
        ApplyInspectorProperties();
    }

    /// <summary>将RO轨道整体移动到播放头帧，区间长度始终保留原秒数精度。</summary>
    /// <param name="selection">当前拖拽的窗口选择类型。</param>
    /// <param name="index">原事件下标。</param>
    /// <param name="frame">鼠标投影的非负目标帧。</param>
    bool TryMoveRoEvent(SelType selection, int index, int frame)
    {
        // 注册定义只为RO轨道提供秒移动委托。
        TrackDef definition = GetDefBySel(selection);
        if (definition == null || definition.roMove == null || index < 0 || index >= definition.count()) return false;
        definition.roMove(index, MSkillTrackTiming.ToSeconds(configFile, frame));
        return true;
    }

    /// <summary>定位RO校验结果，依据原At秒数导航；单对象后处理保持索引零。</summary>
    /// <param name="issue">包含RO字段名与原列表下标的独立校验记录。</param>
    bool TryLocateRoIssue(ActionValidationIssue issue)
    {
        // 验证标识只按稳定字段名分派，不使用本地化标题。
        SelType selection = SelType.None;
        switch (issue.Track)
        {
            case "ChangeColorList": selection = SelType.ChangeColor; break;
            case "SkillBarList": selection = SelType.SkillBar; break;
            case "MobUnitList": selection = SelType.MobUnit; break;
            case "AIEventList": selection = SelType.AIEvent; break;
            case "BuffList": selection = SelType.Buff; break;
            case "ShakeList": selection = SelType.Shake; break;
            case "CameraPostEffect": selection = SelType.CameraPostEffect; break;
            case "CameraFxData": selection = SelType.CameraFx; break;
            case "RotateList": selection = SelType.Rotate; break;
            default: return false;
        }
        // 当前RO轨道定义绑定真实数据回调。
        TrackDef definition = GetDefBySel(selection);
        if (definition == null || issue.EventIndex < 0 || issue.EventIndex >= definition.count()) return false;
        selType = selection; selIdx = issue.EventIndex;
        frameSelectIndex = MSkillTrackTiming.ToFrame(configFile, definition.roBegin(selIdx));
        timelineScrollPos.x = Mathf.Max(0, frameSelectIndex*pixelsPerFrame-120f);
        Repaint(); return true;
    }

    /// <summary>单个RO后处理配置为空时计数为零，不把它伪装成可重复事件列表。</summary>
    int RoPostEffectCount()
    {
        if (configFile == null || configFile.CameraPostEffect == null) return 0;
        // Unity内联序列化可能把null还原为全零对象，旧配置占位不自动恢复显示轨道。
        MCameraPostEffectData item = configFile.CameraPostEffect;
        if (!item.UseCameraEffect && !item.EveryOne && item.Range == 0f && item.At == 0f && item.End == 0f &&
            item.EffectType == CameraEffectType.CameraEffectType_RadialBlur && item.Args0 == 0f && item.Args1 == 0f && item.Index == 0)
            return 0;
        return 1;
    }

    /// <summary>在播放头创建RO模型变色原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoChangeColor()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillChangeColorData item = new MSkillChangeColorData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        item.End = item.At;
        if (configFile.ChangeColorList == null) configFile.ChangeColorList = new List<MSkillChangeColorData>();
        configFile.ChangeColorList.Add(item); selType = SelType.ChangeColor; selIdx = configFile.ChangeColorList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO模型变色原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">ChangeColorList原事件下标，从零开始。</param>
    void RemoveRoChangeColor(int index)
    {
        if (configFile == null || configFile.ChangeColorList == null || index < 0 || index >= configFile.ChangeColorList.Count) return;
        PushUndo();
        configFile.ChangeColorList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO模型变色开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoChangeColor(int index, float seconds)
    {
        if (configFile == null || configFile.ChangeColorList == null || index < 0 || index >= configFile.ChangeColorList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillChangeColorData item = configFile.ChangeColorList[index];
        if (item == null) return;
        // 保留原End-At秒差，连续拖帧不积累帧量化误差。
        float spanSeconds = item.End-item.At;
        item.End = seconds+spanSeconds;
        item.At = seconds;
    }

    /// <summary>在播放头创建RO技能进度条原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoSkillBar()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillBarData item = new MSkillBarData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.SkillBarList == null) configFile.SkillBarList = new List<MSkillBarData>();
        configFile.SkillBarList.Add(item); selType = SelType.SkillBar; selIdx = configFile.SkillBarList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO技能进度条原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">SkillBarList原事件下标，从零开始。</param>
    void RemoveRoSkillBar(int index)
    {
        if (configFile == null || configFile.SkillBarList == null || index < 0 || index >= configFile.SkillBarList.Count) return;
        PushUndo();
        configFile.SkillBarList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO技能进度条开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoSkillBar(int index, float seconds)
    {
        if (configFile == null || configFile.SkillBarList == null || index < 0 || index >= configFile.SkillBarList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillBarData item = configFile.SkillBarList[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建RO召唤单位原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoMobUnit()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillMobUnitData item = new MSkillMobUnitData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.MobUnitList == null) configFile.MobUnitList = new List<MSkillMobUnitData>();
        configFile.MobUnitList.Add(item); selType = SelType.MobUnit; selIdx = configFile.MobUnitList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO召唤单位原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">MobUnitList原事件下标，从零开始。</param>
    void RemoveRoMobUnit(int index)
    {
        if (configFile == null || configFile.MobUnitList == null || index < 0 || index >= configFile.MobUnitList.Count) return;
        PushUndo();
        configFile.MobUnitList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO召唤单位开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoMobUnit(int index, float seconds)
    {
        if (configFile == null || configFile.MobUnitList == null || index < 0 || index >= configFile.MobUnitList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillMobUnitData item = configFile.MobUnitList[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建ROAI事件原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoAIEvent()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillAIEventData item = new MSkillAIEventData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.AIEventList == null) configFile.AIEventList = new List<MSkillAIEventData>();
        configFile.AIEventList.Add(item); selType = SelType.AIEvent; selIdx = configFile.AIEventList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除ROAI事件原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">AIEventList原事件下标，从零开始。</param>
    void RemoveRoAIEvent(int index)
    {
        if (configFile == null || configFile.AIEventList == null || index < 0 || index >= configFile.AIEventList.Count) return;
        PushUndo();
        configFile.AIEventList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移ROAI事件开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoAIEvent(int index, float seconds)
    {
        if (configFile == null || configFile.AIEventList == null || index < 0 || index >= configFile.AIEventList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillAIEventData item = configFile.AIEventList[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建ROBuff事件原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoBuff()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillBuffData item = new MSkillBuffData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.BuffList == null) configFile.BuffList = new List<MSkillBuffData>();
        configFile.BuffList.Add(item); selType = SelType.Buff; selIdx = configFile.BuffList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除ROBuff事件原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">BuffList原事件下标，从零开始。</param>
    void RemoveRoBuff(int index)
    {
        if (configFile == null || configFile.BuffList == null || index < 0 || index >= configFile.BuffList.Count) return;
        PushUndo();
        configFile.BuffList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移ROBuff事件开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoBuff(int index, float seconds)
    {
        if (configFile == null || configFile.BuffList == null || index < 0 || index >= configFile.BuffList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillBuffData item = configFile.BuffList[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建RORO震屏原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoShake()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MCameraShakeData item = new MCameraShakeData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.ShakeList == null) configFile.ShakeList = new List<MCameraShakeData>();
        configFile.ShakeList.Add(item); selType = SelType.Shake; selIdx = configFile.ShakeList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RORO震屏原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">ShakeList原事件下标，从零开始。</param>
    void RemoveRoShake(int index)
    {
        if (configFile == null || configFile.ShakeList == null || index < 0 || index >= configFile.ShakeList.Count) return;
        PushUndo();
        configFile.ShakeList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RORO震屏开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoShake(int index, float seconds)
    {
        if (configFile == null || configFile.ShakeList == null || index < 0 || index >= configFile.ShakeList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MCameraShakeData item = configFile.ShakeList[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建RO镜头后处理原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoCameraPostEffect()
    {
        if (configFile == null) return;
        if (RoPostEffectCount() > 0) { selType = SelType.CameraPostEffect; selIdx = 0; return; }
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MCameraPostEffectData item = new MCameraPostEffectData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        item.End = item.At;
        item.UseCameraEffect = true;
        configFile.CameraPostEffect = item; selType = SelType.CameraPostEffect; selIdx = 0;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO镜头后处理原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">原事件下标，后处理仅允许零。</param>
    void RemoveRoCameraPostEffect(int index)
    {
        if (configFile == null || index != 0 || configFile.CameraPostEffect == null) return;
        PushUndo();
        configFile.CameraPostEffect = null;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO镜头后处理开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoCameraPostEffect(int index, float seconds)
    {
        if (configFile == null || index != 0 || configFile.CameraPostEffect == null) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MCameraPostEffectData item = configFile.CameraPostEffect;
        // 保留原End-At秒差，连续拖帧不积累帧量化误差。
        float spanSeconds = item.End-item.At;
        item.End = seconds+spanSeconds;
        item.At = seconds;
    }

    /// <summary>在播放头创建RO镜头特效原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoCameraFx()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillCameraFxData item = new MSkillCameraFxData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        if (configFile.CameraFxData == null) configFile.CameraFxData = new List<MSkillCameraFxData>();
        configFile.CameraFxData.Add(item); selType = SelType.CameraFx; selIdx = configFile.CameraFxData.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO镜头特效原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">CameraFxData原事件下标，从零开始。</param>
    void RemoveRoCameraFx(int index)
    {
        if (configFile == null || configFile.CameraFxData == null || index < 0 || index >= configFile.CameraFxData.Count) return;
        PushUndo();
        configFile.CameraFxData.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO镜头特效开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoCameraFx(int index, float seconds)
    {
        if (configFile == null || configFile.CameraFxData == null || index < 0 || index >= configFile.CameraFxData.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillCameraFxData item = configFile.CameraFxData[index];
        if (item == null) return;
        item.At = seconds;
    }

    /// <summary>在播放头创建RO旋转原数据，所有其他字段保持原默认供后续填写。</summary>
    void AddRoRotate()
    {
        if (configFile == null) return;
        PushUndo();
        // 创建的领域事件只属于当前作者资产，不触发游戏表现或服务器请求。
        MSkillRotateData item = new MSkillRotateData();
        item.At = MSkillTrackTiming.ToSeconds(configFile, frameSelectIndex);
        item.End = item.At;
        if (configFile.RotateList == null) configFile.RotateList = new List<MSkillRotateData>();
        configFile.RotateList.Add(item); selType = SelType.Rotate; selIdx = configFile.RotateList.Count-1;
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>删除RO旋转原对象，原资产Undo负责恢复，不清理其他轨道数据。</summary>
    /// <param name="index">RotateList原事件下标，从零开始。</param>
    void RemoveRoRotate(int index)
    {
        if (configFile == null || configFile.RotateList == null || index < 0 || index >= configFile.RotateList.Count) return;
        PushUndo();
        configFile.RotateList.RemoveAt(index);
        _totalFramesCache = -1; Repaint();
    }

    /// <summary>平移RO旋转开始时刻；秒精度长度与循环参数保持。</summary>
    /// <param name="index">待移动的原事件下标。</param>
    /// <param name="seconds">新的开始秒数，由当前FPS换算且非负。</param>
    void MoveRoRotate(int index, float seconds)
    {
        if (configFile == null || configFile.RotateList == null || index < 0 || index >= configFile.RotateList.Count) return;
        // 移动原对象引用，Undo或保存仍对应同一作者资产。
        MSkillRotateData item = configFile.RotateList[index];
        if (item == null) return;
        // 保留原End-At秒差，连续拖帧不积累帧量化误差。
        float spanSeconds = item.End-item.At;
        item.End = seconds+spanSeconds;
        item.At = seconds;
    }

    /// <summary>序列化边界按具体RO类型复制数值和路径；新增RO数据不包含Unity资源引用。</summary>
    bool TryCopyRoSelected()
    {
        if (configFile == null || selIdx < 0) return false;
        switch (selType)
        {
            case SelType.ChangeColor: if (configFile.ChangeColorList == null || selIdx >= configFile.ChangeColorList.Count || configFile.ChangeColorList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.ChangeColorList[selIdx]); return true;
            case SelType.SkillBar: if (configFile.SkillBarList == null || selIdx >= configFile.SkillBarList.Count || configFile.SkillBarList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.SkillBarList[selIdx]); return true;
            case SelType.MobUnit: if (configFile.MobUnitList == null || selIdx >= configFile.MobUnitList.Count || configFile.MobUnitList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.MobUnitList[selIdx]); return true;
            case SelType.AIEvent: if (configFile.AIEventList == null || selIdx >= configFile.AIEventList.Count || configFile.AIEventList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.AIEventList[selIdx]); return true;
            case SelType.Buff: if (configFile.BuffList == null || selIdx >= configFile.BuffList.Count || configFile.BuffList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.BuffList[selIdx]); return true;
            case SelType.Shake: if (configFile.ShakeList == null || selIdx >= configFile.ShakeList.Count || configFile.ShakeList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.ShakeList[selIdx]); return true;
            case SelType.CameraPostEffect: if (selIdx != 0 || configFile.CameraPostEffect == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.CameraPostEffect); return true;
            case SelType.CameraFx: if (configFile.CameraFxData == null || selIdx >= configFile.CameraFxData.Count || configFile.CameraFxData[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.CameraFxData[selIdx]); return true;
            case SelType.Rotate: if (configFile.RotateList == null || selIdx >= configFile.RotateList.Count || configFile.RotateList[selIdx] == null) { _clipboardJson = null; return true; } _clipboardJson = JsonUtility.ToJson(configFile.RotateList[selIdx]); return true;
            default: return false;
        }
    }

    /// <summary>按具体RO类型恢复剪贴板，平移原At/End秒差；单个后处理已有值时拒绝覆盖。</summary>
    /// <param name="frame">本次粘贴的播放头帧，换算成At秒数。</param>
    bool TryPasteRoClipboard(int frame)
    {
        if (configFile == null) return false;
        // 本次粘贴的目标开始秒数，数据不另存整数帧。
        float seconds = MSkillTrackTiming.ToSeconds(configFile, frame);
        switch (_clipboardType)
        {
            case SelType.ChangeColor:
            {
                // JSON边界恢复明确的模型变色类型，路径和数值均沿用原快照。
                MSkillChangeColorData item = JsonUtility.FromJson<MSkillChangeColorData>(_clipboardJson);
                if (item == null) return true;
                // 保留原秒区间，不从绘图整数帧重建长度。
                float spanSeconds = item.End-item.At;
                item.End = seconds+spanSeconds;
                item.At = seconds;
                if (configFile.ChangeColorList == null) configFile.ChangeColorList = new List<MSkillChangeColorData>();
                configFile.ChangeColorList.Add(item); selIdx = configFile.ChangeColorList.Count-1;
                selType = SelType.ChangeColor; _totalFramesCache = -1; return true;
            }
            case SelType.SkillBar:
            {
                // JSON边界恢复明确的技能进度条类型，路径和数值均沿用原快照。
                MSkillBarData item = JsonUtility.FromJson<MSkillBarData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.SkillBarList == null) configFile.SkillBarList = new List<MSkillBarData>();
                configFile.SkillBarList.Add(item); selIdx = configFile.SkillBarList.Count-1;
                selType = SelType.SkillBar; _totalFramesCache = -1; return true;
            }
            case SelType.MobUnit:
            {
                // JSON边界恢复明确的召唤单位类型，路径和数值均沿用原快照。
                MSkillMobUnitData item = JsonUtility.FromJson<MSkillMobUnitData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.MobUnitList == null) configFile.MobUnitList = new List<MSkillMobUnitData>();
                configFile.MobUnitList.Add(item); selIdx = configFile.MobUnitList.Count-1;
                selType = SelType.MobUnit; _totalFramesCache = -1; return true;
            }
            case SelType.AIEvent:
            {
                // JSON边界恢复明确的AI事件类型，路径和数值均沿用原快照。
                MSkillAIEventData item = JsonUtility.FromJson<MSkillAIEventData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.AIEventList == null) configFile.AIEventList = new List<MSkillAIEventData>();
                configFile.AIEventList.Add(item); selIdx = configFile.AIEventList.Count-1;
                selType = SelType.AIEvent; _totalFramesCache = -1; return true;
            }
            case SelType.Buff:
            {
                // JSON边界恢复明确的Buff事件类型，路径和数值均沿用原快照。
                MSkillBuffData item = JsonUtility.FromJson<MSkillBuffData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.BuffList == null) configFile.BuffList = new List<MSkillBuffData>();
                configFile.BuffList.Add(item); selIdx = configFile.BuffList.Count-1;
                selType = SelType.Buff; _totalFramesCache = -1; return true;
            }
            case SelType.Shake:
            {
                // JSON边界恢复明确的RO震屏类型，路径和数值均沿用原快照。
                MCameraShakeData item = JsonUtility.FromJson<MCameraShakeData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.ShakeList == null) configFile.ShakeList = new List<MCameraShakeData>();
                configFile.ShakeList.Add(item); selIdx = configFile.ShakeList.Count-1;
                selType = SelType.Shake; _totalFramesCache = -1; return true;
            }
            case SelType.CameraPostEffect:
            {
                if (RoPostEffectCount() > 0) return true;
                // JSON边界恢复明确的镜头后处理类型，路径和数值均沿用原快照。
                MCameraPostEffectData item = JsonUtility.FromJson<MCameraPostEffectData>(_clipboardJson);
                if (item == null) return true;
                // 保留原秒区间，不从绘图整数帧重建长度。
                float spanSeconds = item.End-item.At;
                item.End = seconds+spanSeconds;
                item.At = seconds;
                configFile.CameraPostEffect = item; selIdx = 0;
                selType = SelType.CameraPostEffect; _totalFramesCache = -1; return true;
            }
            case SelType.CameraFx:
            {
                // JSON边界恢复明确的镜头特效类型，路径和数值均沿用原快照。
                MSkillCameraFxData item = JsonUtility.FromJson<MSkillCameraFxData>(_clipboardJson);
                if (item == null) return true;
                item.At = seconds;
                if (configFile.CameraFxData == null) configFile.CameraFxData = new List<MSkillCameraFxData>();
                configFile.CameraFxData.Add(item); selIdx = configFile.CameraFxData.Count-1;
                selType = SelType.CameraFx; _totalFramesCache = -1; return true;
            }
            case SelType.Rotate:
            {
                // JSON边界恢复明确的旋转类型，路径和数值均沿用原快照。
                MSkillRotateData item = JsonUtility.FromJson<MSkillRotateData>(_clipboardJson);
                if (item == null) return true;
                // 保留原秒区间，不从绘图整数帧重建长度。
                float spanSeconds = item.End-item.At;
                item.End = seconds+spanSeconds;
                item.At = seconds;
                if (configFile.RotateList == null) configFile.RotateList = new List<MSkillRotateData>();
                configFile.RotateList.Add(item); selIdx = configFile.RotateList.Count-1;
                selType = SelType.Rotate; _totalFramesCache = -1; return true;
            }
            default: return false;
        }
    }
}
