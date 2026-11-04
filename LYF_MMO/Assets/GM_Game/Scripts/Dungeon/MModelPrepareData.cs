using System.Collections.Generic;
using UnityEngine;

/// <summary>沿用 RO 模型准备数据，保存模型空间包围盒、蒙皮列表和阴影挂点。</summary>
public class MModelPrepareData : MonoBehaviour
{
    /// <summary>公共怪物动画图中的一个占位剪辑和本模型用于替换它的真实动作。</summary>
    [System.Serializable]
    private struct ModelMotion
    {
        /// <summary>MMonsterAnimator 中要替换的原始剪辑，决定生效的状态。</summary>
        public AnimationClip Slot;
        /// <summary>本实体配置的动作，伤员、尸体等剧情实体各自保留出生姿势。</summary>
        public AnimationClip Clip;
    }

    /// <summary>随预制体加载的动作绑定，同一公共动画图上的每个实体独立生效。</summary>
    [SerializeField] private ModelMotion[] motions;

    /// <summary>一次性将该预制体的真实动作装入实体当前的公共动画图。</summary>
    /// <param name="animator">已经绑定本模型，由实体拥有并在回收时释放覆盖资源的动画器。</param>
    public void ApplyMotions(MAnimator animator)
    {
        // Unity 一次提交整组替换，避免逐个状态重建动画绑定。
        List<KeyValuePair<AnimationClip, AnimationClip>> clips =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(motions.Length);
        // 当前下标对应公共状态和该状态使用的模型动作。
        for (int index = 0; index < motions.Length; index++)
        {
            clips.Add(new KeyValuePair<AnimationClip, AnimationClip>(motions[index].Slot, motions[index].Clip));
        }
        animator.GetOrCreateOverrideController().ApplyOverrides(clips);
    }

    /// <summary>本模型的蒙皮渲染器，换装 NPC 可包含多个部件。</summary>
    public SkinnedMeshRenderer[] SkinnedMeshRenderers;
    /// <summary>模型未缩放时的包围盒尺寸，单位米。</summary>
    public Vector3 ProjectSize;
    /// <summary>模型根节点局部空间包围盒中心，单位米。</summary>
    public Vector3 ProjectCenter;
    /// <summary>包围盒最低、最高的局部 Y 坐标。</summary>
    public Vector2 BoundRange;
    /// <summary>RO 模型的阴影挂点，缺省允许为空。</summary>
    public Transform ShadowPoint;
    /// <summary>原模型是否忽略投影，本组件不创建额外阴影。</summary>
    public bool IgnoreShadow;
}
