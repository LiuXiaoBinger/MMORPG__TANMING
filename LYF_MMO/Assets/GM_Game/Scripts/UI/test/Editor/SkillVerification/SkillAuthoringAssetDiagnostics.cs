using UnityEditor;
using UnityEngine;

/// <summary>专用只读资产诊断，不生成角色、不挂场景组件、不进入正式业务运行。</summary>
public static class SkillAuthoringAssetDiagnostics
{
    /// <summary>读取实际预制体层级及 Humanoid 右手映射，帮助核对旧 JX 挂点迁移到 MJS。</summary>
    public static void DumpModelSockets()
    {
        Dump("Assets/GM_Game/Prefabs/Role/Role_MJS.prefab");
        Dump("Assets/GM_Game/Prefabs/Role/Role_JX.prefab");
    }

    /// <summary>只读列出指定角色的骨骼路径与 Avatar 语义映射。</summary>
    /// <param name="path">待核对的正式角色预制体路径。</param>
    private static void Dump(string path)
    {
        // 加载的是预制体资产本身，不在 Scene 里实例化角色。
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) throw new System.InvalidOperationException("模型不存在：" + path);
        // 实际 Transform 层级用完整相对路径输出，避免名称猜测。
        Transform[] nodes = model.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < nodes.Length; index++)
        {
            Debug.Log("MODEL_SOCKET " + path + " " + AnimationUtility.CalculateTransformPath(nodes[index], model.transform));
        }
        // 预制体 Avatar 的人形映射在编辑状态可只读获取，不要求 Animator 开始播放。
        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int index = 0; index < animators.Length; index++)
        {
            // 每个动画器的 Avatar 独立报告，禁止默选多个动画器的首项。
            Animator animator = animators[index];
            if (animator.avatar == null || !animator.avatar.isHuman) continue;
            HumanBone[] bones = animator.avatar.humanDescription.human;
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                Debug.Log("AVATAR_BONE " + path + " " + bones[boneIndex].humanName + "=" + bones[boneIndex].boneName);
            }
        }
    }
}
