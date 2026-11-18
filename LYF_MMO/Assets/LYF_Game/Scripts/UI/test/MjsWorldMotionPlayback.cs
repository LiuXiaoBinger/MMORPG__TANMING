#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>在 Play Mode 给当前本地魔剑士试播动作，宿主为编辑器临时测试对象。</summary>
[DefaultExecutionOrder(10000)]
public sealed class MjsWorldMotionPlayback : MonoBehaviour
{
    /// <summary>小键盘 1 至 5 对应五个独立动作，6 对应三段连播。</summary>
    private static readonly Key[] MotionKeys =
    {
        Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6
    };

    /// <summary>本次通过正式场景世界解析到的本地玩家。</summary>
    private PlayerEntity _player;

    /// <summary>绑定的正式角色 Animator，用于在模型切换时识别旧绑定。</summary>
    private Animator _animator;

    /// <summary>按一段、二段、三段、短轻功和长轻功排序的独立骨骼动作。</summary>
    private MjsWorldMotionPose[] _motions;

    /// <summary>第一段测试开始前保存的原角色旋转通道。</summary>
    private Quaternion[] _originalRotations;

    /// <summary>第一段测试开始前保存的骨盆局部位置。</summary>
    private Vector3 _originalPelvis;

    /// <summary>当前动作下标，小于零表示由正式动画驱动。</summary>
    private int _playing = -1;

    /// <summary>单播或连播需要停止的最后一个动作下标。</summary>
    private int _last;

    /// <summary>当前独立片段开始时的游戏时间，单位为秒。</summary>
    private float _start;

    /// <summary>从 LYF_Game 自己的场景世界读取本地玩家，供编辑器测试窗口使用。</summary>
    /// <returns>当前场景唯一的本地玩家，尚未加载时为空。</returns>
    public static PlayerEntity GetLocalPlayer()
    {
        return Global.Instance?.SceneWorld?.LocalRole;
    }

    /// <summary>将本次临时组件与正式魔剑士及测试动作关联。</summary>
    /// <param name="player">当前客户端场景世界持有的唯一 PlayerEntity。</param>
    /// <param name="animator">该玩家模型的正式 Animator。</param>
    /// <param name="motions">已按该 Animator 的骨骼绑定的五个片段。</param>
    public void Initialize(PlayerEntity player, Animator animator, MjsWorldMotionPose[] motions)
    {
        _player = player;
        _animator = animator;
        _motions = motions;
        _originalRotations = new Quaternion[motions[0].BoneCount];
    }

    /// <summary>从首帧开始单播，或通过三个独立片段连播跳跃。</summary>
    /// <param name="selection">0 至 4 对应独立片段，5 表示依次播放三个跳跃动作。</param>
    public void Play(int selection)
    {
        if (_playing < 0)
        {
            _originalPelvis = _motions[0].Capture(_originalRotations);
        }
        _playing = selection;
        _last = selection;
        if (selection == _motions.Length)
        {
            _playing = 0;
            _last = 2;
        }
        _start = Time.time;
    }

    /// <summary>结束测试姿态覆盖，并恢复测试前的骨骼通道。</summary>
    public void Stop()
    {
        if (_playing >= 0 && IsCurrentModel())
        {
            _motions[0].Restore(_originalRotations, _originalPelvis);
        }
        _playing = -1;
    }

    /// <summary>读取测试按键，然后在正式 Animator 更新结束后采样当前片段。</summary>
    private void LateUpdate()
    {
        if (!IsCurrentModel())
        {
            Stop();
            Destroy(gameObject);
            return;
        }

        ReadKeyboard();
        if (_playing < 0)
        {
            return;
        }

        // 当前独立片段已经播放的时间，跨片段时扣除已经播放的长度。
        float elapsed = Time.time - _start;
        while (_playing < _last && elapsed >= _motions[_playing].Duration)
        {
            _start += _motions[_playing].Duration;
            _playing++;
            elapsed = Time.time - _start;
        }
        if (elapsed > _motions[_playing].Duration)
        {
            Stop();
            return;
        }
        _motions[_playing].Sample(elapsed);
    }

    /// <summary>确认本地角色及 Animator 仍是测试初次绑定的模型。</summary>
    /// <returns>当前世界仍持有原本地角色的模型时返回 true。</returns>
    private bool IsCurrentModel()
    {
        // 直接从正式场景世界读取当前角色，模型池换绑后不复写旧姿态。
        PlayerEntity current = GetLocalPlayer();
        return current == _player && current?.Model?.Ator?.UnityAnimator == _animator && _animator != null;
    }

    /// <summary>只在临时测试启用后读取小键盘，正式角色方向键仍沿用项目输入。</summary>
    private void ReadKeyboard()
    {
        // 当前游戏键盘设备，键盘未接入时窗口按钮仍可触发试播。
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (keyboard.numpad0Key.wasPressedThisFrame)
        {
            Stop();
            return;
        }
        for (int index = 0; index < MotionKeys.Length; index++)
        {
            if (keyboard[MotionKeys[index]].wasPressedThisFrame)
            {
                Play(index);
                return;
            }
        }
    }

    /// <summary>退出 Play 或关闭测试窗口时归还原角色姿态。</summary>
    private void OnDisable()
    {
        Stop();
    }
}
#endif
