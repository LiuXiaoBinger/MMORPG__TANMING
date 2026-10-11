using System;
using System.Collections.Generic;
using System.Reflection;
using Ethan.ActionEditor;
using UnityEditor;
using UnityEngine;

/// <summary>编辑器显示边界的字段、工具提示与枚举标签；不更改序列化名称、枚举值或用户文本。</summary>
public static class SkillEditorPropertyLabels
{
    /// <summary>一项框架字段的中英名称及用途说明，仅用于 GUI 显示。</summary>
    private readonly struct LabelEntry
    {
        /// <summary>英文模式下显示的字段名称，包含原字段的单位。</summary>
        public readonly string English;
        /// <summary>中文模式下显示的字段名称，包含原字段的单位。</summary>
        public readonly string Chinese;
        /// <summary>英文模式的字段用途提示，不读取或翻译用户输入。</summary>
        public readonly string EnglishTooltip;
        /// <summary>中文模式的字段用途提示，不参与资产持久化。</summary>
        public readonly string ChineseTooltip;

        /// <summary>保存一项编辑器文案，四个字符串均属于显示数据。</summary>
        /// <param name="english">英文标签。</param>
        /// <param name="chinese">中文标签。</param>
        /// <param name="englishTooltip">英文用途提示。</param>
        /// <param name="chineseTooltip">中文用途提示。</param>
        public LabelEntry(string english, string chinese, string englishTooltip, string chineseTooltip)
        {
            English = english;
            Chinese = chinese;
            EnglishTooltip = englishTooltip;
            ChineseTooltip = chineseTooltip;
        }
    }

