using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ethan.ActionEditor;

/// <summary>把原 Y 音效事件、动画窗口与前向程序位移段转换为本游戏正式表现；不接管角色移动或权威结算。</summary>
public static class SkillEditorAudioMotionTools
{
    /// <summary>读取固定音量单次完整声音；触发帧按作者 FPS 换秒，与编辑器现有预览语义一致。</summary>
    /// <param name="config">当前作者资产；引用必须通过 Unity 加载，不按文件名猜测资源身份。</param>
    /// <param name="durationSeconds">作者制作时间轴允许的触发上限，单位秒；声音尾部可以超过此范围。</param>
    /// <returns>按时间及原列表下标稳定排列的正式音效事件。</returns>
    public static List<SkillRuntimeAudioEvent> ReadAudio(SkillConfigSO config, float durationSeconds)
    {
        // 第三方序列化适配边界，避免包 Global 与游戏 Global 同名冲突。
        SerializedObject serialized = new SerializedObject(config);
        // 原 fxList 包含粒子与音效，音效 contentKind 的原值为二。
        SerializedProperty effects = serialized.FindProperty("fxList");
        // 本次转换结果，强引用声音由正式配置保持依赖。
        List<SkillRuntimeAudioEvent> result = new List<SkillRuntimeAudioEvent>();
        // 按作者原顺序读取，原下标保留为同秒排序键。
        for (int index = 0; index < effects.arraySize; index++)
        {
            // 此条原编辑事件，粒子由已有导出路径独立处理。
            SerializedProperty item = effects.GetArrayElementAtIndex(index);
            if (item.FindPropertyRelative("contentKind").intValue != 2) continue;
            // 音效触发帧，零表示动作开始时立即触发。
            int frame = item.FindPropertyRelative("keyNumber").intValue;
            // 当前强引用的 Unity 声音资产，禁止空引用导出。
            AudioClip clip = item.FindPropertyRelative("audioClip").objectReferenceValue as AudioClip;
            // 原编辑器固定音量，允许范围零至一。
            float volume = item.FindPropertyRelative("baseVolume").floatValue;
            // 时间轴 FPS 决定帧到秒换算，不能采用音频采样率。
            float timeSeconds = frame / config.timelineFrameRate;
            if (frame < 0 || !Finite(timeSeconds) || timeSeconds > durationSeconds || clip == null || !Finite(volume) || volume < 0f || volume > 1f)
                throw new InvalidOperationException("音效引用、帧或音量无效：" + config.name + "/" + index);
            // 结束帧是作者制作范围标记，现有声音预览始终从开始帧播放完整剪辑；导出保留相同语义，不截音或循环。
            int endFrame = item.FindPropertyRelative("endKeyNumber").intValue;
            if (item.FindPropertyRelative("particleSystem").objectReferenceValue != null ||
                item.FindPropertyRelative("audioRandomize").boolValue ||
                item.FindPropertyRelative("playbackSpeed").floatValue != 1f ||
                item.FindPropertyRelative("customLifetime").floatValue != 0f ||
                item.FindPropertyRelative("pitchVariation").floatValue != 0.1f ||
                item.FindPropertyRelative("volumeVariation").floatValue != 0.05f)
                throw new InvalidOperationException("音效含未接入的混合粒子、随机音量音高、变速或自定义寿命参数：" + config.name +
                    "/fxList[" + index + "]，音频 " + clip.name + "，开始帧 " + frame + "，结束帧 " + endFrame + "；当前正式音效只支持固定音量单次播放。");
            result.Add(new SkillRuntimeAudioEvent { SourceEventIndex = index, Frame = frame, TimeSeconds = timeSeconds, Clip = clip, Volume = volume });
        }
        result.Sort(CompareAudio);
        return result;
    }

