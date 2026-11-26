using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

/// <summary>从当前实例的活动实体和场景配置定位任务，不在客户端推进目标次数。</summary>
public partial class MSceneDungeon
{
    /// <summary>查询点击目标的真实位置并派发现有角色导航，跨层时返回要使用的门户。</summary>
    /// <param name="role">本副本的本地角色。</param><param name="task">所点委托或当前服务端阶段。</param>
    /// <param name="via">无法直达时的可用跨层门；直接到目标时为空。</param>
    internal bool NavigateQuest(PlayerEntity role, TaskViewData task, out MSceneInteraction via)
    {
        via = null;
        if (document == null || instanceId != TeamData.Instance.DungeonInstanceId) return false;
        if (!task.IsDungeonObjective)
            return NavigateActor(role, LubanMgr.Instance.GetQuestInfoById(task.TaskId).TargetId, -1);
        // 完成后的同一行改指向真实的胜利出口，由玩家原有的退出按钮离开。
        DungeonFlowState flow = TeamData.Instance.DungeonFlow;
        if (flow == null) return false;
        if (flow.Finished) return NavigateArea(role, Resource.questExit, flow);
        if (task.State == TaskViewState.Finished) return false;
        // 有原生引导点的目标直接使用表中的世界坐标。
        DungeonTargetTable target = LubanMgr.Instance.GetDungeonTargetTableById(-task.TaskId);
        if (target.GuidePos.Length >= 3 && NavigatePoint(role, new Vector3(target.GuidePos[0], target.GuidePos[1], target.GuidePos[2]))) return true;
        // 没有引导点时，优先解释当前目标本身定义的原 NPC、怪物、采集、区域或清波编号。
        int type = (int)target.Target[0];
        if (type == 1 || type == 2)
        {
            if (NavigateActor(role, (int)target.Target[1], -1)) return true;
        }
        if (type == 5 && NavigateArea(role, (int)target.Target[2], flow)) return true;
        if (type == 8)
        {
            // 原表中从第二项起每个数字都是待清波次。
            for (int i = 1; i < target.Target.Length; i++)
                if (NavigateWave(role, (int)target.Target[i], flow)) return true;
        }
        if (type == 3)
        {
            // 采集类按当前活动波次中的采集配置号匹配。
            for (int i = 0; i < Waves.Count; i++)
                if (ROSceneXml.Int(Waves[i].Data, "collection_info/collection_id", -1) == (int)target.Target[1] &&
                    NavigateWave(role, Waves[i].Id, flow)) return true;
        }
        if (Resource.questGuides == null) return false;
        // AI 事件没有固有位置，场景登记只引用实际触发这些事件的原配置编号。
        for (int i = 0; i < Resource.questGuides.Length; i++)
        {
            // 当前点击的目标独立选择目标点，绝不取阶段中的第一条代替。
            DungeonQuestGuide guide = Resource.questGuides[i];
            if (guide.targetId != target.ID) continue;
            if (guide.waves != null)
            {
                // 采集已完成后同一目标可继续引到尚存活的召唤怪物。
                for (int j = 0; j < guide.waves.Length; j++)
                    if (NavigateWave(role, guide.waves[j], flow)) return true;
            }
            if (guide.area >= 0 && NavigateArea(role, guide.area, flow)) return true;
            if (guide.portals == null) return false;
            // 只有原服务端已激活且本层真的能走到的按钮可以用于换层。
            for (int j = 0; j < guide.portals.Length; j++)
            {
                // 运行中的入口与附近交互按钮共用同一个真实物件。
                MSceneInteraction portal = interactions.Find(item => item.WaveId == 0 && item.ObjectId == guide.portals[j]);
                if (portal == null || !NavigatePoint(role, portal.Position)) continue;
                via = portal;
                return true;
            }
        }
        return false;
    }