    /// <summary>以真实序列化字段名查询文案；字典不会替换字段名或存入技能 SO。</summary>
    private static readonly Dictionary<string, LabelEntry> Labels = new Dictionary<string, LabelEntry>
    {
        { "skillID" , new LabelEntry("Skill ID", "技能 ID", "Skill ID", "技能 ID") },
        { "skillName" , new LabelEntry("Action name", "行为名称", "Action name", "行为名称") },
        { "skillDescription" , new LabelEntry("Description", "行为说明", "Description", "行为说明") },
        { "skillType" , new LabelEntry("Skill type", "技能类型", "Skill type", "技能类型") },
        { "previewModelName" , new LabelEntry("Preview model name", "预览模型名称", "Preview model name", "预览模型名称") },
        { "ownerType" , new LabelEntry("Actor profile", "角色类型", "Actor profile", "角色类型") },
        { "skillCD" , new LabelEntry("Cooldown (s)", "冷却时间（秒）", "Cooldown (s)", "冷却时间（秒）") },
        { "cancelPriority" , new LabelEntry("Cancel priority", "取消优先级", "Cancel priority", "取消优先级") },
        { "energyCostOnActivate" , new LabelEntry("Activation energy cost", "启动能量消耗", "Activation energy cost", "启动能量消耗") },
        { "defaultEnergyGainOnHit" , new LabelEntry("Default energy gain on hit", "默认命中能量收益", "Default energy gain on hit", "默认命中能量收益") },
        { "timingVersion" , new LabelEntry("Timing version", "计时版本", "Timing version", "计时版本") },
        { "actionEditorSchemaVersion" , new LabelEntry("Schema version", "数据结构版本", "Schema version", "数据结构版本") },
        { "timelineFrameRate" , new LabelEntry("Timeline FPS", "时间轴帧率", "Frames per second used to convert timeline frames to seconds.", "时间轴每秒帧数，用于帧号与秒数换算。") },
        { "phases" , new LabelEntry("Phases", "行为阶段", "Phases", "行为阶段") },
        { "flowNodes" , new LabelEntry("Action flow", "行为流程", "Action flow", "行为流程") },
        { "animSegments" , new LabelEntry("Animation segments", "动画片段", "Animation segments", "动画片段") },
        { "skillClip" , new LabelEntry("Animation clip", "动画剪辑", "Animation clip", "动画剪辑") },
        { "exitFrame" , new LabelEntry("Exit frame (0 = auto)", "结束帧（0 为自动）", "Exit frame (0 = auto)", "结束帧（0 为自动）") },
        { "moveSegmentList" , new LabelEntry("Displacement segments", "位移片段", "Displacement segments", "位移片段") },
        { "moveCurve" , new LabelEntry("Legacy movement curve", "旧位移曲线", "Legacy movement curve", "旧位移曲线") },
        { "totalMoveDistance" , new LabelEntry("Total movement distance (m)", "总位移距离（米）", "Total movement distance (m)", "总位移距离（米）") },
        { "tracks" , new LabelEntry("Tracks", "轨道列表", "Tracks", "轨道列表") },
        { "jumpList" , new LabelEntry("Action transitions", "行为跳转", "Legacy action-transition windows; this track does not move the actor into the air.", "旧行为跳转窗口；该轨道不会让角色物理起跳。") },
        { "attackList" , new LabelEntry("Collisions", "攻击判定", "Collisions", "攻击判定") },
        { "cameraCues" , new LabelEntry("Camera cues", "镜头表现", "Camera cues", "镜头表现") },
        { "fxList" , new LabelEntry("Effects and audio", "特效与音效", "Effects and audio", "特效与音效") },
        { "cancelList" , new LabelEntry("Exit windows", "退出窗口", "Exit windows", "退出窗口") },
        { "projectileList" , new LabelEntry("Projectiles", "飞行物", "Projectiles", "飞行物") },
        { "hitFxList" , new LabelEntry("Hit effects", "命中特效", "Hit effects", "命中特效") },
        { "interactionWindows" , new LabelEntry("Interaction windows", "交互窗口", "Interaction windows", "交互窗口") },
        { "moveCancelFrame" , new LabelEntry("Move cancel frame (-1 = disabled)", "移动取消帧（-1 为关闭）", "Move cancel frame (-1 = disabled)", "移动取消帧（-1 为关闭）") },
        { "warningCueList" , new LabelEntry("Telegraphs", "预警表现", "Telegraphs", "预警表现") },
        { "superArmorList" , new LabelEntry("Super armor windows", "霸体窗口", "Super armor windows", "霸体窗口") },
        { "adjustMotionList" , new LabelEntry("Facing windows", "朝向调整窗口", "Facing windows", "朝向调整窗口") },
        { "trailToggleList" , new LabelEntry("Trail windows", "拖尾窗口", "Trail windows", "拖尾窗口") },
        { "isSuperArmorSkill" , new LabelEntry("Super armor skill", "霸体技能", "Super armor skill", "霸体技能") },
        { "phase2AttackList" , new LabelEntry("Phase 2 collisions", "第二阶段攻击判定", "Phase 2 collisions", "第二阶段攻击判定") },
        { "phase2FxList" , new LabelEntry("Phase 2 effects", "第二阶段特效", "Phase 2 effects", "第二阶段特效") },
        { "phase2RangeMultiplier" , new LabelEntry("Phase 2 range multiplier", "第二阶段范围倍率", "Phase 2 range multiplier", "第二阶段范围倍率") },
        { "nextSkill" , new LabelEntry("Next action", "后续行为", "Next action", "后续行为") },
        { "beginKey" , new LabelEntry("Start frame", "开始帧", "Start frame", "开始帧") },
        { "endKey" , new LabelEntry("End frame (exclusive)", "结束帧（不包含）", "The transition window excludes this end frame. This is action branching, not a physical jump.", "行为跳转窗口不包含该结束帧；这是行为分支，不是物理跳跃。") },
        { "triggerCommandId" , new LabelEntry("Input command ID", "输入命令 ID", "Input command ID", "输入命令 ID") },
        { "triggerKey" , new LabelEntry("Legacy trigger key", "旧触发按键", "Legacy trigger key", "旧触发按键") },
        { "autoTrigger" , new LabelEntry("Automatic transition", "自动衔接", "Automatic transition", "自动衔接") },
        { "fadeDuration" , new LabelEntry("Blend duration (s)", "过渡时长（秒）", "Blend duration (s)", "过渡时长（秒）") },
        { "requiredEnergy" , new LabelEntry("Required energy", "所需能量", "Required energy", "所需能量") },
        { "energyCost" , new LabelEntry("Energy cost", "能量消耗", "Energy cost", "能量消耗") },
        { "requestSlowPromptOnJump" , new LabelEntry("Slow motion transition prompt", "跳转慢动作提示", "Slow motion transition prompt", "跳转慢动作提示") },
        { "derivePromptVfx" , new LabelEntry("Transition prompt effect", "跳转提示特效", "Transition prompt effect", "跳转提示特效") },
        { "derivePromptOffset" , new LabelEntry("Prompt offset (m)", "提示偏移（米）", "Prompt offset (m)", "提示偏移（米）") },
        { "derivePromptScale" , new LabelEntry("Prompt scale", "提示缩放", "Prompt scale", "提示缩放") },
        { "derivePromptFollow" , new LabelEntry("Follow actor", "跟随角色", "Follow actor", "跟随角色") },
        { "derivePromptSound" , new LabelEntry("Transition prompt audio", "跳转提示音效", "Transition prompt audio", "跳转提示音效") },
        { "derivePromptVolume" , new LabelEntry("Prompt volume (0–1)", "提示音量（0–1）", "Prompt volume (0–1)", "提示音量（0–1）") },
        { "collisionName" , new LabelEntry("Collision name", "判定名称", "Collision name", "判定名称") },
        { "responseMode" , new LabelEntry("Contact response", "接触响应", "Contact response", "接触响应") },
        { "contactSignal" , new LabelEntry("Contact signal ID", "接触信号 ID", "Contact signal ID", "接触信号 ID") },
        { "boxHeight" , new LabelEntry("Box height (m)", "盒体高度（米）", "Box height (m)", "盒体高度（米）") },
        { "collisionRotation" , new LabelEntry("Collision rotation (deg)", "判定旋转（度）", "Collision rotation (deg)", "判定旋转（度）") },
        { "unblockable" , new LabelEntry("Cannot be blocked", "无法格挡", "Cannot be blocked", "无法格挡") },
        { "unparryable" , new LabelEntry("Cannot be parried", "无法招架", "Cannot be parried", "无法招架") },
        { "interactionTag" , new LabelEntry("Interaction tag", "交互标签", "Interaction tag", "交互标签") },
        { "keyNumber" , new LabelEntry("Start frame", "开始帧", "Zero-based start frame on the shared timeline.", "共享时间轴上的零起始帧号。") },
        { "endKeyNumber" , new LabelEntry("End frame", "结束帧", "End frame; the event kind determines whether it is inclusive or a single-frame event.", "结束帧；是否包含该帧或视为单帧事件由事件类型决定。") },
        { "isReWrite" , new LabelEntry("Override hit shape", "覆盖判定形状", "Override hit shape", "覆盖判定形状") },
        { "shapeType" , new LabelEntry("Shape", "判定形状", "Shape", "判定形状") },
        { "parameter1" , new LabelEntry("Radius / length (m)", "半径／长度（米）", "Radius / length (m)", "半径／长度（米）") },
        { "parameter2" , new LabelEntry("Width (m)", "宽度（米）", "Width (m)", "宽度（米）") },
        { "offset" , new LabelEntry("Offset (m)", "偏移（米）", "Offset (m)", "偏移（米）") },
        { "damageRatio" , new LabelEntry("Damage multiplier", "伤害倍率", "Damage multiplier", "伤害倍率") },
        { "knockbackForce" , new LabelEntry("Knockback force", "击退强度", "Knockback force", "击退强度") },
        { "autoAim" , new LabelEntry("Automatic aiming", "自动瞄准", "Automatic aiming", "自动瞄准") },
        { "impactLevel" , new LabelEntry("Impact level", "冲击等级", "Impact level", "冲击等级") },
        { "toughnessDamage" , new LabelEntry("Toughness damage", "韧性伤害", "Toughness damage", "韧性伤害") },
        { "damageMode" , new LabelEntry("Detection mode", "检测模式", "Detection mode", "检测模式") },
        { "tickInterval" , new LabelEntry("Detection interval (frames)", "检测间隔（帧）", "Detection interval (frames)", "检测间隔（帧）") },
        { "energyDeltaOnHit" , new LabelEntry("Energy change on hit", "命中能量变化", "Energy change on hit", "命中能量变化") },
        { "enemyAttackType" , new LabelEntry("Enemy attack type", "敌人攻击类型", "Enemy attack type", "敌人攻击类型") },
        { "requestSlowPromptOnHit" , new LabelEntry("Slow motion hit prompt", "命中慢动作提示", "Slow motion hit prompt", "命中慢动作提示") },
        { "slowPromptRequiredEnergy" , new LabelEntry("Prompt energy threshold", "提示能量门槛", "Prompt energy threshold", "提示能量门槛") },
        { "requestGlowPromptOnHit" , new LabelEntry("Glow hit prompt", "命中发光提示", "Glow hit prompt", "命中发光提示") },
        { "useBoneTracking" , new LabelEntry("Track bone", "跟随骨骼", "Track bone", "跟随骨骼") },
        { "trackBonePath" , new LabelEntry("Bone path", "骨骼路径", "Bone path", "骨骼路径") },
        { "boneOffsets" , new LabelEntry("Sampled bone offsets (m)", "骨骼采样偏移（米）", "Sampled bone offsets (m)", "骨骼采样偏移（米）") },
        { "hitVfx" , new LabelEntry("Hit effect", "命中特效", "Hit effect", "命中特效") },
        { "hitVfxOffset" , new LabelEntry("Hit effect offset (m)", "命中特效偏移（米）", "Hit effect offset (m)", "命中特效偏移（米）") },
        { "hitVfxRotation" , new LabelEntry("Hit effect rotation (deg)", "命中特效旋转（度）", "Hit effect rotation (deg)", "命中特效旋转（度）") },
        { "hitVfxScale" , new LabelEntry("Hit effect scale", "命中特效缩放", "Hit effect scale", "命中特效缩放") },
        { "hitVfxPlaybackSpeed" , new LabelEntry("Hit effect playback speed", "命中特效播放倍率", "Hit effect playback speed", "命中特效播放倍率") },
        { "hitVfxFollowTarget" , new LabelEntry("Follow hit target", "跟随受击目标", "Follow hit target", "跟随受击目标") },
        { "hitVfxUseAttackerRotation" , new LabelEntry("Use attacker rotation", "采用攻击者朝向", "Use attacker rotation", "采用攻击者朝向") },
        { "hitSound" , new LabelEntry("Hit audio", "命中音效", "Hit audio", "命中音效") },
        { "hitSoundVolume" , new LabelEntry("Hit volume (0–1)", "命中音量（0–1）", "Hit volume (0–1)", "命中音量（0–1）") },
        { "hitSoundRandomize" , new LabelEntry("Randomize hit audio", "随机化命中音效", "Randomize hit audio", "随机化命中音效") },
        { "hitSoundPitchVariation" , new LabelEntry("Hit pitch variation", "命中音高变化", "Hit pitch variation", "命中音高变化") },
        { "hitSoundVolumeVariation" , new LabelEntry("Hit volume variation", "命中音量变化", "Hit volume variation", "命中音量变化") },
        { "enableHitPause" , new LabelEntry("Enable hit stop", "开启命中停顿", "Enable hit stop", "开启命中停顿") },
        { "hitPauseDuration" , new LabelEntry("Hit stop duration (s)", "命中停顿时长（秒）", "Hit stop duration (s)", "命中停顿时长（秒）") },
        { "enableCameraShake" , new LabelEntry("Enable camera shake", "开启镜头震动", "Enable camera shake", "开启镜头震动") },
        { "cameraShakeDuration" , new LabelEntry("Shake duration (s)", "震动时长（秒）", "Shake duration (s)", "震动时长（秒）") },
        { "cameraShakeAmplitude" , new LabelEntry("Shake amplitude", "震动幅度", "Shake amplitude", "震动幅度") },
        { "cameraShakeFrequency" , new LabelEntry("Shake frequency (Hz)", "震动频率（赫兹）", "Shake frequency (Hz)", "震动频率（赫兹）") },
        { "enableChromaticAberration" , new LabelEntry("Enable chromatic aberration", "开启色差", "Enable chromatic aberration", "开启色差") },
        { "chromaticAberrationDuration" , new LabelEntry("Chromatic aberration duration (s)", "色差时长（秒）", "Chromatic aberration duration (s)", "色差时长（秒）") },
        { "chromaticAberrationIntensity" , new LabelEntry("Chromatic aberration intensity", "色差强度", "Chromatic aberration intensity", "色差强度") },
        { "contentKind" , new LabelEntry("Effect content", "效果内容", "Effect content", "效果内容") },
        { "particleSystem" , new LabelEntry("Visual effect", "粒子特效", "Visual effect", "粒子特效") },
        { "socketPath" , new LabelEntry("Socket path", "挂点路径", "Path relative to the preview actor. Keep the actual hierarchy names unchanged.", "相对于预览角色的挂点路径，保留真实层级名称。") },
        { "placementKind" , new LabelEntry("Placement", "定位方式", "Root/socket follows the actor; target top, forward point and absolute world positions do not follow.", "根节点／挂点跟随角色；目标头顶、前方落点和绝对世界位置不跟随。") },
        { "forwardDistance" , new LabelEntry("Forward distance (m)", "前方距离（米）", "Distance in metres for the forward point or fallback when the target is missing.", "前方落点或无目标兜底使用的距离，单位为米。") },
        { "audioClip" , new LabelEntry("Audio clip", "音效片段", "Audio clip", "音效片段") },
        { "followCharacter" , new LabelEntry("Follow actor", "跟随角色", "Follow actor", "跟随角色") },
        { "rotation" , new LabelEntry("Rotation (deg)", "旋转（度）", "Rotation (deg)", "旋转（度）") },
        { "scale" , new LabelEntry("Scale", "缩放倍率", "Scale", "缩放倍率") },
        { "playbackSpeed" , new LabelEntry("Playback speed", "播放倍率", "Playback speed", "播放倍率") },
        { "useWorldSpace" , new LabelEntry("Use world coordinates", "使用世界坐标", "Use world coordinates", "使用世界坐标") },
        { "customLifetime" , new LabelEntry("Custom lifetime (s; 0 = automatic)", "自定义寿命（秒；0 为自动）", "Seconds; zero uses the actual particle lifetime including remaining particles and trails.", "单位为秒；0 使用真实粒子寿命，包括余粒和尾迹。") },
        { "audioRandomize" , new LabelEntry("Randomize audio", "随机化音效", "Randomize audio", "随机化音效") },
        { "pitchVariation" , new LabelEntry("Pitch variation", "音高变化", "Pitch variation", "音高变化") },
        { "volumeVariation" , new LabelEntry("Volume variation", "音量变化", "Volume variation", "音量变化") },
        { "baseVolume" , new LabelEntry("Base volume (0–1)", "基础音量（0–1）", "Base volume (0–1)", "基础音量（0–1）") },
        { "followTarget" , new LabelEntry("Follow target", "跟随目标", "Follow target", "跟随目标") },
        { "useAttackerRotation" , new LabelEntry("Use attacker rotation", "采用攻击者朝向", "Use attacker rotation", "采用攻击者朝向") },
        { "vfxOffset" , new LabelEntry("Effect offset (m)", "特效偏移（米）", "Effect offset (m)", "特效偏移（米）") },
        { "vfxRotation" , new LabelEntry("Effect rotation (deg)", "特效旋转（度）", "Effect rotation (deg)", "特效旋转（度）") },
        { "vfxScale" , new LabelEntry("Effect scale", "特效缩放", "Effect scale", "特效缩放") },
        { "volume" , new LabelEntry("Volume (0–1)", "音量（0–1）", "Volume (0–1)", "音量（0–1）") },
        { "warningVfx" , new LabelEntry("Telegraph effect", "预警特效", "Telegraph effect", "预警特效") },
        { "warningSound" , new LabelEntry("Telegraph audio", "预警音效", "Telegraph audio", "预警音效") },
        { "causePlayerStagger" , new LabelEntry("Stagger player", "使玩家失衡", "Stagger player", "使玩家失衡") },
        { "prefab" , new LabelEntry("Projectile prefab", "飞行物预制体", "Projectile prefab", "飞行物预制体") },
        { "firePointPath" , new LabelEntry("Firing socket path", "发射挂点路径", "Firing socket path", "发射挂点路径") },
        { "spawnOffset" , new LabelEntry("Spawn offset (m)", "生成偏移（米）", "Spawn offset (m)", "生成偏移（米）") },
        { "spawnRotation" , new LabelEntry("Spawn rotation (deg)", "生成旋转（度）", "Spawn rotation (deg)", "生成旋转（度）") },
        { "speed" , new LabelEntry("Speed (m/s)", "速度（米／秒）", "Speed (m/s)", "速度（米／秒）") },
        { "lifetime" , new LabelEntry("Lifetime (s)", "寿命（秒）", "Lifetime (s)", "寿命（秒）") },
        { "shotCount" , new LabelEntry("Projectile count", "发射数量", "Projectile count", "发射数量") },
        { "spreadAngle" , new LabelEntry("Spread angle (deg)", "散射角（度）", "Spread angle (deg)", "散射角（度）") },
        { "tracking" , new LabelEntry("Track target", "追踪目标", "Track target", "追踪目标") },
        { "trackingDelay" , new LabelEntry("Tracking delay (s)", "追踪延迟（秒）", "Tracking delay (s)", "追踪延迟（秒）") },
        { "trackingDuration" , new LabelEntry("Tracking duration (s)", "追踪时长（秒）", "Tracking duration (s)", "追踪时长（秒）") },
        { "trackingTurnSpeed" , new LabelEntry("Tracking turn speed (deg/s)", "追踪转向速度（度／秒）", "Tracking turn speed (deg/s)", "追踪转向速度（度／秒）") },
        { "destroyable" , new LabelEntry("Can be destroyed", "可被销毁", "Can be destroyed", "可被销毁") },
        { "muzzleVfx" , new LabelEntry("Muzzle effect", "枪口特效", "Muzzle effect", "枪口特效") },
        { "fireSound" , new LabelEntry("Firing audio", "发射音效", "Firing audio", "发射音效") },
        { "impactVfx" , new LabelEntry("Impact effect", "撞击特效", "Impact effect", "撞击特效") },
        { "impactSound" , new LabelEntry("Impact audio", "撞击音效", "Impact audio", "撞击音效") },
        { "impactSoundVolume" , new LabelEntry("Impact volume (0–1)", "撞击音量（0–1）", "Impact volume (0–1)", "撞击音量（0–1）") },
        { "impactVfxOffset" , new LabelEntry("Impact effect offset (m)", "撞击特效偏移（米）", "Impact effect offset (m)", "撞击特效偏移（米）") },
        { "impactVfxRotation" , new LabelEntry("Impact effect rotation (deg)", "撞击特效旋转（度）", "Impact effect rotation (deg)", "撞击特效旋转（度）") },
        { "impactVfxScale" , new LabelEntry("Impact effect scale", "撞击特效缩放", "Impact effect scale", "撞击特效缩放") },
        { "impactVfxPlaybackSpeed" , new LabelEntry("Impact effect playback speed", "撞击特效播放倍率", "Impact effect playback speed", "撞击特效播放倍率") },
        { "destroyVfx" , new LabelEntry("Destruction effect", "销毁特效", "Destruction effect", "销毁特效") },
        { "destroySound" , new LabelEntry("Destruction audio", "销毁音效", "Destruction audio", "销毁音效") },
        { "fireShake" , new LabelEntry("Shake on firing", "发射时震动", "Shake on firing", "发射时震动") },
        { "fireShakeDuration" , new LabelEntry("Firing shake duration (s)", "发射震动时长（秒）", "Firing shake duration (s)", "发射震动时长（秒）") },
        { "fireShakeAmplitude" , new LabelEntry("Firing shake amplitude", "发射震动幅度", "Firing shake amplitude", "发射震动幅度") },
        { "fireShakeFrequency" , new LabelEntry("Firing shake frequency (Hz)", "发射震动频率（赫兹）", "Firing shake frequency (Hz)", "发射震动频率（赫兹）") },
        { "fireChromaticAberration" , new LabelEntry("Chromatic aberration on firing", "发射时色差", "Chromatic aberration on firing", "发射时色差") },
        { "fireChromaticDuration" , new LabelEntry("Firing chromatic duration (s)", "发射色差时长（秒）", "Firing chromatic duration (s)", "发射色差时长（秒）") },
        { "fireChromaticIntensity" , new LabelEntry("Firing chromatic intensity", "发射色差强度", "Firing chromatic intensity", "发射色差强度") },
        { "fireFreeze" , new LabelEntry("Hit stop on firing", "发射时停顿", "Hit stop on firing", "发射时停顿") },
        { "fireFreezeDuration" , new LabelEntry("Firing hit stop duration (s)", "发射停顿时长（秒）", "Firing hit stop duration (s)", "发射停顿时长（秒）") },
        { "hitFreeze" , new LabelEntry("Hit stop on impact", "撞击时停顿", "Hit stop on impact", "撞击时停顿") },
        { "hitFreezeDuration" , new LabelEntry("Impact hit stop duration (s)", "撞击停顿时长（秒）", "Impact hit stop duration (s)", "撞击停顿时长（秒）") },
        { "hitShake" , new LabelEntry("Shake on impact", "撞击时震动", "Shake on impact", "撞击时震动") },
        { "hitShakeDuration" , new LabelEntry("Impact shake duration (s)", "撞击震动时长（秒）", "Impact shake duration (s)", "撞击震动时长（秒）") },
        { "hitShakeAmplitude" , new LabelEntry("Impact shake amplitude", "撞击震动幅度", "Impact shake amplitude", "撞击震动幅度") },
        { "hitShakeFrequency" , new LabelEntry("Impact shake frequency (Hz)", "撞击震动频率（赫兹）", "Impact shake frequency (Hz)", "撞击震动频率（赫兹）") },
        { "hitChromaticAberration" , new LabelEntry("Chromatic aberration on impact", "撞击时色差", "Chromatic aberration on impact", "撞击时色差") },
        { "hitChromaticDuration" , new LabelEntry("Impact chromatic duration (s)", "撞击色差时长（秒）", "Impact chromatic duration (s)", "撞击色差时长（秒）") },
        { "hitChromaticIntensity" , new LabelEntry("Impact chromatic intensity", "撞击色差强度", "Impact chromatic intensity", "撞击色差强度") },
        { "minCancelPriority" , new LabelEntry("Minimum cancel priority", "最低取消优先级", "Minimum cancel priority", "最低取消优先级") },
        { "allowMoveCancel" , new LabelEntry("Allow move cancel", "允许移动取消", "Allow move cancel", "允许移动取消") },
        { "allowDodgeCancel" , new LabelEntry("Allow dodge cancel", "允许闪避取消", "Allow dodge cancel", "允许闪避取消") },
        { "allowJumpCancel" , new LabelEntry("Allow jump cancel", "允许跳跃取消", "Allow jump cancel", "允许跳跃取消") },
        { "clip" , new LabelEntry("Animation clip", "动画剪辑", "Animation clip", "动画剪辑") },
        { "startFrame" , new LabelEntry("Timeline start frame", "时间轴起始帧", "Timeline start frame", "时间轴起始帧") },
        { "clipStartFrame" , new LabelEntry("Clip start frame", "剪辑裁剪起始帧", "Clip start frame", "剪辑裁剪起始帧") },
        { "clipEndFrame" , new LabelEntry("Clip end frame (0 = full clip)", "剪辑裁剪结束帧（0 为完整）", "Source-clip frame; zero keeps the complete clip. Cropping remains unsupported by this host.", "源剪辑帧号；0 保留完整剪辑。本宿主仍不支持裁剪发布。") },
        { "blendInFrames" , new LabelEntry("Blend-in frames", "混合过渡帧数", "Blend-in frames", "混合过渡帧数") },
        { "armorLevel" , new LabelEntry("Armor level", "霸体等级", "Armor level", "霸体等级") },
        { "tintColor" , new LabelEntry("Preview tint", "预览颜色", "Preview tint", "预览颜色") },
        { "allowTurning" , new LabelEntry("Allow target tracking", "允许追踪目标朝向", "Allow target tracking", "允许追踪目标朝向") },
        { "targetKind" , new LabelEntry("Tracking target", "追踪目标来源", "Tracking target", "追踪目标来源") },
        { "rotationSpeed" , new LabelEntry("Turn speed (deg/s)", "转向速度（度／秒）", "Turn speed (deg/s)", "转向速度（度／秒）") },
        { "yAxisOnly" , new LabelEntry("Rotate around Y only", "仅绕 Y 轴转向", "Rotate around Y only", "仅绕 Y 轴转向") },
        { "particle" , new LabelEntry("Trail particle", "拖尾粒子", "Trail particle", "拖尾粒子") },
        { "trailPath" , new LabelEntry("Trail renderer path", "拖尾渲染器路径", "Trail renderer path", "拖尾渲染器路径") },
        { "useEmitting" , new LabelEntry("Control trail emission", "控制拖尾发射", "Control trail emission", "控制拖尾发射") },
        { "type" , new LabelEntry("Track type", "轨道类型", "Track type", "轨道类型") },
        { "displayName" , new LabelEntry("Custom display name", "自定义显示名称", "Custom display name", "自定义显示名称") },
        { "expanded" , new LabelEntry("Expanded", "展开", "Expanded", "展开") },
        { "stacks" , new LabelEntry("Stacks", "层数", "Stacks", "层数") },
        { "timer" , new LabelEntry("Timer (s)", "计时（秒）", "Timer (s)", "计时（秒）") },
        { "chargeTime" , new LabelEntry("Charge time (s)", "蓄力时长（秒）", "Charge time (s)", "蓄力时长（秒）") },
        { "damageMultiplier" , new LabelEntry("Damage multiplier", "伤害倍率", "Damage multiplier", "伤害倍率") },
        { "toughnessMultiplier" , new LabelEntry("Toughness multiplier", "韧性伤害倍率", "Toughness multiplier", "韧性伤害倍率") },
        { "knockbackMultiplier" , new LabelEntry("Knockback multiplier", "击退倍率", "Knockback multiplier", "击退倍率") },
        { "driveMode" , new LabelEntry("Displacement driver", "位移驱动", "Displacement driver", "位移驱动") },
        { "rootMotionScale" , new LabelEntry("Root motion scale", "根运动倍率", "Root motion scale", "根运动倍率") },
        { "space" , new LabelEntry("Direction source", "方向来源", "Direction source", "方向来源") },
        { "origin" , new LabelEntry("Movement origin", "移动中心", "Movement origin", "移动中心") },
        { "target" , new LabelEntry("Target source", "目标来源", "Target source", "目标来源") },
        { "resolveDirectionPerFrame" , new LabelEntry("Resolve direction every frame", "每帧重新解算方向", "Resolve direction every frame", "每帧重新解算方向") },
        { "customDirection" , new LabelEntry("Custom direction", "自定义方向", "Custom direction", "自定义方向") },
        { "curve" , new LabelEntry("Cumulative movement curve", "累计位移曲线", "Cumulative movement curve", "累计位移曲线") },
        { "maxDistance" , new LabelEntry("Distance limit (m)", "距离上限（米）", "Distance limit (m)", "距离上限（米）") },
        { "targetOffset" , new LabelEntry("Target offset (m)", "目标偏移（米）", "Target offset (m)", "目标偏移（米）") },
        { "offsetSpace" , new LabelEntry("Offset coordinate space", "偏移坐标空间", "Offset coordinate space", "偏移坐标空间") },
        { "stopAtTargetOffset" , new LabelEntry("Stop at target offset", "在目标偏移处停止", "Stop at target offset", "在目标偏移处停止") },
        { "arriveTolerance" , new LabelEntry("Arrival tolerance (m)", "到达容差（米）", "Arrival tolerance (m)", "到达容差（米）") },
        { "minKeepDistance" , new LabelEntry("Minimum separation (m)", "最小间距（米）", "Minimum separation (m)", "最小间距（米）") },
        { "verticalCurve" , new LabelEntry("Vertical displacement curve", "垂直位移曲线", "Vertical displacement curve", "垂直位移曲线") },
        { "verticalDistance" , new LabelEntry("Vertical distance (m)", "垂直距离（米）", "Vertical distance (m)", "垂直距离（米）") },
        { "alignFacing" , new LabelEntry("Align facing", "对齐朝向", "Align facing", "对齐朝向") },
        { "ignoreGravity" , new LabelEntry("Ignore gravity", "忽略重力", "Ignore gravity", "忽略重力") },
        { "ignoreActorCollision" , new LabelEntry("Ignore actor collision", "忽略角色碰撞", "Ignore actor collision", "忽略角色碰撞") },
        { "overshootPolicy" , new LabelEntry("Constraint overflow policy", "约束溢出策略", "Constraint overflow policy", "约束溢出策略") },
        { "kind" , new LabelEntry("Flow node kind", "流程节点类型", "Flow node kind", "流程节点类型") },
        { "limitWindow" , new LabelEntry("Limit active window", "限制生效窗口", "Limit active window", "限制生效窗口") },
        { "priorityRule" , new LabelEntry("Priority rule", "优先级规则", "Compares the requested action priority; changing display language never changes the stored enum.", "比较请求行为的优先级；切换显示语言不会改写枚举数据。") },
        { "minimumPriority" , new LabelEntry("Minimum priority", "最低优先级", "Minimum priority", "最低优先级") },
        { "category" , new LabelEntry("Action category ID", "行为分类 ID", "Action category ID", "行为分类 ID") },
        { "nextAction" , new LabelEntry("Next action", "后续行为", "Next action", "后续行为") },
        { "automatic" , new LabelEntry("Automatic branch", "自动分支", "Automatic branch", "自动分支") },
        { "command" , new LabelEntry("Input command ID", "输入命令 ID", "Input command ID", "输入命令 ID") },
        { "conditions" , new LabelEntry("Conditions", "条件列表", "Conditions", "条件列表") },
        { "id" , new LabelEntry("Identifier", "标识符", "Identifier", "标识符") },
        { "beginFrame" , new LabelEntry("Start frame", "开始帧", "Start frame", "开始帧") },
        { "endFrame" , new LabelEntry("End frame", "结束帧", "End frame", "结束帧") },
        { "signal" , new LabelEntry("Interaction signal ID", "交互信号 ID", "Interaction signal ID", "交互信号 ID") },
        { "priority" , new LabelEntry("Priority", "优先级", "Priority", "优先级") },
        { "sourceAngle" , new LabelEntry("Source angle (0–360 deg)", "来源角度（0–360 度）", "Source angle (0–360 deg)", "来源角度（0–360 度）") },
        { "response" , new LabelEntry("Interaction response", "交互响应", "Interaction response", "交互响应") },
        { "suppressHitReaction" , new LabelEntry("Suppress hit reaction", "抑制受击反应", "Suppress hit reaction", "抑制受击反应") },
        { "maxActivations" , new LabelEntry("Activation limit (0 = unlimited)", "触发上限（0 为不限）", "Activation limit (0 = unlimited)", "触发上限（0 为不限）") },
        { "label" , new LabelEntry("Custom label", "自定义标签", "Custom label", "自定义标签") },
        { "positionOffset" , new LabelEntry("Camera position offset (m)", "镜头位置偏移（米）", "Camera position offset (m)", "镜头位置偏移（米）") },
        { "rotationOffset" , new LabelEntry("Camera rotation offset (deg)", "镜头旋转偏移（度）", "Camera rotation offset (deg)", "镜头旋转偏移（度）") },
        { "fieldOfViewOffset" , new LabelEntry("Field of view offset (deg)", "视场角偏移（度）", "Field of view offset (deg)", "视场角偏移（度）") },
        { "shakeAmplitude" , new LabelEntry("Shake amplitude", "震动幅度", "Shake amplitude", "震动幅度") },
        { "shakeFrequency" , new LabelEntry("Shake frequency (Hz)", "震动频率（赫兹）", "Shake frequency (Hz)", "震动频率（赫兹）") },
        { "envelope" , new LabelEntry("Weight envelope", "权重曲线", "Weight envelope", "权重曲线") },
        { "operation" , new LabelEntry("Condition operator", "条件运算", "Condition operator", "条件运算") },
        { "children" , new LabelEntry("Child conditions", "子条件", "Child conditions", "子条件") },
        { "minimum" , new LabelEntry("Minimum value", "最小值", "Minimum value", "最小值") },
        { "resource" , new LabelEntry("Resource ID", "资源 ID", "Resource ID", "资源 ID") },
        { "requiredTag" , new LabelEntry("Required interaction tag", "所需交互标签", "Required interaction tag", "所需交互标签") },
        { "requireBlockable" , new LabelEntry("Require blockable attack", "要求攻击可格挡", "Require blockable attack", "要求攻击可格挡") },
        { "requireParryable" , new LabelEntry("Require parryable attack", "要求攻击可招架", "Require parryable attack", "要求攻击可招架") },
        { "attackIndex" , new LabelEntry("Attack event index", "攻击事件索引", "Attack event index", "攻击事件索引") },
        { "PresentationId" , new LabelEntry("Presentation ID", "表现 ID", "Presentation ID", "表现 ID") },
        { "SkillId" , new LabelEntry("Skill ID", "技能 ID", "Skill ID", "技能 ID") },
        { "EffectId" , new LabelEntry("Effect ID", "效果 ID", "Effect ID", "效果 ID") },
        { "ProfessionId" , new LabelEntry("Profession ID", "职业 ID", "Profession ID", "职业 ID") },
        { "ModelKey" , new LabelEntry("Model key", "模型资源键", "Model key", "模型资源键") },
        { "WeaponType" , new LabelEntry("Weapon type", "武器类型", "Weapon type", "武器类型") },
        { "VariantIndex" , new LabelEntry("Variant index", "动作变体索引", "Variant index", "动作变体索引") },
        { "Config" , new LabelEntry("Editable skill configuration", "技能编辑配置", "Editable skill configuration", "技能编辑配置") },
        { "PreviewModel" , new LabelEntry("Preview actor", "预览角色", "Preview actor", "预览角色") },
        { "SourceClip" , new LabelEntry("Original animation clip", "原动画剪辑", "Original animation clip", "原动画剪辑") },
        { "SkillStateTimeMs" , new LabelEntry("Skill state duration (ms)", "技能状态时长（毫秒）", "Action-slot duration in milliseconds; independent from the full animation length.", "动作槽占用时长，单位为毫秒；与完整动画长度独立。") },
        { "FxSources" , new LabelEntry("Original effect event references", "原特效事件参考", "Original effect event references", "原特效事件参考") },
        { "SourceEventIndex" , new LabelEntry("Original event index", "原事件索引", "Original event index", "原事件索引") },
        { "OriginalTimeSeconds" , new LabelEntry("Original event time (s)", "原事件时刻（秒）", "Original event time (s)", "原事件时刻（秒）") },
        { "ImportedFrame" , new LabelEntry("Imported timeline frame", "导入时间轴帧", "Imported timeline frame", "导入时间轴帧") },
        { "Stage" , new LabelEntry("Original effect stage", "原特效阶段", "Original effect stage", "原特效阶段") },
        { "Rules" , new LabelEntry("Skill casting rules", "技能施法规则", "Skill casting rules", "技能施法规则") },
        { "InteruptSkill" , new LabelEntry("Skills this skill can interrupt", "此技能允许打断的技能", "List the old skill IDs that this new skill is allowed to interrupt; these are skill IDs, not cast UUIDs.", "填写此新技能允许打断的旧技能 ID；不是单次施法 UUID。") },
        { "MoveCasting" , new LabelEntry("Allow horizontal movement while casting", "允许施法时水平移动", "Allows ordinary horizontal movement during this skill. Starting a jump is still disabled in this project.", "允许技能中的普通水平移动。本项目技能中仍禁止主动起跳。") },
        { "size", new LabelEntry("Size", "数量", "Number of serialized array elements.", "序列化数组元素数量。") },
        { "m_Script" , new LabelEntry("Script", "脚本", "Script", "脚本") },
    };

