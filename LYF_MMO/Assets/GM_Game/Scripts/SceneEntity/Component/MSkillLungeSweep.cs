using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主动前移类技能的命中自适应扫掠：按施法者正前方的活体敌人截断本帧前向增量。
/// 只截断曲线本帧给出的增量，从不把角色拉向其他方向；贴身时增量归零，
/// 动画与竖直跳高仍按原时间轴播放，权威位置仍由既有移动上报链路提交。
/// 本类不保存任何跨帧状态，每帧都按当前实体位置实时查询，敌人移动立即生效。
/// </summary>
public static class MSkillLungeSweep
{
    /// <summary>双方半径之和低于该值时按该值作为接触距离，避免贴脸穿模，单位米。</summary>
    private const float MinimumContactDistance = 0.9f;

    /// <summary>前向走廊的横向半宽下限，保证正前方稍微偏一点的敌人也能被拦住，单位米。</summary>
    private const float MinimumCorridorHalfWidth = 0.9f;

    /// <summary>扫掠时复用的候选容器，只在主线程顺序访问，不跨帧持有实体引用。</summary>
    private static readonly List<MEntity> Candidates = new List<MEntity>();

    /// <summary>判断本次施法是否属于需要命中自适应的魔剑士前移技能。</summary>
    /// <param name="skillId">施法实例的技能主表编号。</param>
    /// <param name="presentationId">本次施法锁定的表现行主键，用于区分同一技能的连续普攻段。</param>
    /// <returns>需要按前方敌人截断前移距离时返回 true。</returns>
    public static bool ShouldAdapt(int skillId, int presentationId)
    {
        // 键位 2 的斩天剑自带长距离前冲，贴身时不应继续前冲。
        if (skillId == 10060) return true;
        // 键位 3 的刀刃风暴为新增前移跳斩。
        if (skillId == 10110) return true;
        // 普攻三段共用技能编号，只有第三段带前冲表现行 3001003。
        if (skillId == 10010 && presentationId == 3001003) return true;
        return false;
    }

    /// <summary>按前方最近可攻击敌人截断本帧前向增量，空地与身后目标保持曲线原值。</summary>
    /// <param name="caster">本次施法实体，位置取当前已提交的权威位置。</param>
    /// <param name="direction">施法时锁定的水平世界方向，长度不限，内部做水平归一化。</param>
    /// <param name="desiredStep">曲线本帧新增的前向距离，单位米，可正可负。</param>
    /// <returns>不超过期望值的前向距离，单位米；贴身或已到达接触距离时为零。</returns>
    public static float ClampForwardStep(MEntity caster, Vector3 direction, float desiredStep)
    {
        // 本帧没有前向推进需求时不必查询世界，回拉量也不做截断，保持原曲线。
        if (desiredStep <= 0f) return desiredStep;
        if (caster == null) return desiredStep;
        // 水平方向退化时无法判断前后，保持曲线原值。
        Vector3 forward = new Vector3(direction.x, 0f, direction.z);
        if (forward.sqrMagnitude <= 0.0001f) return desiredStep;
        forward.Normalize();
        // 取所有前方候选里"实际可走距离"最小的那个，而不是单纯轴向最近的；斜前方远一点的目标可能先接触。
        float allowed = FindNearestContactAllowance(caster, forward, desiredStep);
        if (allowed >= desiredStep) return desiredStep;
        if (allowed < 0f) return 0f;
        return allowed;
    }

