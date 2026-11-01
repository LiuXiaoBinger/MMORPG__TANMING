using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>模型展示界面：切换怪物和 NPC、播放动作，并按模型大小调整轨道相机。</summary>
public sealed class ROCreatureModelShowcase : MonoBehaviour
{
    /// <summary>预制体保存的模型与动作清单，按左侧按钮顺序排列。</summary>
    public ROCreatureModelDefinition[] models;

    /// <summary>当前展示模型的父节点，模型切换时只替换它拥有的实例。</summary>
    public Transform modelRoot;

    /// <summary>轨道相机的注视点，切换模型时移动到模型包围盒中心。</summary>
    public Transform cameraTarget;

    /// <summary>沿用演示项目的轨道相机，展示场景配置为鼠标右键旋转。</summary>
    public RODemoOrbitCamera orbitCamera;

    /// <summary>模型包围球半径到相机距离的无单位倍率；魔神塔场景调大以容纳腾空动作。</summary>
    public float framingDistanceMultiplier = 3.6f;

    /// <summary>左侧模型按钮，控件和引用均保存在展示预制体中。</summary>
    public Button[] modelButtons;

    /// <summary>下方动作按钮，数量覆盖当前模型清单的最大动作数。</summary>
    public Button[] animationButtons;

    /// <summary>动作按钮的文本引用，与 animationButtons 一一对应。</summary>
    public Text[] animationButtonLabels;

    /// <summary>当前模型的名称文本。</summary>
    public Text modelNameLabel;

    /// <summary>当前播放动作的名称文本。</summary>
    public Text animationNameLabel;

    /// <summary>编辑态保存初始展示模型，运行时随模型切换更新为当前实例。</summary>
    public GameObject displayedModel;

    /// <summary>当前模型在 models 中的索引，仅在展示界面的运行生命周期内有效。</summary>
    private int currentModelIndex;

    /// <summary>当前动作的播放图，切换动作、模型或销毁界面时释放。</summary>
    private PlayableGraph animationGraph;

    /// <summary>当前动作的特效实例，切换动作或模型时统一清理。</summary>
    private readonly List<GameObject> activeEffects = new List<GameObject>();

    /// <summary>当前动作的配置，尚未开始播放时为空。</summary>
    private ROCreatureAnimationDefinition currentAnimation;

    /// <summary>当前动作开始时的游戏时间，单位为秒。</summary>
    private float animationStartedAt;

    /// <summary>下一个待触发特效在当前动作有序事件数组中的索引。</summary>
    private int nextEffectIndex;

    /// <summary>技能目标的展示点，位于当前模型正前方，用于播放目标侧与受击特效。</summary>
    private Transform effectTarget;

    /// <summary>一次性注册预制体按钮，并选择列表中的第一只模型。</summary>
    private void Awake()
    {
        // 空目标只提供技能表现落点，不创建战斗实体或造成伤害。
        GameObject targetObject = new GameObject("ROSkillEffectPreviewTarget");
        effectTarget = targetObject.transform;
        effectTarget.SetParent(modelRoot, false);
        // i 为模型按钮索引；按钮和模型配置由生成时的清单对应。
        for (int i = 0; i < modelButtons.Length; i++)
        {
            // 单个按钮捕获自己的模型索引，避免所有按钮引用循环结束后的值。
            int modelIndex = i;
            modelButtons[i].onClick.AddListener(() => SelectModel(modelIndex));
        }
        // i 为预制体动作按钮索引，多余按钮在切换模型时隐藏。
        for (int i = 0; i < animationButtons.Length; i++)
        {
            // 单个动作按钮固定使用其当前列表位置。
            int animationIndex = i;
            animationButtons[i].onClick.AddListener(() => PlayAnimation(animationIndex));
        }
        SelectModel(0);
    }

    /// <summary>实例化选中的模型、更新动作按钮，并播放该模型的默认动作。</summary>
    /// <param name="index">models 中由模型按钮传入的有效索引。</param>
    public void SelectModel(int index)
    {
        ReleaseAnimation();
        if (displayedModel != null)
        {
            Destroy(displayedModel);
        }
        currentModelIndex = index;
        // 当前模型配置，实例、动作列表和标题都取自同一条记录。
        ROCreatureModelDefinition definition = models[index];
        displayedModel = Instantiate(definition.prefab, modelRoot);
        displayedModel.name = definition.prefab.name;
        displayedModel.transform.localPosition = Vector3.zero;
        // 原始模型的 Animator 直接接收片段播放图，不需要为每个动作另建控制器。
        Animator animator = displayedModel.GetComponentInChildren<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        modelNameLabel.text = definition.displayName;
        // i 为动作按钮槽位，当前模型动作数量之外的槽位不显示。
        for (int i = 0; i < animationButtons.Length; i++)
        {
            // 当前槽位是否对应一个可播放动作。
            bool visible = i < definition.animations.Length;
            animationButtons[i].gameObject.SetActive(visible);
            if (visible)
            {
                animationButtonLabels[i].text = definition.animations[i].displayName;
            }
        }
        FrameModel();
        PlayAnimation(definition.defaultAnimation);
    }