    /// <summary>原框架 Header、Tooltip 与校验说明的完整文本映射；未知扩展和用户文本原样保留。</summary>
    private static readonly Dictionary<string, string> FrameworkChinese = new Dictionary<string, string>
    {
        { "Energy requirements", "能量条件" },
        { "Transition cue", "行为衔接提示" },
        { "Interaction filters", "交互过滤" },
        { "Base damage", "基础伤害" },
        { "Damage mode", "伤害模式" },
        { "Hit stop", "命中停顿" },
        { "Camera shake", "镜头震动" },
        { "Chromatic aberration", "色差" },
        { "Impact and toughness", "冲击与韧性" },
        { "Hit effect", "命中特效" },
        { "Hit audio", "命中音效" },
        { "Bone tracking", "骨骼追踪" },
        { "Effect transform", "特效变换" },
        { "Audio variation", "音效随机变化" },
        { "Projectile", "飞行物" },
        { "Impact effect", "撞击特效" },
        { "Launch shake", "发射震动" },
        { "Launch chromatic effect", "发射色差" },
        { "Launch hit stop", "发射命中停顿" },
        { "Impact hit stop", "撞击命中停顿" },
        { "Impact shake", "撞击震动" },
        { "Impact chromatic effect", "撞击色差" },
        { "Particle binding", "粒子绑定" },
        { "Actor-relative trail path", "角色局部拖尾路径" },
        { "Drive mode", "驱动方式" },
        { "Direction and target", "方向与目标" },
        { "Horizontal displacement", "水平位移" },
        { "Destination constraints", "目的地约束" },
        { "Use TrailRenderer.emitting instead of toggling the GameObject. Emitting preserves existing trail segments; toggling the object hides them immediately. Applies to path bindings.", "采用 TrailRenderer.emitting 控制拖尾发射，保留已有拖尾片段；切换 GameObject 会立即隐藏它们。此设置作用于路径绑定。" },
        { "Offsets are relative to a dedicated presentation pivot, not the gameplay camera controller.", "偏移相对于独立表现枢轴，不作用于玩法镜头控制器。" },
        { "Empty allows any category. Otherwise matches the category supplied by the host, e.g. Dodge or Movement.", "空值允许任意分类；非空时匹配宿主提供的分类，例如 Dodge 或 Movement。" },
        { "Empty matches any attack tag.", "空值匹配任意攻击标签。" },
        { "Device-independent command ID interpreted by the host. Takes precedence over the legacy trigger key when nonempty.", "由宿主解释的设备无关命令 ID；非空时优先于旧触发按键。" },
        { "Animation transition blend duration in seconds. Zero uses the host default.", "动画衔接混合时长，单位为秒；0 使用宿主默认值。" },
        { "Minimum energy required by the host rules. Zero means no requirement.", "宿主规则要求的最低能量；0 表示无能量门槛。" },
        { "Energy consumed by the host after the requirement succeeds. Zero means no cost.", "满足条件后由宿主消耗的能量；0 表示不消耗。" },
        { "Request a slow-motion decision window when the transition triggers. Implemented by the host.", "行为衔接触发时请求慢动作决策窗口，由宿主实现。" },
        { "Effect shown during a transition window when energy requirements are met.", "行为衔接窗口内满足能量条件时显示的特效。" },
        { "Local offset of the transition cue.", "衔接提示相对于角色的局部偏移，单位为米。" },
        { "Scale of the transition cue.", "行为衔接提示特效的缩放倍率。" },
        { "Whether the cue follows the actor.", "提示特效是否随角色移动。" },
        { "Optional audio when the cue appears.", "提示出现时播放的可选音效。" },
        { "Cue audio volume.", "提示音效音量，范围为 0 至 1。" },
        { "Full box height in world units.", "盒体完整高度，单位为米。" },
        { "Box rotation relative to the actor, in degrees.", "盒体相对于角色的旋转，单位为度。" },
        { "Single Hit: one hit within the frame window. Repeated Hits: query every tick interval.", "单次命中在帧窗口内只命中一次；重复命中按检测间隔查询。" },
        { "Frame interval between repeated hit queries. Minimum one.", "重复命中查询之间的帧间隔，最小为 1 帧。" },
        { "Request hit stop on a confirmed hit.", "确认命中时请求命中停顿。" },
        { "Hit stop duration in seconds; typical range 0.02 to 0.08.", "命中停顿时长，单位为秒；常用范围为 0.02 至 0.08 秒。" },
        { "Request camera shake.", "请求镜头震动。" },
        { "Shake duration in seconds.", "镜头震动时长，单位为秒。" },
        { "Shake amplitude.", "镜头震动幅度。" },
        { "Shake frequency in Hz.", "镜头震动频率，单位为赫兹。" },
        { "Request chromatic aberration on a confirmed hit. Requires a host receiver.", "确认命中时请求色差，需要宿主接收器支持。" },
        { "Chromatic effect duration in seconds.", "色差效果时长，单位为秒。" },
        { "Peak chromatic intensity from zero to one.", "色差峰值强度，范围为 0 至 1。" },
        { "Impact tier used by the host interruption rules.", "宿主打断规则使用的冲击等级。" },
        { "Toughness damage applied by the host.", "由宿主结算的韧性伤害。" },
        { "Legacy enemy attack classification.", "旧敌人攻击分类。" },
        { "Deprecated. Use impactLevel to select Light, Medium or Heavy hit reactions.", "已弃用；使用 impactLevel 选择轻、中、重受击反应。" },
        { "Energy gained (positive) or spent (negative) on a confirmed hit.", "确认命中后的能量变化；正数增加，负数消耗。" },
        { "Request a slow-motion decision window on hit; the host checks energy.", "命中时请求慢动作决策窗口，能量条件由宿主检查。" },
        { "Minimum energy required for the slow-motion request.", "慢动作请求所需的最低能量。" },
        { "Request a glow cue on hit; implemented by the host presentation layer.", "命中时请求发光提示，由宿主表现层实现。" },
        { "Particle effect played on the hit target.", "在受击目标上播放的粒子特效。" },
        { "Effect offset relative to the target.", "特效相对于受击目标的局部偏移，单位为米。" },
        { "Effect Euler rotation.", "特效欧拉旋转，单位为度。" },
        { "Hit effect scale.", "命中特效缩放倍率。" },
        { "Playback speed multiplier; one is normal speed.", "播放速度倍率；1 为正常速度。" },
        { "Whether the effect follows the target.", "特效是否跟随受击目标。" },
        { "Use attacker orientation when enabled; otherwise use target orientation.", "启用时采用攻击者朝向，否则采用受击目标朝向。" },
        { "Audio played on a confirmed hit.", "确认命中后播放的音效。" },
        { "Hit volume from zero to one.", "命中音效音量，范围为 0 至 1。" },
        { "Randomize hit audio.", "随机化命中音效音高与音量。" },
        { "Pitch variation around the base value.", "在基础音高附近的随机变化幅度。" },
        { "Volume variation around the base value.", "在基础音量附近的随机变化幅度。" },
        { "Sample hit shape offsets per frame to follow a bone path.", "逐帧采样判定体偏移以跟随骨骼路径。" },
        { "Offset zero belongs to the start frame; offset i belongs to start+i. Count must equal end-start+1.", "第 0 项对应开始帧，第 i 项对应开始帧加 i；数量必须等于结束帧减开始帧加 1。" },
        { "Editor bone path relative to the actor root.", "编辑器骨骼路径，相对于角色根节点，实际路径名保持原样。" },
        { "Optional actor socket path. A single name must identify one child transform.", "可选角色挂点路径；只写单个名称时必须唯一对应一个子节点。" },
        { "Independent effect scale on each axis.", "特效各轴独立缩放倍率。" },
        { "Particle playback multiplier; one is normal speed.", "粒子播放倍率；1 为正常速度。" },
        { "Use world coordinates instead of actor-local coordinates.", "使用世界坐标；关闭时使用角色局部坐标。" },
        { "Effect cleanup delay in seconds. Zero uses the particle duration.", "特效清理延迟，单位为秒；0 使用粒子原持续时长。" },
        { "Randomize audio pitch and volume.", "随机化音效音高和音量。" },
        { "Pitch is one plus or minus this value.", "以 1 为基础音高，在此数值范围内上下随机变化。" },
        { "Volume is base volume plus or minus this value, clamped to zero through one.", "基础音量在此幅度内上下随机变化，最终限制在 0 至 1。" },
        { "Base volume from zero to one.", "基础音量，范围为 0 至 1。" },
        { "Attack event index; minus one applies to every attack.", "攻击事件索引；-1 表示应用于全部攻击。" },
        { "Effect prefab played on the hit target.", "在受击目标上播放的特效预制体。" },
        { "Effect scale.", "特效缩放倍率。" },
        { "Effect playback multiplier.", "特效播放倍率。" },
        { "Use attacker orientation instead of target orientation.", "采用攻击者朝向。" },
        { "Volume from zero to one.", "音量，范围为 0 至 1。" },
        { "Projectile prefab. The host provides its movement and collision receiver.", "飞行物预制体；运动及碰撞接收器由宿主提供。" },
        { "Child path to the muzzle relative to the actor root. Empty uses spawn offset.", "相对于角色根节点的发射口路径；空值使用生成偏移。" },
        { "Local spawn offset used when no muzzle path is assigned.", "没有发射口路径时使用的角色局部生成偏移，单位为米。" },
        { "Launch speed in world units per second.", "发射速度，单位为米／秒。" },
        { "Lifetime in seconds before host cleanup.", "宿主清理前的寿命，单位为秒。" },
        { "Damage multiplier.", "伤害倍率。" },
        { "Knockback strength.", "击退强度。" },
        { "Impact tier.", "冲击等级。" },
        { "Scale applied to the spawned projectile; one preserves prefab scale.", "生成的飞行物缩放倍率；1 保留预制体原缩放。" },
        { "Additional Euler rotation relative to the flight direction, for prefab orientation correction.", "相对于飞行方向附加的欧拉旋转，用于纠正预制体朝向，单位为度。" },
        { "Continuously track a target during flight.", "飞行过程中持续追踪目标。" },
        { "Homing duration in seconds from launch; zero tracks for the full lifetime.", "从发射开始计算的追踪时长，单位为秒；0 表示全寿命追踪。" },
        { "Homing turn speed in degrees per second.", "追踪转向速度，单位为度／秒。" },
        { "Delay before homing starts, in seconds.", "开始追踪前的延迟，单位为秒。" },
        { "Aim at the nearest enemy; otherwise use actor forward.", "瞄准最近敌人；关闭时使用角色前方。" },
        { "Number of projectiles per shot; values above one form a fan.", "每次发射的飞行物数量；大于 1 时形成扇形。" },
        { "Fan spread angle in degrees, used for multiple projectiles.", "多飞行物扇形散布角，单位为度。" },
        { "Optional muzzle effect.", "可选发射口特效。" },
        { "Optional launch audio.", "可选发射音效。" },
        { "Whether incoming attacks can destroy this projectile.", "飞行物是否可被来袭攻击摧毁。" },
        { "Optional destruction effect.", "可选摧毁特效。" },
        { "Optional destruction audio.", "可选摧毁音效。" },
        { "Optional impact effect overriding the host projectile default.", "覆盖宿主飞行物默认命中表现的可选特效。" },
        { "Effect offset relative to the impact point.", "相对于命中点的特效偏移，单位为米。" },
        { "Impact effect Euler rotation.", "命中特效欧拉旋转，单位为度。" },
        { "Impact effect scale.", "命中特效缩放倍率。" },
        { "Playback multiplier; one is normal speed.", "播放倍率；1 为正常速度。" },
        { "Optional impact audio overriding the host projectile default.", "覆盖宿主飞行物默认命中表现的可选音效。" },
        { "Impact volume from zero to one.", "命中音效音量，范围为 0 至 1。" },
        { "Request camera shake on launch.", "发射时请求镜头震动。" },
        { "Launch shake duration in seconds.", "发射震动时长，单位为秒。" },
        { "Launch shake amplitude.", "发射震动幅度。" },
        { "Launch shake frequency in Hz.", "发射震动频率，单位为赫兹。" },
        { "Request a chromatic flash on launch. Requires host presentation.", "发射时请求色差闪光，需要宿主表现支持。" },
        { "Launch chromatic duration in seconds.", "发射色差时长，单位为秒。" },
        { "Peak launch chromatic intensity from zero to one.", "发射色差峰值强度，范围为 0 至 1。" },
        { "Request a short action pause on launch.", "发射时请求短暂行为停顿。" },
        { "Launch hit stop duration in seconds; typical range 0.01 to 0.03.", "发射命中停顿时长，单位为秒；常用范围为 0.01 至 0.03 秒。" },
        { "Request hit stop on impact.", "命中时请求停顿。" },
        { "Impact hit stop duration in seconds.", "命中停顿时长，单位为秒。" },
        { "Request shake on impact.", "命中时请求镜头震动。" },
        { "Impact shake duration in seconds.", "命中震动时长，单位为秒。" },
        { "Impact shake amplitude.", "命中震动幅度。" },
        { "Impact shake frequency in Hz.", "命中震动频率，单位为赫兹。" },
        { "Request a chromatic flash on impact.", "命中时请求色差闪光。" },
        { "Impact chromatic duration in seconds.", "命中色差时长，单位为秒。" },
        { "Peak impact chromatic intensity from zero to one.", "命中色差峰值强度，范围为 0 至 1。" },
        { "Only actions at or above this priority may cancel the current recovery.", "只有不低于此优先级的行为可取消当前后摇。" },
        { "Cancel blend duration in seconds. Zero uses the host default.", "取消时的混合时长，单位为秒；0 使用宿主默认值。" },
        { "Blend frames from the previous segment. Zero is a hard cut; applies from the second segment onward.", "从上一片段混合的帧数；0 为直接切换，从第二片段起生效。" },
        { "Host armor tier: zero is L1, one is L2. Immunity and interruption are host rules.", "宿主霸体等级：0 为 L1，1 为 L2；免疫和打断由宿主规则控制。" },
        { "Optional armor tint for host presentation.", "宿主表现使用的可选霸体颜色。" },
        { "Allow target tracking in this window. Disable to lock facing; locks override overlapping turn windows.", "在窗口内允许追踪目标；关闭则锁定朝向，锁定优先于重叠的转向窗口。" },
        { "Turn speed in degrees per second. Zero snaps to the target.", "转向速度，单位为度／秒；0 表示直接朝向目标。" },
        { "Restrict rotation to the vertical axis; otherwise rotate in 3D.", "只绕竖直轴旋转；关闭时允许三维旋转。" },
        { "Optional particle reference. Host receivers decide how to resolve and control it; persistent actions should use actor-relative bindings.", "可选粒子引用，由宿主接收器解析和控制；持续行为应采用相对于角色的绑定。" },
        { "Child path relative to the actor root. Ignored when a particle reference is assigned.", "相对于角色根节点的子路径；已指定粒子引用时忽略。" },
        { "Use TrailRenderer.emitting instead of toggling the GameObject. Enabling through this flag avoids resetting existing trails.", "通过 TrailRenderer.emitting 控制发射，避免切换 GameObject 并重置已有拖尾。" },
        { "Charge time needed to enter this stage, in seconds.", "进入此阶段所需的蓄力时长，单位为秒。" },
        { "Damage multiplier for this stage.", "此蓄力阶段的伤害倍率。" },
        { "Toughness damage multiplier for this stage.", "此蓄力阶段的韧性伤害倍率。" },
        { "Impact tier for this stage.", "此蓄力阶段的冲击等级。" },
        { "Knockback multiplier for this stage.", "此蓄力阶段的击退倍率。" },
        { "Program mode solves authored movement. Animation mode uses horizontal root motion; the host owns gravity and collision.", "程序模式计算配置位移；动画模式采用水平根运动，重力和碰撞由宿主负责。" },
        { "Animation mode: horizontal root-motion scale. Zero removes horizontal motion without changing the pose.", "动画模式的水平根运动倍率；0 关闭水平位移且保留动画姿势。" },
        { "Program mode: movement direction source.", "程序模式的位移方向来源。" },
        { "Program mode: Self moves relatively; Target converges on a destination.", "程序模式：自身采用相对位移，目标模式汇聚到目的地。" },
        { "Program mode: target source. Missing targets use the resolver fallback, then actor forward.", "程序模式的目标来源；缺少目标时先采用解析器兜底，再采用角色前方。" },
        { "Program mode: recompute direction every frame. Disabled locks the initial direction.", "程序模式逐帧重新计算方向；关闭时锁定初始方向。" },
        { "Program mode: custom direction for World Direction or Local Direction.", "程序模式下世界方向或局部方向使用的自定义方向向量。" },
        { "Normalized cumulative distance curve within this segment: time zero to one maps to the start and end frames.", "片段内归一化累计距离曲线；时间 0 至 1 对应开始帧至结束帧。" },
        { "Hard cap on cumulative horizontal distance. Zero produces no horizontal displacement.", "累计水平距离的硬上限，单位为米；0 不产生水平位移。" },
        { "Program mode: offset added to the target position.", "程序模式叠加到目标位置的偏移，单位为米。" },
        { "Offset space; Target Local rotates with the target.", "偏移坐标空间；目标局部空间随目标旋转。" },
        { "Stop at the destination even if the curve is unfinished; do not overshoot.", "即使曲线未播放完，到达目的地也停止，避免越过。" },
        { "Arrival tolerance in world units to avoid oscillation near moving targets.", "到达容差，单位为米；用于避免在移动目标附近来回振荡。" },
        { "Minimum separation from the target. If already closer, toward-target motion is suppressed without pushing the actor backward.", "与目标的最小距离，单位为米；已经更近时抑制朝目标位移，不把角色反向推开。" },
        { "Normalized cumulative vertical curve within this segment.", "片段内归一化累计竖直位移曲线。" },
        { "Total vertical displacement. Zero disables the vertical component.", "总竖直位移，单位为米；0 关闭竖直分量。" },
        { "Align actor facing to displacement direction, subject to authored facing locks.", "角色朝向对齐位移方向，仍受已配置的朝向锁定约束。" },
        { "Ignore gravity while this segment is active. Overlapping requests combine.", "片段有效时忽略重力，重叠请求合并。" },
        { "Ignore actor collisions while retaining static geometry collision. Requires backend support.", "忽略角色间碰撞但保留静态场景碰撞，需要后端支持。" },
        { "Policy for remaining curve distance after constraints clip movement.", "约束裁剪位移后，剩余曲线距离的处理策略。" },
        { "Full cone angle centered on defender forward.", "以防御者前方为中心的完整锥角，单位为度。" },
        { "Stable window ID; can also identify a host response.", "稳定窗口 ID，也可标识宿主响应；实际 ID 不随语言改变。" },
        { "Only matching signals are evaluated. Use incoming-hit for defensive hit queries.", "只评估匹配信号；防御受击查询使用 incoming-hit。" },
        { "Highest priority wins; equal priorities use list order.", "最高优先级胜出，同优先级采用列表顺序。" },
        { "Full source cone around actor forward; 360 accepts all directions.", "以角色前方为中心的完整来源锥角，单位为度；360 接受所有方向。" },
        { "Incoming damage multiplier. Zero is immune; 0.2 takes twenty percent.", "来袭伤害倍率；0 表示免疫，0.2 表示承受 20% 伤害。" },
        { "Maximum successful activations per action. Zero is unlimited.", "每次行为成功触发的最大次数；0 表示不限次数。" },
        { "All conditions must pass. Reuse condition assets or extend InteractionCondition.", "所有条件必须通过；可复用条件资源或扩展 InteractionCondition。" },
        { "0 preserves legacy timing; 1 uses an explicit action timeline rate. Use the backed-up migration for existing actions.", "0 保留旧计时，1 使用明确行为时间轴帧率；现有行为请采用带备份迁移。" },
        { "Optional inclusive phase labels. These do not change event boundaries or action duration.", "可选阶段标签包含起止帧，不改变事件边界或行为时长。" },
        { "Host metadata identifying the actor profile. All authoring tracks are available to players and enemies.", "标识角色类型的宿主元数据；全部创作轨道都可用于玩家与敌人。" },
        { "Higher-priority actions may cancel lower-priority actions according to host rules.", "高优先级行为可按宿主规则取消低优先级行为。" },
        { "Activation energy cost checked by the host before playback.", "播放前由宿主检查的启动能量消耗。" },
        { "Default energy gain on a confirmed hit; individual attacks can override it.", "确认命中后的默认能量收益；单个攻击可覆盖。" },
        { "Animation segments evaluated in start-frame order.", "按开始帧顺序计算的动画片段。" },
        { "Displacement segments. When present they replace the legacy whole-action curve. Each segment curve uses local normalized time, unlike the legacy whole-action curve.", "存在位移片段时替代旧整行为曲线；各片段采用自己的局部归一化时间。" },
        { "Legacy cumulative distance curve over the whole action; this is distance, not speed. Used only when there are no displacement segments.", "旧整行为累计距离曲线，数值表示距离而非速度；只在没有位移片段时生效。" },
        { "Legacy distance paired with the whole-action curve; used only without displacement segments.", "与旧整行为曲线配对的距离，单位为米；只在没有位移片段时生效。" },
        { "Visible track layout, ordering and expansion. Hiding a track does not disable its events.", "可见轨道布局、顺序与折叠状态；隐藏轨道不会禁用事件。" },
        { "First frame allowing host movement interruption. Zero means any time; minus one disables it.", "宿主允许移动打断的首帧；0 表示任意时刻，-1 表示关闭。" },
        { "Telegraph flashes or sounds before an attack; available to any actor.", "攻击前的预警闪光或音效，可用于任意角色。" },
        { "Armor windows evaluated by the host.", "由宿主评估的霸体窗口。" },
        { "Target tracking and facing-lock windows for any actor.", "任意角色的目标追踪和朝向锁定窗口。" },
        { "Trail activation windows; resolved by host presentation.", "由宿主表现解析的拖尾开启窗口。" },
        { "Legacy whole-action armor flag. Host rules may derive it from armor windows.", "旧整行为霸体标记，宿主可由霸体窗口推导。" },
        { "Additional phase-two attacks, combined with the base attacks.", "第二阶段额外攻击，与基础攻击合并。" },
        { "Additional phase-two effects and audio.", "第二阶段额外特效与音效。" },
        { "Host phase-two hit-shape scale; 1.5 expands the base shape by fifty percent.", "宿主第二阶段判定体倍率；1.5 表示基础判定扩大 50%。" },
        { "Action identity", "行为身份" },
        { "Actor profile", "角色类型" },
        { "Action parameters", "行为参数" },
        { "Cancel priority", "取消优先级" },
        { "Host combat rules", "宿主战斗规则" },
        { "Animation timeline", "动画时间轴" },
        { "Legacy animation fields", "旧动画字段" },
        { "Motion", "运动" },
        { "Timeline layout", "时间轴布局" },
        { "Timeline events", "时间轴事件" },
        { "Interaction windows", "交互窗口" },
        { "Interruption", "打断" },
        { "Actor-specific rules", "角色规则" },
        { "Phase variants", "阶段变体" },
        { "Damage", "伤害" },
        { "Projectile scale", "飞行物缩放" },
        { "Projectile rotation", "飞行物旋转" },
        { "Homing", "追踪" },
        { "Launch settings", "发射设置" },
        { "Effects", "特效" },
        { "Destructible projectile", "可摧毁飞行物" },
        { "Vertical displacement", "竖直位移" },
        { "Options", "选项" },
        { "Action config is null.", "行为配置为空。" },
        { "Unsupported timing version.", "不支持此计时版本。" },
        { "Timeline FPS must be finite and between 1 and 240.", "时间轴帧率必须为 1 至 240 的有限数值。" },
        { "Legacy mixed clip FPS: preview and host timing may differ. Explicit migration required.", "旧配置混用了动画帧率，预览与宿主计时可能不同，需要明确计时迁移。" },
        { "Camera values must be finite, shake values nonnegative, and an envelope is required.", "镜头参数必须为有限数值，震动值不能为负且必须指定包络曲线。" },
        { "Envelope key times and values must be finite.", "包络曲线关键点的时间和值必须为有限数值。" },
        { "Turning requires a valid target kind and a finite nonnegative speed.", "转向需要有效目标类型和非负有限速度。" },
        { "Phase requires an ID and an ordered non-negative frame range.", "阶段必须具有 ID 及顺序正确的非负帧范围。" },
        { "Phase 2 range multiplier must be greater than zero.", "第二阶段范围倍率必须大于 0。" },
        { "Move cancel frame must be -1 or a non-negative frame.", "移动取消帧必须为 -1 或非负帧号。" },
        { "Exit frame cannot be negative.", "结束帧不能为负。" },
        { "Interaction window is null.", "交互窗口为空。" },
        { "Window IDs must be nonempty and unique within an action.", "窗口 ID 不能为空且在同一行为内必须唯一。" },
        { "Interaction signal cannot be empty.", "交互信号不能为空。" },
        { "Window must have an inclusive range inside the action exit frame.", "窗口的包含式帧范围必须位于行为结束帧以内。" },
        { "Unknown interaction response.", "未知交互响应类型。" },
        { "Source angle must be between 0 and 360.", "来源角度必须在 0 至 360 度之间。" },
        { "Activation limit must be >= 0 and damage multiplier between 0 and 1.", "触发次数上限必须非负，伤害倍率必须在 0 至 1 之间。" },
        { "Interaction condition reference is missing.", "缺少交互条件引用。" },
        { "At least one animation segment is required.", "至少需要一个动画片段。" },
        { "No IActionAnimator capability is available.", "没有可用的 IActionAnimator 动画接口。" },
        { "Animation segment has no clip.", "动画片段没有指定剪辑。" },
        { "Explicit animation crop must have a positive duration.", "明确动画裁剪时长必须大于 0。" },
        { "Animation frames cannot be negative.", "动画帧号不能为负。" },
        { "Clip end frame is earlier than clip start frame.", "剪辑结束帧早于开始帧。" },
        { "Animation starts after the configured exit frame.", "动画开始帧晚于配置的结束帧。" },
        { "Event list is null; initialize it before authoring.", "事件列表为空，请在编辑前初始化。" },
        { "Event entry is null.", "事件项为空。" },
        { "Event frames cannot be negative.", "事件帧号不能为负。" },
        { "Event end frame is earlier than its start frame.", "事件结束帧早于开始帧。" },
        { "Event is outside the configured exit frame.", "事件超出配置的结束帧。" },
        { "Collision dimensions must be positive and finite; offset and rotation must be finite.", "判定尺寸必须为正有限数，偏移和旋转必须为有限数。" },
        { "Select a collision response and provide a nonempty contact signal for Signal mode.", "请选择判定响应；信号模式必须提供非空接触信号。" },
        { "Socket path cannot be combined with Use World Space.", "挂点路径不能同时使用世界坐标。" },
        { "Typed Effect event requires exactly its matching reference: Visual Effect uses a particle system; Audio uses an audio clip.", "明确类型的效果事件只能具有匹配引用：特效采用粒子系统，音效采用音频剪辑。" },
        { "Legacy empty FX/Sound placeholder is skipped during playback.", "旧空特效／音效占位在播放时跳过。" },
        { "FX/Sound event requires a particle system or audio clip.", "特效／音效事件需要粒子系统或音频剪辑。" },
        { "Hit effect requires a VFX or audio reference.", "命中表现需要特效或音效引用。" },
        { "Warning cue requires a VFX or audio reference.", "预警表现需要特效或音效引用。" },
        { "Projectile prefab is required.", "必须指定飞行物预制体。" },
        { "Move segment has no duration and no distance.", "位移片段没有时长和距离。" },
        { "Move distances and tolerances cannot be negative.", "位移距离和容差不能为负。" },
        { "Program-driven move segment requires a cumulative movement curve.", "程序驱动位移片段需要累计位移曲线。" },
        { "Event reaches or follows Complete; gameplay at and after the completion frame will not execute.", "事件到达或晚于完成行为节点；完成帧及之后的玩法事件不会执行。" },
        { "Flow requires a valid kind and nonnegative, ordered frames.", "流程需要有效类型及顺序正确的非负帧号。" },
        { "Branch requires a target and a command unless Automatic is enabled.", "行为分支需要目标，非自动模式还需要输入命令。" },
        { "An automatic branch at frame zero cannot target itself.", "第 0 帧的自动分支不能指向自身。" },
        { "Allow Exit requires a valid priority rule.", "允许退出需要有效优先级规则。" },
        { "A Flow condition reference is missing.", "缺少流程条件引用。" },
        { "Node starts at or after Complete and cannot execute.", "节点开始帧位于或晚于完成行为节点，无法执行。" },
        { "Multiple completion boundaries are configured. The earliest reachable boundary ends the action.", "配置了多个完成边界，最早可达边界将结束行为。" },
        { "Missing condition.", "缺少条件引用。" },
        { "Condition graph contains a cycle.", "条件图存在循环引用。" },
        { "Unknown condition operator.", "未知条件运算符。" },
        { "Condition group must contain children.", "条件组必须包含子条件。" },
        { "Not requires exactly one child.", "取反运算必须恰好包含一个子条件。" },
        { "Resource name and a finite threshold are required.", "必须提供资源名称和有限阈值。" },
        { "Facing angle must be between 0 and 360.", "朝向角必须在 0 至 360 度之间。" },
    };

