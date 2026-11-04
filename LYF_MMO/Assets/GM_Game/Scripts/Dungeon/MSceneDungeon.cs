using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>正式魔神塔的出生运行器：读取编辑器 XML，加入统一实体世界，并随副本卸载回收。</summary>
public class MSceneDungeon : MonoBehaviour
{
    /// <summary>当前项目剑气普攻的稳定 ID，对应服务端 ROArtSkill 的三段表现。</summary>
    public const int BasicAttackId = 10010;
    /// <summary>已加载的怪物按波次出生标识定位；与异步模型完成顺序无关。</summary>
    private readonly Dictionary<ulong, MonsterEntity> monsters = new Dictionary<ulong, MonsterEntity>();
    /// <summary>本场景开始时确认的服务端实例号。</summary>
    private ulong instanceId;
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

    /// <summary>正式场景加载成功后读取同一份编辑配置，首帧即可请求无前置波次。</summary>
    private void Start()
    {
        world = Global.Instance.SceneWorld;
        instanceId = TeamData.Instance.DungeonInstanceId;
        TeamData.Instance.MonsterAttrUpdated += OnMonsterAttrUpdate;
        if (UIRoot.Instance != null && instanceId != 0UL)
        {
            // 预制体保存十张原始数字 Sprite、材质和 RO 普攻曲线，显示在功能窗口下方。
            GameObject prefab = Resources.Load<GameObject>("Dungeons/Moshenta/Hurt_Normal_Attack");
            battleHudWord = Instantiate(prefab, UIRoot.Instance._canvas.transform, false).GetComponent<MBattleHudWordMgr>();
            battleHudWord.transform.SetAsFirstSibling();
        }
        catalog = ROSceneCreatureCatalog.Load();
        // Resources 中的原 XML 随编辑器保存更新，不另外烘焙出生点副本。
        TextAsset source = Resources.Load<TextAsset>("Dungeons/Moshenta/RO_DaNaTuoSiZhiZhan_EZ");
        // XML 是配置序列化边界；只枚举原波次对象，不使用画布显示节点计数。
        XElement[] records = XDocument.Parse(source.text).Descendants("MSceneWaveData").ToArray();
        for (int index = 0; index < records.Length; index++)
        {
            // 本次副本独有的波次状态。
            MSceneRuntimeWave wave = new MSceneRuntimeWave();
            wave.Data = records[index];
            wave.Id = ROSceneXml.Int(wave.Data, "base_info/id");
            Waves.Add(wave);
        }
    }

    /// <summary>按原延迟和间隔推进出生；血量与击杀前置不在本次纯表现范围内伪造满足。</summary>
    private void Update()
    {
        for (int index = 0; index < Waves.Count; index++)
        {
            // 按原文件顺序推进，出生点与顺序均可从编辑器定位。
            MSceneRuntimeWave wave = Waves[index];
            if (!wave.Requested && CanStart(wave)) Request(wave);
            if (wave.Requested && !wave.Spawned && Time.time >= wave.DueTime) SpawnBatch(wave);
        }
    }

    /// <summary>接受真实机制事件；WaitEvent/TriggerEvent 波次只在对应名字到达时创建。</summary>
    /// <param name="eventName">RO base_info/pre_scene_ai_event 保存的事件名。</param>
    public void SendEvent(string eventName)
    {
        for (int index = 0; index < Waves.Count; index++)
        {
            // 当前波次原前置类型，禁止通过事件绕过普通 And/Or 条件。
            MSceneRuntimeWave wave = Waves[index];
            string option = ROSceneXml.Text(wave.Data, "base_info/pre_wave_option");
            if (option != "PreWaveOptionTypeWaitEvent" && option != "PreWaveOptionTypeTriggerEvent") continue;
            if (ROSceneXml.Text(wave.Data, "base_info/pre_scene_ai_event") == eventName) Request(wave);
        }
    }