    /// <summary>读取动画模式位移窗；水平方向取动画本身，重力和碰撞继续由角色组件处理。</summary>
    /// <param name="config">作者资产，程序曲线模式由 ReadCharge 独立解释。</param>
    /// <param name="durationSeconds">完整动作的运行时长上限，单位秒。</param>
    /// <returns>按开始秒排序且互不重叠的动画位移段。</returns>
    public static List<SkillRuntimeRootMotionSegment> ReadRootMotion(SkillConfigSO config, float durationSeconds)
    {
        // 原包类型仅在序列化边界读取，运行端使用具名领域类型。
        SerializedObject serialized = new SerializedObject(config);
        // 原 Y Move 轨道段列表；空列表表示动画留在原地。
        SerializedProperty moves = serialized.FindProperty("moveSegmentList");
        // 此次转换的位移窗口，不修改原作者段或程序专用参数。
        List<SkillRuntimeRootMotionSegment> result = new List<SkillRuntimeRootMotionSegment>();
        // 每条窗口的原列表下标，用于错误定位。
        for (int index = 0; index < moves.arraySize; index++)
        {
            // 原序列化窗口，Animation 原枚举值是一。
            SerializedProperty item = moves.GetArrayElementAtIndex(index);
            // 位移开始秒，按作者明确 FPS 换算。
            float start = item.FindPropertyRelative("keyNumber").intValue / config.timelineFrameRate;
            // 原 Y 窗口包含末帧，运行半开区间延长一帧后夹到完整动作末尾。
            float end = (Mathf.Max(item.FindPropertyRelative("keyNumber").intValue, item.FindPropertyRelative("endKeyNumber").intValue) + 1f) / config.timelineFrameRate;
            // 水平动画位移倍率，零允许保持原地；禁止负值反向移动。
            float scale = item.FindPropertyRelative("rootMotionScale").floatValue;
            if (item.FindPropertyRelative("driveMode").intValue == 0)
            {
                continue;
            }
            if (item.FindPropertyRelative("driveMode").intValue != 1)
                throw new InvalidOperationException("位移驱动类型未接入：" + config.name + "/" + index);
            if (!Finite(start) || !Finite(end) || !Finite(scale) || start < 0f || end <= start || start >= durationSeconds ||
                item.FindPropertyRelative("endKeyNumber").intValue / config.timelineFrameRate > durationSeconds + 0.002f || scale < 0f)
                throw new InvalidOperationException("根运动位移段时间或倍率无效：" + config.name + "/" + index);
            if (item.FindPropertyRelative("ignoreGravity").boolValue || item.FindPropertyRelative("ignoreActorCollision").boolValue)
                throw new InvalidOperationException("根运动不能绕过当前角色的重力或碰撞：" + config.name + "/" + index);
            // Y MoveSolver 在 Animation 模式直接返回；curve、target、maxDistance、verticalDistance、alignFacing 等程序参数不执行，原值保留。
            result.Add(new SkillRuntimeRootMotionSegment { StartSeconds = start, EndSeconds = Mathf.Min(end, durationSeconds), Scale = scale });
        }
        result.Sort(CompareRootMotion);
        // 检查排序后相邻窗口，防止同一动画增量被两个位移段重复放大。
        for (int index = 1; index < result.Count; index++)
        {
            if (result[index].StartSeconds < result[index - 1].EndSeconds)
                throw new InvalidOperationException("根运动位移段不允许重叠：" + config.name);
        }
        ValidateMotionOverlap(result, ReadCharge(config, durationSeconds));
        return result;
    }

