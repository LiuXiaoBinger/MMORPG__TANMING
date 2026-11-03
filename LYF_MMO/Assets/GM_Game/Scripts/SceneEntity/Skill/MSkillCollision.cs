using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>以正式技能时钟查询编辑器碰撞体，仅把实际重叠的怪物送入既有受击状态。</summary>
public class MSkillCollision
{
    /// <summary>每条碰撞轨道已命中目标的最后帧，防止一个模型多个 Collider 重复受击。</summary>
    private readonly List<Dictionary<ulong, int>> hits = new List<Dictionary<ulong, int>>();
    /// <summary>上次查询的技能秒数，用于处理低帧率跨越单帧窗口的情况。</summary>
    private float lastTime = -1f;

    /// <summary>施法回池或取消时解除所有目标引用与已命中记录。</summary>
    public void Reset()
    {
        hits.Clear();
        lastTime = -1f;
    }

    /// <summary>沿技能时间轴的有效窗口做球体/盒体查询，不改血量或发送伤害协议。</summary>
    /// <param name="firer">本次技能的实际施法实体。</param>
    /// <param name="presentation">已经加载的该次表现配置。</param>
    /// <param name="time">与动画和程序位移共用的技能时钟，单位秒。</param>
    /// <param name="targetUid">本次施法锁定的目标身份，零表示未选中目标。</param>
    public void Update(MEntity firer, SkillRuntimePresentation presentation, float time, ulong targetUid)
    {
        if (!firer.Model.IsLoaded) return;
        // 使用当前真实模型变换，与编辑器 ActionCollisionGeometry 的坐标语义一致。
        Transform actor = firer.Model.GameObject.Transform;
        for (int index = 0; index < presentation.Collisions.Count; index++)
        {
            if (hits.Count <= index) hits.Add(new Dictionary<ulong, int>());
            // 当前轨道的整数帧范围，结束帧包含在窗口中。
            SkillRuntimeCollision collision = presentation.Collisions[index];
            int frame = Mathf.FloorToInt(time * collision.FrameRate);
            int previousFrame = Mathf.FloorToInt(lastTime * collision.FrameRate);
            if (frame < collision.StartFrame || previousFrame > collision.EndFrame) continue;
            if (frame > collision.EndFrame && previousFrame >= collision.EndFrame) continue;
            // 卡帧跨过窗口时只补查最后一个有效采样，不在每个空帧重复制造命中。
            int sample = Mathf.Clamp(frame, collision.StartFrame, collision.EndFrame);
            Vector3 center = actor.TransformPoint(collision.Offsets[sample - collision.StartFrame]);
            if (collision.AtTarget)
            {
                center += actor.forward * collision.ForwardDistance;
                // 与目标落点特效共用本次 TargetUid；不按距离随意换一个目标。
                ROObject selected;
                if (Global.Instance.SceneWorld.TryGetEntity(targetUid, out selected))
                {
                    MEntity targetEntity = selected as MEntity;
                    if (targetEntity != null) center = targetEntity.Position + collision.Offsets[sample - collision.StartFrame];
                }
            }
            Collider[] overlaps;
            if (collision.Shape == 0)
                overlaps = Physics.OverlapSphere(center, collision.Radius, Physics.AllLayers, QueryTriggerInteraction.Collide);
            else
                overlaps = Physics.OverlapBox(center, collision.BoxSize * 0.5f, actor.rotation * Quaternion.Euler(collision.Rotation), Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int targetIndex = 0; targetIndex < overlaps.Length; targetIndex++)
            {
                // 正式怪物挂接组件提供真实实体身份；NPC 和地图碰撞体自然排除。
                MSceneMonsterHoster host = overlaps[targetIndex].GetComponentInParent<MSceneMonsterHoster>();
                if (host == null) continue;
                MEntity target = host.GetROObject() as MEntity;
                if (target == null || target.LifecycleState != EntityLifecycleState.Active || target == firer) continue;
                // 同一次窗口每个目标只触发一次；重复轨道按作者帧间隔放行。
                int lastHit;
                if (hits[index].TryGetValue(target.UID, out lastHit))
                {
                    if (collision.RepeatFrames == 0 || sample - lastHit < collision.RepeatFrames) continue;
                }
                hits[index][target.UID] = sample;
                // 原模型未提供 BeHit 时保留现有动作，不用其他怪物动作冒充。
                Animator animator = target.Model.Ator.UnityAnimator;
                if (animator.HasState(0, Animator.StringToHash("Base Layer." + MAnimator.STATE_BEHIT)))
                    target.DispatchEvent(MEventType.DamageReceived, null);
            }
        }
        lastTime = time;
    }
}
