using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>只维护当前副本原Buff实例的视觉生命周期，权威增删与数值仍由服务器决定。</summary>
public sealed class MSceneMonsterBuffPlayer
{
    /// <summary>一条权威Buff实例对应的当前视觉对象。</summary>
    private sealed class Instance
    {
        /// <summary>最近一次权威剩余寿命与收到时刻。</summary>
        public MSceneMonsterBuffState State;
        /// <summary>此实例的原视觉配置。</summary>
        public MSceneMonsterBuffCatalog.Buff Data;
        /// <summary>实际目标角色或怪物，尚未加载时为空。</summary>
        public MEntity Target;
        /// <summary>模型已就绪且已发起本Buff的原资源加载。</summary>
        public bool Initialized;
        /// <summary>本实例拥有的持续原特效。</summary>
        public readonly List<GameObject> Effects = new List<GameObject>();
        /// <summary>原925曲线组件，异步完成后用于更新两端。</summary>
        public MSceneMonsterLinkCurve Link;
        /// <summary>改变过的模型材质原引用，结束时原样恢复。</summary>
        public readonly Dictionary<Renderer, Material[]> OriginalMaterials = new Dictionary<Renderer, Material[]>();
        /// <summary>本实例克隆的材质，回收时释放。</summary>
        public readonly List<Material> Materials = new List<Material>();
    }
    /// <summary>沿当前副本现有SpawnId字典找实体的强类型入口。</summary>
    private readonly Func<ulong, MEntity> findMonster;
    /// <summary>沿原SceneWorld查找实际玩家角色。</summary>
    private readonly SceneWorldSystem world;
    /// <summary>服务器实例编号对应的视觉实例，不按BuffId混合不同来源。</summary>
    private readonly Dictionary<ulong, Instance> instances = new Dictionary<ulong, Instance>();
    /// <summary>本副本只读Buff表现表。</summary>
    private readonly MSceneMonsterBuffCatalog catalog;
    /// <summary>半透时复用的原Shader异步资源句柄。</summary>
    private AssetOperationHandle visibility;
    /// <summary>副本已卸载时拒绝迟到的资源回调。</summary>
    private bool disposed;

    /// <summary>绑定现有实体索引，不创建新角色或另一套Buff结算系统。</summary>
    /// <param name="sceneWorld">当前副本原实体世界。</param>
    /// <param name="resolver">当前副本SpawnId到原实体的查找。</param>
    /// <param name="resource">本场景出生时绑定的资源登记，不随全局场景切换。</param>
    public MSceneMonsterBuffPlayer(SceneWorldSystem sceneWorld, Func<ulong, MEntity> resolver, DungeonSceneResource resource)
    {
        world = sceneWorld;
        findMonster = resolver;
        catalog = MSceneMonsterBuffCatalog.Load(resource.battleResourceRoot);
    }

    /// <summary>新增、校正寿命或移除同一服务器Buff实例。</summary>
    /// <param name="state">当前实例已筛选的真实Buff更新。</param>
    public void Apply(MSceneMonsterBuffState state)
    {
        if (catalog == null) return;
        if (state.Data.Removed) { Remove(state.Data.BuffInstanceId); return; }
        // 同一实例的剩余时间刷新保留已经存在的视觉对象。
        Instance value;
        if (instances.TryGetValue(state.Data.BuffInstanceId, out value)) { value.State = state; return; }
        // 只加载本副本真实存在的原Buff表现。
        MSceneMonsterBuffCatalog.Buff data = catalog.Find(state.Data.BuffId);
        if (data == null) return;
        instances.Add(state.Data.BuffInstanceId, new Instance { State = state, Data = data });
    }

    /// <summary>恢复异步完成的目标模型、更新原曲线与过期回收。</summary>
    public void Update()
    {
        // 回收可能修改字典，保留原实例引用的快照再遍历。
        List<ulong> keys = new List<ulong>(instances.Keys);
        for (int index = 0; index < keys.Count; index++)
        {
            // 当前服务器Buff实例的原视觉记录。
            Instance value = instances[keys[index]];
            if (value.State.RemainingSeconds <= 0d) { Remove(keys[index]); continue; }
            if (!value.Initialized)
            {
                value.Target = FindTarget(value.State.Data);
                if (value.Target == null) continue;
                if (value.Target.Model.GameObject.Transform == null) continue;
                value.Initialized = true;
                Initialize(value);
            }
            if (value.Link == null) continue;
            // 原LinkType3使用实际Buff施加者，不猜测最近目标。
            MEntity caster = findMonster(value.State.Data.CasterSpawnId);
            if (caster == null) { value.Link.gameObject.SetActive(false); continue; }
            value.Link.gameObject.SetActive(true);
            value.Link.SetStartPosition(LinkPoint(caster), LinkPoint(value.Target));
        }
    }

    /// <summary>按协议目标身份查找同一场景实体。</summary>
    /// <param name="data">目标SpawnId和RoleId互斥的原Buff数据。</param>
    private MEntity FindTarget(MonsterBuffData data)
    {
        if (data.TargetSpawnId != 0) return findMonster(data.TargetSpawnId);
        // 主角与远端角色使用各自现有索引。
        RoleEntity role = world.LocalRole;
        if (role != null && role.GetID() == data.TargetRoleId) return role;
        world.TryGetRole(data.TargetRoleId, out role);
        return role;
    }

