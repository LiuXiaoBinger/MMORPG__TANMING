using UnityEngine;

/// <summary>解析粒子挂点，已确认的旧 JX 右手路径通过模型 Avatar 映射到当前实际右手骨骼。</summary>
public static class SkillFxSocketResolver
{
    /// <summary>旧 Jx_Skill02 使用的具名右手路径，只为这一已核实语义提供跨模型适配。</summary>
    private const string LegacyRightHandPath = "root/Hips/Spine/Chest/UpperChest/shoulder_R/Arm_R/forearm_R/hand_R";

    /// <summary>按完整路径优先定位，已知旧右手路径缺失时使用唯一 Avatar RightHand 映射。</summary>
    /// <param name="modelRoot">角色模型实际根节点，预制体和运行实例均可读取。</param>
    /// <param name="socketPath">完整特效挂点路径，不对未知路径做名称猜测。</param>
    /// <returns>确切节点；未知、缺失或映射歧义时返回 null，不回退角色根。</returns>
    public static Transform Resolve(Transform modelRoot, string socketPath)
    {
        if (modelRoot == null || string.IsNullOrEmpty(socketPath)) return null;
        // 完整路径存在时保持原模型层级语义，不启动任何跨模型映射。
        Transform direct = modelRoot.Find(socketPath);
        if (direct != null) return direct;
        if (socketPath != LegacyRightHandPath) return null;
        // Avatar 描述是已导入模型的语义映射，不需要 Animator 正在播放。
        Animator[] animators = modelRoot.GetComponentsInChildren<Animator>(true);
        // 原模型 Transform 快照保留实际节点引用，只读扫描不修改骨骼层级。
        Transform[] nodes = modelRoot.GetComponentsInChildren<Transform>(true);
        // 所有有效动画器必须得出同一确切节点，否则拒绝任意选择首项。
        Transform result = null;
        for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
        {
            // 当前动画器的角色 Avatar，非人形或未绑定 Avatar 不提供右手语义。
            Avatar avatar = animators[animatorIndex].avatar;
            if (avatar == null || !avatar.isHuman) continue;
            // Humanoid 字段含实际骨骼名，MJS 的 RightHand 已核实为 hand_r。
            HumanBone[] bones = avatar.humanDescription.human;
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                if (bones[boneIndex].humanName != "RightHand") continue;
                // 一个 Avatar 的映射骨骼必须在当前模型中唯一，不能只取首个同名节点。
                Transform candidate = null;
                for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
                {
                    if (nodes[nodeIndex].name != bones[boneIndex].boneName) continue;
                    if (candidate != null && candidate != nodes[nodeIndex]) return null;
                    candidate = nodes[nodeIndex];
                }
                if (candidate == null) return null;
                if (result != null && result != candidate) return null;
                result = candidate;
            }
        }
        return result;
    }
}
