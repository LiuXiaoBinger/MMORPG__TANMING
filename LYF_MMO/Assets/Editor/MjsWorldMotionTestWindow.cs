using System;
using MMoRpgCommon.SceneEntity;
using UnityEditor;
using UnityEngine;

/// <summary>主工程大世界中对照正式动作和新版跳跃轻功的编辑器测试窗口。</summary>
public sealed class MjsWorldMotionTestWindow : EditorWindow
{
    /// <summary>仅供此编辑器窗口使用的五个独立动作所在目录。</summary>
    private const string ClipFolder = "Assets/Editor/MjsWorldMotionTestClips/";

    /// <summary>和小键盘 1 至 5 一一对应的测试资源名，实际资产为独立 .anim。</summary>
    private static readonly string[] ClipNames =
    {
        "MJS_Jump_01_Takeoff", "MJS_Jump_02_AirFlip", "MJS_Jump_03_TurnLand",
        "MJS_Qinggong_Short", "MJS_Qinggong_Long"
    };

    /// <summary>按播放入口排列的窗口按钮文字，最后一个按钮仅串接三个跳跃片段。</summary>
    private static readonly string[] NewLabels =
    {
        "一段跳", "二段跳", "三段跳", "短轻功", "长轻功", "三段连播"
    };

    /// <summary>原项目动作按钮的顺序，包含跳跃的三个原表现阶段。</summary>
    private static readonly string[] OldLabels =
    {
        "待机", "走路", "原起跳", "原空中", "原落地"
    };

    /// <summary>与原动作按钮对应的正式 Animator 状态入口，由 MAnimator 解析资源。</summary>
    private static readonly string[] OldStates =
    {
        MAnimator.STATE_IDLE, MAnimator.STATE_NONZD_MOVE_WALK,
        MAnimator.STATE_JUMP_START, MAnimator.STATE_JUMP_LOOP, MAnimator.STATE_JUMP_END
    };

    /// <summary>当前大世界 Play 会话中创建的临时播放组件。</summary>
    private MjsWorldMotionPlayback _playback;

    /// <summary>由窗口手动试播原动作时使用的正式动画包装器。</summary>
    private MAnimator _nativePreview;

    /// <summary>开始原动作对照前的正式动画状态。</summary>
    private string _restoreState;

    /// <summary>窗口手动播放的原动作状态，角色已自行换状态时不回切。</summary>
    private string _previewState;

    /// <summary>用户最近一次试播命令的结果，供窗口显示。</summary>
    private string _status = string.Empty;

    /// <summary>打开测试窗口，可在进入大世界前停靠在 Game 窗口旁边。</summary>
    [MenuItem("工具/魔剑士/大世界动作测试")]
    public static void Open()
    {
        GetWindow<MjsWorldMotionTestWindow>("大世界动作测试");
    }