    /// <summary>沿原MBuffEffectLink规则选头部挂点，非玩家为原高度0.8。</summary>
    /// <param name="entity">真实施加者或受Buff角色。</param>
    private static Vector3 LinkPoint(MEntity entity)
    {
        if (entity is RoleEntity)
        {
            // 现有模型原骨骼挂点，玩家有HelmetPoint时使用该挂点。
            Transform[] points = entity.Model.GameObject.Transform.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < points.Length; index++)
                if (points[index].name == "HelmetPoint") return points[index].position - Vector3.up * entity.Height * 0.1f;
        }
        return entity.Position + Vector3.up * entity.Height * 0.8f;
    }

    /// <summary>目标模型就绪后创建本Buff实际使用的持续FX与模型效果。</summary>
    /// <param name="value">已绑定真实目标的Buff实例。</param>
    private void Initialize(Instance value)
    {
        for (int index = 0; index < value.Data.Effects.Length; index++) LoadEffect(value, value.Data.Effects[index], false);
        if (!string.IsNullOrEmpty(value.Data.LinkPath))
            LoadEffect(value, new MSceneMonsterFxData { Path = value.Data.LinkPath, Scale = Vector3.one }, true);
        if (value.Data.ChangeEffect == 0) return;
        if (value.Data.Alpha >= 1f) { ApplyMaterials(value, null); return; }
        if (visibility == null) visibility = Global.Instance.YooPackage.LoadAssetAsync(catalog.VisibilityShader);
        if (visibility.IsDone) ApplyMaterials(value, visibility.AssetObject as Shader);
        else visibility.Completed += handle => ApplyMaterials(value, handle.AssetObject as Shader);
    }

    /// <summary>通过原ResourceMgr加载特效，移除/离场/过期后迟到对象立即销毁。</summary>
    /// <param name="value">持有该资源的服务器Buff实例。</param>
    /// <param name="data">原挂点、高度、缩放与资源地址。</param>
    /// <param name="link">是否为原925曲线对象。</param>
    private void LoadEffect(Instance value, MSceneMonsterFxData data, bool link)
    {
        ResourceMgr.Instance.LoadPrefabAsync(data.Path, model =>
        {
            if (!IsAlive(value)) { UnityEngine.Object.Destroy(model); return; }
            // 原FxPoint优先于按模型高度比例的挂点。
            Transform anchor = value.Target.Model.GameObject.Transform;
            if (!string.IsNullOrEmpty(data.Bone))
            {
                // 原骨骼名字在现有模型层级中直接查找。
                Transform[] bones = anchor.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < bones.Length; index++)
                    if (bones[index].name == data.Bone) { anchor = bones[index]; break; }
            }
            model.transform.SetParent(anchor, false);
            model.transform.position = anchor.position + Vector3.up * value.Target.Height * data.HeightRatio;
            model.transform.localScale = data.Scale;
            value.Effects.Add(model);
            if (link) value.Link = model.GetComponent<MSceneMonsterLinkCurve>();
        });
    }

    /// <summary>只检查真实异步资源生命周期，不能让移除后的回调恢复Buff。</summary>
    /// <param name="value">等待完成的原Buff实例。</param>
    private bool IsAlive(Instance value)
    {
        if (disposed || value.State.RemainingSeconds <= 0d) return false;
        if (!instances.ContainsKey(value.State.Data.BuffInstanceId)) return false;
        return value.Target.Model.GameObject.Transform != null;
    }

    /// <summary>按原Visibility shader及_Color语义修改本实例的材质副本。</summary>
    /// <param name="value">指定原Type26的Buff实例。</param>
    /// <param name="shader">半透时使用的原Shader；单纯颜色变更为空。</param>
    private void ApplyMaterials(Instance value, Shader shader)
    {
        if (!IsAlive(value)) return;
        // 仅目标既有模型Renderer，不处理其后挂载的Buff粒子。
        SkinnedMeshRenderer[] renderers = value.Target.Model.GameObject.Transform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            // 本副本角色与怪物的真实蒙皮材质引用，排除后挂载的特效Mesh。
            Renderer renderer = renderers[index];
            // 保存原数组以便Buff移除后恢复，不修改共享材质资产。
            Material[] original = renderer.sharedMaterials;
            // 本Buff拥有的材质副本数组。
            Material[] changed = new Material[original.Length];
            value.OriginalMaterials.Add(renderer, original);
            for (int item = 0; item < original.Length; item++)
            {
                changed[item] = new Material(original[item]);
                if (shader != null) changed[item].shader = shader;
                changed[item].SetFloat("_Visibility", value.Data.Alpha);
                if (value.Data.ChangeEffect == 3) changed[item].SetColor("_Color", value.Data.Color);
                value.Materials.Add(changed[item]);
            }
            renderer.sharedMaterials = changed;
        }
    }

    /// <summary>精确回收一个Buff实例，保留其他来源的同ID效果。</summary>
    /// <param name="instanceId">本实例内单调唯一的Buff身份。</param>
    private void Remove(ulong instanceId)
    {
        // 移除包可先于异步模型完成，记录存在时即清理其所有已创建内容。
        Instance value;
        if (!instances.TryGetValue(instanceId, out value)) return;
        instances.Remove(instanceId);
        for (int index = 0; index < value.Effects.Count; index++) UnityEngine.Object.Destroy(value.Effects[index]);
        // 还原本Buff接管的原模型材质。
        List<Renderer> renderers = new List<Renderer>(value.OriginalMaterials.Keys);
        for (int index = 0; index < renderers.Count; index++)
            if (renderers[index] != null) renderers[index].sharedMaterials = value.OriginalMaterials[renderers[index]];
        for (int index = 0; index < value.Materials.Count; index++) UnityEngine.Object.Destroy(value.Materials[index]);
    }

    /// <summary>副本卸载时回收全部Buff表现与异步Shader引用。</summary>
    public void Dispose()
    {
        disposed = true;
        // 释放期间会修改原字典，按唯一ID快照清理。
        List<ulong> keys = new List<ulong>(instances.Keys);
        for (int index = 0; index < keys.Count; index++) Remove(keys[index]);
        if (visibility != null) visibility.Release();
    }
}
