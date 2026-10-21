using System;
using UnityEngine;

/// <summary>RO 同名水平位移指令；本项目从 Y 程序段导出（含可选横向），不使用 Animator 根增量。</summary>
[Serializable]
public sealed class MSkillChargeData
{
    /// <summary>相对本次施法计时原点的开始秒数，非负。</summary>
    public float At;
    /// <summary>相对施法原点的结束秒数，允许等于 At 表示一次瞬时位移。</summary>
    public float End;
    /// <summary>RO 横向累计比例曲线，可选；为空时横向包络距离必须为零。</summary>
    public AnimationCurve CurveX;
    /// <summary>段内归一化时间对应的前向累计比例曲线，沿用 Y 原曲线及包围方式。</summary>
    public AnimationCurve CurveZ;
    /// <summary>RO 横向包络距离，单位米且有限；曲线按此尺度输出有符号横移，允许配零距离表示无实际横移。</summary>
    public float DistanceX;
    /// <summary>前向累计距离的硬上限，单位米且非负；对应 Y maxDistance，不是原 RO 的额外限制。</summary>
    public float DistanceZ;
    /// <summary>RO 按施法点追加距离开关；当前 Forward/Self 子集必须为 false。</summary>
    public bool AimTarget;
    /// <summary>Y Continue 策略允许累计曲线下降时回拉，前向硬上限仍然保留。</summary>
    public bool AllowReverse;
    /// <summary>Y Stop 策略在曲线回拉或达到距离上限时结束该段，不能与 AllowReverse 同时启用。</summary>
    public bool StopAtDistanceLimit;

    /// <summary>核验运行指令的可解释范围，禁止未接入的追目标内容被静默忽略。</summary>
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
        // 任一时间类约束失败即整段无效。
        bool timeBad = durationInvalid || timeInvalid || rangeInvalid;
        if (timeBad)
        {
            return false;
        }
        if (!IsFeatureValid())
        {
            return false;
        }
        // 前向与可选横向曲线按同一套关键帧规则核验。
        if (!IsCurveKeyValid(CurveZ))
        {
            return false;
        }
        if (CurveX != null && !IsCurveKeyValid(CurveX))
        {
            return false;
        }
        return IsEndpointFinite();
    }

    /// <summary>核验当前制作子集接入的功能开关与前向距离约束。</summary>
    /// <returns>追目标关闭、策略不冲突且前向距离合法时返回 true。</returns>
    private bool IsFeatureValid()
    {
        // 当前制作子集仍不接受追目标开关。
        if (AimTarget)
        {
            return false;
        }
        // 回拉与停在距离上限不能同时启用。
        if (AllowReverse && StopAtDistanceLimit)
        {
            return false;
        }
        // 前向曲线必须存在。
        if (CurveZ == null)
        {
            return false;
        }
        // 前向距离上限必须有限且非负。
        if (!Finite(DistanceZ) || DistanceZ < 0f)
        {
            return false;
        }
        // 横向配对约束单独核验，保持原短路顺序。
        return IsLateralValid();
    }

    /// <summary>核验横向曲线与包络的配对约束。</summary>
    /// <returns>无曲线时距离必须为零；有曲线时距离必须有限且曲线非空。</returns>
    private bool IsLateralValid()
    {
        if (CurveX == null)
        {
            return DistanceX == 0f;
        }
        // 曲线存在时包络允许为零，运行期按包络零值自然不产生横移。
        return Finite(DistanceX) && CurveX.keys.Length > 0;
    }

    /// <summary>按既有累计曲线规则核验关键帧时间、值、切线与权重，前向与横向共用。</summary>
    /// <param name="curve">待核验的累计比例曲线，调用前保证非空。</param>
    /// <returns>关键帧全部可解释且时间严格递增时返回 true。</returns>
    private static bool IsCurveKeyValid(AnimationCurve curve)
    {
        // 曲线关键帧快照只用于登记前校验；运动 Update 不分配数组。
        Keyframe[] keys = curve.keys;
        if (keys.Length == 0)
        {
            return false;
        }
        // index 是曲线原顺序下标；无穷切线是 Unity 常量段合法表示，只拒绝非数切线。
        for (int index = 0; index < keys.Length; index++)
        {
            // 当前累计比例关键帧；权重必须是有限非负值，不能产生不可解释的插值输入。
            Keyframe key = keys[index];
            // 时间、值与切入切线必须有限，不能产生不可解释的插值。
            bool valueInvalid = !Finite(key.time) || !Finite(key.value) ||
                float.IsNaN(key.inTangent);
            // 切出切线与两侧权重必须有限，无穷权重会产生退化插值输入。
            bool tangentInvalid = float.IsNaN(key.outTangent) ||
                !Finite(key.inWeight) || !Finite(key.outWeight);
            // 两侧权重必须非负。
            bool weightInvalid = key.inWeight < 0f || key.outWeight < 0f;
            // 加权模式枚举必须处于已知范围。
            bool modeInvalid = (int)key.weightedMode < 0 ||
                (int)key.weightedMode > 3;
            // 相邻关键帧时间必须递增，index>0 守卫下标前移避免越界。
            bool orderInvalid = index > 0 && key.time <= keys[index - 1].time;
            // 当前关键帧任一约束失败即拒绝本曲线。
            bool keyBad = valueInvalid || tangentInvalid || weightInvalid;
            if (keyBad || modeInvalid || orderInvalid)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>核验两轴曲线在零与一的端点评价结果有限。</summary>
    /// <returns>前向两端必须有限；横向曲线存在时两端同样必须有限。</returns>
    private bool IsEndpointFinite()
    {
        // 前向曲线两端评价结果必须有限。
        bool forwardFinite = Finite(CurveZ.Evaluate(0f)) && Finite(CurveZ.Evaluate(1f));
        if (!forwardFinite)
        {
            return false;
        }
        // 横向曲线可选；存在时两端评价结果同样必须有限。
        if (CurveX == null)
        {
            return true;
        }
        return Finite(CurveX.Evaluate(0f)) && Finite(CurveX.Evaluate(1f));
    }

    /// <summary>检查时间、距离或曲线比例可以参与有限数运算。</summary>
    /// <param name="value">待检查的浮点标量。</param>
    /// <returns>既非非数也非无穷时返回 true。</returns>
    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
