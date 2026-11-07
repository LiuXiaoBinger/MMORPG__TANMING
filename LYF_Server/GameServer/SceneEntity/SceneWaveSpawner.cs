using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;

/// <summary>对应 RO 的 SceneWaveSpawner；每个副本实例拥有独立的怪物实体世界和生命值。</summary>
public sealed class SceneWaveSpawner
{
    /// <summary>仅反序列化正式目录中的实体配置，模型动作列表由 Unity 使用。</summary>
    private sealed class CreatureCatalog
    {
        /// <summary>以原 RO 配置号标识的怪物和剧情实体。</summary>
        public ROSceneCreatureEntity[] entities { get; set; }
    }

    /// <summary>从正式场景 XML 展开的一个初始出生位，同一模板可供多个副本读取。</summary>
    private sealed class SpawnPoint
    {
        /// <summary>波次号和波内序号合成的跨端出生标识。</summary>
        public ulong SpawnId;
        /// <summary>世界坐标，单位米。</summary>
        public Vector3 Position;
        /// <summary>正式目录中的生命上限和战斗体积。</summary>
        public ROSceneCreatureEntity Creature;
        /// <summary>只有第18波 5515 的 StandBy 配置会在此保存场景巡逻线。</summary>
        public XElement[] OrcPatrol;
        /// <summary>原出生点的 Unity 绕 Y 轴角度。</summary>
        public float Facing;
    }

    /// <summary>配置只解析一次；每个队伍重新创建自己的怪物实例。</summary>
    private readonly List<SpawnPoint> spawn_points_;
    /// <summary>本实例选择的原DungeonsTable行，包含独立的场景和目标配置。</summary>
    public DungeonsTable Dungeon { get; private set; }
    /// <summary>按SceneID定位的正式场景XML，不借用其他副本资源。</summary>
    internal XDocument Config { get; private set; }
    /// <summary>当前副本私有的服务端实体容器。</summary>
    private readonly EntityWorld world_ = new EntityWorld();
    /// <summary>与出生点一一对应的真实服务端怪物，生命保存在实体上。</summary>
    private readonly List<GameServer.SceneEntity.MonsterEntity> monsters_ = new List<GameServer.SceneEntity.MonsterEntity>();
    /// <summary>本实例第18波的唯一行为树宿主。</summary>
    private MoshentaOrc orc;
    /// <summary>该兽人在本实例怪物表的下标，其他同模板怪物不会启动树。</summary>
    private int orcIndex = -1;
    /// <summary>实际创建实体对应的出生配置，随事件激活追加。</summary>
    private readonly List<SpawnPoint> activePoints = new List<SpawnPoint>();
    /// <summary>本实例独立的原场景图执行状态。</summary>
    public MoshentaSceneFlow Flow { get; private set; }

    /// <summary>对应 RO 的 FirstCreateMonster，只生成当前副本起始激活的可战斗波次。</summary>
    /// <param name="dungeonId">MS已批准的原副本配置号。</param>
    public SceneWaveSpawner(int dungeonId)
    {
        Dungeon = LubanMgr.Instance.GetDungeonsTableById(dungeonId);
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Dungeons." + Dungeon.SceneID + ".Waves.xml")) Config = XDocument.Load(source);
        spawn_points_ = LoadSpawnPoints();
        Flow = new MoshentaSceneFlow(this);
    }

    /// <summary>入场前核对所选表行对应的两份正式资源，防止把未移植场景传入XML加载器。</summary>
    /// <param name="dungeon">MS请求的原副本配置；未知配置为null。</param>
    public static bool HasSceneResources(DungeonsTable dungeon)
    {
        if (dungeon == null) return false;
        return typeof(SceneWaveSpawner).Assembly.GetManifestResourceInfo("Dungeons." + dungeon.SceneID + ".Waves.xml") != null
            && typeof(SceneWaveSpawner).Assembly.GetManifestResourceInfo("Dungeons." + dungeon.SceneID + ".Creatures.json") != null;
    }