    /// <summary>判断无前置或纯出生前置；战斗前置仍等待真正的战斗结果接入。</summary>
    /// <param name="wave">尚未请求创建的原波次。</param>
    private bool CanStart(MSceneRuntimeWave wave)
    {
        if (ROSceneXml.Text(wave.Data, "base_info/not_active_create") == "true") return false;
        // RO 前置枚举保持原文本语义。
        string option = ROSceneXml.Text(wave.Data, "base_info/pre_wave_option");
        if (option == "PreWaveOptionTypeNone") return true;
        if (option != "PreWaveOptionTypeAnd" && option != "PreWaveOptionTypeOr") return false;
        // 前置阈值表示剩余数量/血量百分比，本次不扣血所以均保持百分之百。
        XElement[] previous = wave.Data.Element("base_info").Element("pre_wave_list").Elements().ToArray();
        bool any = false;
        for (int index = 0; index < previous.Length; index++)
        {
            // 按原波次编号寻找对应的出生完成状态。
            int previousId = ROSceneXml.Int(previous[index], "pre_wave_id");
            MSceneRuntimeWave dependency = Waves.Find(item => item.Id == previousId);
            bool fulfilled = dependency != null && dependency.Spawned;
            int countLimit = ROSceneXml.Int(previous[index], "pre_wave_num", -1);
            int healthLimit = ROSceneXml.Int(previous[index], "pre_wave_blood", -1);
            if (countLimit >= 0 && countLimit < 100) fulfilled = false;
            if (healthLimit >= 0 && healthLimit < 100) fulfilled = false;
            if (option == "PreWaveOptionTypeAnd" && !fulfilled) return false;
            any = any || fulfilled;
        }
        if (option == "PreWaveOptionTypeAnd") return true;
        return any;
    }

    /// <summary>从前置满足时刻计算原毫秒延迟，避免每帧重新开始倒计时。</summary>
    /// <param name="wave">已经满足前置条件的波次。</param>
    private void Request(MSceneRuntimeWave wave)
    {
        if (wave.Requested) return;
        wave.Requested = true;
        wave.DueTime = Time.time + ROSceneXml.Int(wave.Data, "base_info/delay_create_time") * 0.001f;
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
        // 采集与组事件没有角色模型；原空模型控制怪也不伪造可攻击对象。
        if (creature == null || creature.modelName == "Monster_Empty")
        {
            wave.Spawned = true;
            return;
        }
        // 点位 count 展开后再受 real_num 限制，NPC 单点同样遵循配置数量。
        XElement[] points = ROSceneXml.SpawnPoints(wave.Data);
        int total = 0;
        for (int index = 0; index < points.Length; index++) total += ROSceneXml.Int(points[index], "count", 1);
        total = Mathf.Clamp(ROSceneXml.Int(wave.Data, "monster_info/monster_base_data/entity_info/real_num", total), 0, total);
        // 间隔数据的 time 单位沿用 RO 秒数。
        XElement interval = ROSceneXml.Find(wave.Data, "base_info/interval_rush_data");
        int batchEnd = total;
        if (interval != null) batchEnd = Mathf.Min(total, wave.SpawnCount + Mathf.Max(1, ROSceneXml.Int(interval, "count", 1)));
        int ordinal = 0;
        for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
        {
            for (int copyIndex = 0; copyIndex < ROSceneXml.Int(points[pointIndex], "count", 1); copyIndex++)
            {
                if (ordinal >= wave.SpawnCount && ordinal < batchEnd) SpawnCreature(wave, creature, points[pointIndex], ordinal);
                ordinal++;
            }
        }
        wave.SpawnCount = batchEnd;
        wave.Spawned = batchEnd >= total;
        wave.DueTime = Time.time + ROSceneXml.Float(interval, "interval");
    }

