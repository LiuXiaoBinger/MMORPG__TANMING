using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>正式副本的出生运行器：按场景登记读取 XML，加入统一实体世界，并随副本卸载回收。</summary>
public class MSceneDungeon : MonoBehaviour
{
    /// <summary>当前项目剑气普攻的稳定 ID，对应服务端 ROArtSkill 的三段表现。</summary>
    public const int BasicAttackId = 10010;
    /// <summary>已加载的怪物按波次出生标识定位；与异步模型完成顺序无关。</summary>
    private readonly Dictionary<ulong, MEntity> monsters = new Dictionary<ulong, MEntity>();
    /// <summary>本场景开始时确认的服务端实例号。</summary>
    private ulong instanceId;
    /// <summary>当前副本原Buff实例视觉生命周期，退出时随场景释放。</summary>
    private MSceneMonsterBuffPlayer buffPlayer;
    /// <summary>挂在主画布底层、生命周期跟随本副本的普通伤害跳字网格。</summary>
    private MBattleHudWordMgr battleHudWord;

    /// <summary>本次进入副本解析出的原波次，不与其他场景共享状态。</summary>
    public readonly List<MSceneRuntimeWave> Waves = new List<MSceneRuntimeWave>();
    /// <summary>本副本创建的运行实体身份，退出时只回收这一批实体。</summary>
    private readonly List<ulong> entityUids = new List<ulong>();
    /// <summary>与出生时相同的实体世界，清理时不依赖全局对象的销毁顺序。</summary>
    private SceneWorldSystem world;
    /// <summary>编辑器与运行时共用的模型和动作目录。</summary>
    private ROSceneCreatureCatalog catalog;
    /// <summary>本场景出生时绑定的资源登记，模型播放器始终读取所属场景，不跟随全局切场景变化。</summary>
    public DungeonSceneResource Resource { get; private set; }

    /// <summary>按原配置编号保存物件和空气墙，激活状态完全由服务器快照决定。</summary>
    private readonly Dictionary<int, GameObject> sceneObjects = new Dictionary<int, GameObject>();
    /// <summary>门户与采集交互候选，随当前权威快照重建。</summary>
    private readonly List<MSceneInteraction> interactions = new List<MSceneInteraction>();
    /// <summary>每波拥有的运行实体，阶段重置时只回收对应实体。</summary>
    private readonly Dictionary<int, List<ulong>> waveEntities = new Dictionary<int, List<ulong>>();
    /// <summary>每个异步出生身份只提交一次，权威重置时删除。</summary>
    private readonly HashSet<ulong> requestedSpawns = new HashSet<ulong>();
    /// <summary>原采集法阵特效，完成采集后隐藏，退出时随副本根销毁。</summary>
    private GameObject collectionEffect;
    /// <summary>当前场景同份编辑文档，保存物件位置与效果配置。</summary>
    private XDocument document;