    /// <summary>在激活且未完成的波次内选择实有的采集物、NPC 或敌人。</summary>
    /// <param name="role">本地角色。</param><param name="waveId">编辑器原波次编号。</param><param name="flow">服务器当前阶段快照。</param>
    private bool NavigateWave(PlayerEntity role, int waveId, DungeonFlowState flow)
    {
        if (!flow.ActiveWaveIds.Contains(waveId) || flow.CompletedWaveIds.Contains(waveId)) return false;
        // 法阵等采集实体的位置与原副本交互按钮相同。
        MSceneInteraction collection = interactions.Find(item => item.WaveId == waveId);
        if (collection != null) return NavigatePoint(role, collection.Position);
        return NavigateActor(role, 0, waveId);
    }

    /// <summary>按实体模板或出生波次寻找仍存活的最近可达实体。</summary>
    /// <param name="role">用于比较距离的本地角色。</param><param name="configId">目标配置编号，零表示按波次。</param>
    /// <param name="waveId">场景波次编号，负值表示按配置号寻找。</param>
    private bool NavigateActor(PlayerEntity role, int configId, int waveId)
    {
        // 出生标识高 32 位保存波次，低 32 位区分同波的多个实体。
        List<ulong> keys = new List<ulong>(monsters.Keys);
        // 只展开当前符合身份的对象，按位置距离从近到远尝试导航。
        List<MEntity> candidates = new List<MEntity>();
        // 本次扫描的副本出生标识下标。
        for (int i = 0; i < keys.Count; i++)
        {
            // 运行时对象由当前实例世界持有，使用实时坐标而非怪物原出生点。
            MEntity entity = monsters[keys[i]];
            if (waveId >= 0 && (int)(keys[i] >> 32) != waveId) continue;
            if (configId != 0 && entity.ConfigId != configId) continue;
            if (!entity.Model.IsLoaded || entity.IsDead) continue;
            if (entity is MonsterEntity monster && monster.NonCombatant) continue;
            candidates.Add(entity);
        }
        candidates.Sort((left, right) => (left.Position - role.Position).sqrMagnitude.CompareTo((right.Position - role.Position).sqrMagnitude));
        // 从当前最近的存活对象开始，跳过本层不可达者。
        for (int i = 0; i < candidates.Count; i++)
        {
            // 对话 NPC 复用已有走近交谈，怪物只指引位置，战斗仍由玩家操作。
            MEntity entity = candidates[i];
            if (!NavigatePoint(role, entity.Position)) continue;
            if (entity is NpcEntity npc) TouchObject.singleton.ClickNpc(npc);
            return true;
        }
        return false;
    }

    /// <summary>按编辑 XML 中的原区域位置和偏移定位当前活动机关。</summary>
    /// <param name="role">本地角色。</param><param name="id">原场景物件编号，零也是合法编号。</param>
    /// <param name="flow">游戏服已经激活的区域集合。</param>
    private bool NavigateArea(PlayerEntity role, int id, DungeonFlowState flow)
    {
        if (id < 0 || !flow.ActiveObjectIds.Contains(id)) return false;
        // 触发区域也包含没有客户端可见模型的事件盒。
        XElement area = System.Linq.Enumerable.FirstOrDefault(document.Descendants("MSceneTriggerObjectData"),
            value => ROSceneXml.Int(value, "id") == id);
        if (area == null) return false;
        return NavigatePoint(role, ROSceneXml.Vector(area, "position") + ROSceneXml.Vector(area, "box/offset"));
    }

    /// <summary>预查本层完整路径，确保导航吸附的终点仍在目标附近。</summary>
    /// <param name="role">本地角色。</param><param name="point">目标脚下或机关盒心，单位米。</param>
    private static bool NavigatePoint(PlayerEntity role, Vector3 point)
    {
        // 当前场景共享导航器返回的完整路径，失败时继续尝试配置的跨层入口。
        List<Vector3> path = new List<Vector3>();
        if (!MNavigationMgr.singleton.TryGetPath(role.Position, point, path)) return false;
        // 过滤吸附到另一层或相邻不连通平台的末点。
        Vector3 end = path[path.Count - 1];
        if ((end - point).sqrMagnitude > 9f) return false;
        role.DispatchEvent(MEventType.MEvent_Navigation, MNavigationEventArgs.Create(end));
        return true;
    }
}