    /// <summary>原波次条件已满足后创建对应实体，未激活的模板不会参与战斗。</summary>
    /// <param name="waveId">XML 波次配置号。</param>
    internal void ActivateWave(int waveId)
    {
        for (int index = 0; index < spawn_points_.Count; index++)
        {
            // 配置点与按服务端工厂装配的实体在整个实例内保持同一下标。
            SpawnPoint point = spawn_points_[index];
            if ((int)(point.SpawnId >> 32) != waveId) continue;
            activePoints.Add(point);
            GameServer.SceneEntity.MonsterEntity monster = (GameServer.SceneEntity.MonsterEntity)EntityFactory.Create(EntityType.Monster, point.Creature.id);
            monster.MapId = Dungeon.SceneID;
            monster.Transform.X = point.Position.X;
            monster.Transform.Y = point.Position.Y;
            monster.Transform.Z = point.Position.Z;
            monster.Transform.Yaw = point.Facing;
            monster.MaxHp = point.Creature.maxHp;
            monster.CurrentHp = monster.MaxHp;
            world_.Add(monster);
            monsters_.Add(monster);
            if (point.OrcPatrol != null)
            {
                orcIndex = monsters_.Count - 1;
                orc = new MoshentaOrc(monster, point.SpawnId, point.OrcPatrol);
            }
        }
    }