    /// <summary>把 Y 前向 Self 程序段投影到 RO 同名 ChargeList；不新增作者字段或自动重导资产。</summary>
    /// <param name="config">原明确 FPS 的作者稿，非默认未接入参数会给出具体错误。</param>
    /// <param name="durationSeconds">原完整动作秒数，程序位移不能延长动作锁定窗口。</param>
    /// <returns>按开始秒排序的强类型程序位移指令，Program 结束帧直接换秒，不加动画窗口的一帧。</returns>
    public static List<MSkillChargeData> ReadCharge(SkillConfigSO config, float durationSeconds)
    {
        // 第三方序列化边界读取原 Y 程序字段，避免两个程序集 Global 同名冲突。
        SerializedObject serialized = new SerializedObject(config);
        // 现有位移段混合承载 Animation 与 Program，按 driveMode 分别导出。
        SerializedProperty moves = serialized.FindProperty("moveSegmentList");
        // 本次只读转换结果，不改写用户曲线、帧或作者段。
        List<MSkillChargeData> result = new List<MSkillChargeData>();
        // index 是作者段原列表下标，错误据此准确定位。
        for (int index = 0; index < moves.arraySize; index++)
        {
            // 原 Y 段，程序模式原值为零，动画模式原值为一。
            SerializedProperty item = moves.GetArrayElementAtIndex(index);
            if (item.FindPropertyRelative("driveMode").intValue == 1)
            {
                continue;
            }
            if (item.FindPropertyRelative("driveMode").intValue != 0)
            {
                throw new InvalidOperationException("位移驱动类型未接入：" + config.name + "/" + index);
            }
            if (item.FindPropertyRelative("space").intValue != 0 ||
                item.FindPropertyRelative("origin").intValue != 0 ||
                item.FindPropertyRelative("target").intValue < 0 || item.FindPropertyRelative("target").intValue > 1 ||
                item.FindPropertyRelative("resolveDirectionPerFrame").boolValue ||
                item.FindPropertyRelative("customDirection").vector3Value != Vector3.forward ||
                item.FindPropertyRelative("targetOffset").vector3Value != Vector3.zero ||
                item.FindPropertyRelative("offsetSpace").intValue != 1 ||
                item.FindPropertyRelative("stopAtTargetOffset").boolValue ||
                item.FindPropertyRelative("arriveTolerance").floatValue != 0.05f ||
                item.FindPropertyRelative("minKeepDistance").floatValue != 0f ||
                item.FindPropertyRelative("verticalDistance").floatValue != 0f ||
                item.FindPropertyRelative("rootMotionScale").floatValue != 1f ||
                item.FindPropertyRelative("alignFacing").boolValue ||
                item.FindPropertyRelative("ignoreGravity").boolValue ||
                item.FindPropertyRelative("ignoreActorCollision").boolValue ||
                !IsDefaultVerticalCurve(item.FindPropertyRelative("verticalCurve").animationCurveValue))
            {
                throw new InvalidOperationException("程序位移当前仅支持前向、自身中心、固定方向及原重力碰撞；含未接入参数：" + config.name + "/moveSegmentList[" + index + "]");
            }
            // 原 Y 三种超界策略，分别是 Clamp、Stop、Continue，不能默默吞掉未知值。
            int policy = item.FindPropertyRelative("overshootPolicy").intValue;
            if (policy < 0 || policy > 2)
            {
                throw new InvalidOperationException("程序位移超界策略无效：" + config.name + "/" + index);
            }
            // 开始帧按作者 FPS 转秒，程序零时长段直接评价末值。
            int startFrame = item.FindPropertyRelative("keyNumber").intValue;
            // 原 Program 段的末帧，等于或早于起帧时采用起帧；与 Animation 半开窗口独立。
            int endFrame = Mathf.Max(startFrame, item.FindPropertyRelative("endKeyNumber").intValue);
            // 原累计距离曲线，复制关键帧和 wrap，运行资产不共享作者可变曲线对象。
            AnimationCurve curve = item.FindPropertyRelative("curve").animationCurveValue;
            if (curve == null)
            {
                throw new InvalidOperationException("程序位移缺少累计距离曲线：" + config.name + "/" + index);
            }
            // 发布独立曲线副本，保留加权关键帧及原包围行为。
            AnimationCurve runtimeCurve = new AnimationCurve(curve.keys);
            runtimeCurve.preWrapMode = curve.preWrapMode;
            runtimeCurve.postWrapMode = curve.postWrapMode;
            // Y→RO 数据适配：仅导前向 Z，不伪造横移或瞄准目标。
            MSkillChargeData data = new MSkillChargeData
            {
                At = startFrame / config.timelineFrameRate, End = endFrame / config.timelineFrameRate,
                CurveZ = runtimeCurve, DistanceZ = item.FindPropertyRelative("maxDistance").floatValue,
                AllowReverse = policy == 2, StopAtDistanceLimit = policy == 1
            };
            if (!data.IsValid(durationSeconds))
            {
                throw new InvalidOperationException("程序位移开始、结束、距离或曲线无效：" + config.name + "/" + index);
            }
            result.Add(data);
        }
        result.Sort(CompareCharge);
        // index 核排序后的程序段，相邻区间可共享终点，同起点的点事件不可重复推动。
        for (int index = 1; index < result.Count; index++)
        {
            if (result[index].At < result[index - 1].End || result[index].At == result[index - 1].At)
            {
                throw new InvalidOperationException("程序位移段不允许重叠或同时开始：" + config.name);
            }
        }
        return result;
    }