    /// <summary>按圆形真实接触点截断本帧增量：轴向距离减 sqrt(接触半径²−横偏²)。</summary>
    /// <param name="casterPosition">施法者当前位置，取扣除高度后的世界坐标。</param>
    /// <param name="casterRadius">施法者战斗半径，单位米。</param>
    /// <param name="forward">水平单位前向。</param>
    /// <param name="desiredStep">本帧期望前向距离，单位米，必须为正。</param>
    /// <param name="targetPosition">前方目标的当前位置。</param>
    /// <param name="targetRadius">前方目标的战斗半径，单位米。</param>
    /// <returns>本帧可实际推进的距离，单位米，取值范围零到期望值。</returns>
    public static float ClampAgainstTarget(Vector3 casterPosition, float casterRadius, Vector3 forward,
        float desiredStep, Vector3 targetPosition, float targetRadius)
    {
        if (desiredStep <= 0f) return desiredStep;
        // 只取水平面的位置差，台阶高低不影响前后判定。
        Vector3 delta = targetPosition - casterPosition;
        Vector3 flat = new Vector3(delta.x, 0f, delta.z);
        float axial = Vector3.Dot(flat, forward);
        // 施法方向固定的右向量，用于取目标偏离前向轴线的横向距离。
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float side = Mathf.Abs(Vector3.Dot(flat, right));
        // 圆体接触距离取双方半径之和，过小时按最小值兜底，避免贴脸穿模。
        float contact = casterRadius + targetRadius;
        if (contact < MinimumContactDistance) contact = MinimumContactDistance;
        // 横偏不小于接触半径时两圆在前向上不可能相交，本帧不受该目标限制。
        if (side >= contact) return desiredStep;
        // 两圆相切时前向圆心距为 sqrt(contact²−side²)，比只减半径更早拦住斜前方的目标。
        float allowed = axial - Mathf.Sqrt(contact * contact - side * side);
        // 已经贴身或更近时本帧不再前冲，动画与竖直跳高照常播完。
        if (allowed < 0f) allowed = 0f;
        if (allowed >= desiredStep) return desiredStep;
        return allowed;
    }

    /// <summary>取正前方走廊内所有活体敌人里"本帧允许前进距离"最小者，即最早接触的那个。</summary>
    /// <param name="caster">本次施法实体。</param>
    /// <param name="forward">水平单位前向。</param>
    /// <param name="desiredStep">本帧期望前向距离，单位米，必须为正。</param>
    /// <returns>本帧可前进的最大距离；前方没有构成接触的敌人时返回 float.MaxValue。</returns>
    private static float FindNearestContactAllowance(MEntity caster, Vector3 forward, float desiredStep)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null) return float.MaxValue;
        Candidates.Clear();
        Global.Instance.SceneWorld.CopyEntities(Candidates);
        // 施法方向固定的右向量，用于计算目标偏离走廊中线的横向距离。
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float bestAllowed = float.MaxValue;
        for (int index = 0; index < Candidates.Count; index++)
        {
            MEntity candidate = Candidates[index];
            if (ReferenceEquals(candidate, caster)) continue;
            // 只把参战怪物当作阻挡目标，剧情角色、不可战斗者与友方不参与。
            if (!MSkillTargetDefine.IsEnemy(candidate)) continue;
            // 已经死亡或离场的怪物不挡路，符合继续推进的预期。
            if (!MSkillTargetDefine.IsValidTarget(candidate)) continue;
            Vector3 delta = candidate.Position - caster.Position;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            // 身后、侧面或与本体重合的方向不构成阻挡，也就不会产生回拉。
            float axial = Vector3.Dot(flat, forward);
            if (axial <= 0f) continue;
            // 横向偏离超过接触半径即视为从侧面经过，不阻挡前移。
            float lateral = Mathf.Abs(Vector3.Dot(flat, right));
            float halfWidth = caster.Radius + candidate.Radius;
            if (halfWidth < MinimumCorridorHalfWidth) halfWidth = MinimumCorridorHalfWidth;
            if (lateral > halfWidth) continue;
            // 该候选实际允许的前进量；斜前方横偏大时比同轴向目标更早接触，故按允许量取最小。
            float allowed = ClampAgainstTarget(caster.Position, caster.Radius, forward, desiredStep,
                candidate.Position, candidate.Radius);
            if (allowed < bestAllowed)
            {
                bestAllowed = allowed;
            }
        }
        Candidates.Clear();
        return bestAllowed;
    }
}