    /// <summary>获取指定字段的当前语言名称，未知扩展字段保持调用方提供的真实显示名称。</summary>
    /// <param name="fieldName">实际序列化字段名，保持原拼写。</param>
    /// <param name="englishFallback">未知字段的 Unity 原显示名称。</param>
    /// <returns>框架显示标签；不会改写用户字段值。</returns>
    public static string Label(string fieldName, string englishFallback)
    {
        // 当前字段的只读中英文案，查询失败时保留外部扩展名称。
        LabelEntry entry;
        if (Labels.TryGetValue(fieldName, out entry)) return SkillEditorLocalization.Text(entry.English, entry.Chinese);
        return englishFallback;
    }

    /// <summary>获取序列化字段的双语标签与工具提示，数组元素只翻译固定的“元素”前缀。</summary>
    /// <param name="property">Unity 原属性游标，不修改其名称或数据。</param>
    /// <returns>本次绘制使用的标签；语言切换后重新计算。</returns>
    public static GUIContent Content(SerializedProperty property)
    {
        // 标签查原字段名，提示先使用原声明内容，保留原用途、空间和范围语义。
        string label = Label(property.name, property.displayName);
        // 同名字段的不同上下文仍使用各自声明 Tooltip，而不按名称粗略覆盖。
        string tooltip = property.tooltip;
        // 元数据边界只读取字段属性，不读取弱类型领域值。
        FieldInfo field = ResolveField(property);
        if (string.IsNullOrEmpty(tooltip) && field != null)
        {
            // Unity Tooltip 元数据不参与资产保存。
            TooltipAttribute attribute = field.GetCustomAttribute<TooltipAttribute>();
            if (attribute != null) tooltip = attribute.tooltip;
        }
        if (!string.IsNullOrEmpty(tooltip)) tooltip = FrameworkText(tooltip);
        else
        {
            // 没有原提示时才使用新增的双语字段说明。
            LabelEntry entry;
            if (Labels.TryGetValue(property.name, out entry)) tooltip = SkillEditorLocalization.Text(entry.EnglishTooltip, entry.ChineseTooltip);
        }
        if (property.name.StartsWith("data[", StringComparison.Ordinal)) label = SkillEditorLocalization.Text("Element ", "元素 ") + property.name.Substring(4);
        return new GUIContent(label, tooltip);
    }

