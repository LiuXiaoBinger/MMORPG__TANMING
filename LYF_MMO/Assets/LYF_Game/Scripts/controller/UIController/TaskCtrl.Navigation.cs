using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/// <summary>沿角色已有的导航组件跑向任务人物、下一座公共传送门或副本阶段目标。</summary>
public partial class TaskCtrl
{
    /// <summary>当前点击的委托；用于换图后继续同一委托。</summary>
    private int _routeQuestId;
    /// <summary>当前正在走向的门，仅其目标地图确认加载后才继续寻路。</summary>
    private PublicCityPortal _routeDoor;
    /// <summary>这次点击正在等待人物模型或走到门口的协程，切换目标时停止。</summary>
    private Coroutine _walk;
    /// <summary>正在等待权威跨层结果的阶段，零表示本次点击只需步行。</summary>
    private int _dungeonTransferTask;

    /// <summary>从任务列表、快捷追踪和详情按钮进入同一条寻路流程。</summary>
    /// <param name="data">玩家点击时显示的角色委托或副本条目。</param>
    public void RequestNavigate(TaskViewData data)
    {
        CancelQuestRoute();
        if (data == null) return;
        // 已加载的本地角色拥有当前场景的唯一导航组件。
        PlayerEntity role = Global.Instance.SceneWorld?.LocalRole;
        if (role == null || !role.Model.IsLoaded) return;
        role.NavComp.Interrupt();
        if (data.IsDungeonObjective || (data.Quest != null && data.Quest.NpcId == 0))
        {
            // 副本的实体和机关只在当前实例存在时可作目的地。
            MSceneDungeon dungeon = Object.FindFirstObjectByType<MSceneDungeon>();
            if (dungeon == null)
            {
                TipsMgr.Instance.ShowSystemTips(data.Description);
                return;
            }
            // 进入实例后使用激活的阶段或真实存活的击杀对象。
            if (!dungeon.NavigateQuest(role, data, out MSceneInteraction via))
                TipsMgr.Instance.ShowSystemTips("当前目标尚未出现或没有可达路线");
            else if (via != null)
                _walk = _mainView.StartCoroutine(UseDungeonPortal(role, data.TaskId, via));
            return;
        }
        if (data.Quest == null) return;
        _routeQuestId = data.TaskId;
        _walk = _mainView.StartCoroutine(WalkToNpc(role, data.Quest.NpcId));
    }

    /// <summary>停止本次任务的跨图续走和等待，不干预角色手动移动。</summary>
    internal void CancelQuestRoute()
    {
        if (_walk != null && _mainView != null) _mainView.StopCoroutine(_walk);
        _walk = null;
        _routeDoor = null;
        _routeQuestId = 0;
        _dungeonTransferTask = 0;
    }

    /// <summary>走到已开放的副本门后复用原交互，阶段由服务器继续推进。</summary>
    /// <param name="role">本地角色。</param><param name="taskId">需要跨层的任务条目。</param>
    /// <param name="portal">本场景配置的实际前进传送门。</param>
    private IEnumerator UseDungeonPortal(PlayerEntity role, int taskId, MSceneInteraction portal)
    {
        while (!portal.Contains(role.Position))
        {
            if (!role.NavComp.IsNavigation || (role.NavComp.Destination - portal.Position).sqrMagnitude > 9f) yield break;
            yield return null;
        }
        _dungeonTransferTask = taskId;
        TeamCtrl.RequestDungeonInteraction(portal);
    }

    /// <summary>副本门使用成功并应用服务器落点后，重新定位本次点击的阶段。</summary>
    /// <param name="notice">只由本地角色自己的副本交互回执调用。</param>
    internal void OnDungeonQuestTransfer(DungeonInteractRet notice)
    {
        if (_dungeonTransferTask == 0) return;
        // 服务端拒绝或阶段已替换时，保留原交互提示，不重发传送。
        int id = _dungeonTransferTask;
        _dungeonTransferTask = 0;
        if (notice.Success) RequestNavigate(TaskMgr.Instance.GetTaskById(id));
    }

    /// <summary>公共地图加载、导航网和 NPC 管理器就绪后续走刚才的任务。</summary>
    internal void OnQuestMapLoaded()
    {
        if (_routeDoor == null) return;
        // 只跟随刚才走入的那条通路，其他来源的换图结束本次寻路。
        PlayerEntity role = Global.Instance.SceneWorld.LocalRole;
        if (_routeDoor.Destination.MapId != role.BaseInfo.MapId)
        {
            CancelQuestRoute();
            return;
        }
        // 重新取当前角色快照，按刚刚落地的地图选择下一站。
        TaskViewData current = TaskMgr.Instance.GetTaskById(_routeQuestId);
        RequestNavigate(current);
    }

