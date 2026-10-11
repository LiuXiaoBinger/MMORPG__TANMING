using System;
using System.Collections.Generic;
using Ethan.ActionEditor;
using UnityEngine;

/// <summary>RO秒时间到Y帧时间轴的唯一投影与制作校验；原数据只存秒，不产生第二套可漂移的帧字段。</summary>
public static class MSkillTrackTiming
{
    /// <summary>把原RO秒时刻投影到最近时间轴帧；非法时间交校验报告，绘图时安全返回零。</summary>
    /// <param name="config">决定显式FPS或原动画FPS的作者配置。</param>
    /// <param name="seconds">从技能开始计时的秒数，允许小数，不修改原字段。</param>
    /// <returns>非负零起始帧；过大时饱和到整数上限。</returns>
    public static int ToFrame(SkillConfigSO config, float seconds)
    {
        if (!Finite(seconds) || seconds <= 0f) return 0;
        // 双精度中间值避免高FPS和长循环乘法溢出。
        double frames = (double)seconds * Rate(config);
        if (frames >= int.MaxValue) return int.MaxValue;
        return (int)Math.Round(frames, MidpointRounding.AwayFromZero);
    }

    /// <summary>把时间轴帧转换为原RO秒时刻，拖动和粘贴统一使用当前作者FPS。</summary>
    /// <param name="config">决定时间轴FPS的作者配置。</param>
    /// <param name="frame">非负零起始时间轴帧，负数钳到零。</param>
    /// <returns>相对技能开始的秒数。</returns>
    public static float ToSeconds(SkillConfigSO config, int frame)
    {
        return Mathf.Max(0, frame) / Rate(config);
    }

    /// <summary>读取安全绘图FPS；非法配置仍由完整校验报告，临时绘图按30FPS处理。</summary>
    /// <param name="config">可能尚未完成FPS编辑的作者配置。</param>
    /// <returns>有效FPS或临时30FPS。</returns>
    private static float Rate(SkillConfigSO config)
    {
        // 原Y时间轴模式决定的帧率，保持旧模式与显式模式原含义。
        float rate = ActionTiming.FrameRate(config);
        if (!Finite(rate) || rate < 1f) return 30f;
        return rate;
    }