    /// <summary>按照 Unity 原 enumNames 的顺序生成显示选项，非连续枚举仍写回原 enumValueIndex。</summary>
    /// <param name="property">枚举属性，原 intValue 和索引均由 Unity 管理。</param>
    /// <returns>每个原枚举选项对应的当前语言文案。</returns>
    public static GUIContent[] EnumLabels(SerializedProperty property)
    {
        // Unity 提供的声明选项顺序，不按枚举数值重新排序。
        string[] names = property.enumNames;
        // 当前语言显示数组，不作为业务数据或序列化载荷。
        GUIContent[] labels = new GUIContent[names.Length];
        // 枚举声明项的零起始显示索引。
        for (int index = 0; index < names.Length; index++) labels[index] = new GUIContent(EnumText(names[index], property.name));
        return labels;
    }

    /// <summary>绘制枚举显示；Flags 使用真实位值切换，普通枚举沿用 Unity 原索引。</summary>
    /// <param name="property">本次可编辑的枚举属性，只有用户选择菜单项才写数据。</param>
    /// <param name="label">已翻译的字段标签及提示。</param>
    /// <param name="applyEdit">菜单明确编辑后的原事务回调；语言重绘不调用。</param>
    public static void DrawEnum(SerializedProperty property, GUIContent label, Action applyEdit = null)
    {
        // 反射仅用于 Unity 序列化适配，读取声明类型以识别 Flags，不把反射对象传入业务层。
        Type enumType = ResolveFieldType(property);
        if (enumType != null && enumType.IsEnum && Attribute.IsDefined(enumType, typeof(FlagsAttribute)))
        {
            DrawFlags(property, label, enumType, applyEdit);
            return;
        }
        EditorGUI.BeginChangeCheck();
        // 显示索引独立于枚举数值，例如 Uncancellable=99 仍保留其原值。
        int selected = EditorGUILayout.Popup(label, property.enumValueIndex, EnumLabels(property));
        if (EditorGUI.EndChangeCheck()) property.enumValueIndex = selected;
    }