    /// <summary>按客户端相同的 XML 点位 count、波内顺序和 real_num 展开真实开场怪物。</summary>
    private List<SpawnPoint> LoadSpawnPoints()
    {
        // 资源直接嵌入同一份 Unity 正式配置，发布游戏服不依赖开发机项目路径。
        CreatureCatalog catalog;
        using (StreamReader reader = new StreamReader(typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Dungeons." + Dungeon.SceneID + ".Creatures.json")))
            catalog = new JavaScriptSerializer().Deserialize<CreatureCatalog>(reader.ReadToEnd());
        // 仅战斗怪物具备生命配置，NPC 即使和怪物编号相同也不进入索引。
        Dictionary<int, ROSceneCreatureEntity> creatures = catalog.entities.Where(row => row.maxHp > 0 && !row.isNpc && !row.nonCombatant).ToDictionary(row => row.id);
        // XML 序列化边界；波次条件和坐标均按原标签解析。
        XDocument scene = Config;
        // 当前副本的波次以及同一份场景文件里的巡逻线保持相同的编号体系。
        XElement[] waves = scene.Descendants("MSceneWaveData").ToArray();
        // 波内点位的展开次序与 MSceneDungeon.SpawnBatch 一致。
        List<SpawnPoint> result = new List<SpawnPoint>();
        for (int index = 0; index < waves.Length; index++)
        {
            // 被动机制、事件与隐藏波次不属于当前开场战斗。
            XElement wave = waves[index];
            XElement info = wave.Element("base_info");
            // 此处只展开模板，是否出生由每个实例的原波次条件决定。
            // 怪物模板与实际可创建数量来自同条波次。
            XElement monster = wave.Element("monster_info");
            ROSceneCreatureEntity creature;
            if (monster == null || !creatures.TryGetValue((int)monster.Element("entity_id"), out creature)) continue;
            // 实体模板 5515 另有三个出生波次，本次只接入18波选择的 StandBy 和 PingPong。
            XElement[] orcPatrol = null;
            if ((int)info.Element("id") == 18 && creature.id == 5515)
            {
                // 本波所选树的零基索引、巡逻方式和路径号均来自原 XML。
                XElement ai = monster.Element("monster_base_data");
                // 同一波的巡逻段同时指定路径号和往返方式。
                XElement patrol = ai.Element("patrol_data");
                // 使用该行实体配置的原树列表，以本波从零计数的下标取实际主树。
                int treeIndex = (int)ai.Element("ai_tree_choose");
                if (MoshentaOrcDefinition.Trees[treeIndex] == OrcStandByTree.MainName && (string)patrol.Element("patrol_type") == "ScenePatrolType_PingPong")
                {
                    // 此波引用的场景巡逻线 ID，从中取得有序点及各点停留时间。
                    int pathId = (int)patrol.Element("patrol_path_id");
                    orcPatrol = scene.Descendants("MScenePatrolPathData").First(path => (int)path.Element("id") == pathId)
                        .Descendants("MScenePatrolPointData").ToArray();
                }
            }
            int limit = (int?)monster.Element("monster_base_data").Element("entity_info").Element("real_num") ?? int.MaxValue;
            // 当前正式魔神塔的战斗波次按点列表排布。
            XElement[] points = monster.Descendants("MScenePositionData").ToArray();
            int ordinal = 0;
            for (int pointIndex = 0; pointIndex < points.Length && ordinal < limit; pointIndex++)
            {
                // 一个点可创建多只同模板怪物，每一只仍有自己的血量和出生标识。
                XElement point = points[pointIndex];
                XElement position = point.Element("position");
                int copies = (int?)point.Element("count") ?? 1;
                for (int copy = 0; copy < copies && ordinal < limit; copy++)
                {
                    ordinal++;
                    result.Add(new SpawnPoint { SpawnId = ((ulong)(uint)(int)info.Element("id") << 32) | (uint)ordinal,
                        Position = new Vector3((float)position.Element("x"), (float)position.Element("y"), (float)position.Element("z")),
                        Facing = (float?)point.Element("face") ?? 0f, Creature = creature, OrcPatrol = orcPatrol });
                }
            }
        }
        return result;
    }

    /// <summary>把本实例此刻的血量随入场通知发送；重复入场仍读取同一批实体。</summary>
    /// <param name="notice">将发给该队所有成员的入场协议。</param>
    public void FillEnterScene(TeamEnterDungeonNtf notice)
    {
        notice.Flow = Flow.Snapshot();
        for (int index = 0; index < monsters_.Count; index++) notice.Monsters.Add(GetHpData(index));
        if (orc != null) notice.MonsterActions.Add(orc.Snapshot());
    }

    /// <summary>队伍实例的逻辑帧推进兽人位置和技能结果。</summary>
    /// <param name="deltaMilliseconds">本帧经过的毫秒。</param>
    /// <param name="notice">本实例的同帧通知，只有产生变化才会发送。</param>
    public void Update(int deltaMilliseconds, MonsterAttrUpdateNtf notice)
    {
        // GeneComponent 在世界帧内写真实资源，保留帧前 HP 用于同帧网络变化和新死亡判定。
        int[] previousHp = new int[monsters_.Count];
        for (int i = 0; i < monsters_.Count; i++) previousHp[i] = monsters_[i].CurrentHp;
        // 正式怪物世界必须推进 GeneComponent，目标破甲等限时效果才能真实到期。
        world_.Update(deltaMilliseconds);
        // 整帧只推进一次死亡事件，不能把已经零血的怪物重复视作新击败。
        bool monsterDied = false;
        for (int i = 0; i < previousHp.Length; i++)
        {
            if (previousHp[i] == monsters_[i].CurrentHp) continue;
            // 周期变化后的实际资源差，治疗只同步 HP，不显示负伤害。
            int damage = Math.Max(0, previousHp[i] - monsters_[i].CurrentHp);
            notice.Monsters.Add(GetHpData(i, damage));
            // 归因使用施加角色编号，不能把装备 UID 用来查在线角色。
            OnlineRole caster = RoleManager.Instance.GetOnlineRole((int)monsters_[i].Components.Get<GeneComponent>(EntityComponentType.Gene).LastDamageFirerId);
            if (i == orcIndex) orc.OnHit(caster, damage, notice);
            if (previousHp[i] > 0 && monsters_[i].CurrentHp == 0)
            {
                monsterDied = true;
                Flow.CountMonsterDeath(activePoints[i].Creature.id);
                if (caster != null) caster.GetComponent<RoleEquipComponent>().Trigger(new GeneTriggerArgs(GeneEventType.Kill, null, 0, damage));
            }
        }
        if (monsterDied)
        {
            CheckCompletedWaves();
            Flow.MonsterDied();
        }
        if (orc != null) orc.Update(deltaMilliseconds, notice);
    }

    /// <summary>按 RO ChangeHP 顺序查询本实例存活怪物、扣血并生成同一批属性通知。</summary>
    /// <param name="notice">本次命中对应实例的下行包，只追加真正扣血的怪物。</param>
    /// <param name="origin">服务器已保存的角色当前位置，单位米。</param>
    /// <param name="forward">施法开始时确认的水平单位朝向，X、Y 分量对应世界 X、Z。</param>
    /// <param name="damage">已学普攻主表的每次伤害，单位生命点。</param>
    /// <param name="hit_targets">当前碰撞窗口已经命中的服务端实体编号。</param>
    public void ChangeHP(MonsterAttrUpdateNtf notice, Vector3 origin, Vector2 forward, int damage, HashSet<ulong> hit_targets)
    {
        ChangeHP(notice, origin, forward, damage, hit_targets, null);
    }

    /// <summary>按当前怪物坐标结算玩家普攻，只有本次实际扣血才沿用同一施害者产生仇恨。</summary>
    /// <param name="notice">目标副本的属性与动作通知。</param>
    /// <param name="origin">服务器玩家当前位置，米。</param>
    /// <param name="forward">玩家的水平单位朝向。</param>
    /// <param name="damage">本窗口的普攻伤害。</param>
    /// <param name="hit_targets">本窗口已受击的怪物运行时身份集合。</param>
    /// <param name="caster">同实例的在线攻击者，旧的无来源扣血流程允许空值。</param>
    internal void ChangeHP(MonsterAttrUpdateNtf notice, Vector3 origin, Vector2 forward, int damage, HashSet<ulong> hit_targets, OnlineRole caster)
    {
        // 同一攻击窗口共用施害位置，观察者按自己的镜头投影受击方向。
        notice.FirerX = origin.X;
        notice.FirerY = origin.Y;
        notice.FirerZ = origin.Z;
        // 本攻击窗口是否真实产生新死亡，避免非致命伤害和快照补发误触发场景目标。
        bool monsterDied = false;
        // 当前权威实体的零基下标，本轮结束后才允许清波生成新实体。
        for (int index = 0; index < monsters_.Count; index++)
        {
            // 实例内的权威实体，零血后不再参与随后窗口或其他队员的结算。
            GameServer.SceneEntity.MonsterEntity monster = monsters_[index];
            if (monster.CurrentHp <= 0 || hit_targets.Contains(monster.UID)) continue;
            // 当前普攻使用宽3、高2.5、深3米的前向盒，中心位于施法者前1.7米、高1.1米。
            SpawnPoint point = activePoints[index];
            // 怪物行走后，命中盒必须与服务端当前实体坐标比较，出生点只提供体积配置。
            Vector3 delta = new Vector3(monster.Transform.X, monster.Transform.Y, monster.Transform.Z) - origin;
            if (delta.Y > 2.35f || delta.Y + point.Creature.boundHeight < -0.15f) continue;
            // 在盒体局部水平面取离怪物中心最近的点，与怪物圆柱半径比较。
            float side = Math.Abs(delta.X * forward.Y - delta.Z * forward.X);
            float depth = Math.Abs(delta.X * forward.X + delta.Z * forward.Y - 1.7f);
            float dx = Math.Max(0f, side - 1.5f);
            float dz = Math.Max(0f, depth - 1.5f);
            if (dx * dx + dz * dz > point.Creature.boundRadius * point.Creature.boundRadius) continue;
            hit_targets.Add(monster.UID);
            // 命中的真实怪物持有自己的临时减防，角色装备容器属于在线角色组件。
            GeneRuntime targetGenes = monster.Components.Get<GeneComponent>(EntityComponentType.Gene).Runtime;
            // 当前攻击者的最终属性在结果点读取，动作中换装也不会使用旧的攻击面板。
            RoleEquipComponent equipment = null;
            // 保留旧的无来源调用伤害；真实角色普攻同时结算外功和内功对应防御。
            int calculatedDamage = damage;
            if (caster != null)
            {
                equipment = caster.GetComponent<RoleEquipComponent>();
                calculatedDamage = MonsterDefenseAttributes.Get(point.Creature.id).CalculateDamage(equipment.CalculateAttribute(), targetGenes, damage);
            }
            // 只报告这只怪本次实际失去的生命值，致死一击最多扣到零。
            int hpBeforeHit = monster.CurrentHp;
            monster.CurrentHp = Math.Max(0, hpBeforeHit - calculatedDamage);
            // 事件只能使用当前结算得到的实际伤害，零伤害不伪造有效攻击命中。
            int actualDamage = hpBeforeHit - monster.CurrentHp;
            if (equipment != null && actualDamage > 0)
            {
                targetGenes.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, equipment.Runtime, ROArtSkill.BasicAttackId, actualDamage));
                equipment.Trigger(new GeneTriggerArgs(GeneEventType.AttackHit, targetGenes, ROArtSkill.BasicAttackId, actualDamage));
                if (monster.CurrentHp == 0) equipment.Trigger(new GeneTriggerArgs(GeneEventType.Kill, targetGenes, ROArtSkill.BasicAttackId, actualDamage));
            }
            if (monster.CurrentHp == 0)
            {
                monsterDied = true;
                Flow.CountMonsterDeath(point.Creature.id);
            }
            notice.Monsters.Add(GetHpData(index, hpBeforeHit - monster.CurrentHp));
            if (index == orcIndex) orc.OnHit(caster, hpBeforeHit - monster.CurrentHp, notice);
        }
        // 本攻击窗口结束后再推进清波，避免创建新怪物时改变本次命中集合。
        CheckCompletedWaves();
        if (monsterDied) Flow.MonsterDied();
    }

