using System.Collections.Generic;
using UnityEngine;

/// <summary>把现有技能编辑器碰撞轨道导出为客户端命中表现数据，不复制包内战斗框架。</summary>
public static class SkillEditorCollisionTools
{
    /// <summary>沿用原球/盒尺寸、帧窗口、重复次数和逐帧骨骼偏移。</summary>
    /// <param name="binding">当前已校验的作者配置及宿主定位规则。</param>
    public static List<SkillRuntimeCollision> Read(SkillEditorBinding binding)
    {
        // 包内几何来自作者配置；目标落点规则由本游戏的绑定资产保存。
        SkillConfigSO config = binding.Config;
        // 仅导出伤害形状；Signal 轨道仍属于原编辑器接触信号，不冒充怪物受击。
        List<SkillRuntimeCollision> result = new List<SkillRuntimeCollision>();
        for (int index = 0; index < config.attackList.Count; index++)
        {
            // 由包类型推断源记录，避免包 Global 与游戏 Global 的同名冲突。
            var source = config.attackList[index];
            if ((int)source.responseMode != 0) continue;
            // 正式数据保持原几何语义：尺寸用世界米，中心为角色本地坐标。
            SkillRuntimeCollision collision = new SkillRuntimeCollision();
            collision.StartFrame = source.keyNumber;
            collision.EndFrame = Mathf.Max(source.keyNumber, source.endKeyNumber);
            collision.FrameRate = config.timelineFrameRate;
            collision.Shape = source.shapeType;
            collision.AtTarget = binding.CollisionAtTarget;
            collision.ForwardDistance = binding.CollisionForwardDistance;
            collision.Radius = source.parameter1;
            collision.BoxSize = new Vector3(source.parameter1, source.boxHeight, source.parameter2);
            collision.Rotation = source.collisionRotation;
            if ((int)source.damageMode != 0) collision.RepeatFrames = Mathf.Max(1, source.tickInterval);
            collision.Offsets = new Vector3[collision.EndFrame - collision.StartFrame + 1];
            for (int frame = collision.StartFrame; frame <= collision.EndFrame; frame++)
                collision.Offsets[frame - collision.StartFrame] = source.GetOffsetAtFrame(frame);
            result.Add(collision);
        }
        return result;
    }
}