    /// <summary>按原声明类型定位 Flags 元数据，支持嵌套列表路径并保持 propertyPath 不变。</summary>
    /// <param name="property">Unity 原始属性路径，Array.data 段仅用于类型导航。</param>
    /// <returns>最后一个字段的声明类型，未知第三方结构返回 null。</returns>
    private static Type ResolveFieldType(SerializedProperty property)
    {
        // 仅获取 Unity 对象的类型信息；这是序列化元数据边界，不读取弱类型业务载荷。
        Type current = property.serializedObject.targetObject.GetType();
        // 原属性路径的分段快照，不更改字段或对象本身。
        string[] segments = property.propertyPath.Split('.');
        // 当前导航段的零起始索引。
        for (int index = 0; index < segments.Length; index++)
        {
            if (segments[index] == "Array")
            {
                if (current.IsArray) current = current.GetElementType();
                else if (current.IsGenericType) current = current.GetGenericArguments()[0];
                else return null;
                index++;
                continue;
            }
            // 当前字段可能在基类中声明，按继承链查找相同原名称。
            Type declaring = current;
            // 反射字段仅用于类型定位，不获取或传递其业务值。
            FieldInfo field = null;
            while (declaring != null && field == null)
            {
                field = declaring.GetField(segments[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                declaring = declaring.BaseType;
            }
            if (field == null) return null;
            current = field.FieldType;
        }
        return current;
    }

    /// <summary>查询原属性对应字段的声明元数据，列表元素导航不修改列表。</summary>
    /// <param name="property">Unity 原属性路径。</param>
    /// <returns>原字段元数据；未知结构返回空。</returns>
    private static FieldInfo ResolveField(SerializedProperty property)
    {
        // 反射与序列化元数据边界，仅读取类型与属性声明。
        Type current = property.serializedObject.targetObject.GetType();
        // 属性路径的导航分段快照，字段名称原样使用。
        string[] segments = property.propertyPath.Split('.');
        // 最后一次匹配的声明字段，用于读取 Unity 属性特性。
        FieldInfo result = null;
        for (/* 当前原属性路径分段的零起始索引，Array 段会同时跳过下一 data[n] 段。 */ int index = 0; index < segments.Length; index++)
        {
            if (segments[index] == "Array")
            {
                if (current.IsArray) current = current.GetElementType();
                else if (current.IsGenericType) current = current.GetGenericArguments()[0];
                else return null;
                index++;
                continue;
            }
            // 按继承链寻找当前原字段，不读取其领域值。
            Type declaring = current;
            result = null;
            while (declaring != null && result == null)
            {
                result = declaring.GetField(segments[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                declaring = declaring.BaseType;
            }
            if (result == null) return null;
            current = result.FieldType;
        }
        return result;
    }

    /// <summary>翻译已知框架提示，保留未知异常及用户自定义说明。</summary>
    /// <param name="message">原框架完整文案。</param>
    /// <returns>当前语言的只读显示文案。</returns>
    public static string FrameworkText(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        // 已知完整英文说明对应的中文，避免按子字符串误改用户内容。
        string chinese;
        if (FrameworkChinese.TryGetValue(message, out chinese)) return SkillEditorLocalization.Text(message, chinese);
        return message;
    }

    /// <summary>只翻译验证结果显示，不改错误码、轨道定位或原错误对象。</summary>
    /// <param name="message">验证器原消息，包含可能的事件索引或声明类型名。</param>
    /// <returns>保留原动态数据的双语显示。</returns>
    public static string ValidationMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        // 两类已知动态模板仍保留原事件索引或接口声明名称。
        const string overlapPrefix = "Conflicts with overlapping AdjustMotion event ";
        if (message.StartsWith(overlapPrefix, StringComparison.Ordinal) && message.EndsWith(".", StringComparison.Ordinal))
            return SkillEditorLocalization.Text(message, "与重叠的朝向调整事件 " + message.Substring(overlapPrefix.Length, message.Length - overlapPrefix.Length - 1) + " 冲突。");
        // 该模板仅翻译框架外壳，尖括号内的真实 C# 类型名保持原样。
        const string handlerPrefix = "No IActionEventHandler<";
        const /* 原处理器缺失诊断的固定后缀，用于只翻译框架模板并保留动态类型名称。 */ string handlerSuffix = "> is registered.";
        if (message.StartsWith(handlerPrefix, StringComparison.Ordinal) && message.EndsWith(handlerSuffix, StringComparison.Ordinal))
            return SkillEditorLocalization.Text(message, "尚未注册 IActionEventHandler<" + message.Substring(handlerPrefix.Length, message.Length - handlerPrefix.Length - handlerSuffix.Length) + ">。");
        return FrameworkText(message);
    }

    /// <summary>翻译宿主规则导出的稳定错误模板，原技能 ID 和未知异常保持原样。</summary>
    /// <param name="message">宿主原异常文本。</param>
    /// <returns>当前语言错误显示。</returns>
    public static string HostMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        switch (message)
        {
            case "请退出运行模式后编辑和导出施法规则。规则在启动前同步装载。": return SkillEditorLocalization.Text("Exit Play Mode before editing or exporting casting rules. Rules are loaded before startup.", message);
            case "运行中不能导出权限，防止界面显示已修改但本次进程仍使用旧规则。": return SkillEditorLocalization.Text("Casting rules cannot be exported during Play Mode because the running process still uses its loaded rules.", message);
            case "未找到正式 tbskillinfo.bytes，拒绝导出无法验证的技能权限。": return SkillEditorLocalization.Text("The formal tbskillinfo.bytes was not found; casting rules cannot be validated or exported.", message);
        }
        // 原错误前缀数组只识别框架模板，后缀用户数据不参与翻译。
        string[] chinesePrefixes = { "权限技能不在旧主表中：", "InteruptSkill 引用不存在的旧技能：" };
        // 与中文前缀一一对应的英文框架说明。
        string[] englishPrefixes = { "Casting-rule skill is missing from the original table: ", "InteruptSkill refers to an unknown original skill: " };
        for (/* 当前双语宿主提示前缀的零起始索引，两组前缀使用同一索引对应。 */ int index = 0; index < chinesePrefixes.Length; index++)
            if (message.StartsWith(chinesePrefixes[index], StringComparison.Ordinal)) return SkillEditorLocalization.Text(englishPrefixes[index], chinesePrefixes[index]) + message.Substring(chinesePrefixes[index].Length);
        return FrameworkText(message);
    }

    /// <summary>Flags 菜单只切换实际声明的位，保留复合值、非连续位和未声明的原始位。</summary>
    /// <param name="property">待绘制的枚举属性，写入只发生在明确的菜单回调中。</param>
    /// <param name="label">当前语言字段标签。</param>
    /// <param name="enumType">已经确认带 FlagsAttribute 的声明类型。</param>
    /// <param name="applyEdit">窗口原属性提交回调；宿主普通检查器没有此回调时采用原生提交。</param>
    private static void DrawFlags(SerializedProperty property, GUIContent label, Type enumType, Action applyEdit)
    {
        // 枚举元数据边界按真实位值读取；不把 Enum 或 Array 作为业务事件载荷。
        Array values = Enum.GetValues(enumType);
        // 声明名称顺序与 GetValues 一致，用于显示每个真实位。
        string[] names = Enum.GetNames(enumType);
        // 当前完整位组合，语言重绘不规范化或清除未知位。
        int currentValue = property.intValue;
        // 当前选中位名称的显示集合，仅用于 GUI。
        List<string> selectedNames = new List<string>();
        // 所有已声明位的并集，用于保留并显示未知位。
        int knownBits = 0;
        // 声明枚举项的零起始索引。
        for (int index = 0; index < names.Length; index++)
        {
            // 序列化元数据转换边界：Unity 枚举保留原 32 位有符号位模式。
            int bits = unchecked((int)Convert.ToInt64(values.GetValue(index)));
            knownBits |= bits;
            if ((bits == 0 && currentValue == 0) || (bits != 0 && (currentValue & bits) == bits)) selectedNames.Add(EnumText(names[index], property.name));
        }
        if ((currentValue & ~knownBits) != 0) selectedNames.Add(SkillEditorLocalization.Text("Other bits: ", "其他位：") + "0x" + unchecked((uint)(currentValue & ~knownBits)).ToString("X"));
        // 没有声明项匹配时显示完整数值，避免假装清空现有数据。
        string summary = string.Join(", ", selectedNames);
        if (summary.Length == 0) summary = currentValue.ToString();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        if (EditorGUILayout.DropdownButton(new GUIContent(summary), FocusType.Keyboard))
        {
            // 菜单只持有原属性副本，并在用户点击后立即应用该一次编辑。
            SerializedProperty captured = property.Copy();
            // 按真实位显示的临时上下文菜单，不修改原枚举声明。
            GenericMenu menu = new GenericMenu();
            // 每个菜单项使用声明的位模式，不依赖其数组索引。
            for (int index = 0; index < names.Length; index++)
            {
                // 当前菜单项真实位值，由回调按值捕获。
                int bits = unchecked((int)Convert.ToInt64(values.GetValue(index)));
                // 当前项是否包含在完整原组合中。
                bool enabled = (bits == 0 && currentValue == 0) || (bits != 0 && (currentValue & bits) == bits);
                menu.AddItem(new GUIContent(EnumText(names[index], property.name)), enabled, () =>
                {
                    captured.serializedObject.Update();
                    if (bits == 0) captured.intValue = 0;
                    else if ((captured.intValue & bits) == bits) captured.intValue &= ~bits;
                    else captured.intValue |= bits;
                    if (applyEdit != null) applyEdit();
                    else captured.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>绘制原字段的本地化装饰，避免 Unity 默认 HeaderDrawer 再显示英文。</summary>
    /// <param name="property">当前原字段，数组元素不重复父字段装饰。</param>
    private static void DrawDecorations(SerializedProperty property)
    {
        if (property.name.StartsWith("data[", StringComparison.Ordinal)) return;
        // 反射只读取序列化字段的显示特性，未知扩展标题保持原样。
        FieldInfo field = ResolveField(property);
        if (field == null) return;
        // 原间距仍按像素保留。
        SpaceAttribute space = field.GetCustomAttribute<SpaceAttribute>();
        if (space != null) EditorGUILayout.Space(space.height);
        // 原标题按完整框架文案映射，不注册全局 HeaderDrawer。
        HeaderAttribute header = field.GetCustomAttribute<HeaderAttribute>();
        if (header != null) { EditorGUILayout.Space(); EditorGUILayout.LabelField(FrameworkText(header.header), EditorStyles.boldLabel); }
    }

    /// <summary>使用相同 Unity 叶控件绘制双语标签，用户明确编辑后才写入缓存。</summary>
    /// <param name="property">原属性，保存其真实类型、范围、引用和数值。</param>
    /// <param name="label">当前语言字段标签及原语义提示。</param>
    /// <param name="applyEdit">Flags 菜单用户编辑后走原窗口事务；普通绘制不会调用。</param>
    /// <param name="honorConstraints">宿主默认检查器保留 Range/Min，原创作数值入口仍保持原绕过规则。</param>
    public static void DrawLeaf(SerializedProperty property, GUIContent label, Action applyEdit = null, bool honorConstraints = false)
    {
        if (property.propertyType == SerializedPropertyType.Enum)
        {
            DrawDecorations(property);
            DrawEnum(property, label, applyEdit);
            return;
        }
        // 原字段元数据只用于显示适配，不向业务接口传递反射对象。
        FieldInfo field = ResolveField(property);
        // 保留延迟提交、滑块和文本区域特性，只有真实编辑才写回。
        bool delayed = field != null && field.IsDefined(typeof(DelayedAttribute), true);
        // 原范围特性，仅宿主默认字段采用约束。
        RangeAttribute range = null;
        // 原最小值特性，仅实际编辑时限制数值。
        MinAttribute minimum = null;
        if (honorConstraints && field != null) { range = field.GetCustomAttribute<RangeAttribute>(); minimum = field.GetCustomAttribute<MinAttribute>(); }
        DrawDecorations(property);
        switch (property.propertyType)
        {
            case SerializedPropertyType.Float:
                EditorGUI.BeginChangeCheck();
                if (property.type == "double")
                {
                    // 双精度字段沿用 DoubleField，避免显示适配降为单精度。
                    double value = property.doubleValue;
                    if (delayed) value = EditorGUILayout.DelayedDoubleField(label, value);
                    else value = EditorGUILayout.DoubleField(label, value);
                    if (EditorGUI.EndChangeCheck()) { if (minimum != null) value = System.Math.Max(value, minimum.min); property.doubleValue = value; }
                }
                else
                {
                    // 当前原浮点值，滑块范围仍使用声明的原上下限。
                    float value = property.floatValue;
                    if (range != null) value = EditorGUILayout.Slider(label, value, range.min, range.max);
                    else if (delayed) value = EditorGUILayout.DelayedFloatField(label, value);
                    else value = EditorGUILayout.FloatField(label, value);
                    if (EditorGUI.EndChangeCheck()) { if (minimum != null) value = Mathf.Max(value, minimum.min); property.floatValue = value; }
                }
                return;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.ArraySize:
                EditorGUI.BeginChangeCheck();
                if (property.type == "long" || property.type == "ulong")
                {
                    // 长整数字段保留 64 位语义，不按显示索引转换。
                    long value = EditorGUILayout.LongField(label, property.longValue);
                    if (EditorGUI.EndChangeCheck()) { if (minimum != null) value = System.Math.Max(value, (long)minimum.min); property.longValue = value; }
                }
                else
                {
                    // 普通整数字段保持原滑块／延迟控件与值范围。
                    int value = property.intValue;
                    if (range != null) value = EditorGUILayout.IntSlider(label, value, (int)range.min, (int)range.max);
                    else if (delayed) value = EditorGUILayout.DelayedIntField(label, value);
                    else value = EditorGUILayout.IntField(label, value);
                    if (EditorGUI.EndChangeCheck()) { if (minimum != null) value = Mathf.Max(value, (int)minimum.min); property.intValue = value; }
                }
                return;
            case SerializedPropertyType.String:
                // 原文本区域行数约束和多行属性由声明元数据提供。
                TextAreaAttribute area = null;
                // 原固定多行显示行数。
                MultilineAttribute multiline = null;
                if (field != null) { area = field.GetCustomAttribute<TextAreaAttribute>(); multiline = field.GetCustomAttribute<MultilineAttribute>(); }
                EditorGUI.BeginChangeCheck();
                // 用户原字符串不翻译，只有字段说明使用当前语言。
                string stringValue = property.stringValue;
                if (area != null)
                {
                    EditorGUILayout.LabelField(label);
                    // 根据真实文本估算行数，并保持声明的最低和最高显示行数。
                    int lines = Mathf.Clamp(stringValue.Split('\n').Length, area.minLines, area.maxLines);
                    stringValue = EditorGUILayout.TextArea(stringValue, EditorStyles.textArea, GUILayout.MinHeight(lines * EditorGUIUtility.singleLineHeight));
                }
                else if (multiline != null)
                {
                    EditorGUILayout.LabelField(label);
                    stringValue = EditorGUILayout.TextArea(stringValue, GUILayout.Height(multiline.lines * EditorGUIUtility.singleLineHeight));
                }
                else if (delayed) stringValue = EditorGUILayout.DelayedTextField(label, stringValue);
                else stringValue = EditorGUILayout.TextField(label, stringValue);
                if (EditorGUI.EndChangeCheck()) property.stringValue = stringValue;
                return;
            case SerializedPropertyType.Boolean:
                EditorGUI.BeginChangeCheck();
                // 原布尔字段的复选框值。
                bool booleanValue = EditorGUILayout.Toggle(label, property.boolValue);
                if (EditorGUI.EndChangeCheck()) property.boolValue = booleanValue;
                return;
            case SerializedPropertyType.ObjectReference:
                // 原引用声明类型保留资产筛选及拖放约束。
                Type referenceType = ResolveFieldType(property);
                if (referenceType == null || !typeof(UnityEngine.Object).IsAssignableFrom(referenceType)) referenceType = typeof(UnityEngine.Object);
                EditorGUI.BeginChangeCheck();
                // Unity 对象仅处于序列化适配边界，不构造弱类型业务载荷。
                bool allowSceneReferences = !EditorUtility.IsPersistent(property.serializedObject.targetObject);
                // 持久化资源不接受场景引用，临时对象仍沿用 Unity 原拖放语义。
                UnityEngine.Object reference = EditorGUILayout.ObjectField(label, property.objectReferenceValue, referenceType, allowSceneReferences);
                if (EditorGUI.EndChangeCheck()) property.objectReferenceValue = reference;
                return;
            case SerializedPropertyType.Vector2:
                EditorGUI.BeginChangeCheck();
                // 原二维向量控件保持分量和单位语义。
                Vector2 vector2 = EditorGUILayout.Vector2Field(label, property.vector2Value);
                if (EditorGUI.EndChangeCheck()) property.vector2Value = vector2;
                return;
            case SerializedPropertyType.Vector3:
                EditorGUI.BeginChangeCheck();
                // 原三维向量控件保持分量，不把数值拆作字符串。
                Vector3 vector3 = EditorGUILayout.Vector3Field(label, property.vector3Value);
                if (EditorGUI.EndChangeCheck()) property.vector3Value = vector3;
                return;
            case SerializedPropertyType.Vector4:
                EditorGUI.BeginChangeCheck();
                // 原四维向量分量。
                Vector4 vector4 = EditorGUILayout.Vector4Field(label, property.vector4Value);
                if (EditorGUI.EndChangeCheck()) property.vector4Value = vector4;
                return;
            case SerializedPropertyType.Quaternion:
                // 四元数保留 x/y/z/w 原始分量，不改为欧拉角存储。
                Quaternion quaternion = property.quaternionValue;
                EditorGUI.BeginChangeCheck();
                // Unity 原四分量显示语义。
                Vector4 components = EditorGUILayout.Vector4Field(label, new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                if (EditorGUI.EndChangeCheck()) property.quaternionValue = new Quaternion(components.x, components.y, components.z, components.w);
                return;
            case SerializedPropertyType.Color:
                // 原颜色使用特性保留 Alpha 与 HDR 控件模式。
                ColorUsageAttribute usage = null;
                if (field != null) usage = field.GetCustomAttribute<ColorUsageAttribute>();
                // 未声明时沿用原普通颜色控件。
                bool showAlpha = true;
                // 未声明 HDR 时不切换为 HDR 色值编辑。
                bool hdr = false;
                if (usage != null) { showAlpha = usage.showAlpha; hdr = usage.hdr; }
                EditorGUI.BeginChangeCheck();
                // 原颜色数据保留 RGBA 及 HDR 范围。
                Color color = EditorGUILayout.ColorField(label, property.colorValue, true, showAlpha, hdr);
                if (EditorGUI.EndChangeCheck()) property.colorValue = color;
                return;
            case SerializedPropertyType.AnimationCurve:
                EditorGUI.BeginChangeCheck();
                // Unity 原曲线编辑器保留关键点、切线与包络编辑能力。
                AnimationCurve curve = EditorGUILayout.CurveField(label, property.animationCurveValue);
                if (EditorGUI.EndChangeCheck()) property.animationCurveValue = curve;
                return;
            case SerializedPropertyType.Bounds:
                EditorGUI.BeginChangeCheck();
                // 原包围盒中心与尺寸，仍由同种 Unity 控件编辑。
                Bounds bounds = EditorGUILayout.BoundsField(label, property.boundsValue);
                if (EditorGUI.EndChangeCheck()) property.boundsValue = bounds;
                return;
            case SerializedPropertyType.Rect:
                EditorGUI.BeginChangeCheck();
                // 原矩形的位置与尺寸。
                Rect rect = EditorGUILayout.RectField(label, property.rectValue);
                if (EditorGUI.EndChangeCheck()) property.rectValue = rect;
                return;
            default:
                // 未涉及技能数据的外部扩展绘制器仍交给 Unity；仅替换标签，不改其实现。
                EditorGUILayout.PropertyField(property, label, false);
                return;
        }
    }

    /// <summary>绘制宿主资产的递归字段，向量、颜色等仍使用 Unity 原控件。</summary>
    /// <param name="property">本次递归绘制的原属性。</param>
    public static void DrawProperty(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
        {
            DrawDecorations(property);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, Content(property), true);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;
            if (property.isArray)
            {
                EditorGUI.BeginChangeCheck();
                // 数组尺寸只在用户实际输入时写入，与语言设置无关。
                int count = EditorGUILayout.IntField(SkillEditorLocalization.Text("Count", "数量"), property.arraySize);
                if (EditorGUI.EndChangeCheck()) { property.arraySize = Mathf.Max(0, count); EditorGUI.indentLevel--; return; }
                // 数组元素的零起始索引，不重新复制或替换领域对象。
                for (int index = 0; index < property.arraySize; index++) DrawProperty(property.GetArrayElementAtIndex(index));
            }
            else
            {
                // 容器结束标记与子字段游标仅在本次 GUI 调用中有效。
                SerializedProperty end = property.GetEndProperty();
                // 子字段游标副本，不改变上层遍历的位置。
                SerializedProperty child = property.Copy();
                if (child.NextVisible(true))
                    do { if (SerializedProperty.EqualContents(child, end)) break; DrawProperty(child); } while (child.NextVisible(false));
            }
            EditorGUI.indentLevel--;
            return;
        }
        DrawLeaf(property, Content(property), null, true);
    }

    /// <summary>翻译枚举框架名称；物理按键字符和未知扩展标识保持原样。</summary>
    /// <param name="name">枚举原声明名称，不修改该名称或值。</param>
    /// <param name="fieldName">原字段名称，用于区分 Jump 轨道与物理输入。</param>
    /// <returns>当前语言的显示名称。</returns>
    public static string EnumText(string name, string fieldName = "")
    {
        if (name.StartsWith("Lv", StringComparison.Ordinal)) return SkillEditorLocalization.Text("Level ", "等级 ") + name.Substring(2);
        if (name.StartsWith("Alpha", StringComparison.Ordinal)) return name.Substring(5);
        if (name.StartsWith("Keypad", StringComparison.Ordinal)) return SkillEditorLocalization.Text("Keypad ", "小键盘 ") + EnumText(name.Substring(6));
        if (name.StartsWith("Mouse", StringComparison.Ordinal)) return SkillEditorLocalization.Text("Mouse button ", "鼠标键 ") + name.Substring(5);
        if (name.StartsWith("Joystick", StringComparison.Ordinal)) return SkillEditorLocalization.Text("Joystick ", "手柄 ") + name.Substring(8).Replace("Button", SkillEditorLocalization.Text(" button ", " 按键 "));
        switch (name)
        {
            case "Player": return SkillEditorLocalization.Text("Player", "玩家");
            case "Enemy": return SkillEditorLocalization.Text("Enemy", "敌人");
            case "Animation": return SkillEditorLocalization.Text("Animation", "动画");
            case "Attack": return SkillEditorLocalization.Text("Attack", "攻击判定");
            case "Fx": return SkillEditorLocalization.Text("Visual effect", "特效");
            case "Sound": return SkillEditorLocalization.Text("Audio", "音效");
            case "Jump": return SkillEditorLocalization.Text("Action transition", "行为跳转");
            case "Move": return SkillEditorLocalization.Text("Displacement", "位移");
            case "Warning": return SkillEditorLocalization.Text("Telegraph", "预警");
            case "Cancel": return SkillEditorLocalization.Text("Exit window", "退出窗口");
            case "Projectile": return SkillEditorLocalization.Text("Projectile", "飞行物");
            case "HitFx": return SkillEditorLocalization.Text("Hit effect", "命中特效");
            case "SuperArmor": return SkillEditorLocalization.Text("Super armor", "霸体");
            case "AdjustMotion": return SkillEditorLocalization.Text("Facing", "朝向调整");
            case "Trail": return SkillEditorLocalization.Text("Trail", "拖尾");
            case "Camera": return SkillEditorLocalization.Text("Camera", "镜头");
            case "Flow": return SkillEditorLocalization.Text("Action flow", "行为流程");
            case "Interaction": return SkillEditorLocalization.Text("Interaction", "交互");
            case "Light": return SkillEditorLocalization.Text("Light", "轻");
            case "Medium": return SkillEditorLocalization.Text("Medium", "中");
            case "Heavy": return SkillEditorLocalization.Text("Heavy", "重");
            case "Uncancellable": return SkillEditorLocalization.Text("Uncancellable", "不可取消");
            case "Normal": return SkillEditorLocalization.Text("Normal", "普通");
            case "OneShot": return SkillEditorLocalization.Text("Once", "单次");
            case "Continuous": return SkillEditorLocalization.Text("Continuous", "持续");
            case "Damage": return SkillEditorLocalization.Text("Damage", "伤害");
            case "Signal": return SkillEditorLocalization.Text("Signal", "信号");
            case "Legacy": return SkillEditorLocalization.Text("Legacy", "旧数据兼容");
            case "VisualEffect": return SkillEditorLocalization.Text("Visual effect", "粒子特效");
            case "Audio": return SkillEditorLocalization.Text("Audio", "音效");
            case "Program": return SkillEditorLocalization.Text("Program driven", "程序驱动");
            case "Forward": return SkillEditorLocalization.Text("Forward", "自身前方");
            case "TowardTarget": return SkillEditorLocalization.Text("Toward target", "朝向目标");
            case "AwayFromTarget": return SkillEditorLocalization.Text("Away from target", "远离目标");
            case "WorldDirection": return SkillEditorLocalization.Text("World direction", "世界方向");
            case "LocalDirection": return SkillEditorLocalization.Text("Local direction", "局部方向");
            case "InputDirection": return SkillEditorLocalization.Text("Input direction", "输入方向");
            case "Self": return SkillEditorLocalization.Text("Self", "自身");
            case "Target": return SkillEditorLocalization.Text("Target", "目标");
            case "None": return SkillEditorLocalization.Text("None", "无");
            case "LockedTarget": return SkillEditorLocalization.Text("Locked target", "锁定目标");
            case "NearestEnemy": return SkillEditorLocalization.Text("Nearest enemy", "最近敌人");
            case "World": return SkillEditorLocalization.Text("World space", "世界空间");
            case "TargetLocal": return SkillEditorLocalization.Text("Target local space", "目标局部空间");
            case "Clamp": return SkillEditorLocalization.Text("Clamp", "裁剪");
            case "Stop": return SkillEditorLocalization.Text("Stop", "停止");
            case "Continue": return SkillEditorLocalization.Text("Continue", "继续");
            case "All": return SkillEditorLocalization.Text("All", "全部满足");
            case "Any":
                if (fieldName == "priorityRule") return SkillEditorLocalization.Text("Any priority", "任意优先级");
                return SkillEditorLocalization.Text("Any", "任意满足");
            case "Not": return SkillEditorLocalization.Text("Not", "取反");
            case "Custom": return SkillEditorLocalization.Text("Custom", "自定义");
            case "Evade": return SkillEditorLocalization.Text("Evade", "闪避");
            case "Block": return SkillEditorLocalization.Text("Block", "格挡");
            case "Parry": return SkillEditorLocalization.Text("Parry", "招架");
            case "AllowExit": return SkillEditorLocalization.Text("Allow exit", "允许退出");
            case "Branch": return SkillEditorLocalization.Text("Branch", "行为分支");
            case "Complete": return SkillEditorLocalization.Text("Complete", "完成行为");
            case "HigherThanCurrent": return SkillEditorLocalization.Text("Higher than current", "高于当前");
            case "AtLeast": return SkillEditorLocalization.Text("At least", "达到最低值");
            case "ActorRoot": return SkillEditorLocalization.Text("Actor root", "角色根节点");
            case "NamedSocket": return SkillEditorLocalization.Text("Named socket", "具名挂点");
            case "TargetTop": return SkillEditorLocalization.Text("Target top", "目标头顶");
            case "CasterForwardWorld": return SkillEditorLocalization.Text("Caster forward world point", "角色前方世界落点");
            case "WorldAbsolute": return SkillEditorLocalization.Text("Absolute world position", "绝对世界坐标");
            case "Divide": return SkillEditorLocalization.Text("Divide", "除号");
            case "Multiply": return SkillEditorLocalization.Text("Multiply", "乘号");
            case "Enter": return SkillEditorLocalization.Text("Enter", "回车");
            case "Backspace": return SkillEditorLocalization.Text("Backspace", "退格");
            case "Delete": return SkillEditorLocalization.Text("Delete", "删除");
            case "Tab": return SkillEditorLocalization.Text("Tab", "制表键");
            case "Clear": return SkillEditorLocalization.Text("Clear", "清除键");
            case "Return": return SkillEditorLocalization.Text("Enter", "回车");
            case "Pause": return SkillEditorLocalization.Text("Pause", "暂停键");
            case "Escape": return SkillEditorLocalization.Text("Escape", "退出键");
            case "Space": return SkillEditorLocalization.Text("Space", "空格");
            case "UpArrow": return SkillEditorLocalization.Text("Up arrow", "方向上");
            case "DownArrow": return SkillEditorLocalization.Text("Down arrow", "方向下");
            case "RightArrow": return SkillEditorLocalization.Text("Right arrow", "方向右");
            case "LeftArrow": return SkillEditorLocalization.Text("Left arrow", "方向左");
            case "Insert": return SkillEditorLocalization.Text("Insert", "插入键");
            case "Home": return SkillEditorLocalization.Text("Home", "起始键");
            case "End": return SkillEditorLocalization.Text("End", "结束键");
            case "PageUp": return SkillEditorLocalization.Text("Page up", "上翻页");
            case "PageDown": return SkillEditorLocalization.Text("Page down", "下翻页");
            case "LeftShift": return SkillEditorLocalization.Text("Left Shift", "左 Shift");
            case "RightShift": return SkillEditorLocalization.Text("Right Shift", "右 Shift");
            case "LeftControl": return SkillEditorLocalization.Text("Left Ctrl", "左 Ctrl");
            case "RightControl": return SkillEditorLocalization.Text("Right Ctrl", "右 Ctrl");
            case "LeftAlt": return SkillEditorLocalization.Text("Left Alt", "左 Alt");
            case "RightAlt": return SkillEditorLocalization.Text("Right Alt", "右 Alt");
            case "CapsLock": return SkillEditorLocalization.Text("Caps Lock", "大写锁定");
            case "Numlock": return SkillEditorLocalization.Text("Num Lock", "数字锁定");
            case "ScrollLock": return SkillEditorLocalization.Text("Scroll Lock", "滚动锁定");
            case "LeftCommand": return SkillEditorLocalization.Text("Left Command", "左 Command");
            case "RightCommand": return SkillEditorLocalization.Text("Right Command", "右 Command");
            case "LeftWindows": return SkillEditorLocalization.Text("Left Windows", "左 Windows");
            case "RightWindows": return SkillEditorLocalization.Text("Right Windows", "右 Windows");
            case "AltGr": return SkillEditorLocalization.Text("AltGr", "AltGr");
            case "Help": return SkillEditorLocalization.Text("Help", "帮助键");
            case "Print": return SkillEditorLocalization.Text("Print Screen", "打印屏幕");
            case "SysReq": return SkillEditorLocalization.Text("System request", "系统请求");
            case "Break": return SkillEditorLocalization.Text("Break", "中断键");
            case "Menu": return SkillEditorLocalization.Text("Menu", "菜单键");
            case "Exclaim": return SkillEditorLocalization.Text("Exclamation mark", "感叹号");
            case "DoubleQuote": return SkillEditorLocalization.Text("Double quote", "双引号");
            case "Hash": return SkillEditorLocalization.Text("Hash", "井号");
            case "Dollar": return SkillEditorLocalization.Text("Dollar", "美元符号");
            case "Percent": return SkillEditorLocalization.Text("Percent", "百分号");
            case "Ampersand": return SkillEditorLocalization.Text("Ampersand", "与符号");
            case "Quote": return SkillEditorLocalization.Text("Single quote", "单引号");
            case "LeftParen": return SkillEditorLocalization.Text("Left parenthesis", "左圆括号");
            case "RightParen": return SkillEditorLocalization.Text("Right parenthesis", "右圆括号");
            case "Asterisk": return SkillEditorLocalization.Text("Asterisk", "星号");
            case "Plus": return SkillEditorLocalization.Text("Plus", "加号");
            case "Comma": return SkillEditorLocalization.Text("Comma", "逗号");
            case "Minus": return SkillEditorLocalization.Text("Minus", "减号");
            case "Period": return SkillEditorLocalization.Text("Period", "句点");
            case "Slash": return SkillEditorLocalization.Text("Slash", "斜杠");
            case "Colon": return SkillEditorLocalization.Text("Colon", "冒号");
            case "Semicolon": return SkillEditorLocalization.Text("Semicolon", "分号");
            case "Less": return SkillEditorLocalization.Text("Less than", "小于号");
            case "Equals": return SkillEditorLocalization.Text("Equals", "等号");
            case "Greater": return SkillEditorLocalization.Text("Greater than", "大于号");
            case "Question": return SkillEditorLocalization.Text("Question mark", "问号");
            case "At": return SkillEditorLocalization.Text("At sign", "艾特符号");
            case "LeftBracket": return SkillEditorLocalization.Text("Left bracket", "左方括号");
            case "Backslash": return SkillEditorLocalization.Text("Backslash", "反斜杠");
            case "RightBracket": return SkillEditorLocalization.Text("Right bracket", "右方括号");
            case "Caret": return SkillEditorLocalization.Text("Caret", "脱字号");
            case "Underscore": return SkillEditorLocalization.Text("Underscore", "下划线");
            case "BackQuote": return SkillEditorLocalization.Text("Backquote", "反引号");
            default: return name;
        }
    }
}