    /// <summary>按已出生实体的实际生命判断清波，不能把尚未创建的波次视为完成。</summary>
    private void CheckCompletedWaves()
    {
        // 本窗口实际出现的波次与仍存活的波次。
        HashSet<int> spawned = new HashSet<int>();
        HashSet<int> living = new HashSet<int>();
        for (int index = 0; index < monsters_.Count; index++)
        {
            // 同波次多只实体必须全部死亡。
            int waveId = (int)(activePoints[index].SpawnId >> 32);
            spawned.Add(waveId);
            if (monsters_[index].CurrentHp > 0) living.Add(waveId);
        }
        // 集合快照允许完成事件继续产生新的波次。
        int[] waveIds = spawned.ToArray();
        for (int index = 0; index < waveIds.Length; index++)
            if (!living.Contains(waveIds[index])) Flow.Complete(waveIds[index]);
    }

    /// <summary>将变化后的完整波次、血量及动作放进同一条可靠通知。</summary>
    /// <param name="notice">所属实例的可靠通知。</param>
    internal void FillFlow(MonsterAttrUpdateNtf notice)
    {
        if (!Flow.Dirty) return;
        notice.Flow = Flow.Snapshot();
        for (int index = 0; index < monsters_.Count; index++)
        {
            // 本次已经包含的伤害条目保留实际扣血值，仅补全其他实体快照。
            ulong spawnId = activePoints[index].SpawnId;
            if (!notice.Monsters.Any(row => row.SpawnId == spawnId)) notice.Monsters.Add(GetHpData(index));
        }
        if (orc != null) notice.MonsterActions.Add(orc.Snapshot());
        Flow.Acknowledge();
    }

    /// <summary>查询本实例兽人是否仍对当前玩家保有有效攻击目标。</summary>
    /// <param name="role">正在交互的角色。</param>
    internal bool IsFighting(OnlineRole role)
    {
        return orc != null && orc.Target == role;
    }

    /// <summary>全队死亡后释放实体并重新建立原 XML 初始场景。</summary>
    internal void Reset()
    {
        Clear();
        orcIndex = -1;
        Flow = new MoshentaSceneFlow(this);
    }

    /// <summary>提取跨端出生位和权威实体当前生命值。</summary>
    /// <param name="index">当前实例内与出生配置一一对应的实体下标。</param>
    /// <param name="damage">本次实际扣血点数；完整入场快照沿用零值。</param>
    private MonsterHpData GetHpData(int index, int damage = 0)
    {
        return new MonsterHpData { SpawnId = activePoints[index].SpawnId, Hp = monsters_[index].CurrentHp, MaxHp = monsters_[index].MaxHp, Damage = damage };
    }

    /// <summary>最后一名队员离开时释放此实例中的实体和组件。</summary>
    public void Clear()
    {
        orc = null;
        if (Flow != null) Flow.Clear();
        world_.Clear();
        monsters_.Clear();
        activePoints.Clear();
    }
}
