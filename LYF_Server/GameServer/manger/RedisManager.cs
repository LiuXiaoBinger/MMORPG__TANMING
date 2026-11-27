using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;

/// <summary>在后台读取跨 GS 角色缓存，回到游戏逻辑帧装载角色或向中心服查档。</summary>
public sealed class RedisManager
{
    /// <summary>进程内所有转入角色共用的逻辑帧入口。</summary>
    private static readonly RedisManager instance = new RedisManager();
    /// <summary>供原游戏固定帧入口及场景处理器取得同一个缓存管理器。</summary>
    public static RedisManager Instance { get { return instance; } }
    /// <summary>一次异步读取及其发起时的角色与中心服连接。</summary>
    private sealed class PendingRead
    {
        /// <summary>新 GS 为这次交接创建的在线角色对象。</summary>
        internal OnlineRole Role;
        /// <summary>未命中时发送 EnterWroldReq 的既有中心服链路。</summary>
        internal NetClient Center;
        /// <summary>完成后含本次指定版本的两个持久模块，缺失时为空。</summary>
        internal Task<SceneRoleSnapshot> Result;
    }
    /// <summary>只由游戏逻辑线程增删的在途缓存读取。</summary>
    private readonly List<PendingRead> pending = new List<PendingRead>();
    /// <summary>每台 GS 的单一 Redis 连接初始化任务，不阻塞启动和逻辑帧。</summary>
    private Task connecting;

    /// <summary>复用项目已使用的 Redis 客户端连接共享缓存。</summary>
    /// <param name="realm">本节点与中心服共用的 Redis 部署地址。</param>
    internal void Start(RealmTopology realm)
    {
        connecting = Task.Run(() => RedisMgr.Instance.Init(realm.RedisHost, realm.RedisPort));
    }

    /// <summary>按原 GS 已确认的保存版本查找角色数据。</summary>
    /// <param name="role">保留交接血蓝和冷却的目标角色。</param>
    /// <param name="center">缓存不可用时沿用的中心服连接。</param>
    internal void Load(OnlineRole role, NetClient center)
    {
        if (!role.IncomingScene.HasSavedVersion || connecting == null || connecting.Status != TaskStatus.RanToCompletion)
        {
            RequestCenter(role, center);
            return;
        }
        pending.Add(new PendingRead { Role = role, Center = center,
            Result = RedisMgr.Instance.ReadSceneRoleAsync((int)role.GetID(), role.IncomingScene.SavedVersion) });
    }

    /// <summary>消费已完成的 Redis 读取，角色和客户端回包继续由逻辑线程处理。</summary>
    /// <param name="deltaMilliseconds">沿用游戏固定帧入口的毫秒步长，网络完成由任务状态决定。</param>
    public void Update(int deltaMilliseconds)
    {
        // 倒序移除已完成项，保留其余正等待 Redis 的角色。
        for (int index = pending.Count - 1; index >= 0; index--)
        {
            // 一个角色的一次交接对应这项读取。
            PendingRead read = pending[index];
            if (!read.Result.IsCompleted) continue;
            pending.RemoveAt(index);
            // 异步返回期间可能已断线，会话所指对象必须仍是此次到达的角色。
            OnlineRole role = read.Role;
            if (RoleManager.Instance.GetOnlineRoleBySession(role.GateSessionId, role.UnitySessionId) != role) continue;
            // 两份回包已经同时通过缓存完整性、角色和存档版本核对。
            SceneRoleSnapshot snapshot = read.Result.GetAwaiter().GetResult();
            if (snapshot == null || !MatchesGridCounts(role.IncomingScene, snapshot.Inventory.RoleKanpsackInfo) ||
                !role.GetComponent<RoleSkillComponent>().LoadLearnedSkills(snapshot.Skills))
            {
                RequestCenter(role, read.Center);
                continue;
            }
            LogMsg.Info("跨 GS 角色缓存命中，角色=" + role.GetID() + "，版本=" + role.IncomingScene.SavedVersion);
            role.SendToClient(NetDefine.CMD_RoleSkillInfoCode, snapshot.Skills.ToByteString());
            role.SendToClient(NetDefine.CMD_RoleKnapsackInfoCode, snapshot.Inventory.ToByteString());
            RoleKnapsackInfoHandler.Apply(role, snapshot.Inventory);
        }
    }

    /// <summary>扩容通过独立协议写库，即使物品版本相同也要使用离场时的实际容量。</summary>
    /// <param name="transfer">原游戏服交出的各背包当前容量。</param>
    /// <param name="inventory">待采用的缓存背包和容量快照。</param>
    private static bool MatchesGridCounts(SceneRoleTransfer transfer, RoleKanpsackInfo inventory)
    {
        // 拆包前的缓存没有这两份容量，交给中心服迁移旧物品并重新生成完整快照。
        if (!inventory.KanpsackTypeCountLst.Any(row => row.Type == (int)KnapsackType.RolePackManual)) return false;
        if (!inventory.KanpsackTypeCountLst.Any(row => row.Type == (int)KnapsackType.RolePackGem)) return false;
        for (int index = 0; index < transfer.OpenedGrids.Count; index++)
        {
            // 以类型编号关联容量，中心服按数据库行顺序返回。
            Kanpsacktypecount expected = transfer.OpenedGrids[index];
            // 缺少该包裹的容量或旧值与离场值不同都应回源。
            Kanpsacktypecount cached = inventory.KanpsackTypeCountLst.FirstOrDefault(row => row.Type == expected.Type);
            if (cached?.Count != expected.Count) return false;
        }
        return true;
    }

    /// <summary>用原复合会话向中心服发出一次完整的技能和背包查询。</summary>
    /// <param name="role">正在目标 GS 等待恢复的在线角色。</param>
    /// <param name="center">该区真实的中心服会话。</param>
    private static void RequestCenter(OnlineRole role, NetClient center)
    {
        // 保留网关和客户端会话号，中心服原有的两个回包按相同路由返回。
        BasePackage route = new BasePackage { GateSessionId = role.GateSessionId, UnitySessionId = role.UnitySessionId };
        LogMsg.Info("跨 GS 角色查中心服，角色=" + role.GetID());
        center.Send(route, NetDefine.CMD_EnterWroldCode,
            new EnterWroldReq { RoleId = (int)role.GetID() }.ToByteString());
    }
}