    /// <summary>读取同一份编辑配置并订阅服务端阶段，客户端不判断出生前置。</summary>
    private void Start()
    {
        world = Global.Instance.SceneWorld;
        instanceId = TeamData.Instance.DungeonInstanceId;
        Resource = DungeonSceneResource.Load(TeamData.Instance.DungeonSceneId);
        buffPlayer = new MSceneMonsterBuffPlayer(world, FindMonster, Resource);
        TeamData.Instance.MonsterBuffUpdated += OnMonsterBuffUpdated;
        // 入场时已经收到的原Buff快照，模型就绪后使用剩余寿命恢复。
        List<MSceneMonsterBuffState> buffs = TeamData.Instance.GetMonsterBuffs();
        for (int index = 0; index < buffs.Count; index++) buffPlayer.Apply(buffs[index]);
        TeamData.Instance.MonsterAttrUpdated += OnMonsterAttrUpdate;
        TeamData.Instance.MonsterMotionUpdated += OnMonsterMotion;
        TeamData.Instance.MonsterSpawned += SpawnDynamic;
        TeamData.Instance.MonsterRemoved += RemoveDynamic;
        TeamData.Instance.MonsterPlayerHit += OnMonsterPlayerHit;
        TeamData.Instance.MonsterHeadExpression += OnHeadExpression;
        TeamData.Instance.DungeonFlowUpdated += ApplyFlow;
        if (UIRoot.Instance != null && instanceId != 0UL && !string.IsNullOrEmpty(Resource.hitWordResource))
        {
            // 预制体保存十张原始数字 Sprite、材质和 RO 普攻曲线，显示在功能窗口下方。
            // 热更组件从已收集的 YooAsset 预制体加载，避免 Player 内置 Resources 丢失脚本绑定。
            ResourceMgr.Instance.LoadPrefabAsync("Assets/GM_Game/Resources/" + Resource.hitWordResource + ".prefab",
                OnBattleHudLoaded);
        }
        // 读取本次入场 SceneID 的资源，不能沿用另一副本的波次或怪物目录。
        catalog = ROSceneCreatureCatalog.Load(Resource.creatureResource);
        // Resources 中的原 XML 随编辑器保存更新，不另外烘焙出生点副本。
        TextAsset source = Resources.Load<TextAsset>(Resource.flowResource);
        // XML 是配置序列化边界；只枚举原波次对象，不使用画布显示节点计数。
        document = XDocument.Parse(source.text);
        XElement[] records = document.Descendants("MSceneWaveData").ToArray();
        for (int index = 0; index < records.Length; index++)
        {
            // 本次副本独有的波次状态。
            MSceneRuntimeWave wave = new MSceneRuntimeWave();
            wave.Data = records[index];
            wave.Id = ROSceneXml.Int(wave.Data, "base_info/id");
            Waves.Add(wave);
            waveEntities.Add(wave.Id, new List<ulong>());
        }
        ApplyFlow();
        // 入场包中的召唤物不在XML固定波次中，按当前服务端快照恢复。
        List<MonsterSpawnData> summons = TeamData.Instance.GetMonsterSpawns();
        for (int index = 0; index < summons.Count; index++) SpawnDynamic(summons[index]);
    }

    /// <summary>只把同实例Buff通知交给当前场景，切场景期间旧模型不接受新副本表现。</summary>
    /// <param name="state">TeamData已经记录收到时刻的原Buff更新。</param>
    private void OnMonsterBuffUpdated(MSceneMonsterBuffState state)
    {
        if (instanceId != TeamData.Instance.DungeonInstanceId) return;
        buffPlayer.Apply(state);
    }

    /// <summary>按服务端出生位查找已经绑定好的怪物或NPC实体。</summary>
    /// <param name="spawnId">当前实例中的固定或动态出生身份。</param>
    private MEntity FindMonster(ulong spawnId)
    {
        // 原模型字典中的同一实体引用。
        MEntity entity;
        monsters.TryGetValue(spawnId, out entity);
        return entity;
    }

    /// <summary>仅寻找主角附近的已解锁交互；阶段与出生不由帧循环推进。</summary>
    private void Update()
    {
        if (buffPlayer != null) buffPlayer.Update();
        if (world.LocalRole == null) return;
        // 选择最近可用物件，离开后恢复为空。
        MSceneInteraction nearest = null;
        float distance = float.MaxValue;
        for (int index = 0; index < interactions.Count; index++)
        {
            // 已解锁交互及其与主角的距离平方。
            MSceneInteraction candidate = interactions[index];
            float candidateDistance = (candidate.Position - world.LocalRole.Position).sqrMagnitude;
            if (!candidate.Contains(world.LocalRole.Position) || candidateDistance >= distance) continue;
            nearest = candidate;
            distance = candidateDistance;
        }
        TeamData.Instance.SetNearbyInteraction(nearest);
    }

