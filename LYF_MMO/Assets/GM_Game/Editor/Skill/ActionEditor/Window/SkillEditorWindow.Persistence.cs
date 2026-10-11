// 配置、Undo、保存及备份迁移，始终保护原资产身份和已有编辑稿。
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
    #region 保存加载
    /// <summary>通过当前语言保存对话框创建新技能，默认资源文件身份仍为原NewAction。</summary>
    void CreateNewConfig(){
        // 新资产目标目录，需位于项目 Assets 内。
        string directory=ActionEditorSettings.instance.DefaultAssetDirectory;
        ActionConfigMigrationService.EnsureAssetDirectory(directory);
        // 保存对话框返回的新作者资产项目路径，取消时为空字符串。
        string path=EditorUtility.SaveFilePanelInProject(SkillEditorLocalization.Text("Create action", "创建技能"),"NewAction","asset",SkillEditorLocalization.Text("Choose where to save the action asset.", "选择技能资源保存位置。"),directory);
        if(string.IsNullOrEmpty(path)) return;
        // 本次新创建的作者资产，只有明确创建操作才保存。
        var asset=ScriptableObject.CreateInstance<SkillConfigSO>();
        asset.InitializeExplicitTiming();
        asset.SetActionEditorSchemaVersion(ActionConfigMigrationService.CurrentSchemaVersion);
        AssetDatabase.CreateAsset(asset,path);
        AssetDatabase.SaveAssetIfDirty(asset);
        configFile=asset;
        LoadConfig();
        Selection.activeObject=asset;
    }
    /// <param name="selected">本次要切换到的作者资产引用，保留其真实序列化身份。</param>
    /// <summary>结束旧配置的预览与延迟编辑，切换配置后清空选择和滚动状态。</summary>
    void SelectConfig(SkillConfigSO selected){
        StopPreviewSession();
        _editGesture.Complete();
        CommitDeferredChanges();
        configFile=selected;
        inspScrollPos=Vector2.zero;
        timelineScrollPos=Vector2.zero;
        selType=SelType.None;selIdx=-1;
        isDraggingItem=isDraggingSegment=isDraggingSegEdge=false;
        if(configFile!=null)LoadConfig();
        else { _serializedConfig=null;RefreshValidation(); }
        Repaint();
    }
    /// <summary>结束旧预览和编辑手势，重建当前配置绑定及个人预览模型，再刷新验证。</summary>
    void LoadConfig(){
        StopPreviewSession();
        _editGesture.Complete();
        CommitDeferredChanges();
        if(configFile==null) return;
        _serializedConfig=new SerializedObject(configFile);
        RefreshConfigBindings();
        previewModel=ActionEditorUserSettings.instance.GetPreviewModel(configFile);
        if(previewModel==null&&!string.IsNullOrEmpty(configFile.previewModelName)){
            // 当前原粒子或音频事件引用。
            var f=GameObject.Find(configFile.previewModelName);
            if(f!=null){ previewModel=f; previewAnim=previewModel.GetComponent<Animator>(); } }
        selType=SelType.None; selIdx=-1;
        RefreshValidation();
    }

    /// <summary>读取各原事件集合并保持对象引用；旧单剪辑和空布局只在窗口缓存补兼容显示。</summary>
    void RefreshConfigBindings(){
        if(configFile==null) return;
        InvalidatePreviewFrame();
        flowNodes=configFile.flowNodes??new List<ActionFlowNode>();
        cameraCues=configFile.cameraCues??new List<ActionCameraCue>();
        warningCueList=configFile.warningCueList??new List<Global.WarningCue>();
        jumpList=configFile.jumpList??new List<Global.Jump>();
        atkList=configFile.attackList??new List<Global.Attack>();
        fxAndSoundList=configFile.fxList??new List<Global.FxAndSound>();
        cancelList=configFile.cancelList??new List<Global.CancelPoint>();
        projectileList=configFile.projectileList??new List<Global.Projectile>();
        hitFxList=configFile.hitFxList??new List<Global.HitFx>();
        superArmorList=configFile.superArmorList??new List<Global.SuperArmorSegment>();
        adjustMotionList=configFile.adjustMotionList??new List<Global.AdjustMotionSegment>();
        trailToggleList=configFile.trailToggleList??new List<Global.TrailToggle>();
        moveSegmentList=configFile.moveSegmentList??new List<Global.MoveSegment>();
        interactionWindows=configFile.interactionWindows??new List<InteractionWindow>();
        // 多段动画
        animSegments=configFile.animSegments??new List<Global.AnimClipSegment>();
        // 向后兼容：如果 animSegments 为空但 skillClip 有值，自动创建一段
        if(animSegments.Count==0&&configFile.skillClip!=null)
            animSegments=new List<Global.AnimClipSegment>{new Global.AnimClipSegment{clip=configFile.skillClip,startFrame=0}};
        // 动态轨道
        trackList=configFile.tracks??new List<Global.SkillTrack>();
        // 向后兼容：如果轨道列表为空，创建默认轨道
        if(trackList.Count==0){
            trackList=new List<Global.SkillTrack>();
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Animation,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Move,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Attack,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Fx,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Camera,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Jump,expanded=true});
            trackList.Add(new Global.SkillTrack{type=Global.TrackType.Interaction,expanded=true});
        }
    }
    /// <summary>显式保存当前技能并显示本地化结果，语言切换不会调用此入口。</summary>
    void SaveConfig(){
        if(configFile==null) return;
        CommitDeferredChanges();
        ActionEditorTransaction.SaveIfDirty(configFile);
        RefreshValidation();
        Debug.Log(SkillEditorLocalization.Text($"Action saved: {configFile.name}", $"技能已保存：{configFile.name}"));
    }

    /// <summary>将窗口缓存重新绑定到当前资产原列表；只在编辑事务中同步，不重新生成旧轨道。</summary>
    void BindListsToConfig(){
        if(configFile==null) return;
        if(!ReferenceEquals(configFile.flowNodes,flowNodes)) configFile.flowNodes=flowNodes;
        if(!ReferenceEquals(configFile.cameraCues,cameraCues)) configFile.cameraCues=cameraCues;
        if(!ReferenceEquals(configFile.warningCueList,warningCueList)) configFile.warningCueList=warningCueList;
        if(!ReferenceEquals(configFile.jumpList,jumpList)) configFile.jumpList=jumpList;
        if(!ReferenceEquals(configFile.attackList,atkList)) configFile.attackList=atkList;
        if(!ReferenceEquals(configFile.fxList,fxAndSoundList)) configFile.fxList=fxAndSoundList;
        if(!ReferenceEquals(configFile.cancelList,cancelList)) configFile.cancelList=cancelList;
        if(!ReferenceEquals(configFile.projectileList,projectileList)) configFile.projectileList=projectileList;
        if(!ReferenceEquals(configFile.hitFxList,hitFxList)) configFile.hitFxList=hitFxList;
        if(!ReferenceEquals(configFile.superArmorList,superArmorList)) configFile.superArmorList=superArmorList;
        if(!ReferenceEquals(configFile.adjustMotionList,adjustMotionList)) configFile.adjustMotionList=adjustMotionList;
        if(!ReferenceEquals(configFile.trailToggleList,trailToggleList)) configFile.trailToggleList=trailToggleList;
        if(!ReferenceEquals(configFile.moveSegmentList,moveSegmentList)) configFile.moveSegmentList=moveSegmentList;
        if(!ReferenceEquals(configFile.interactionWindows,interactionWindows)) configFile.interactionWindows=interactionWindows;
        if(!ReferenceEquals(configFile.animSegments,animSegments)) configFile.animSegments=animSegments;
        if(!ReferenceEquals(configFile.tracks,trackList)) configFile.tracks=trackList;
        if(configFile.animSegments.Count>0) configFile.skillClip=configFile.animSegments[0].clip;
    }

    /// <summary>连续鼠标拖拽的单次撤销记录拥有者。</summary>
    readonly ActionEditorGesture _editGesture=new ActionEditorGesture();
    /// <summary>菜单同步修改的原资产，延迟提交期间锁定身份防串配置。</summary>
    SkillConfigSO _pendingEditConfig;
    /// <summary>记录当前资产编辑并绑定原列表，延迟提交始终锁定本次修改的资产身份。</summary>
    void PushUndo(){
        if(configFile==null) return;
        ActionEditorTransaction.Record(configFile,SkillEditorLocalization.Text("Edit Action", "编辑技能"));
        BindListsToConfig();
        // Menu callbacks mutate the selected asset synchronously. Capture its identity;
        // a later selection change must never commit cached fields to the new asset.
        _pendingEditConfig=configFile;
        EditorApplication.delayCall-=CommitDeferredChanges;
        EditorApplication.delayCall+=CommitDeferredChanges;
    }
    /// <summary>先提交菜单产生的延迟编辑并结束手势，再撤销 Unity 的上一事务。</summary>
    void UndoLatest(){
        _editGesture.Complete();
        CommitDeferredChanges();
        Undo.PerformUndo();
    }
    /// <summary>结束当前拖拽手势后重做 Unity 的上一编辑事务。</summary>
    void RedoLatest(){
        _editGesture.Complete();
        Undo.PerformRedo();
    }

    /// <summary>在可能改变配置的鼠标或键盘事件前记录原资产，不为普通重绘建立编辑记录。</summary>
    void PrepareUndoForCurrentEvent(){
        if(configFile==null) return;
        // 当前 Unity GUI 输入事件，消费后避免其他处理器重复响应。
        var e=Event.current;
        if(e.type==EventType.MouseDown||e.type==EventType.KeyDown||e.type==EventType.ExecuteCommand||e.type==EventType.ContextClick||e.type==EventType.DragPerform)
            ActionEditorTransaction.Record(configFile,SkillEditorLocalization.Text("Edit Action", "编辑技能"));
    }

    /// <summary>提交本次拖拽等直接字段编辑，重新绑定列表、标脏并刷新验证。</summary>
    void CommitGuiChanges(){
        if(configFile==null) return;
        BindListsToConfig();
        ActionEditorTransaction.MarkChanged(configFile);
        RefreshValidation();
        InvalidatePreviewFrame();
    }

    /// <summary>只提交菜单操作实际修改的原资产，配置切换后不能把缓存写入新配置。</summary>
    void CommitDeferredChanges(){
        EditorApplication.delayCall-=CommitDeferredChanges;
        if(_pendingEditConfig==null) return;
        ActionEditorTransaction.MarkChanged(_pendingEditConfig);
        _pendingEditConfig=null;
        RefreshValidation();
        Repaint();
        InvalidatePreviewFrame();
    }

    /// <summary>重新运行原完整配置验证并统计错误警告，出现错误时停止动画预览。</summary>
    void RefreshValidation(){
        _validationIssues.Clear();
        _validationErrorCount=0;
        _validationWarningCount=0;
        if(configFile==null) return;
        _validationIssues.AddRange(ActionConfigValidator.Validate(configFile));
        // 当前可索引集合的零起始遍历下标，访问原对象引用。
        for(int i=0;i<_validationIssues.Count;i++){
            if(_validationIssues[i].Severity==ActionValidationSeverity.Error) _validationErrorCount++;
            else if(_validationIssues[i].Severity==ActionValidationSeverity.Warning) _validationWarningCount++;
        }
        if(_validationErrorCount>0) SetPreviewPlaying(false);
    }

    /// <summary>以本地化确认对话框调用原备份迁移，只在用户明确选择升级时执行。</summary>
    void MigrateCurrentConfig(){
        if(configFile==null) return;
        if(!EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Upgrade action", "升级技能"),SkillEditorLocalization.Text($"Back up {configFile.name}, then upgrade to schema {ActionConfigMigrationService.CurrentSchemaVersion}.", $"先备份{configFile.name}，再升级到结构版本{ActionConfigMigrationService.CurrentSchemaVersion}。"),SkillEditorLocalization.Text("Back up and upgrade", "备份并升级"),SkillEditorLocalization.Text("Cancel", "取消"))) return;
        if(ActionConfigMigrationService.MigrateWithBackup(configFile,out var /* 迁移服务创建的备份资产路径，用于显示恢复提示。 */ backup,out var /* 原迁移服务返回的错误详情，不作为界面逻辑标识。 */ error)){
            Debug.Log(SkillEditorLocalization.Text($"[ActionEditor] Upgraded {configFile.name}. Backup: {backup}", $"[ActionEditor] 已升级{configFile.name}。备份：{backup}"));
            LoadConfig();
        }else EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Migration failed", "迁移失败"),MigrationErrorText(error),SkillEditorLocalization.Text("OK", "确定"));
    }

    /// <summary>选择原备份路径并确认恢复，标题本地化而文件身份和原错误保持。</summary>
    void RestoreCurrentConfig(){
        if(configFile==null) return;
        // 用户明确选择的备份文件绝对路径。
        string file=EditorUtility.OpenFilePanel(SkillEditorLocalization.Text("Select action backup", "选择技能备份"), "Assets/ActionEditorBackups", "asset");
        if(string.IsNullOrEmpty(file)) return;
        // 用户所选备份文件转换后的项目相对资产路径。
        string path=FileUtil.GetProjectRelativePath(file);
        // 从所选备份路径加载的作者资产，用于恢复当前配置。
        var backup=AssetDatabase.LoadAssetAtPath<SkillConfigSO>(path);
        if(backup==null){ EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Cannot restore", "无法恢复"),SkillEditorLocalization.Text("Select an action backup asset inside this project.", "请选择本项目中的技能备份资源。"),SkillEditorLocalization.Text("OK", "确定")); return; }
        if(!EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Restore action", "恢复技能"),SkillEditorLocalization.Text($"Restore all data in {configFile.name} from {path}.\nCurrent unsaved data will be backed up. Asset identity and scene references are preserved.", $"从{path}恢复{configFile.name}的全部数据。\n当前未保存的数据会先备份，资源身份与场景引用会保留。"),SkillEditorLocalization.Text("Back up and restore", "备份并恢复"),SkillEditorLocalization.Text("Cancel", "取消"))) return;
        if(ActionConfigMigrationService.RestoreWithBackup(configFile,backup,out var /* 恢复操作前创建的原资产备份路径。 */ recovery,out var /* 原迁移服务返回的错误详情，不作为界面逻辑标识。 */ error)){
            LoadConfig(); Debug.Log(SkillEditorLocalization.Text($"[ActionEditor] Restored. Recovery backup: {recovery}", $"[ActionEditor] 已恢复。恢复前备份：{recovery}"));
        }else EditorUtility.DisplayDialog(SkillEditorLocalization.Text("Restore failed", "恢复失败"),MigrationErrorText(error),SkillEditorLocalization.Text("OK", "确定"));
    }

    /// <summary>本地化已知迁移诊断的显示，未知系统异常保留原详情供定位，不改变原错误数据。</summary>
    /// <param name="error">迁移服务返回的原错误，可能含资源路径或系统异常文字。</param>
    /// <returns>当前语言的已知错误，未知详情在中文模式带中文说明。</returns>
    static string MigrationErrorText(string error)
    {
        if (error == "No action config selected.") return SkillEditorLocalization.Text(error, "未选择技能配置。");
        if (error == "Only project Assets can be migrated.") return SkillEditorLocalization.Text(error, "只能迁移项目Assets中的资源。");
        if (error == "Backup could not be loaded.") return SkillEditorLocalization.Text(error, "无法加载备份。");
        if (error == "Select a project action and a different asset from Assets/ActionEditorBackups.") return SkillEditorLocalization.Text(error, "请选择项目中的技能及Assets/ActionEditorBackups中另一份备份资源。");
        if (error == "The backup requires a newer action editor.") return SkillEditorLocalization.Text(error, "此备份需要更新版本的技能编辑器。");
        if (error == "Directory must be inside Assets.") return SkillEditorLocalization.Text(error, "目录必须位于Assets内。");
        // 动态诊断中的路径原样保留，不按语言改变其资源身份。
        const string backupPrefix = "Failed to back up ";
        if (!string.IsNullOrEmpty(error) && error.StartsWith(backupPrefix, StringComparison.Ordinal))
            return SkillEditorLocalization.Text(error, "备份失败：" + error.Substring(backupPrefix.Length));
        return SkillEditorLocalization.Text(error, "原始错误详情：\n" + error);
    }

    /// <summary>依据窗口可用宽度设置每帧像素，把整段时间轴放入当前视口。</summary>
    void FitTimeline(){
        // 当前时间轴总帧数，至少一帧，供计算每帧像素缩放。
        int total=Mathf.Max(1,GetTotalFrames());
        // 扣除轨道头和属性面板后的可用时间轴像素宽度。
        float available=Mathf.Max(100f,position.width-TRACK_HEADER_WIDTH-40f-(position.width>=WIDE_LAYOUT_THRESHOLD?inspectorWidth:0f));
        pixelsPerFrame=Mathf.Clamp(available/total,MIN_PPF,MAX_PPF);
        timelineScrollPos.x=0f;
        Repaint();
    }
    #endregion
}