    /// <summary>同图复用 NPC 点击交谈，不同图先走向能通往目标地图的最近入口。</summary>
    /// <param name="role">点击时的本地主角。</param><param name="npcId">当前任务阶段的人物配置号。</param>
    private IEnumerator WalkToNpc(PlayerEntity role, int npcId)
    {
        // 场景加载回调激活了业务根，下一帧 Start 会定位常驻角色至真实到达点。
        yield return null;
        // 目标地图直接使用出生表，包括零号主城和建筑室内。
        NpcParseInfo npc = LubanMgr.Instance.GetNpcInfoById(npcId);
        // 活动场景实际使用的公共门目录；副本不能通过旧城门离开。
        MainCityMgr city = MainCityMgr.Instance;
        if (npc == null || city == null) yield break;
        // 每座公共地图在现有目录中都有至少一道出口。
        IReadOnlyList<PublicCityPortal> doors = PublicCityPortal.InScene(city.gameObject.scene.name);
        if (doors.Count == 0)
        {
            TipsMgr.Instance.ShowSystemTips("请先离开副本，再前往任务人物");
            yield break;
        }
        if (doors[0].MapId == npc.MapID)
        {
            // 管理器先创建实体再异步装载模型，失败或卸载时 GetNpc 会移除原实体。
            NpcManager manager = city.GetComponent<NpcManager>();
            // 当前任务人物由本地图管理器持有，使用实时位置跟随正在移动的实体。
            NpcEntity target = manager.GetNpc(npcId);
            while (target != null && !target.Model.IsLoaded)
            {
                yield return null;
                if (manager == null) yield break;
                target = manager.GetNpc(npcId);
            }
            if (target == null) TipsMgr.Instance.ShowSystemTips("本场景没有可交谈的任务人物");
            else
            {
                // 任务抵达后可立即交谈，留出移动上报间隔内的步行距离。
                TouchObject.singleton.ClickNpc(target, NpcEntity.TalkDistance - 3f);
                if (!role.NavComp.IsNavigation && Vector3.Distance(role.Position, target.Position) > NpcEntity.TalkDistance)
                    TipsMgr.Instance.ShowSystemTips("当前没有通往任务人物的路线");
            }
            yield break;
        }

        // 候选门须能把角色送近目的地图；同等换图次数比较真实导航距离。
        PublicCityPortal door = NearestDoor(role.Position, doors, npc.MapID);
        if (door == null)
        {
            TipsMgr.Instance.ShowSystemTips("当前地图没有可达的任务传送点");
            yield break;
        }
        _routeDoor = door;
        // 门心与普通踩圈、服务型入口的服务端校验共用同一份坐标。
        Vector3 center = new Vector3(door.X, door.Y, door.Z);
        role.DispatchEvent(MEventType.MEvent_Navigation, MNavigationEventArgs.Create(center));
        // 马车和台座在原交谈距离内操作，避免要求角色穿入服务模型的碰撞体。
        float radius = 2.5f;
        if (door.UseService) radius = NpcEntity.TalkDistance;
        while (!door.Contains(role.Position.x, role.Position.y, role.Position.z, radius))
        {
            if (!role.NavComp.IsNavigation || (role.NavComp.Destination - center).sqrMagnitude > 9f)
            {
                _routeDoor = null;
                yield break;
            }
            yield return null;
        }
        // 普通门由场景踩圈提交一次；马车、台座到达后提交该门原有的服务请求。
        if (door.UseService)
        {
            role.NavComp.Interrupt();
            MNetClient.Instance.Send(SceneLineCode.Portal, new ScenePortalRequest { DoorId = door.DoorId }.ToByteString());
        }
    }

    /// <summary>反向遍历公共地图通路，避免选择近在身边但只能绕回原图的门。</summary>
    /// <param name="start">角色当前的世界坐标，单位米。</param><param name="doors">出发地图的所有入口。</param>
    /// <param name="targetMap">任务人物所在的公共地图编号。</param>
    internal static PublicCityPortal NearestDoor(Vector3 start, IReadOnlyList<PublicCityPortal> doors, int targetMap)
    {
        // 从目标地图计算每个公共地图还需几次传送。
        Dictionary<int, int> hops = new Dictionary<int, int> { { targetMap, 0 } };
        // 广度优先队列，每个地图只进入一次。
        Queue<int> pending = new Queue<int>();
        pending.Enqueue(targetMap);
        while (pending.Count > 0)
        {
            // 当前层的地图编号及其所有双向通路。
            int map = pending.Dequeue();
            IReadOnlyList<PublicCityPortal> exits = PublicCityPortal.InMap(map);
            // 枚举相邻地图，多个门通往同图也只记录一次。
            for (int i = 0; i < exits.Count; i++)
            {
                // 对端的地图由共享的门对配置给出。
                int next = exits[i].Destination.MapId;
                if (hops.ContainsKey(next)) continue;
                hops.Add(next, hops[map] + 1);
                pending.Enqueue(next);
            }
        }
        // 同一批候选共用路线缓冲区，距离包括从角色脚下到第一个转折点。
        List<Vector3> path = new List<Vector3>();
        // 最短可走距离及对应入口；无可达通路时返回空。
        float shortest = float.PositiveInfinity;
        PublicCityPortal best = null;
        // 所在地图没有连到目标的通路时，不尝试任意传送门。
        if (doors.Count == 0 || !hops.TryGetValue(doors[0].MapId, out int fromHops)) return null;
        // 每个门既要能缩短换图次数，又要能从角色脚下真实走到门圈。
        for (int i = 0; i < doors.Count; i++)
        {
            // 当前出发端的门，本地路径长度只对此端有意义。
            PublicCityPortal door = doors[i];
            if (hops[door.Destination.MapId] >= fromHops) continue;
            // 本场景导航返回的末点可能吸附到邻面，必须仍在该门范围内。
            Vector3 center = new Vector3(door.X, door.Y, door.Z);
            if (!MNavigationMgr.singleton.TryGetPath(start, center, path)) continue;
            // 实际步行路线终点，单位米。
            Vector3 end = path[path.Count - 1];
            if (!door.Contains(end.x, end.y, end.z, 2.5f)) continue;
            // 对同图多道门比较整段折线路程。
            float length = Vector3.Distance(start, path[0]);
            // 当前转折点下标，从第二个点开始累加相邻段长度。
            for (int j = 1; j < path.Count; j++) length += Vector3.Distance(path[j - 1], path[j]);
            if (length >= shortest) continue;
            shortest = length;
            best = door;
        }
        return best;
    }
}