    /// <summary>消费完整权威快照，支持模型晚加载、晚加入与同实例重置。</summary>
    private void ApplyFlow()
    {
        if (document == null || instanceId != TeamData.Instance.DungeonInstanceId) return;
        // 当前服务端阶段，不使用 XML 的 None 条件自行创建。
        DungeonFlowState flow = TeamData.Instance.DungeonFlow;
        if (flow == null) return;
        for (int index = 0; index < Waves.Count; index++)
        {
            // 对应原 XML 波次，出生条件已经由服务器求值。
            MSceneRuntimeWave wave = Waves[index];
            if (flow.ActiveWaveIds.Contains(wave.Id)) SpawnBatch(wave);
            else
            {
                // 该波实体身份列表在模型加载完成时登记。
                List<ulong> owned = waveEntities[wave.Id];
                for (int entityIndex = 0; entityIndex < owned.Count; entityIndex++)
                {
                    world.RemoveEntity(owned[entityIndex]);
                    entityUids.Remove(owned[entityIndex]);
                }
                owned.Clear();
                // 出生字典使用稳定的高32位波次编号。
                ulong[] keys = requestedSpawns.Where(key => (int)(key >> 32) == wave.Id).ToArray();
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    requestedSpawns.Remove(keys[keyIndex]);
                    monsters.Remove(keys[keyIndex]);
                }
            }
        }
        RefreshSceneObjects(flow);
    }

    /// <summary>恢复服务器的物件、墙与采集状态；所有位置仍读取编辑 XML。</summary>
    /// <param name="flow">本实例完整阶段快照。</param>
    private void RefreshSceneObjects(DungeonFlowState flow)
    {
        interactions.Clear();
        TeamData.Instance.SetNearbyInteraction(null);
        // 原区域对象带有门户效果、空气墙、按钮等选项。
        XElement[] objects = document.Descendants("MSceneTriggerObjectData").ToArray();
        for (int index = 0; index < objects.Length; index++)
        {
            // 当前对象配置与快照身份。
            XElement data = objects[index];
            int id = ROSceneXml.Int(data, "id");
            bool wall = data.Descendants("SceneTriggerObjectOpType").Any(option => option.Value == "StoOpTypeAirWall");
            bool button = data.Descendants("SceneTriggerObjectOpType").Any(option => option.Value == "StoOpTypePlayerButton");
            bool active = flow.ActiveObjectIds.Contains(id);
            if (wall) active = flow.ActiveWallIds.Contains(id);
            if (!wall && !button) continue;
            // 已加载表现复用；关闭时同时关闭碰撞和导航障碍。
            GameObject instance;
            if (!sceneObjects.TryGetValue(id, out instance))
            {
                if (!active) continue;
                // 正式资源根分别持有原 RO 传送效果与实体空气墙。
                string resource = Resource.portalResource;
                if (wall) resource = Resource.airWallResource;
                if (string.IsNullOrEmpty(resource)) continue;
                instance = Instantiate(Resources.Load<GameObject>(resource), transform);
                instance.name = "Scene object " + id;
                instance.transform.position = ROSceneXml.Vector(data, "position") + ROSceneXml.Vector(data, "box/offset");
                instance.transform.rotation = Quaternion.Euler(0, ROSceneXml.Float(data, "face"), 0);
                if (wall) instance.transform.localScale = ROSceneXml.Vector(data, "box/scale");
                sceneObjects.Add(id, instance);
            }
            instance.SetActive(active);
            if (active && button)
            {
                // 配置盒中的正式传送按钮，不使用自动触碰传送。
                MSceneInteraction interaction = new MSceneInteraction();
                interaction.ObjectId = id;
                interaction.Title = "传送";
                interaction.Position = instance.transform.position;
                interaction.Rotation = instance.transform.rotation;
                interaction.Extents = ROSceneXml.Vector(data, "box/scale") * 0.5f;
                interactions.Add(interaction);
            }
        }
        if (collectionEffect != null) collectionEffect.SetActive(false);
        for (int index = 0; index < Waves.Count; index++)
        {
            // 采集仅在服务器已激活且尚未完成时提供操作。
            MSceneRuntimeWave wave = Waves[index];
            if (wave.Data.Element("collection_info") == null) continue;
            if (!flow.ActiveWaveIds.Contains(wave.Id) || flow.CompletedWaveIds.Contains(wave.Id)) continue;
            // 采集物位置沿用原配置出生点。
            XElement[] points = ROSceneXml.SpawnPoints(wave.Data);
            if (points.Length == 0) continue;
            MSceneInteraction interaction = new MSceneInteraction();
            if (collectionEffect == null && !string.IsNullOrEmpty(Resource.collectionResource))
            {
                collectionEffect = Instantiate(Resources.Load<GameObject>(Resource.collectionResource), transform);
                collectionEffect.transform.position = ROSceneXml.Vector(points[0], "position");
            }
            if (collectionEffect != null) collectionEffect.SetActive(true);
            interaction.WaveId = wave.Id;
            interaction.Title = Resource.collectionTitle;
            interaction.Position = ROSceneXml.Vector(points[0], "position");
            interaction.Rotation = Quaternion.identity;
            interaction.Extents = Vector3.one * 2.5f;
            interactions.Add(interaction);
        }
    }

    /// <summary>按原点位数量、实际总数和分批间隔提交实例创建。</summary>
    /// <param name="wave">本帧到期的波次。</param>
    private void SpawnBatch(MSceneRuntimeWave wave)
    {
        // NPC 与怪物来自不同配置表，避免编号碰撞。
        bool isNpc = wave.Data.Element("npc_info") != null;
        int configId = ROSceneXml.Int(wave.Data, "monster_info/entity_id");
        if (isNpc) configId = ROSceneXml.Int(wave.Data, "npc_info/npc_id");
        ROSceneCreatureEntity creature = null;
        for (int index = 0; index < catalog.entities.Length; index++)
        {
            if (catalog.entities[index].id == configId && catalog.entities[index].isNpc == isNpc)
                creature = catalog.entities[index];
        }
        // 采集与组事件没有实体；原空模型控制怪使用无Renderer的正式机制预制体。
        if (creature == null)
        {
            wave.Spawned = true;
            return;
        }
        // 点位 count 展开后再受 real_num 限制，NPC 单点同样遵循配置数量。
        XElement[] points = ROSceneXml.SpawnPoints(wave.Data);
        int total = 0;
        for (int index = 0; index < points.Length; index++) total += ROSceneXml.Int(points[index], "count", 1);
        total = Mathf.Clamp(ROSceneXml.Int(wave.Data, "monster_info/monster_base_data/entity_info/real_num", total), 0, total);
        // 服务器已经提交本批出生，客户端不另起本地延迟。
        int batchEnd = total;
        int ordinal = 0;
        for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
        {
            for (int copyIndex = 0; copyIndex < ROSceneXml.Int(points[pointIndex], "count", 1); copyIndex++)
            {
                if (ordinal < batchEnd) SpawnCreature(wave, creature, points[pointIndex], ordinal);
                ordinal++;
            }
        }
        wave.SpawnCount = batchEnd;
        wave.Spawned = batchEnd >= total;
    }

    /// <summary>实例化正式资源后建立实体、模型、控制器的同一身份绑定。</summary>
    /// <param name="wave">原出生波次。</param>
    /// <param name="creature">原表对应的正式资源映射。</param>
    /// <param name="point">原场景位置和朝向，单位米/角度。</param>
    /// <param name="ordinal">按点位 count 展开的波内零基序号，在加载资源前即已确定。</param>
    private void SpawnCreature(MSceneRuntimeWave wave, ROSceneCreatureEntity creature, XElement point, int ordinal)
    {
        // 身份由原波次及序号组成，与服务器怪物字典一致。
        ulong spawnId = ((ulong)(uint)wave.Id << 32) | (uint)(ordinal + 1);
        // 已启用战斗配置的快照包含所有仍在场 Actor，零 HP 机制/NPC 也据此排除已经清尸的固定点。
        MonsterHpData authority;
        if (!string.IsNullOrEmpty(Resource.battleConfig))
        {
            if (!TeamData.Instance.GetMonsterAttr(spawnId, out authority)) return;
        }
        else if (!creature.isNpc && creature.maxHp > 0)
        {
            if (!TeamData.Instance.GetMonsterAttr(spawnId, out authority)) return;
        }
        if (!requestedSpawns.Add(spawnId)) return;
        ResourceMgr.Instance.LoadPrefabAsync(creature.runtimePrefabPath, model =>
        {
            // 资源完成前副本已退出时，不将迟到模型加入新场景的世界。
            if (this == null || instanceId != TeamData.Instance.DungeonInstanceId)
            {
                Destroy(model);
                return;
            }
            if (!TeamData.Instance.DungeonFlow.ActiveWaveIds.Contains(wave.Id))
            {
                Destroy(model);
                return;
            }
            // 资源加载期间收到的移除同样有效，不能让晚完成模型重新生成尸体。
            if (!string.IsNullOrEmpty(Resource.battleConfig) && !TeamData.Instance.GetMonsterAttr(spawnId, out authority))
            {
                Destroy(model);
                return;
            }
            model.transform.SetParent(transform, false);
            model.transform.position = ROSceneXml.Vector(point, "position");
            model.transform.rotation = Quaternion.Euler(0f, ROSceneXml.Float(point, "face"), 0f);
            model.transform.localScale = Vector3.one * creature.scale;
            model.name = "Wave " + wave.Id + " · " + creature.displayName;
            // 沿用统一 Builder 装配真实实体及其状态、属性和模型组件。
            EntityType entityType = EntityType.Monster;
            if (creature.isNpc) entityType = EntityType.Npc;
            MEntity entity = (MEntity)world.CreateEntity(entityType, creature.id);
            entityUids.Add(entity.UID);
            waveEntities[wave.Id].Add(entity.UID);
            // 正式预制体上持久保存模型准备数据与类型匹配的控制器。
            RoleCtrlBase controller = model.GetComponent<RoleCtrlBase>();
            if (creature.isNpc)
            {
                // 副本 NPC 的控制器持有与普通实体容器相同的领域实例。
                MSceneNpcHoster npc = (MSceneNpcHoster)controller;
                npc.WaveId = wave.Id;
                // 两个既有编号入口均写入原 NpcTable 主键，供查看和后续交互读取。
                NpcEntity npcEntity = (NpcEntity)entity;
                npcEntity.NpcId = creature.id;
                npcEntity.NpcID = creature.id;
                npcEntity.Name = creature.displayName;
                npc.Initialize(npcEntity);
                controller._roleType = RoleType.NPC;
            }
            else
            {
                ((MSceneMonsterHoster)controller).WaveId = wave.Id;
                // 尸体和伤员仍按原点位生成，其能否被攻击由本条实体配置决定。
                ((MonsterEntity)entity).NonCombatant = creature.nonCombatant;
                controller._roleType = RoleType.Monster;
            }
            controller.SetROObject(entity);
            entity.SetEntityCtrl(controller);
            entity.Model.SetGameObject(model, string.Empty, false);
            // 实体碰撞尺寸与实际模型包围盒一致，不使用角色职业表猜测怪物尺寸。
            MModelPrepareData prepared = model.GetComponent<MModelPrepareData>();
            prepared.ApplyMotions(entity.Model.Ator);
            entity.Radius = Mathf.Max(prepared.ProjectSize.x, prepared.ProjectSize.z) * 0.5f * creature.scale;
            entity.Height = prepared.ProjectSize.y * creature.scale;
            if (!creature.isNpc)
            {
                // 两端使用相同的出生序号关联实例属性，Unity 自身的实体 UID 仍交给 SceneWorld。
                MonsterEntity monster = (MonsterEntity)entity;
                monsters.Add(spawnId, monster);
                MonsterHpData data;
                if (instanceId == TeamData.Instance.DungeonInstanceId && TeamData.Instance.GetMonsterAttr(spawnId, out data))
                    ChangeHP(monster, data, false);
                // 晚加载的模型从服务器当前段进度恢复，保持入场死亡优先。
                MSceneMonsterMotion motion;
                if (instanceId == TeamData.Instance.DungeonInstanceId && TeamData.Instance.GetMonsterMotion(spawnId, out motion))
                    ((MSceneMonsterHoster)controller).ApplyMotion(motion);
            }
            if (creature.isNpc)
            {
                monsters.Add(spawnId, entity);
                // NPC控制节点与其原模型共用服务端SpawnId，不再次创建怪物。
                MSceneMonsterMotion npcMotion;
                if (TeamData.Instance.GetMonsterMotion(spawnId, out npcMotion))
                    ((MSceneNpcHoster)controller).ApplyMotion(npcMotion);
            }
            // 原 invisible 波次只隐藏模型和判定体，不改写出生配置。
            if (ROSceneXml.Text(wave.Data, "base_info/init_invisible") == "true") model.SetActive(false);
        });
    }

    /// <summary>只把本实例动作交给对应出生位的现有模型，未完成加载时由 TeamData 缓存。</summary>
    /// <param name="motion">对应怪物最近收到的服务端位姿和动作阶段。</param>
    private void OnMonsterMotion(MSceneMonsterMotion motion)
    {
        // 预制体已完成出生时可通过此服务端编号定位对应运行实体。
        MEntity monster;
        if (instanceId != TeamData.Instance.DungeonInstanceId || !monsters.TryGetValue(motion.Action.SpawnId, out monster)) return;
        // 已有NPC控制器仍负责交互，只复用技能表现播放器。
        MSceneNpcHoster npc = monster.GetEntityCtrl() as MSceneNpcHoster;
        if (npc != null) npc.ApplyMotion(motion);
        else ((MSceneMonsterHoster)monster.GetEntityCtrl()).ApplyMotion(motion);
    }

    /// <summary>沿既有资源、实体和控制器链创建不属于固定波次的召唤实体。</summary>
    /// <param name="spawn">服务端真实EntityTable编号、出生位置与身份。</param>
    private void SpawnDynamic(MonsterSpawnData spawn)
    {
        if (instanceId != TeamData.Instance.DungeonInstanceId || !requestedSpawns.Add(spawn.SpawnId)) return;
        // 召唤闭包也登记在本副本同一目录，Dummy使用不可见机制预制体。
        ROSceneCreatureEntity creature = catalog.FindEntity(spawn.EntityId);
        ResourceMgr.Instance.LoadPrefabAsync(creature.runtimePrefabPath, model =>
        {
            if (this == null || instanceId != TeamData.Instance.DungeonInstanceId)
            {
                Destroy(model);
                return;
            }
            if (!TeamData.Instance.HasMonsterSpawn(spawn.SpawnId))
            {
                Destroy(model);
                return;
            }
            model.transform.SetParent(transform, false);
            model.transform.position = new Vector3(spawn.X, spawn.Y, spawn.Z);
            model.transform.rotation = Quaternion.Euler(0f, spawn.Face, 0f);
            model.transform.localScale = Vector3.one * creature.scale;
            // 复用SceneWorld中的真实怪物类型，不另建召唤物世界。
            MonsterEntity monster = (MonsterEntity)world.CreateEntity(EntityType.Monster, creature.id);
            entityUids.Add(monster.UID);
            monsters.Add(spawn.SpawnId, monster);
            // 原三个召唤物都是IsDummy，无可点击怪物模型。
            monster.NonCombatant = true;
            MSceneMonsterHoster controller = model.GetComponent<MSceneMonsterHoster>();
            controller.WaveId = -1;
            controller._roleType = RoleType.Monster;
            controller.SetROObject(monster);
            monster.SetEntityCtrl(controller);
            monster.Model.SetGameObject(model, string.Empty, false);
            model.GetComponent<MModelPrepareData>().ApplyMotions(monster.Model.Ator);
            // 读取加载期间可能改变的权威血量与正在进行的技能，不重放出生时旧状态。
            MonsterHpData hp;
            if (TeamData.Instance.GetMonsterAttr(spawn.SpawnId, out hp)) ChangeHP(monster, hp, false);
            MSceneMonsterMotion motion;
            if (TeamData.Instance.GetMonsterMotion(spawn.SpawnId, out motion)) controller.ApplyMotion(motion);
        });
    }

    /// <summary>移除指定固定或动态身份，实体销毁负责回收其动画、特效及声音。</summary>
    /// <param name="spawnId">本实例已离场的服务器出生标识。</param>
    private void RemoveDynamic(ulong spawnId)
    {
        requestedSpawns.Remove(spawnId);
        // 资源尚未完成时字典没有实体，回调会通过出生缓存检查自行销毁。
        MEntity monster;
        if (!monsters.TryGetValue(spawnId, out monster)) return;
        monsters.Remove(spawnId);
        entityUids.Remove(monster.UID);
        // 固定波持有的世界UID同时移除，后续重置不能再次回收已复用的实体身份。
        List<ulong> owned;
        if (waveEntities.TryGetValue((int)(spawnId >> 32), out owned)) owned.Remove(monster.UID);
        world.RemoveEntity(monster.UID);
    }

    /// <summary>将异步加载的伤害数字挂到当前副本的 HUD，句柄由既有资源管理器持有。</summary>
    /// <param name="hudObject">YooAsset 实例化的完整数字预制体，包含普通攻击、玩家受伤和暴击网格。</param>
    private void OnBattleHudLoaded(GameObject hudObject)
    {
        if (this == null)
        {
            Destroy(hudObject);
            return;
        }
        battleHudWord = hudObject.GetComponent<MBattleHudWordMgr>();
        battleHudWord.transform.SetParent(UIRoot.Instance._canvas.transform, false);
        battleHudWord.transform.SetAsFirstSibling();
    }

    /// <summary>真实命中回包驱动伤害数字及原结果点特效，生命与受击状态仍由TeamCtrl处理。</summary>
    /// <param name="hit">含原技能、结果序号和暴击的服务器结算。</param>
    private void OnMonsterPlayerHit(MonsterPlayerHitData hit)
    {
        if (instanceId != TeamData.Instance.DungeonInstanceId) return;
        if (hit.Damage <= 0) return;
        // 原目标角色身份与SceneWorld实体UID不同，沿正式角色索引查找。
        RoleEntity target = world.LocalRole;
        if (target == null || target.GetID() != hit.TargetRoleId)
        {
            if (!world.TryGetRole(hit.TargetRoleId, out target)) return;
        }
        // 命中效果归对应怪物宿主持有，副本离开和怪物回收会同时释放。
        MEntity caster;
        // 数字只依赖权威命中和受击者；施害者模型晚加载或已移除时仍能显示伤害。
        bool hasCaster = monsters.TryGetValue(hit.SpawnId, out caster);
        if (target.Model.IsLoaded && battleHudWord != null)
        {
            // 有施害者时沿真实攻击方向漂移，模型缺失时从受击者位置向上显示。
            Vector3 firerPosition = target.Position;
            if (hasCaster) firerPosition = caster.Position;
            battleHudWord.AddDraw(target, firerPosition, hit.Damage, hit.Critical);
        }
        if (!hasCaster || hit.SkillId == 0) return;
        // 原NPC也可能参与剧情技能，仍使用同一结算表现入口。
        MSceneNpcHoster npc = caster.GetEntityCtrl() as MSceneNpcHoster;
        if (npc != null) npc.PlayResult(hit, target);
        else ((MSceneMonsterHoster)caster.GetEntityCtrl()).PlayResult(hit, target);
    }

    /// <summary>将原HeadShow5表情挂在当前实例的既有模型，不建立影子实体。</summary>
    /// <param name="notice">已经筛选可见角色的AI通知。</param>
    private void OnHeadExpression(MonsterAiNoticeData notice)
    {
        if (instanceId != TeamData.Instance.DungeonInstanceId) return;
        // 表情来源的固定波次或动态召唤物身份。
        MEntity entity;
        if (!monsters.TryGetValue(notice.SourceSpawnId, out entity)) return;
        // NPC和怪物共用技能表现资源，保留各自原控制器。
        MSceneNpcHoster npc = entity.GetEntityCtrl() as MSceneNpcHoster;
        if (npc != null) npc.ShowHeadExpression();
        else ((MSceneMonsterHoster)entity.GetEntityCtrl()).ShowHeadExpression();
    }

    /// <summary>收到服务端命中时，直接更新该出生位已经显示的怪物。</summary>
    /// <param name="data">该出生位最近一次权威属性。</param>
    /// <param name="firerPosition">本次普攻施害者的服务器世界坐标，单位米。</param>
    private void OnMonsterAttrUpdate(MonsterHpData data, Vector3 firerPosition)
    {
        // 未装载的模型在绑定时读取 TeamData 中的最新血量。
        MEntity monster;
        if (instanceId != TeamData.Instance.DungeonInstanceId || !monsters.TryGetValue(data.SpawnId, out monster)) return;
        if (!ChangeHP(monster, data, true)) return;
        if (data.Damage > 0 && battleHudWord != null)
            battleHudWord.AddDraw((MonsterEntity)monster, firerPosition, data.Damage);
    }

    /// <summary>先应用生命，再沿已有的 DamageReceived / Death 状态播放动作。</summary>
    /// <param name="monster">出生位对应的正式怪物实体。</param>
    /// <param name="data">GameServer 已结算的剩余生命和上限。</param>
    /// <param name="hit">实时命中为真，首次绑定只还原当前存活或死亡姿势。</param>
    /// <returns>属性确实改变时为真，重复通知不重播动作或跳字。</returns>
    private static bool ChangeHP(MEntity monster, MonsterHpData data, bool hit)
    {
        // 原无生命机制和Dummy仅承载动作/特效，零上限不能派发死亡状态。
        if (data.MaxHp == 0 || !(monster is MonsterEntity)) return false;
        // 重复通知不能重播死亡；初次装载时 MaxHp 仍为零。
        if (monster.AttrComp.MaxHp == data.MaxHp && monster.AttrComp.CurrentHp == data.Hp) return false;
        monster.AttrComp.Apply(data.MaxHp, data.Hp, 0, 0);
        // 受击与死亡走实体动作机，先解除当前普攻专用的慢速播放倍率。
        if (hit || data.Hp == 0)
            ((MSceneMonsterHoster)monster.GetEntityCtrl()).EndAttackPlayback();
        if (data.Hp == 0) monster.DispatchEvent(MEventType.Death, null);
        else if (hit) monster.DispatchEvent(MEventType.DamageReceived, null);
        return true;
    }

    /// <summary>随场景卸载从实体世界注销全部副本角色，模型由实体模型组件归还。</summary>
    private void OnDestroy()
    {
        if (buffPlayer != null)
        {
            TeamData.Instance.MonsterBuffUpdated -= OnMonsterBuffUpdated;
            buffPlayer.Dispose();
        }
        TeamData.Instance.MonsterAttrUpdated -= OnMonsterAttrUpdate;
        TeamData.Instance.MonsterMotionUpdated -= OnMonsterMotion;
        TeamData.Instance.MonsterSpawned -= SpawnDynamic;
        TeamData.Instance.MonsterRemoved -= RemoveDynamic;
        TeamData.Instance.MonsterPlayerHit -= OnMonsterPlayerHit;
        TeamData.Instance.MonsterHeadExpression -= OnHeadExpression;
        TeamData.Instance.DungeonFlowUpdated -= ApplyFlow;
        TeamData.Instance.SetNearbyInteraction(null);
        if (battleHudWord != null) Destroy(battleHudWord.gameObject);
        monsters.Clear();
        if (world == null) return;
        for (int index = 0; index < entityUids.Count; index++) world.RemoveEntity(entityUids[index]);
        entityUids.Clear();
        Waves.Clear();
    }
}
