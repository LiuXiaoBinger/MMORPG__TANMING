using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;

/// <summary>将魔剑士走跑跳作者资产中的声音发布到运行配置。</summary>
public static class SkillEditorAudioMotionSetup
{

    #region 字段
    /// <summary>当前正式角色普通动作的真实动画资源目录。</summary>
    private const string CommonAnimationDirectory = "Assets/LYF_Game/Animas/Role/MJS/Common/ALL/";

    /// <summary>八个普通动作作者文件后缀，含战斗持剑走跑与二、三段起跳，不作为服务器技能 ID。</summary>
    private static readonly string[] LocomotionNames = { "Walk", "Run", "JumpStart", "JumpEnd", "BattleWalk", "BattleRun", "JumpStart2", "JumpStart3" };

    /// <summary>普通动作对应的原完整剪辑文件名，不改原 Animator 资源；末两条是魔剑士二、三段起跳剪辑。</summary>
    private static readonly string[] LocomotionClips = { "Anim_MJS_Walk.anim", "Anim_MJS_Run_F.anim", "Anim_JX_Jump_Start.anim", "Anim_JX_Jump_End.anim", "Anim_MJS_Walk_ZD.anim", "Anim_MJS_Run_F_ZD.anim", "Anim_MJS_Jump_2.anim", "Anim_MJS_Jump_3.anim" };
    #endregion

    #region 普通动作音效导出
    /// <summary>将八份普通动作稿单独发布，不改变技能表身份或原角色预制体。</summary>
    [MenuItem("工具/技能/批处理/导出MJS走跑跳声音")]
    public static void ExportLocomotion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在非运行模式发布普通动作。");
        // 固定路径由运行缓存加载，与七技能表现共享原 Runtime collector。
        string path = SkillEditorAssetTools.RuntimeDirectory + "/SkillRuntimeLocomotionConfig_MJS.asset";
        // 已存在时修改同一运行配置，保留 GUID 和依赖引用。
        SkillRuntimeLocomotionConfig runtime = AssetDatabase.LoadAssetAtPath<SkillRuntimeLocomotionConfig>(path);
        if (runtime != null && EditorUtility.IsDirty(runtime)) throw new InvalidOperationException("普通动作运行配置存在未保存修改，未覆盖：" + path);
        // 先转换八份作者稿，任何稿件无效时不修改当前运行资产。
        List<SkillRuntimeLocomotionAction> actions = new List<SkillRuntimeLocomotionAction>();
        // Walk/Run/JumpStart/JumpEnd 的稳定顺序与配置字段对应，末尾追加二、三段起跳。
        for (int index = 0; index < LocomotionNames.Length; index++)
        {
            // 已通过 Unity 加载的普通动作作者稿。
            SkillConfigSO config = AssetDatabase.LoadAssetAtPath<SkillConfigSO>(LocomotionAuthoringPath(index));
            if (config == null || EditorUtility.IsDirty(config)) throw new InvalidOperationException("普通动作缺失或存在未保存修改：" + LocomotionNames[index]);
            // 每份普通动作仍沿用原完整动画，不能静默丢弃裁剪、混合或其他制作轨道。
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CommonAnimationDirectory + LocomotionClips[index]);
            ValidateLocomotion(config, clip);
            actions.Add(new SkillRuntimeLocomotionAction { AnimationLocation = AssetDatabase.GetAssetPath(clip), DurationSeconds = clip.length, AudioEvents = SkillEditorAudioMotionTools.ReadAudio(config, clip.length) });
        }
        if (runtime == null)
        {
            runtime = ScriptableObject.CreateInstance<SkillRuntimeLocomotionConfig>();
            AssetDatabase.CreateAsset(runtime, path);
        }
        Undo.RecordObject(runtime, "发布普通动作声音");
        runtime.Walk = actions[0];
        runtime.Run = actions[1];
        runtime.JumpStart = actions[2];
        runtime.JumpEnd = actions[3];
        runtime.BattleWalk = actions[4];
        runtime.BattleRun = actions[5];
        // 二、三段起跳写入新增具名字段，索引六、七对应数组末两条。
        runtime.JumpStart2 = actions[6];
        runtime.JumpStart3 = actions[7];
        EditorUtility.SetDirty(runtime);
        AssetDatabase.SaveAssetIfDirty(runtime);
    }

    /// <summary>发布前复用正式单动画与字段白名单校验，避免普通动作静默忽略未接入轨道。</summary>
    /// <param name="config">已保存的普通动作作者稿。</param>
    /// <param name="clip">对应的原完整动作剪辑。</param>
    private static void ValidateLocomotion(SkillConfigSO config, AnimationClip clip)
    {
        if (clip == null || config.skillClip != clip || config.skillID != 0) throw new InvalidOperationException("普通动作不能伪装成技能或替换原动画：" + config.name);
        // 只在内存构造验证绑定，不保存为新技能绑定，以免改变七技能数量。
        SkillEditorBinding temporary = ScriptableObject.CreateInstance<SkillEditorBinding>();
        try
        {
            temporary.Config = config;
            temporary.SourceClip = clip;
            temporary.PreviewModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LYF_Game/Prefabs/Role/Role_MJS.prefab");
            SkillEditorAssetTools.ValidateAuthoring(temporary);
            if (SkillEditorAudioMotionTools.ReadCharge(config, clip.length).Count != 0) throw new InvalidOperationException("走跑跳位移由原角色移动组件控制：" + config.name);
        }
        finally { UnityEngine.Object.DestroyImmediate(temporary); }
    }

    /// <summary>得到八份普通动作的固定作者资产路径，独立于正式技能数字 ID。</summary>
    /// <param name="index">普通动作索引，范围零至七；六、七为二、三段起跳。</param>
    /// <returns>现有作者目录下的完整 Unity 资源路径。</returns>
    private static string LocomotionAuthoringPath(int index)
    {
        return SkillEditorAssetTools.AuthoringDirectory + "/SkillConfigSO_MJS_" + LocomotionNames[index] + ".asset";
    }
    #endregion
}
