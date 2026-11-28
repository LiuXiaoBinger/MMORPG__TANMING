using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>一次锁敌逐月的空间轨迹。剑光先落到敌人，角色从施法处或后撤点追到敌人近侧。</summary>
internal sealed class MjsMoonChasePath
{
    /// <summary>位移段前三分之一用于近身退步或远程亮剑，后续时间供人物追身。</summary>
    private const float TurnProgress = 0.38f;
    /// <summary>贴身时从施法点向后拉开的世界米数。</summary>
    private const float RetreatDistance = 5.5f;
    /// <summary>本次锁定的敌人，其当前水平位置决定折返终点。</summary>
    private readonly MEntity target;
    /// <summary>起手时的实体身份，防止对象回池后追向另一个出生单位。</summary>
    private readonly ulong targetUid;
    /// <summary>角色接敌时保留的中心间距，包含双方模型半径，单位米。</summary>
    private readonly float strikeGap;
    /// <summary>开始时与敌人的间距已适合近战时启用退步。</summary>
    private readonly bool retreat;
    /// <summary>上一次交给运动组件的段内进度，取值零至一。</summary>
    private float previousProgress;
    /// <summary>敌人最后一次可见位置，消失后可在原落点收招。</summary>
    private Vector3 lastTargetPoint;
    /// <summary>敌人与施法起点有明显高度差时接管升降，保持到双斩收招。</summary>
    internal bool FollowsHeight { get; private set; }
    /// <summary>施法起点，残影在此停留，单位为世界米。</summary>
    internal Vector3 Origin { get; }
    /// <summary>施法瞬间面向敌人的水平单位方向，退步及近侧站位共用。</summary>
    internal Vector3 Forward { get; }
    /// <summary>起手剑光命中的世界坐标，后续敌人移动只影响人物追身。</summary>
    internal Vector3 LightTarget { get; }

    /// <summary>按出招瞬间双方站位选择远程追击或近身折返。</summary>
    /// <param name="caster">本次使用逐月的角色。</param>
    /// <param name="enemy">输入或服务器广播确定的敌人。</param>
    /// <param name="facing">双方重合时用于退步的施法者水平朝向。</param>
    internal MjsMoonChasePath(MEntity caster, MEntity enemy, Vector3 facing)
    {
        target = enemy;
        targetUid = enemy.UID;
        Origin = caster.Position;
        LightTarget = enemy.Position;
        lastTargetPoint = LightTarget;
        // 水平间距决定近远分支，敌人高度只影响剑光的倾斜。
        Vector3 offset = LightTarget - Origin;
        offset.y = 0f;
        Forward = facing;
        if (offset.sqrMagnitude > 0.000001f) Forward = offset.normalized;
        strikeGap = Mathf.Max(1.35f, caster.Radius + enemy.Radius);
        retreat = Vector3.Distance(Origin, LightTarget) <= strikeGap + 1.2f;
        FollowsHeight = Mathf.Abs(LightTarget.y - Origin.y) > 0.05f;
    }

    /// <summary>模型晚就绪时跳过已经过去的相位，从实际站位继续。</summary>
    /// <param name="progress">本段位移归一化进度，零至一。</param>
    internal void SetProgress(float progress)
    {
        previousProgress = Mathf.Clamp01(progress);
    }

    /// <summary>从角色当前站位推进这一帧，最终位移交给角色控制器处理碰撞。</summary>
    /// <param name="progress">当前位移段归一化进度，末帧为一。</param>
    /// <param name="position">角色经上次碰撞处理后的世界坐标。</param>
    /// <returns>本帧向目标近侧请求的三维位移量，单位米。</returns>
    internal Vector3 Advance(float progress, Vector3 position)
    {
        // 敌人起飞、降落或对象回池时，以本次锁定身份的最后一个有效位置为准。
        if (target.UID == targetUid && target.LifecycleState == EntityLifecycleState.Active)
        {
            lastTargetPoint = target.Position;
            if (Mathf.Abs(lastTargetPoint.y - Origin.y) > 0.05f) FollowsHeight = true;
        }
        // 水平追身结束后只跟随敌人高度，双斩在敌人身边出手；离开技能态后恢复重力。
        if (previousProgress >= 1f)
        {
            if (FollowsHeight) return Vector3.up * (lastTargetPoint.y - position.y);
            return Vector3.zero;
        }
        // 同一帧跨过折返点时，先补本帧尚余的退步，再计算追身。
        Vector3 next = position;
        if (retreat && previousProgress < TurnProgress)
        {
            next = Vector3.Lerp(next, Origin - Forward * RetreatDistance,
                PhaseStep(previousProgress, progress, 0f, TurnProgress));
        }
        if (progress > TurnProgress)
        {
            next = Vector3.Lerp(next, lastTargetPoint - Forward * strikeGap,
                PhaseStep(previousProgress, progress, TurnProgress, 1f));
        }
        previousProgress = progress;
        if (!FollowsHeight) next.y = position.y;
        return next - position;
    }

    /// <summary>把两帧的缓入缓出进度换算成本帧应走的剩余路程比例。</summary>
    /// <param name="previous">上一帧的整段进度。</param>
    /// <param name="current">本帧的整段进度。</param>
    /// <param name="from">该运动相位的段内起点。</param>
    /// <param name="to">该运动相位的段内终点。</param>
    /// <returns>从当前实际站位向该相位终点移动的比例。</returns>
    private static float PhaseStep(float previous, float current, float from, float to)
    {
        // 以已走过的缓动比例为基线，低帧率跨相位时也能抵达正确的近侧落点。
        float before = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, previous));
        // 当前缓动曲线累计比例，除去已交给角色控制器的部分。
        float after = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, current));
        return Mathf.InverseLerp(before, 1f, after);
    }
}