    /// <summary>订阅 Play 生命周期，退出时清理本窗口的临时组件。</summary>
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    /// <summary>关闭窗口或重载脚本时停止测试，并取消生命周期订阅。</summary>
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        ClosePlayback();
    }

    /// <summary>按编辑器的检查节奏刷新本地角色和播放状态。</summary>
    private void OnInspectorUpdate()
    {
        Repaint();
    }

    /// <summary>离开 Play 时释放测试宿主，避免将临时测试组件留在场景中。</summary>
    /// <param name="state">当前编辑器 Play 生命周期阶段。</param>
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ClosePlayback();
        }
    }

    /// <summary>绘制本地角色状态、正式动作入口和本次新增动作入口。</summary>
    private void OnGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("进入大世界后，在这里启用当前魔剑士的动作测试。", MessageType.Info);
            return;
        }

        // 从客户端正式实体世界读取本地角色，测试无需选中场景中的 GameObject。
        PlayerEntity role = MjsWorldMotionPlayback.GetLocalPlayer();
        // 角色正式动画包装器，模型加载完成后提供原有状态机和 Animator。
        MAnimator native = role?.Model?.Ator;
        if (role?.BaseInfo == null || native == null || native.UnityAnimator == null)
        {
            EditorGUILayout.HelpBox("正在等待大世界本地角色和模型加载。", MessageType.Info);
            return;
        }
        if (role.BaseInfo.JobId != (int)RoleJobtype.MJS)
        {
            EditorGUILayout.HelpBox("请使用魔剑士角色进入大世界。", MessageType.Info);
            return;
        }

        if (_playback == null)
        {
            if (GUILayout.Button("启用本次测试"))
            {
                EnablePlayback(role, native.UnityAnimator);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Game 窗口使用小键盘 1–5 单播，6 连播，0 停止。方向沿用项目输入。", MessageType.Info);
            EditorGUILayout.LabelField("原项目动作", EditorStyles.boldLabel);
            for (int index = 0; index < OldLabels.Length; index++)
            {
                if (GUILayout.Button(OldLabels[index]))
                {
                    PreviewOriginal(native, index);
                }
            }

            EditorGUILayout.LabelField("新版动作", EditorStyles.boldLabel);
            for (int index = 0; index < NewLabels.Length; index++)
            {
                if (GUILayout.Button(NewLabels[index]))
                {
                    RestoreOriginal();
                    _playback.Play(index);
                    _status = "已选择 " + NewLabels[index];
                }
            }
            if (GUILayout.Button("关闭本次测试"))
            {
                ClosePlayback();
                _status = "测试已关闭。";
            }
        }
        EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedLabel);
    }

    /// <summary>从正式 Animator 的已配置状态播放原有动作。</summary>
    /// <param name="native">本次本地魔剑士的正式动画包装器。</param>
    /// <param name="index">原动作按钮的下标，对应 OldStates。</param>
    private void PreviewOriginal(MAnimator native, int index)
    {
        _playback.Stop();
        RestoreOriginal();
        // 本次手动切换前的正式状态，关闭对照时用于回切。
        string priorState = native.CurrentState;
        if (native.Play(OldStates[index]))
        {
            _nativePreview = native;
            _restoreState = priorState;
            _previewState = OldStates[index];
            _status = "正在试播原项目动作 " + OldLabels[index];
        }
        else
        {
            _status = "正式 Animator 暂时无法播放 " + OldLabels[index];
        }
    }

    /// <summary>当角色仍处于窗口手动选中的原动作时，恢复试播前的动画状态。</summary>
    private void RestoreOriginal()
    {
        // 当前本地角色实际持有的动画包装器，用于排除角色或模型已经切换的情形。
        MAnimator current = MjsWorldMotionPlayback.GetLocalPlayer()?.Model?.Ator;
        if (current != null && current == _nativePreview && current.CurrentState == _previewState)
        {
            if (!string.IsNullOrEmpty(_restoreState))
            {
                current.Play(_restoreState);
            }
        }
        _nativePreview = null;
        _restoreState = null;
        _previewState = null;
    }

    /// <summary>在当前 Play 会话创建专用宿主，并绑定正式角色的五条测试动作。</summary>
    /// <param name="role">本地场景世界中已加载模型的魔剑士角色。</param>
    /// <param name="animator">正式角色模型用于待机、移动和技能的 Animator。</param>
    private void EnablePlayback(PlayerEntity role, Animator animator)
    {
        // 先全部绑定成功，避免资源或骨骼路径错误时留下临时组件。
        MjsWorldMotionPose[] motions = new MjsWorldMotionPose[ClipNames.Length];
        try
        {
            for (int index = 0; index < ClipNames.Length; index++)
            {
                // 当前动作的独立 Editor 资源路径。
                string path = ClipFolder + ClipNames[index] + ".anim";
                // 已按正式骨架导出的原地测试动作。
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    _status = "找不到测试动作 " + path;
                    return;
                }
                motions[index] = new MjsWorldMotionPose(animator.transform, clip);
            }
        }
        catch (InvalidOperationException error)
        {
            _status = error.Message;
            return;
        }

        // 测试组件挂在独立临时宿主，随本窗口或 Play 会话结束销毁。
        GameObject host = new GameObject("魔剑士大世界动作测试");
        host.hideFlags = HideFlags.HideAndDontSave;
        _playback = host.AddComponent<MjsWorldMotionPlayback>();
        _playback.Initialize(role, animator, motions);
        _status = "已绑定本地魔剑士。";
    }

    /// <summary>销毁本测试创建的宿主，正式角色继续由原动画系统驱动。</summary>
    private void ClosePlayback()
    {
        if (_playback != null)
        {
            _playback.Stop();
            DestroyImmediate(_playback.gameObject);
            _playback = null;
        }
        RestoreOriginal();
    }
}