    /// <summary>从头播放当前模型的指定动作，再次点击同一动作也会重新播放。</summary>
    /// <param name="index">当前模型动作列表中的有效索引。</param>
    public void PlayAnimation(int index)
    {
        ReleaseAnimation();
        // 本次播放的原始片段与按钮名称，均来自当前模型的动作配置。
        ROCreatureAnimationDefinition definition = models[currentModelIndex].animations[index];
        animationGraph = PlayableGraph.Create("ROCreatureShowcaseAnimation");
        // 动画输出绑定到本次展示模型的原始 Animator。
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Creature", displayedModel.GetComponentInChildren<Animator>());
        // 仅播放本次选中的片段，片段原有的循环设置保持有效。
        AnimationClipPlayable playable = AnimationClipPlayable.Create(animationGraph, definition.clip);
        output.SetSourcePlayable(playable);
        animationGraph.Play();
        currentAnimation = definition;
        animationStartedAt = Time.time;
        nextEffectIndex = 0;
        TriggerEffects();
        animationNameLabel.text = "当前动作：" + definition.displayName;
    }

    /// <summary>以同一动作起点驱动特效时间轴，不改变原动画片段的播放速度。</summary>
    private void Update()
    {
        TriggerEffects();
    }

    /// <summary>生成本帧已经到达触发时间的特效，数组顺序在预制体生成时按原配置保存。</summary>
    private void TriggerEffects()
    {
        if (currentAnimation == null)
        {
            return;
        }
        // 本次动作已经播放的秒数，与配置 At 时间使用相同单位。
        float elapsed = Time.time - animationStartedAt;
        while (nextEffectIndex < currentAnimation.effects.Length)
        {
            // 当前最早尚未触发的原技能特效事件。
            ROCreatureEffectDefinition effect = currentAnimation.effects[nextEffectIndex];
            if (effect.startTime > elapsed)
            {
                break;
            }
            // 目标侧特效使用前方展示点，施法侧特效使用当前模型根节点。
            Transform anchor = displayedModel.transform;
            if (effect.targetBased)
            {
                anchor = effectTarget;
            }
            // 每次播放独立创建原版预制体，切换时不会复用上一动作的粒子状态。
            GameObject instance = Instantiate(effect.prefab);
            instance.name = "ROSkillEffect_" + effect.effectId + "_" + effect.prefab.name;
            activeEffects.Add(instance);
            instance.AddComponent<ROCreatureEffectPlayback>().Initialize(effect, anchor);
            nextEffectIndex++;
        }
    }

    /// <summary>只使用模型网格的包围盒落地、居中并调整相机距离，粒子范围不参与取景。</summary>
    private void FrameModel()
    {
        // 模型中的全部渲染组件，按索引读取并排除粒子渲染器。
        Renderer[] renderers = displayedModel.GetComponentsInChildren<Renderer>();
        // 已收集的可见模型网格范围，世界单位为米。
        Bounds bounds = new Bounds();
        // 是否已经用第一个模型网格初始化包围盒。
        bool hasBounds = false;
        // i 为当前模型的渲染组件索引。
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer)
            {
                continue;
            }
            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        // 将模型脚底放到地面，并让水平中心对齐展示台原点。
        Vector3 offset = new Vector3(modelRoot.position.x - bounds.center.x, modelRoot.position.y - bounds.min.y, modelRoot.position.z - bounds.center.z);
        displayedModel.transform.position += offset;
        bounds.center += offset;
        cameraTarget.position = bounds.center;
        effectTarget.position = modelRoot.position + displayedModel.transform.forward * Mathf.Max(bounds.size.z, 3f);
        effectTarget.rotation = displayedModel.transform.rotation;
        // 以包围球半径适配体型，避免切换到大模型时被镜头裁切。
        float radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
        orbitCamera.minDistance = radius;
        orbitCamera.maxDistance = radius * 8f;
        orbitCamera.distance = radius * framingDistanceMultiplier;
        orbitCamera.yaw = 180f;
        orbitCamera.pitch = 8f;
        orbitCamera.UpdateTransform();
    }

    /// <summary>释放当前播放图，Unity 的原始模型和动画资源不由此界面销毁。</summary>
    private void ReleaseAnimation()
    {
        currentAnimation = null;
        // i 是旧动作创建的特效索引；已经自行到期的实例由 Unity 比较为空。
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i] != null)
            {
                Destroy(activeEffects[i]);
            }
        }
        activeEffects.Clear();
        if (animationGraph.IsValid())
        {
            animationGraph.Destroy();
        }
    }

    /// <summary>界面销毁时释放原生动画播放图。</summary>
    private void OnDestroy()
    {
        ReleaseAnimation();
    }
}
