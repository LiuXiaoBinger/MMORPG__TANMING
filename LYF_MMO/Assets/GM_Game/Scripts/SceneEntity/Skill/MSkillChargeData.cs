using System;
using UnityEngine;

/// <summary>RO 同名水平位移指令；本项目从 Y 前向程序段导出，不使用 Animator 根增量。</summary>
[Serializable]
public sealed class MSkillChargeData
{
    /// <summary>相对本次施法计时原点的开始秒数，非负。</summary>
    public float At;
    /// <summary>相对施法原点的结束秒数，允许等于 At 表示一次瞬时位移。</summary>
    public float End;
    /// <summary>RO 横向累计比例曲线；当前前向制作子集必须为空，运行校验拒绝未接入内容。</summary>
    public AnimationCurve CurveX;
    /// <summary>段内归一化时间对应的前向累计比例曲线，沿用 Y 原曲线及包围方式。</summary>
    public AnimationCurve CurveZ;
    /// <summary>RO 横向距离，单位米；当前前向制作子集仅允许零。</summary>
    public float DistanceX;
    /// <summary>前向累计距离的硬上限，单位米且非负；对应 Y maxDistance，不是原 RO 的额外限制。</summary>
    public float DistanceZ;
    /// <summary>RO 按施法点追加距离开关；当前 Forward/Self 子集必须为 false。</summary>
    public bool AimTarget;
    /// <summary>Y Continue 策略允许累计曲线下降时回拉，前向硬上限仍然保留。</summary>
    public bool AllowReverse;
    /// <summary>Y Stop 策略在曲线回拉或达到距离上限时结束该段，不能与 AllowReverse 同时启用。</summary>
    public bool StopAtDistanceLimit;

    /// <summary>核验运行指令的可解释范围，禁止未接入的 RO 横移或追目标内容被静默忽略。</summary>
    /// <param name="durationSeconds">原完整动作的秒数上限，程序运动不延长动作窗口。</param>
    /// <returns>时间、曲线及当前支持字段全部合法时返回 true。</returns>
    public bool IsValid(float durationSeconds)
    {
        // 动作时长必须为正有限秒数。
        bool durationInvalid = !Finite(durationSeconds) || durationSeconds <= 0f;
        // 起止秒数必须有限且非负。
        bool timeInvalid = !Finite(At) || !Finite(End) || At < 0f;
        // 结束不得早于开始，且不能越出动作窗口。
        bool rangeInvalid = End < At || End > durationSeconds;
        // 当前前向制作子集不接受横移曲线、横移距离或追目标开关。
        bool unsupportedFeature = CurveX != null || DistanceX != 0f || AimTarget;
        // 前向曲线必须存在，且距离上限有限非负。
        bool forwardCurveInvalid = CurveZ == null || !Finite(DistanceZ) || DistanceZ < 0f;
        // 回拉与停在距离上限不能同时启用。
        bool reverseConflict = AllowReverse && StopAtDistanceLimit;
        bool timeBad = durationInvalid || timeInvalid || rangeInvalid;
        bool featureBad = unsupportedFeature || forwardCurveInvalid || reverseConflict;
        if (timeBad || featureBad)
        {
            return false;
        }
        // 曲线关键帧快照只用于登记前校验；运动 Update 不分配数组。
        Keyframe[] keys = CurveZ.keys;
        if (keys.Length == 0)
        {
            return false;
        }
        // index 是曲线原顺序下标；无穷切线是 Unity 常量段合法表示，只拒绝非数切线。
        for (int index = 0; index < keys.Length; index++)
        {
            // 当前累计比例关键帧；权重必须是有限非负值，不能产生不可解释的插值输入。
            Keyframe key = keys[index];
            // 时间、值与切线必须有限或非数，不能产生不可解释的插值。
            bool valueInvalid = !Finite(key.time) || !Finite(key.value) ||
                float.IsNaN(key.inTangent);
            bool tangentInvalid = float.IsNaN(key.outTangent) ||
                !Finite(key.inWeight) || !Finite(key.outWeight);
            bool weightInvalid = key.inWeight < 0f || key.outWeight < 0f;
            bool modeInvalid = (int)key.weightedMode < 0 ||
                (int)key.weightedMode > 3;
            // 相邻关键帧时间必须递增，index>0 守卫下标前移避免越界。
            bool orderInvalid = index > 0 && key.time <= keys[index - 1].time;
            bool keyBad = valueInvalid || tangentInvalid || weightInvalid;
            if (keyBad || modeInvalid || orderInvalid)
            {
                return false;
            }
        }
        return Finite(CurveZ.Evaluate(0f)) && Finite(CurveZ.Evaluate(1f));
    }

    /// <summary>检查时间、距离或曲线比例可以参与有限数运算。</summary>
    /// <param name="value">待检查的浮点标量。</param>
    /// <returns>既非非数也非无穷时返回 true。</returns>
    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