    /// <summary>实例化正式资源后建立实体、模型、控制器的同一身份绑定。</summary>
    /// <param name="wave">原出生波次。</param>
    /// <param name="creature">原表对应的正式资源映射。</param>
    /// <param name="point">原场景位置和朝向，单位米/角度。</param>
    /// <param name="ordinal">按点位 count 展开的波内零基序号，在加载资源前即已确定。</param>
    private void SpawnCreature(MSceneRuntimeWave wave, ROSceneCreatureEntity creature, XElement point, int ordinal)
    {
        ResourceMgr.Instance.LoadPrefabAsync(creature.runtimePrefabPath, model =>
        {
            // 资源完成前副本已退出时，不将迟到模型加入新场景的世界。
            if (this == null)
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
            entity.Radius = Mathf.Max(prepared.ProjectSize.x, prepared.ProjectSize.z) * 0.5f * creature.scale;
            entity.Height = prepared.ProjectSize.y * creature.scale;
            if (!creature.isNpc && creature.maxHp > 0)
            {
                // 两端使用相同的出生序号关联实例属性，Unity 自身的实体 UID 仍交给 SceneWorld。
                ulong spawnId = ((ulong)(uint)wave.Id << 32) | (uint)(ordinal + 1);
                MonsterEntity monster = (MonsterEntity)entity;
                monsters.Add(spawnId, monster);
                MonsterHpData data;
                if (instanceId == TeamData.Instance.DungeonInstanceId && TeamData.Instance.GetMonsterAttr(spawnId, out data))
                    ChangeHP(monster, data, false);
            }
            // 原 invisible 波次只隐藏模型和判定体，不改写出生配置。
            if (ROSceneXml.Text(wave.Data, "base_info/init_invisible") == "true") model.SetActive(false);
        });
    }

    /// <summary>收到服务端命中时，直接更新该出生位已经显示的怪物。</summary>
    /// <param name="data">该出生位最近一次权威属性。</param>
    /// <param name="firerPosition">本次普攻施害者的服务器世界坐标，单位米。</param>
    private void OnMonsterAttrUpdate(MonsterHpData data, Vector3 firerPosition)
    {
        // 未装载的模型在绑定时读取 TeamData 中的最新血量。
        MonsterEntity monster;
        if (instanceId != TeamData.Instance.DungeonInstanceId || !monsters.TryGetValue(data.SpawnId, out monster)) return;
        if (!ChangeHP(monster, data, true)) return;
        if (data.Damage > 0 && battleHudWord != null)
            battleHudWord.AddDraw(monster, firerPosition, data.Damage);
    }

    /// <summary>先应用生命，再沿已有的 DamageReceived / Death 状态播放动作。</summary>
    /// <param name="monster">出生位对应的正式怪物实体。</param>
    /// <param name="data">GameServer 已结算的剩余生命和上限。</param>
    /// <param name="hit">实时命中为真，首次绑定只还原当前存活或死亡姿势。</param>
    /// <returns>属性确实改变时为真，重复通知不重播动作或跳字。</returns>
    private static bool ChangeHP(MonsterEntity monster, MonsterHpData data, bool hit)
    {
        // 重复通知不能重播死亡；初次装载时 MaxHp 仍为零。
        if (monster.AttrComp.MaxHp == data.MaxHp && monster.AttrComp.CurrentHp == data.Hp) return false;
        monster.AttrComp.Apply(data.MaxHp, data.Hp, 0, 0);
        if (data.Hp == 0) monster.DispatchEvent(MEventType.Death, null);
        else if (hit) monster.DispatchEvent(MEventType.DamageReceived, null);
        return true;
    }

    /// <summary>随场景卸载从实体世界注销全部副本角色，模型由实体模型组件归还。</summary>
    private void OnDestroy()
    {
        TeamData.Instance.MonsterAttrUpdated -= OnMonsterAttrUpdate;
        if (battleHudWord != null) Destroy(battleHudWord.gameObject);
        monsters.Clear();
        if (world == null) return;
        for (int index = 0; index < entityUids.Count; index++) world.RemoveEntity(entityUids[index]);
        entityUids.Clear();
        Waves.Clear();
    }
}
