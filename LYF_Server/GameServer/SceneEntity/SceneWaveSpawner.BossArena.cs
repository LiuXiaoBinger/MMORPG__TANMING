using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>同一副本内首领场地的待战、开战、胜利和团灭重置。</summary>
public sealed partial class SceneWaveSpawner
{
    /// <summary>场景图中共同组成首领战的出生事件，未配置场地时为空。</summary>
    internal string ArenaWaveEvent { get; private set; }
    /// <summary>首领入口的实体墙编号，负值表示当前场景没有首领场地。</summary>
    internal int ArenaWall { get; private set; } = -1;
    /// <summary>从墙外传入场地的单向入口，胜利时收起。</summary>
    internal int ArenaPortal { get; private set; }
    /// <summary>首领及其控制器、符文的固定波次，保留场景图顺序用于重生。</summary>
    private readonly List<int> arenaWaves = new List<int>();
    /// <summary>从首领首次实际失血到本次胜利或团灭期间为真。</summary>
    internal bool ArenaFighting { get; private set; }
    /// <summary>当前实例的首领已全部被击败，后续死亡不得重新生成首领奖励。</summary>
    internal bool ArenaCleared { get; private set; }

    /// <summary>从当前副本自己的图中取得墙、入口和首领出生波次。</summary>
    private void LoadBossArena()
    {
        // 未登记场地的副本继续使用自己的波次和重置规则。
        XElement arena = Config.Root.Element("data").Element("boss_arena");
        if (arena == null) return;
        ArenaWaveEvent = (string)arena.Attribute("wave_event");
        ArenaWall = (int)arena.Attribute("wall");
        ArenaPortal = (int)arena.Attribute("portal");
        // 同一个出生事件下的控制器与首领同时重置。
        XElement[] waves = Config.Descendants("MSceneWaveData").Select(row => row.Element("base_info")).ToArray();
        for (int index = 0; index < waves.Length; index++)
            if ((string)waves[index].Element("pre_scene_ai_event") == ArenaWaveEvent) arenaWaves.Add((int)waves[index].Element("id"));
    }

    /// <summary>判断固定波次是否属于本场首领战。</summary><param name="wave">场景图中的波次编号。</param>
    internal bool IsArenaWave(int wave) { return arenaWaves.Contains(wave); }

    /// <summary>在首领实际掉血的同一次结算里封路并启动原有战斗树。</summary>
    internal void StartBossFight()
    {
        if (ArenaFighting || ArenaCleared) return;
        ArenaFighting = true;
        Flow.SetArenaObjects(true, true);
        SceneEvent("BossEnterCombat");
    }

    /// <summary>真实死亡结算后，最后一只首领倒下即撤墙并收起单向入口。</summary>
    internal void CheckBossVictory()
    {
        if (!ArenaFighting) return;
        for (int index = 0; index < Actors.Count; index++)
            if (Actors[index].ArenaMember && Actors[index].IsBoss && !Actors[index].IsDead) return;
        ArenaCleared = true;
        ArenaFighting = false;
        Flow.SetArenaObjects(false, false);
    }

    /// <summary>只重置这次首领战，保留已清小怪、已解锁的地图机关以及任务阶段。</summary>
    /// <param name="retired">先发给成员的旧实体移除包，随后才用原出生号发送新首领。</param>
    internal void ResetBossFight(MonsterAttrUpdateNtf retired)
    {
        ArenaFighting = false;
        // 先结束旧时间轴的场景状态，保留前面区域的流程事件和任务。
        SceneEvent("boss_out_combat");
        string[] keys = { "BossState", "BossOpt", "HaterSkillState", "DespairSkillState", "SadSkillState", "DaNaTuoSiState" };
        for (int index = 0; index < keys.Length; index++) variables.Remove(keys[index]);
        // 删除首领和本场召唤的真实实体，旧技能、仇恨和待发事件随其释放。
        for (int index = Actors.Count - 1; index >= 0; index--)
        {
            MoshentaActor actor = Actors[index];
            if (!actor.ArenaMember) continue;
            scheduled.RemoveAll(item => item.Event.SenderId == actor.UnitId);
            sharedHp.Remove(actor.UnitId);
            RemoveActor(actor);
        }
        // 尸体可能已经消失，按固定出生配置清理本场击杀条件。
        for (int index = 0; index < spawn_points_.Count; index++)
            if (IsArenaWave((int)(spawn_points_[index].SpawnId >> 32))) corpseCounts.Remove(spawn_points_[index].Creature.id);
        MoshentaSkillRuntime.ClearScene(this);
        FlushAiNotice(retired);
        Flow.SetArenaObjects(false, true);
        for (int index = 0; index < arenaWaves.Count; index++)
        {
            Flow.RearmArenaWave(arenaWaves[index]);
            ActivateWave(arenaWaves[index]);
        }
    }
}
