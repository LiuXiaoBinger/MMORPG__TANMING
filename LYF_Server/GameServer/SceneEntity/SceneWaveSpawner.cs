using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using GameServer.SceneEntity;
using MMoRpgCommon.SceneEntity;

/// <summary>对应 RO 的 SceneWaveSpawner；每个魔神塔实例拥有独立的怪物实体世界和生命值。</summary>
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
    }

    /// <summary>配置只解析一次；每个队伍重新创建自己的怪物实例。</summary>
    private static readonly List<SpawnPoint> spawn_points_ = LoadSpawnPoints();
    /// <summary>当前副本私有的服务端实体容器。</summary>
    private readonly EntityWorld world_ = new EntityWorld();
    /// <summary>与出生点一一对应的真实服务端怪物，生命保存在实体上。</summary>
    private readonly List<GameServer.SceneEntity.MonsterEntity> monsters_ = new List<GameServer.SceneEntity.MonsterEntity>();

    /// <summary>对应 RO 的 FirstCreateMonster，只生成当前副本起始激活的可战斗波次。</summary>
    public SceneWaveSpawner()
    {
        for (int index = 0; index < spawn_points_.Count; index++)
        {
            // 配置点与按服务端工厂装配的实体在整个实例内保持同一下标。
            SpawnPoint point = spawn_points_[index];
            GameServer.SceneEntity.MonsterEntity monster = (GameServer.SceneEntity.MonsterEntity)EntityFactory.Create(EntityType.Monster, point.Creature.id);
            monster.MapId = TeamManager.MoshentaMapId;
            monster.Transform.X = point.Position.X;
            monster.Transform.Y = point.Position.Y;
            monster.Transform.Z = point.Position.Z;
            monster.MaxHp = point.Creature.maxHp;
            monster.CurrentHp = monster.MaxHp;
            world_.Add(monster);
            monsters_.Add(monster);
        }
    }

    /// <summary>按客户端相同的 XML 点位 count、波内顺序和 real_num 展开真实开场怪物。</summary>
    private static List<SpawnPoint> LoadSpawnPoints()
    {
        // 资源直接嵌入同一份 Unity 正式配置，发布游戏服不依赖开发机项目路径。
        CreatureCatalog catalog;
        using (StreamReader reader = new StreamReader(typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Moshenta.Creatures.json")))
            catalog = new JavaScriptSerializer().Deserialize<CreatureCatalog>(reader.ReadToEnd());
        // 仅战斗怪物具备生命配置，NPC 即使和怪物编号相同也不进入索引。
        Dictionary<int, ROSceneCreatureEntity> creatures = catalog.entities.Where(row => row.maxHp > 0 && !row.isNpc && !row.nonCombatant).ToDictionary(row => row.id);
        // XML 序列化边界；波次条件和坐标均按原标签解析。
        XElement[] waves;
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Moshenta.Waves.xml"))
            waves = XDocument.Load(source).Descendants("MSceneWaveData").ToArray();
        // 波内点位的展开次序与 MSceneDungeon.SpawnBatch 一致。
        List<SpawnPoint> result = new List<SpawnPoint>();
        for (int index = 0; index < waves.Length; index++)
        {
            // 被动机制、事件与隐藏波次不属于当前开场战斗。
            XElement wave = waves[index];
            XElement info = wave.Element("base_info");
            if ((string)info.Element("pre_wave_option") != "PreWaveOptionTypeNone" || (bool)info.Element("not_active_create") || (bool)info.Element("init_invisible")) continue;
            // 怪物模板与实际可创建数量来自同条波次。
            XElement monster = wave.Element("monster_info");
            ROSceneCreatureEntity creature;
            if (monster == null || !creatures.TryGetValue((int)monster.Element("entity_id"), out creature)) continue;
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
                        Position = new Vector3((float)position.Element("x"), (float)position.Element("y"), (float)position.Element("z")), Creature = creature });
                }
            }
        }
        return result;
    }

    /// <summary>把本实例此刻的血量随入场通知发送；重复入场仍读取同一批实体。</summary>
    /// <param name="notice">将发给该队所有成员的入场协议。</param>
    public void FillEnterScene(TeamEnterDungeonNtf notice)
    {
        for (int index = 0; index < monsters_.Count; index++) notice.Monsters.Add(GetHpData(index));
    }

    /// <summary>按 RO ChangeHP 顺序查询本实例存活怪物、扣血并生成同一批属性通知。</summary>
    /// <param name="notice">本次命中对应实例的下行包，只追加真正扣血的怪物。</param>
    /// <param name="origin">服务器已保存的角色当前位置，单位米。</param>
    /// <param name="forward">施法开始时确认的水平单位朝向，X、Y 分量对应世界 X、Z。</param>
    /// <param name="damage">已学普攻主表的每次伤害，单位生命点。</param>
    /// <param name="hit_targets">当前碰撞窗口已经命中的服务端实体编号。</param>
    public void ChangeHP(MonsterAttrUpdateNtf notice, Vector3 origin, Vector2 forward, int damage, HashSet<ulong> hit_targets)
    {
        for (int index = 0; index < monsters_.Count; index++)
        {
            // 实例内的权威实体，零血后不再参与随后窗口或其他队员的结算。
            GameServer.SceneEntity.MonsterEntity monster = monsters_[index];
            if (monster.CurrentHp <= 0 || hit_targets.Contains(monster.UID)) continue;
            // 当前普攻使用宽3、高2.5、深3米的前向盒，中心位于施法者前1.7米、高1.1米。
            SpawnPoint point = spawn_points_[index];
            Vector3 delta = point.Position - origin;
            if (delta.Y > 2.35f || delta.Y + point.Creature.boundHeight < -0.15f) continue;
            // 在盒体局部水平面取离怪物中心最近的点，与怪物圆柱半径比较。
            float side = Math.Abs(delta.X * forward.Y - delta.Z * forward.X);
            float depth = Math.Abs(delta.X * forward.X + delta.Z * forward.Y - 1.7f);
            float dx = Math.Max(0f, side - 1.5f);
            float dz = Math.Max(0f, depth - 1.5f);
            if (dx * dx + dz * dz > point.Creature.boundRadius * point.Creature.boundRadius) continue;
            hit_targets.Add(monster.UID);
            monster.CurrentHp = Math.Max(0, monster.CurrentHp - damage);
            notice.Monsters.Add(GetHpData(index));
        }
    }

    /// <summary>提取跨端出生位和权威实体当前生命值。</summary>
    /// <param name="index">当前实例内与出生配置一一对应的实体下标。</param>
    private MonsterHpData GetHpData(int index)
    {
        return new MonsterHpData { SpawnId = spawn_points_[index].SpawnId, Hp = monsters_[index].CurrentHp, MaxHp = monsters_[index].MaxHp };
    }

    /// <summary>最后一名队员离开时释放此实例中的实体和组件。</summary>
    public void Clear()
    {
        world_.Clear();
        monsters_.Clear();
    }
}