    /// <summary>判定标量是否为有限数，防止时间轴整数换算和未来处理器接收NaN或无穷值。</summary>
    /// <param name="value">需要检查的时间、距离或效果参数。</param>
    /// <returns>仅有限值返回true。</returns>
    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>末次召唤时刻；LifeTime属于召唤对象生命周期，不延长释放时间轴。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillMobUnitData item)
    {
        if (item == null) return 0f;
        if (!item.Loop || item.LoopCount <= 1) return item.At;
        return item.At + (item.LoopCount - 1) * item.LoopCycle;
    }

    /// <summary>AI事件单次触发时刻。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillAIEventData item)
    {
        if (item == null) return 0f;
        return item.At;
    }

    /// <summary>Buff规则单次触发时刻。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillBuffData item)
    {
        if (item == null) return 0f;
        return item.At;
    }

    /// <summary>模型变色区间终点，不回写非法原字段。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillChangeColorData item)
    {
        if (item == null) return 0f;
        return Mathf.Max(item.At, item.End);
    }

    /// <summary>技能条开始时刻加持续时长。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillBarData item)
    {
        if (item == null) return 0f;
        return item.At + item.Duration;
    }

    /// <summary>震屏开始时刻加持续时长。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MCameraShakeData item)
    {
        if (item == null) return 0f;
        return item.At + item.Time;
    }

    /// <summary>单项后处理的制作区间终点；关闭仍显示已保存区间，不代表执行启用。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MCameraPostEffectData item)
    {
        if (item == null) return 0f;
        return Mathf.Max(item.At, item.End);
    }

    /// <summary>镜头特效单次触发时刻。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillCameraFxData item)
    {
        if (item == null) return 0f;
        return item.At;
    }

    /// <summary>RO角速度旋转区间终点。</summary>
    /// <param name="item">原作者事件引用，允许空值以兼容旧资产。</param>
    /// <returns>相对技能开始的末尾秒数。</returns>
    public static float EndSeconds(MSkillRotateData item)
    {
        if (item == null) return 0f;
        return Mathf.Max(item.At, item.End);
    }

    /// <summary>读取新增RO轨道的最大制作末尾帧，循环采用最后触发时刻，空轨道不改变旧技能。</summary>
    /// <param name="config">包含原RO秒字段的技能作者配置。</param>
    /// <returns>最大非负末尾帧，不包含脱手对象的独立生命周期。</returns>
    public static int ContentEnd(SkillConfigSO config)
    {
        if (config == null) return 0;
        // 逐轨道读取原事件引用，未创建列表和空事件均不贡献末尾。
        int endFrame = 0;
        if (config.MobUnitList != null)
        {
            // MobUnitList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.MobUnitList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.MobUnitList[index])));
        }
        if (config.AIEventList != null)
        {
            // AIEventList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.AIEventList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.AIEventList[index])));
        }
        if (config.BuffList != null)
        {
            // BuffList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.BuffList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.BuffList[index])));
        }
        if (config.ChangeColorList != null)
        {
            // ChangeColorList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.ChangeColorList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.ChangeColorList[index])));
        }
        if (config.SkillBarList != null)
        {
            // SkillBarList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.SkillBarList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.SkillBarList[index])));
        }
        if (config.ShakeList != null)
        {
            // ShakeList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.ShakeList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.ShakeList[index])));
        }
        if (config.CameraFxData != null)
        {
            // CameraFxData的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.CameraFxData.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.CameraFxData[index])));
        }
        if (config.RotateList != null)
        {
            // RotateList的零起始列表下标，不改变配置排序或对象身份。
            for (int index = 0; index < config.RotateList.Count; index++)
                endFrame = Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.RotateList[index])));
        }
        return Mathf.Max(endFrame, EndFrame(config, EndSeconds(config.CameraPostEffect)));
    }

    /// <summary>制作区间末尾向上取整，避免不足一帧的秒区间被整数舍入截掉。</summary>
    /// <param name="config">决定当前时间轴FPS的作者配置。</param>
    /// <param name="seconds">RO事件最后触发或结束时刻，单位秒。</param>
    /// <returns>包含完整秒区间的末尾帧。</returns>
    private static int EndFrame(SkillConfigSO config, float seconds)
    {
        if (!Finite(seconds) || seconds <= 0f) return 0;
        // 双精度帧值容纳高帧率和长时间配置，饱和后不整数溢出。
        double frames = (double)seconds * Rate(config);
        if (frames >= int.MaxValue) return int.MaxValue;
        return (int)Math.Ceiling(frames - 0.00001d);
    }

    /// <summary>验证新增RO制作字段；不触发召唤、Buff、AI或镜头业务，要求执行处理器时明确阻止未接入内容。</summary>
    /// <param name="config">已由上层确认非空的作者配置。</param>
    /// <param name="capabilities">为空时只做制作校验，要求处理器时不能把保存成功当作可执行。</param>
    /// <param name="issues">上层校验结果列表，追加具名错误不覆盖原轨道结果。</param>
    public static void Validate(SkillConfigSO config, ActionValidationCapabilities capabilities, List<ActionValidationIssue> issues)
    {
        if (config.MobUnitList != null)
        {
            // 原MobUnitList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.MobUnitList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.MobUnitList[index];
                if (item == null)
                {
                    AddError("MobUnitList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, EndSeconds(item), config, "MobUnitList", index, issues);
                if (item.TemplateID <= 0 || !Finite(item.LifeTime) || item.LifeTime < 0f || !Finite(item.MinRadius) || !Finite(item.MaxRadius) || item.MinRadius < 0f || item.MaxRadius < item.MinRadius ||
                    !Finite(item.OffsetAtX) || !Finite(item.OffsetAtY) || !Finite(item.OffsetAtZ) || !Finite(item.MaxDistanceWithOwner) || item.MaxDistanceWithOwner < 0f ||
                    !Enum.IsDefined(typeof(ESkillCastPointType), item.CastPointType) || !Enum.IsDefined(typeof(ESkillMobPositionType), item.MobPositionType) ||
                    !Enum.IsDefined(typeof(ESkillMobLifeType), item.MobLifeType) || !Enum.IsDefined(typeof(ESkillMobMonsterType), item.MobMonsterType) ||
                    !Enum.IsDefined(typeof(ESkillMobFirerType), item.MobFirerType) || !Enum.IsDefined(typeof(ESkillMobAttrType), item.MobAttrType) ||
                    (item.CastPointType == ESkillCastPointType.ESkillCastPointType_SceneBase && string.IsNullOrWhiteSpace(item.SceneCastKey)) ||
                    item.LoopCount < 0 || !Finite(item.LoopCycle) || item.LoopCycle < 0f || (item.Loop && (item.LoopCount <= 0 || item.LoopCycle <= 0f)))
                    AddError("MobUnitList", index, "召唤须填写正模板ID、合法枚举、有限距离/寿命及有效循环；场景原点须填写SceneCastKey。", issues);
            }
            CheckExecution("MobUnitList", config.MobUnitList.Count > 0, capabilities, issues);
        }
        if (config.AIEventList != null)
        {
            // 原AIEventList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.AIEventList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.AIEventList[index];
                if (item == null)
                {
                    AddError("AIEventList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, item.At, config, "AIEventList", index, issues);
                if (string.IsNullOrWhiteSpace(item.Event)) AddError("AIEventList", index, "AI事件名称不能为空。", issues);
            }
            CheckExecution("AIEventList", config.AIEventList.Count > 0, capabilities, issues);
        }
        if (config.BuffList != null)
        {
            // 原BuffList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.BuffList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.BuffList[index];
                if (item == null)
                {
                    AddError("BuffList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, item.At, config, "BuffList", index, issues);
            }
            CheckExecution("BuffList", config.BuffList.Count > 0, capabilities, issues);
        }
        if (config.ChangeColorList != null)
        {
            // 原ChangeColorList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.ChangeColorList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.ChangeColorList[index];
                if (item == null)
                {
                    AddError("ChangeColorList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, item.End, config, "ChangeColorList", index, issues);
                if (string.IsNullOrWhiteSpace(item.colorGradientPath)) AddError("ChangeColorList", index, "模型变色必须指定原渐变资源路径。", issues);
            }
            CheckExecution("ChangeColorList", config.ChangeColorList.Count > 0, capabilities, issues);
        }
        if (config.SkillBarList != null)
        {
            // 原SkillBarList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.SkillBarList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.SkillBarList[index];
                if (item == null)
                {
                    AddError("SkillBarList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, EndSeconds(item), config, "SkillBarList", index, issues);
                if (!Finite(item.Duration) || item.Duration < 0f) AddError("SkillBarList", index, "技能条持续时间必须为非负有限秒数。", issues);
            }
            CheckExecution("SkillBarList", config.SkillBarList.Count > 0, capabilities, issues);
        }
        if (config.ShakeList != null)
        {
            // 原ShakeList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.ShakeList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.ShakeList[index];
                if (item == null)
                {
                    AddError("ShakeList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, EndSeconds(item), config, "ShakeList", index, issues);
                if (!Finite(item.Time) || item.Time < 0f || !Enum.IsDefined(typeof(CameraMotionSpace), item.Coordinate) ||
                    !Finite(item.FovAmp) || !Finite(item.Frequency) || item.Frequency < 0f ||
                    !Finite(item.AmplitudeX) || !Finite(item.AmplitudeY) || !Finite(item.AmplitudeZ))
                    AddError("ShakeList", index, "震屏时长和整段震动频数必须非负且有限，有符号振幅必须有限，坐标系必须为RO有效枚举。", issues);
            }
            CheckExecution("ShakeList", config.ShakeList.Count > 0, capabilities, issues);
        }
        if (config.CameraFxData != null)
        {
            // 原CameraFxData事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.CameraFxData.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.CameraFxData[index];
                if (item == null)
                {
                    AddError("CameraFxData", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, item.At, config, "CameraFxData", index, issues);
                if (item.CameraFxId <= 0 || !Enum.IsDefined(typeof(ESkillFxShowToType), item.FxShowTo))
                    AddError("CameraFxData", index, "镜头特效须填写正配置ID及有效接收对象。", issues);
            }
            CheckExecution("CameraFxData", config.CameraFxData.Count > 0, capabilities, issues);
        }
        if (config.RotateList != null)
        {
            // 原RotateList事件列表下标，校验不更改排序或生命周期。
            for (int index = 0; index < config.RotateList.Count; index++)
            {
                // 本次被校验的原事件引用，空项应提示而不能静默跳过。
                var item = config.RotateList[index];
                if (item == null)
                {
                    AddError("RotateList", index, "事件不能为空，请删除空项或补齐内容。", issues);
                    continue;
                }
                CheckTime(item.At, item.End, config, "RotateList", index, issues);
                if (!Finite(item.AngularVelocity)) AddError("RotateList", index, "旋转角速度必须为有限度/秒，可正可负。", issues);
            }
            CheckExecution("RotateList", config.RotateList.Count > 0, capabilities, issues);
        }
        if (config.CameraPostEffect != null)
        {
            // RO单项后处理对象，关闭时保留其用户数据并继续检查参数合法性。
            MCameraPostEffectData item = config.CameraPostEffect;
            CheckTime(item.At, item.End, config, "CameraPostEffect", 0, issues, item.UseCameraEffect);
            if (!Finite(item.Range) || item.Range < 0f || !Finite(item.Args0) || !Finite(item.Args1) || !Enum.IsDefined(typeof(CameraEffectType), item.EffectType))
                AddError("CameraPostEffect", 0, "后处理须使用合法类型，距离非负，全部参数有限。", issues);
            CheckExecution("CameraPostEffect", item.UseCameraEffect, capabilities, issues);
        }
    }

    /// <summary>验证原秒区间，显式退出帧不能截断作者已配置事件；RO有符号Index原样保存不加范围限制。</summary>
    /// <param name="begin">事件触发秒数，非负。</param>
    /// <param name="end">最后触发或结束秒数，不小于开始。</param>
    /// <param name="config">提供显式退出帧与时间轴FPS的作者配置。</param>
    /// <param name="track">结果中对应的RO字段名。</param>
    /// <param name="index">事件零起始列表下标，单对象为零。</param>
    /// <param name="issues">接收错误的上层结果列表。</param>
    /// <param name="enforceExit">是否要求区间能在显式退出前执行；关闭的后处理只保留制作区间。</param>
    private static void CheckTime(float begin, float end, SkillConfigSO config, string track, int index, List<ActionValidationIssue> issues, bool enforceExit = true)
    {
        if (!Finite(begin) || !Finite(end) || begin < 0f || end < begin)
        {
            AddError(track, index, "秒时刻必须有限且非负，结束不早于开始。", issues);
            return;
        }
        if (enforceExit && config.exitFrame > 0 && (double)end * Rate(config) > config.exitFrame + 0.00001d)
            AddError(track, index, "事件结束超过显式退出帧，请调整事件或退出帧。", issues);
    }

    /// <summary>要求运行处理器时拒绝当前未调度的RO制作轨道，防止ActionPlayer返回成功但漏事件。</summary>
    /// <param name="track">需要后续宿主实现执行器的RO字段名。</param>
    /// <param name="hasEvents">是否配置了待执行的事件。</param>
    /// <param name="capabilities">上层运行校验能力，空值代表纯制作验证。</param>
    /// <param name="issues">接收未接入错误的上层结果列表。</param>
    private static void CheckExecution(string track, bool hasEvents, ActionValidationCapabilities capabilities, List<ActionValidationIssue> issues)
    {
        if (hasEvents && capabilities != null && capabilities.RequireEventHandlers)
            AddError(track, -1, "该RO轨道当前只支持编辑保存，尚未接入正式执行处理器。", issues);
    }

    /// <summary>统一追加RO制作校验错误，轨道与事件下标保留用于编辑器定位。</summary>
    /// <param name="track">原RO配置字段名。</param>
    /// <param name="index">零起始事件下标，轨道整体问题用负一。</param>
    /// <param name="message">具体中文错误原因。</param>
    /// <param name="issues">上层维护的结果列表引用。</param>
    private static void AddError(string track, int index, string message, List<ActionValidationIssue> issues)
    {
        issues.Add(new ActionValidationIssue("ROTRACK001", ActionValidationSeverity.Error, track, index, message));
    }
}
