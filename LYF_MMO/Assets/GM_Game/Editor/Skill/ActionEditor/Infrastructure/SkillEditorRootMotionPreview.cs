using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;
using ActionData = global::Global;

/// <summary>从动画真实根平移曲线重建指定帧的水平位移，重复采样和拖回时间轴不会累加上次预览位置。</summary>
public static class SkillEditorRootMotionPreview
{
    /// <summary>计算作者动作从零帧到目标帧期间声明的动画根位移。</summary>
    /// <param name="config">当前作者资产，只处理 Move 轨道 Animation 模式。</param>
    /// <param name="animator">隔离预览模型的实际动画组件，用人体缩放还原 Humanoid 根平移。</param>
    /// <param name="frame">本次目标时间轴帧，零起始，支持向后拖动。</param>
    /// <returns>模型初始局部朝向中的水平累计米数；Y 分量固定为零。</returns>
    public static Vector3 Evaluate(SkillConfigSO config, Animator animator, int frame)
    {
        if (config == null || animator == null || frame <= 0 || config.moveSegmentList == null || config.animSegments == null) return Vector3.zero;
        // 时间轴明确帧率，动画源帧率仍由 ActionTiming 处理裁剪。
        float fps = ActionTiming.FrameRate(config);
        // 本次只计算到目标帧，单位秒，绝不使用上一轮采样时钟。
        float targetSeconds = frame / fps;
        // 快照复制引用，排序不改变原编辑稿中的段顺序。
        List<ActionData.AnimClipSegment> animations = new List<ActionData.AnimClipSegment>(config.animSegments);
        animations.Sort(CompareAnimations);
        // 目标帧的累计水平位移，所有段从同一零帧原点积分。
        Vector3 displacement = Vector3.zero;
        // 依序读取原动画段，用后一段起始时刻截断前一段。
        for (int animationIndex = 0; animationIndex < animations.Count; animationIndex++)
        {
            // 本条原动画段对象，空引用或无剪辑不产生位移。
            ActionData.AnimClipSegment animation = animations[animationIndex];
            if (animation == null || animation.clip == null) continue;
            // Humanoid 已将水平根位移烘焙进姿态时，正式 Animator 不会输出该水平位移，预览同样保持原地。
            if (animation.clip.isHumanMotion && AnimationUtility.GetAnimationClipSettings(animation.clip).loopBlendPositionXZ) continue;
            // 动画在时间轴上的开始时刻，单位秒。
            float animationStart = animation.startFrame / fps;
            // 源裁剪起点，单位原动画秒数。
            float sourceStart = ActionTiming.SourceStart(animation);
            // 源裁剪终点，单位原动画秒数。
            float sourceEnd = ActionTiming.SourceEnd(animation);
            // 此段在时间轴中有效的结束时刻。
            float animationEnd = Mathf.Min(targetSeconds, animationStart + sourceEnd - sourceStart);
            if (animationIndex + 1 < animations.Count && animations[animationIndex + 1] != null)
                animationEnd = Mathf.Min(animationEnd, animations[animationIndex + 1].startFrame / fps);
            if (animationEnd <= animationStart) continue;
            // 本条动画的横向根曲线；不存在该轴时对应增量为零。
            AnimationCurve x = RootCurve(animation.clip, "RootT.x", "m_LocalPosition.x");
            // 本条动画的前向根曲线；不读取 RootT.y，以保护原重力/跳跃。
            AnimationCurve z = RootCurve(animation.clip, "RootT.z", "m_LocalPosition.z");
            // Humanoid 标准化根位置恢复到当前实际模型米数；Generic 直接沿资源米数。
            float humanScale = 1f;
            if (animation.clip.isHumanMotion && animator.isHuman) humanScale = animator.humanScale;
            // 每条位移窗口独立限制动画采样范围，程序位移保留原预览路径。
            for (int moveIndex = 0; moveIndex < config.moveSegmentList.Count; moveIndex++)
            {
                // 本条原窗口引用，根运动倍率与时间都从当前作者稿读取。
                ActionData.MoveSegment move = config.moveSegmentList[moveIndex];
                if (move == null || move.driveMode != ActionData.MoveDriveMode.Animation) continue;
                // 位移窗口与动画段重叠区间的开始秒数。
                float begin = Mathf.Max(animationStart, move.keyNumber / fps);
                // 原 Y 包含末帧，转为运行一致的半开时间区间。
                float end = Mathf.Min(animationEnd, (Mathf.Max(move.keyNumber, move.endKeyNumber) + 1f) / fps);
                if (end <= begin) continue;
                // 重叠区间映射回原动画源开始秒数。
                float from = sourceStart + begin - animationStart;
                // 重叠区间映射回原动画源结束秒数。
                float to = sourceStart + end - animationStart;
                displacement.x += Difference(x, from, to) * humanScale * move.rootMotionScale;
                displacement.z += Difference(z, from, to) * humanScale * move.rootMotionScale;
            }
        }
        return displacement;
    }

    /// <summary>取得原动画根轴曲线，Humanoid 优先 RootT，Generic 只读取空路径根 Transform。</summary>
    /// <param name="clip">当前真实动画剪辑。</param>
    /// <param name="humanoidProperty">Humanoid 根轴属性名。</param>
    /// <param name="genericProperty">Generic 根 Transform 的局部位置属性名。</param>
    /// <returns>实际动画曲线；缺失时返回空，不能改读子骨骼冒充根运动。</returns>
    private static AnimationCurve RootCurve(AnimationClip clip, string humanoidProperty, string genericProperty)
    {
        // Unity 提供的真实曲线绑定，禁止根据 YAML 路径猜测。
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        // 逐条检查属性及空根路径，排除腿骨和历史模型位置。
        for (int index = 0; index < bindings.Length; index++)
        {
            if (!string.IsNullOrEmpty(bindings[index].path)) continue;
            if (clip.isHumanMotion && bindings[index].propertyName == humanoidProperty)
                return AnimationUtility.GetEditorCurve(clip, bindings[index]);
            if (!clip.isHumanMotion && bindings[index].propertyName == genericProperty)
                return AnimationUtility.GetEditorCurve(clip, bindings[index]);
        }
        return null;
    }

    /// <summary>计算指定区间内的根轴累计变化；没有曲线的轴保持零位移。</summary>
    /// <param name="curve">当前真实根轴曲线，允许为空。</param>
    /// <param name="beginSeconds">源动画区间起点，单位秒。</param>
    /// <param name="endSeconds">源动画区间终点，单位秒。</param>
    /// <returns>源根坐标的差值，由调用者按模型缩放还原。</returns>
    private static float Difference(AnimationCurve curve, float beginSeconds, float endSeconds)
    {
        if (curve == null) return 0f;
        return curve.Evaluate(endSeconds) - curve.Evaluate(beginSeconds);
    }

    /// <summary>排序动画快照，空段排在末尾而不修改作者原列表。</summary>
    /// <param name="left">第一条原动画段引用。</param>
    /// <param name="right">第二条原动画段引用。</param>
    /// <returns>按开始帧升序的标准比较值。</returns>
    private static int CompareAnimations(ActionData.AnimClipSegment left, ActionData.AnimClipSegment right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return 1;
        if (right == null) return -1;
        return left.startFrame.CompareTo(right.startFrame);
    }
}
