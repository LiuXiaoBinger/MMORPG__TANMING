using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>将世界相机从玩家当前轨道过渡至场景 NPC 的正面半身，再平滑归还原轨道。</summary>
public partial class CameraMgr
{
    /// <summary>独立的对话虚拟相机，其优先级低于普通世界镜头。</summary>
    private CinemachineVirtualCamera _talkCamera;
    /// <summary>实际输出当前世界画面的 Cinemachine 混合器。</summary>
    private CinemachineBrain _talkBrain;
    /// <summary>当前对话角色的场景模型，退出后清空。</summary>
    private Transform _talkModel;
    /// <summary>模型脚底到头骨的高度，单位为米，在对话开始时确定以免镜头随呼吸起伏。</summary>
    private float _talkHeadHeight;
    /// <summary>从世界机位到对话机位的过渡进度，范围 0 到 1。</summary>
    private float _talkWeight;
    /// <summary>本次对话在 Brain 中占用的混合层，-1 表示未占用。</summary>
    private int _talkOverride = -1;
    /// <summary>本次特写临时隐藏的玩家渲染器；这里只保存原来允许绘制的对象。</summary>
    private readonly List<Renderer> _talkHiddenPlayer = new List<Renderer>();
    /// <summary>说话者离场或超出交互范围时，通知主控制器关闭对应窗口。</summary>
    private Action _talkLost;

    /// <summary>读取实际说话者的高度，为当前对话准备独立的正面半身镜头。</summary>
    /// <param name="npc">已加载且正面朝向玩家的交互 NPC。</param>
    /// <param name="onLost">人物离场时沿窗口原入口结束对话。</param>
    internal void BeginNpcTalk(NpcEntity npc, Action onLost)
    {
        // 世界输出相机持有 Brain，FreeLook 自身只提供世界机位。
        Camera output = Camera.main;
        if (output == null || !output.TryGetComponent(out _talkBrain)) return;
        _talkModel = npc.Model.GameObject.Transform;
        _talkLost = onLost;
        // 对人形采用真实头部高度，通用骨架沿用实体身高。
        Animator animator = npc.Model.Ator.UnityAnimator;
        _talkHeadHeight = Mathf.Max(0.3f, npc.Height * 0.88f);
        if (animator != null && animator.isHuman)
        {
            // 人形头骨位于头部中心偏下，保留头顶的画面余量。
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) _talkHeadHeight = Mathf.Max(0.3f, head.position.y - _talkModel.position.y);
        }
        if (_talkCamera == null)
        {
            // 放到世界相机所在场景，以免作为 FreeLook 的子机位参与其内部轨道排序。
            GameObject rig = new GameObject("NPC Dialogue Camera");
            SceneManager.MoveGameObjectToScene(rig, gameObject.scene);
            _talkCamera = rig.AddComponent<CinemachineVirtualCamera>();
            _talkCamera.m_Priority = int.MinValue + 1;
            _talkCamera.m_Lens.FieldOfView = 35f;
            _talkCamera.m_Lens.NearClipPlane = 0.05f;
        }
        _talkCamera.gameObject.SetActive(true);
        if (_roleMainTransform != null)
        {
            // 玩家贴近 NPC 时可能站在两者之间；只隐藏画面，移动、碰撞与模型状态继续工作。
            Renderer[] parts = _roleMainTransform.GetComponentsInChildren<Renderer>(true);
            // 按部件序号记录确实由本次对话隐藏的渲染器。
            for (int index = 0; index < parts.Length; index++)
            {
                // 已被其他展示功能隐藏的部件不纳入本次恢复清单。
                Renderer part = parts[index];
                if (part.forceRenderingOff) continue;
                part.forceRenderingOff = true;
                _talkHiddenPlayer.Add(part);
            }
        }
        ReleaseWorldDrag();
        UpdateGroundHint(false);
        PositionNpcTalkCamera();
    }

    /// <summary>以 NPC 面向玩家的方向略微侧移，留出右侧选项位置并看清上半身。</summary>
    private void PositionNpcTalkCamera()
    {
        // 绕 NPC 转少许角度，避免近距离时玩家正好站在镜头轴线上。
        Vector3 front = Quaternion.AngleAxis(16f, Vector3.up) * _talkModel.forward;
        front = Vector3.ProjectOnPlane(front, Vector3.up).normalized;
        // 镜头瞄点落在上胸；头部上方和对白上方均留下背景。
        Vector3 aim = _talkModel.position + Vector3.up * (_talkHeadHeight * 0.86f);
        // 眼高机位稍高于头骨，按身高确定镜头距离。
        Vector3 eye = _talkModel.position + Vector3.up * (_talkHeadHeight * 1.01f) + front * (_talkHeadHeight * 1.46f);
        _talkCamera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(aim - eye, Vector3.up));
    }

    /// <summary>普通关窗时从当前混合进度返回原世界相机，原缩放和航向由 FreeLook 自身保存。</summary>
    internal void EndNpcTalk()
    {
        _talkModel = null;
        _talkLost = null;
        if (_talkOverride >= 0) return;
        if (_talkCamera != null) _talkCamera.gameObject.SetActive(false);
        for (int index = 0; index < _talkHiddenPlayer.Count; index++)
        {
            // 切场景时原模型可能已经销毁；仍存活的部件恢复原绘制状态。
            Renderer part = _talkHiddenPlayer[index];
            if (part != null) part.forceRenderingOff = false;
        }
        _talkHiddenPlayer.Clear();
    }

    /// <summary>处理进出镜头的平滑过渡；返回 true 时暂停世界鼠标轨道输入。</summary>
    private bool UpdateNpcTalkCamera()
    {
        if (_talkLost == null && _talkOverride < 0) return false;
        if (_talkLost != null)
        {
            if (_talkModel == null || !_talkModel.gameObject.activeInHierarchy)
                _talkLost.Invoke();
            else if (_roleMainTransform != null && Vector3.Distance(_roleMainTransform.position, _talkModel.position) > NpcEntity.TalkDistance)
                _talkLost.Invoke();
        }
        // 入镜和离开都从实际进度续接，快速关窗时不会跳到完整特写。
        float goal = 0f;
        // 默认使用退镜秒数，仍有说话者时改用入镜时长。
        float duration = 0.26f;
        if (_talkModel != null)
        {
            PositionNpcTalkCamera();
            goal = 1f;
            duration = 0.34f;
        }
        _talkWeight = Mathf.MoveTowards(_talkWeight, goal, Time.unscaledDeltaTime / duration);
        if (_talkWeight <= 0f && _talkModel == null)
        {
            ReleaseNpcTalkCamera();
            return false;
        }
        _talkOverride = _talkBrain.SetCameraOverride(_talkOverride, _cinemachine, _talkCamera,
            Mathf.SmoothStep(0f, 1f, _talkWeight), Time.unscaledDeltaTime);
        return true;
    }

    /// <summary>退出或切场景时归还混合层；待机中的特写相机不参与世界机位选择。</summary>
    private void ReleaseNpcTalkCamera()
    {
        if (_talkBrain != null && _talkOverride >= 0) _talkBrain.ReleaseCameraOverride(_talkOverride);
        _talkOverride = -1;
        _talkWeight = 0f;
        EndNpcTalk();
    }
}