    /// <summary>检查未接入的竖直曲线仍为原 Y 零曲线，不能隐藏作者未执行内容。</summary>
    /// <param name="curve">程序段中的原竖直累计曲线。</param>
    /// <returns>原零曲线关键帧和包围方式保持时返回 true。</returns>
    private static bool IsDefaultVerticalCurve(AnimationCurve curve)
    {
        // 直接构造 Y 原默认零曲线，使用真实 Unity 默认 wrap、权重和切线，不能假设 WrapMode.Default。
        AnimationCurve defaults = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        return CurvesEqual(curve, defaults);
    }

    /// <summary>拒绝程序段与动画根窗口同时推动，同端点的相邻区间允许连接。</summary>
    /// <param name="motion">原动画半开水平窗口。</param>
    /// <param name="charge">原程序累计曲线段。</param>
    private static void ValidateMotionOverlap(List<SkillRuntimeRootMotionSegment> motion, List<MSkillChargeData> charge)
    {
        // index 遍历已校验的程序区间，零时长点也不能落在动画接管区间内。
        for (int index = 0; index < charge.Count; index++)
        {
            // rootIndex 遍历动画半开窗口，末点属于后续段。
            for (int rootIndex = 0; rootIndex < motion.Count; rootIndex++)
            {
                if (charge[index].At < motion[rootIndex].EndSeconds && (charge[index].End > motion[rootIndex].StartSeconds ||
                    (charge[index].At == charge[index].End && charge[index].At >= motion[rootIndex].StartSeconds)))
                {
                    throw new InvalidOperationException("程序位移不能与动画根运动窗口重叠。");
                }
            }
        }
    }

    /// <summary>按程序段开始秒排序，登记顺序与多段累计差量保持一致。</summary>
    /// <param name="left">第一条程序段。</param>
    /// <param name="right">第二条程序段。</param>
    /// <returns>标准升序比较值。</returns>
    private static int CompareCharge(MSkillChargeData left, MSkillChargeData right)
    {
        return left.At.CompareTo(right.At);
    }

    /// <summary>检查发布后的声音与位移逐项等于当前作者稿，并检查真实声音依赖。</summary>
    /// <param name="binding">当前正式技能绑定，提供作者稿与原完整动作。</param>
    /// <param name="runtime">待检查的已发布运行资产。</param>
    /// <param name="dependencies">Unity 返回的运行资产递归依赖路径。</param>
    public static void ValidatePublished(SkillEditorBinding binding, SkillRuntimePresentation runtime, string[] dependencies)
    {
        // 重新解析作者稿作为期望值，避免仅比较事件数量漏掉帧或声音换绑。
        List<SkillRuntimeAudioEvent> audio = ReadAudio(binding.Config, runtime.EffectiveTimelineDurationSeconds);
        // 当前作者稿声明的真实动画位移窗口。
        List<SkillRuntimeRootMotionSegment> motion = ReadRootMotion(binding.Config, runtime.DurationSeconds);
        // 当前作者程序轨道的正式投影；旧运行资产空引用兼容为零段。
        List<MSkillChargeData> charge = ReadCharge(binding.Config, runtime.DurationSeconds);
        // 已发布程序段数，不能把尚未导出的新内容误认为旧空资产兼容。
        int publishedChargeCount = 0;
        if (runtime.ChargeList != null)
        {
            publishedChargeCount = runtime.ChargeList.Count;
        }
        if (publishedChargeCount != charge.Count)
        {
            throw new InvalidOperationException("程序位移尚未同步：" + binding.name);
        }
        // index 核程序段所有时间、距离、策略及曲线关键帧和包围方式。
        for (int index = 0; index < charge.Count; index++)
        {
            // 此次转换的期望程序指令。
            MSkillChargeData expected = charge[index];
            // 正式发布的对应程序指令，任何未接入横移或瞄准数据都必须拒绝。
            MSkillChargeData actual = runtime.ChargeList[index];
            if (actual == null || !actual.IsValid(runtime.DurationSeconds) || actual.At != expected.At ||
                actual.End != expected.End || actual.DistanceZ != expected.DistanceZ || actual.AllowReverse != expected.AllowReverse ||
                actual.StopAtDistanceLimit != expected.StopAtDistanceLimit || !CurvesEqual(actual.CurveZ, expected.CurveZ))
            {
                throw new InvalidOperationException("程序位移尚未同步：" + binding.name + "/" + index);
            }
        }
        if (runtime.AudioEvents == null || runtime.RootMotionSegments == null || runtime.AudioEvents.Count != audio.Count || runtime.RootMotionSegments.Count != motion.Count)
            throw new InvalidOperationException("音效或根运动尚未同步：" + binding.name);
        // 每条音效核对触发时间、资源身份、音量以及依赖。
        for (int index = 0; index < audio.Count; index++)
        {
            // 作者稿转换的本条期望音效。
            SkillRuntimeAudioEvent expected = audio[index];
            // 运行资产保存的本条真实音效。
            SkillRuntimeAudioEvent actual = runtime.AudioEvents[index];
            if (actual == null || actual.SourceEventIndex != expected.SourceEventIndex || actual.Frame != expected.Frame ||
                actual.TimeSeconds != expected.TimeSeconds || actual.Clip != expected.Clip || actual.Volume != expected.Volume)
                throw new InvalidOperationException("运行音效尚未同步：" + binding.name + "/" + index);
            if (Array.IndexOf(dependencies, AssetDatabase.GetAssetPath(actual.Clip)) < 0)
                throw new InvalidOperationException("运行资产缺少声音依赖：" + actual.Clip.name);
        }
        // 窗口时间与倍率均逐项核对，不能把空列表视为发布成功。
        for (int index = 0; index < motion.Count; index++)
        {
            // 作者稿计算出的期望根运动区间。
            SkillRuntimeRootMotionSegment expected = motion[index];
            // 已保存运行配置内的实际根运动区间。
            SkillRuntimeRootMotionSegment actual = runtime.RootMotionSegments[index];
            if (actual == null || actual.StartSeconds != expected.StartSeconds || actual.EndSeconds != expected.EndSeconds || actual.Scale != expected.Scale)
                throw new InvalidOperationException("运行根运动尚未同步：" + binding.name + "/" + index);
        }
    }

