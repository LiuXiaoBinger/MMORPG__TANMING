#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>读取单个测试剪辑的骨骼曲线，按正式 Humanoid 模型的局部骨骼坐标采样。</summary>
public sealed class MjsWorldMotionPose
{
    /// <summary>正式角色 Animator 下的骨盆路径，测试只给此骨骼写入位置曲线。</summary>
    private const string PelvisPath = "root/pelvis";

    /// <summary>由剪辑实际绑定路径解析的骨骼，顺序与旋转曲线的四元数分组相同。</summary>
    private readonly Transform[] _bones;

    /// <summary>每根骨骼连续保存 x、y、z、w 四条旋转曲线。</summary>
    private readonly AnimationCurve[] _rotationCurves;

    /// <summary>角色的骨盆关节，位置曲线描述相对角色根的升降。</summary>
    private readonly Transform _pelvis;

    /// <summary>骨盆局部位置的 x、y、z 曲线。曲线输入单位为秒。</summary>
    private readonly AnimationCurve[] _pelvisCurves;

    /// <summary>当前独立动作的长度，单位为秒。</summary>
    public float Duration { get; }

    /// <summary>供测试播放组件保存原姿态的骨骼数量。</summary>
    public int BoneCount { get { return _bones.Length; } }

    /// <summary>将测试剪辑绑定到已加载的本地角色模型。</summary>
    /// <param name="modelRoot">正式 Animator 所在对象，所有曲线路径以此为起点。</param>
    /// <param name="clip">按 SK_CALISTA_01 骨骼轴烘焙的独立测试动作。</param>
    public MjsWorldMotionPose(Transform modelRoot, AnimationClip clip)
    {
        Duration = clip.length;
        // 获取剪辑曲线绑定，初始化后采样直接复用相应的 Transform 和曲线。
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        // 当前收集的旋转关节，每个关节只从四元数 x 通道登记一次。
        List<Transform> bones = new List<Transform>();
        // 与 bones 对应的四元数分量曲线，按坐标轴顺序追加。
        List<AnimationCurve> rotations = new List<AnimationCurve>();

        for (int index = 0; index < bindings.Length; index++)
        {
            // 从 x 曲线识别一整组旋转，位置曲线在循环后单独处理。
            EditorCurveBinding binding = bindings[index];
            if (binding.propertyName != "m_LocalRotation.x")
            {
                continue;
            }
            // 角色模型中实际接受测试旋转的骨骼。
            Transform bone = modelRoot.Find(binding.path);
            if (bone == null)
            {
                throw new InvalidOperationException("角色骨架缺少 " + binding.path);
            }
            bones.Add(bone);
            rotations.AddRange(ReadAxes(clip, binding.path, "m_LocalRotation.", "xyzw"));
        }

        _bones = bones.ToArray();
        _rotationCurves = rotations.ToArray();
        _pelvis = modelRoot.Find(PelvisPath);
        _pelvisCurves = ReadAxes(clip, PelvisPath, "m_LocalPosition.", "xyz");
    }

    /// <summary>在正式 Animator 写完本帧姿态后，覆盖测试动作实际包含的骨骼通道。</summary>
    /// <param name="seconds">从动作首帧起的播放时间，单位为秒。</param>
    public void Sample(float seconds)
    {
        for (int index = 0; index < _bones.Length; index++)
        {
            // 当前关节在连续四元数曲线中的第一个分量。
            int offset = index * 4;
            _bones[index].localRotation = new Quaternion(
                _rotationCurves[offset].Evaluate(seconds),
                _rotationCurves[offset + 1].Evaluate(seconds),
                _rotationCurves[offset + 2].Evaluate(seconds),
                _rotationCurves[offset + 3].Evaluate(seconds)).normalized;
        }
        _pelvis.localPosition = new Vector3(_pelvisCurves[0].Evaluate(seconds),
            _pelvisCurves[1].Evaluate(seconds), _pelvisCurves[2].Evaluate(seconds));
    }

    /// <summary>保存播放前的骨骼旋转和骨盆位置，便于退出测试时还原。</summary>
    /// <param name="rotations">调用方提供的缓存，每根受影响骨骼对应一项。</param>
    /// <returns>原有骨盆局部位置。</returns>
    public Vector3 Capture(Quaternion[] rotations)
    {
        for (int index = 0; index < _bones.Length; index++)
        {
            rotations[index] = _bones[index].localRotation;
        }
        return _pelvis.localPosition;
    }

    /// <summary>恢复开始试播时保存的骨骼通道。</summary>
    /// <param name="rotations">与 Capture 使用同一顺序的骨骼旋转缓存。</param>
    /// <param name="pelvisPosition">测试开始前保存的骨盆局部位置。</param>
    public void Restore(Quaternion[] rotations, Vector3 pelvisPosition)
    {
        for (int index = 0; index < _bones.Length; index++)
        {
            _bones[index].localRotation = rotations[index];
        }
        _pelvis.localPosition = pelvisPosition;
    }

    /// <summary>读取同一骨骼属性的所有坐标轴曲线。</summary>
    /// <param name="clip">当前需要读取的独立测试动画。</param>
    /// <param name="path">相对于正式 Animator 的关节路径。</param>
    /// <param name="property">属性前缀，包含坐标轴前的点。</param>
    /// <param name="axes">坐标轴排列，旋转为 xyzw，位置为 xyz。</param>
    /// <returns>按输入坐标轴顺序排列的曲线。</returns>
    private static AnimationCurve[] ReadAxes(AnimationClip clip, string path, string property, string axes)
    {
        // 存放同一关节属性下的坐标轴曲线。
        AnimationCurve[] curves = new AnimationCurve[axes.Length];
        for (int axis = 0; axis < axes.Length; axis++)
        {
            // 当前坐标轴使用 Unity 序列化的 Transform 绑定名。
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property + axes[axis]);
            curves[axis] = AnimationUtility.GetEditorCurve(clip, binding);
            if (curves[axis] == null)
            {
                throw new InvalidOperationException("测试剪辑缺少曲线 " + clip.name + "/" + path + "/" + binding.propertyName);
            }
        }
        return curves;
    }
}
#endif
