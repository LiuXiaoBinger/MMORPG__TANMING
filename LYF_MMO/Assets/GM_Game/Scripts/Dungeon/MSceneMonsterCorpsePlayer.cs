using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>按原BodyDisappearTime淡出普通怪尸体，不替代服务端的实体离场。</summary>
public sealed class MSceneMonsterCorpsePlayer
{
    /// <summary>原副本闭包的BodyDisappearTime均为2秒，协议另加1毫秒只用于删除调度。</summary>
    private const float BodyDisappearMilliseconds = 2000f;
    /// <summary>当前真实怪物模型及原配置编号。</summary>
    private readonly MonsterEntity monster;
    /// <summary>当前副本表现目录指定保留完整模型，等待服务器移除而不淡出。</summary>
    private readonly bool noFade;
    /// <summary>所属场景 Buff 目录登记的可见度 Shader；未登记战斗表现时为空。</summary>
    private readonly string visibilityShader;
    /// <summary>淡出期间克隆的材质，不修改共享资源。</summary>
    private readonly List<Material> materials = new List<Material>();
    /// <summary>原怪物可见度Shader的异步句柄，实体销毁时释放。</summary>
    private AssetOperationHandle visibility;
    /// <summary>模型材质已经切换为本尸体持有的副本。</summary>
    private bool initialized;

    /// <summary>绑定当前怪物，从所属副本表现目录读取尸体不淡出的实体分类。</summary>
    /// <param name="entity">收到权威Dead动作的原怪物实例。</param>
    public MSceneMonsterCorpsePlayer(MonsterEntity entity)
    {
        monster = entity;
        // 尸体沿当前模型所属的副本登记读取材质，不借用后续场景的目录。
        DungeonSceneResource resource = entity.GetEntityCtrl().GetComponentInParent<MSceneDungeon>().Resource;
        // 同根只读目录与当前副本 Buff 播放器共享，未迁入时没有淡出资源。
        MSceneMonsterBuffCatalog catalog = MSceneMonsterBuffCatalog.Load(resource.battleResourceRoot);
        if (catalog == null) return;
        visibilityShader = catalog.VisibilityShader;
        if (catalog.NoFadeEntityIds == null) return;
        // 当前模型命中本目录保留列表时，沿原服务器删除时间保持可见。
        for (int index = 0; index < catalog.NoFadeEntityIds.Length; index++)
        {
            if (catalog.NoFadeEntityIds[index] != entity.ConfigId) continue;
            noFade = true;
            break;
        }
    }

    /// <summary>从权威剩余时间继续淡出，包含模型异步加载耗时和晚加入进度。</summary>
    /// <param name="motion">Dead动作的剩余毫秒与真实收包时刻。</param>
    public void Update(MSceneMonsterMotion motion)
    {
        if (noFade || string.IsNullOrEmpty(visibilityShader)) return;
        if (visibility == null)
            visibility = Global.Instance.YooPackage.LoadAssetAsync(visibilityShader);
        if (!visibility.IsDone) return;
        if (!initialized)
        {
            initialized = true;
            Initialize(visibility.AssetObject as Shader);
        }
        // 原Dead.remind_time/destroyTime公式；只钳制渲染范围，不本地删除实体。
        float remaining = motion.Action.DurationMs - (float)((Time.realtimeSinceStartupAsDouble - motion.ReceivedAt) * 1000d);
        // 服务器+1ms销毁调度不改变原2秒淡出分母。
        float alpha = Mathf.Clamp01(remaining / BodyDisappearMilliseconds);
        for (int index = 0; index < materials.Count; index++) materials[index].SetFloat("_Visibility", alpha);
    }

    /// <summary>沿原怪物Visibility Shader创建当前模型专用材质。</summary>
    /// <param name="shader">由正式YooAsset资源加载的原Shader。</param>
    private void Initialize(Shader shader)
    {
        // 仅真实模型蒙皮，不修改已挂载的粒子特效材质。
        SkinnedMeshRenderer[] renderers = monster.Model.GameObject.Transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            // 原模型每个材质槽保留纹理和着色参数。
            Material[] original = renderers[index].sharedMaterials;
            // 每个尸体独立持有副本，不改变其他同模型怪物。
            Material[] changed = new Material[original.Length];
            for (int item = 0; item < original.Length; item++)
            {
                changed[item] = new Material(original[item]);
                changed[item].shader = shader;
                materials.Add(changed[item]);
            }
            renderers[index].sharedMaterials = changed;
        }
    }

    /// <summary>服务端离场销毁模型后释放本尸体拥有的材质与Shader引用。</summary>
    public void Dispose()
    {
        for (int index = 0; index < materials.Count; index++) Object.Destroy(materials[index]);
        materials.Clear();
        if (visibility != null) visibility.Release();
    }
}