    /// <summary>逐项核对曲线副本的关键帧、权重及包围方式，避免只比段数漏掉曲线修改。</summary>
    /// <param name="left">正式保存的运行累计曲线。</param>
    /// <param name="right">当前作者重新解析的期望曲线。</param>
    /// <returns>曲线数据完全一致时返回 true。</returns>
    private static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
    {
        if (left == null || right == null || left.preWrapMode != right.preWrapMode || left.postWrapMode != right.postWrapMode)
        {
            return false;
        }
        // 已发布曲线的关键帧快照，校验不会改写其中任一值。
        Keyframe[] leftKeys = left.keys;
        // 当前作者曲线的关键帧快照，长度相同才逐项比较。
        Keyframe[] rightKeys = right.keys;
        if (leftKeys.Length != rightKeys.Length)
        {
            return false;
        }
        // index 遍历关键帧的稳定序列。
        for (int index = 0; index < leftKeys.Length; index++)
        {
            if (!leftKeys[index].Equals(rightKeys[index]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>按秒数与原事件下标排序，保证同帧声音顺序稳定。</summary>
    /// <param name="left">第一条候选声音事件。</param>
    /// <param name="right">第二条候选声音事件。</param>
    /// <returns>负数表示左侧先触发，零表示同一排序键。</returns>
    private static int CompareAudio(SkillRuntimeAudioEvent left, SkillRuntimeAudioEvent right)
    {
        // 首要排序键是明确的动作时钟秒数。
        int order = left.TimeSeconds.CompareTo(right.TimeSeconds);
        if (order != 0) return order;
        return left.SourceEventIndex.CompareTo(right.SourceEventIndex);
    }

    /// <summary>按照位移开始秒排序，后续检查相邻窗口是否重叠。</summary>
    /// <param name="left">第一条根运动窗口。</param>
    /// <param name="right">第二条根运动窗口。</param>
    /// <returns>标准升序比较结果。</returns>
    private static int CompareRootMotion(SkillRuntimeRootMotionSegment left, SkillRuntimeRootMotionSegment right)
    {
        return left.StartSeconds.CompareTo(right.StartSeconds);
    }

    /// <summary>判断资产浮点数是否能安全参与动作时钟或音量计算。</summary>
    /// <param name="value">待核对的秒数、音量或位移倍率。</param>
    /// <returns>有限数返回真；NaN 和无穷返回假。</returns>
    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
