using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using Google.Protobuf;

/// <summary>隔离 TCP 场景流程验收，不连接正在运行的游戏服务或数据库。</summary>
public static class MoshentaEventFlowSmoke
{
    /// <summary>原魔神塔入口的米制坐标期望，仅用于验收，业务按所选场景XML取出生点。</summary>
    private const string ExpectedMoshentaSpawnPos = "132.97_58.89_60.95";
    /// <summary>测试角色生命上限，避免巡逻测试时偶然全灭。</summary>
    private const int StartingHp = 100000000;
    /// <summary>成功断言数量。</summary>
    private static int passed;
    /// <summary>本轮协议证据输出目录。</summary>
    private static string folder;
    private sealed class Peer : IDisposable
    {
        /// <summary>读取游戏服正式下行数据的 TCP 对端。</summary>
        private readonly TcpClient client;
        /// <summary>以小端 uint16 长度头分帧读取协议。</summary>
        private readonly BinaryReader reader;
        /// <summary>角色路由回包时使用的真实网络会话。</summary>
        private readonly Session session;
        /// <summary>注册到当前隔离进程在线索引中的测试角色。</summary>
        public readonly OnlineRole Role;
        /// <summary>该观察者收到的最近一次正式入场快照。</summary>
        public TeamEnterDungeonNtf Entry;
        /// <summary>真实交互回包。</summary>
        public readonly List<DungeonInteractRet> Replies = new List<DungeonInteractRet>();
        /// <summary>本人及同实例观察者经真实回环 TCP 收到的复活结果。</summary>
        public readonly List<RoleReviveRet> Revives = new List<RoleReviveRet>();
        /// <summary>该观察者已收到、按传输顺序排列的怪物属性与行为通知。</summary>
        public readonly List<MonsterAttrUpdateNtf> Notices = new List<MonsterAttrUpdateNtf>();

        /// <summary>建立操作系统分配的本机端口，创建能参与正式队伍和副本逻辑的在线角色。</summary>
        /// <param name="id">本进程内唯一的玩家角色号。</param>
        public Peer(int id)
        {
            // 只在建连时监听，连接建立后立即释放监听端口。
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            client = new TcpClient();
            client.Connect((IPEndPoint)listener.LocalEndpoint);
            client.NoDelay = true;
            // 接受的 Socket 交给正式网关会话，发送和协议压缩走实际网络实现。
            Socket accepted = listener.AcceptSocket();
            accepted.NoDelay = true;
            listener.Stop();
            session = new Session(new Dictionary<int, IContainer>(), null);
            session.ReceiveData(accepted);
            client.ReceiveTimeout = 2000;
            reader = new BinaryReader(client.GetStream());
            Role = new OnlineRole();
            Role.mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo {
                RoleId = id, Level = 20, JobId = 1, Nickname = "兽人测试" + id,
                CurrHp = StartingHp, MaxHp = StartingHp, CurrMp = 500, MaxMp = 500, Pos = "0_0_0"
            }};
            Role.GateSessionId = session.SessionID;
            Role.UnitySessionId = id;
            Role.InitSerializes();
            Role.CompleteRoleDataLoad();
            RoleManager.Instance.AddOnlineRole(id, Role);
        }

        /// <summary>消费当前已写出的真实帧。超时或无完整包时不伪造网络结果。</summary>
        public void Receive()
        {
            // 服务端 Send 在返回前已提交给本机内核，短等待允许调度网卡收包。
            client.Client.Poll(10000, SelectMode.SelectRead);
            while (client.Available > 0)
            {
                // 帧体长度不含两个头字节，ParseData 仍要求传入完整头部。
                ushort length = reader.ReadUInt16();
                // 当前帧的协议数据由正式解压和 CRC 实现验证。
                byte[] body = reader.ReadBytes(length);
                if (body.Length != length) throw new EndOfStreamException();
                // 给网络解析器还原其期望的帧头和完整帧体。
                byte[] frame = new byte[length + 2];
                Buffer.BlockCopy(BitConverter.GetBytes(length), 0, frame, 0, 2);
                Buffer.BlockCopy(body, 0, frame, 2, length);
                // 一个 BasePackage 始终归属于对应角色的客户端会话。
                BasePackage packet = BasePackage.Parser.ParseFrom(NetUtils.Instance.ParseData(frame, length, 0));
                if (packet.UnitySessionId != Role.UnitySessionId) throw new Exception("包发给了错误的客户端会话");
                if (packet.ProtoCode == NetDefine.CMD_DungeonInteractResultCode) Replies.Add(DungeonInteractRet.Parser.ParseFrom(packet.Data));
                if (packet.ProtoCode == NetDefine.CMD_RoleReviveResultCode) Revives.Add(RoleReviveRet.Parser.ParseFrom(packet.Data));
                if (packet.ProtoCode == NetDefine.CMD_TeamEnterDungeonCode) Entry = TeamEnterDungeonNtf.Parser.ParseFrom(packet.Data);
                if (packet.ProtoCode == NetDefine.CMD_MonsterAttrUpdateCode) Notices.Add(MonsterAttrUpdateNtf.Parser.ParseFrom(packet.Data));
            }
        }

        /// <summary>关闭本机回环和测试进程里的在线角色，保持正式游戏服务、数据库及场景独立。</summary>
        public void Dispose()
        {
            RoleManager.Instance.RemoveOnlineRole(Role.mainRoleInfo.BaseInfo.RoleId);
            SessionMgr.Instance.RemoveSession(session.SessionID);
            session.Disconnect();
            reader.Dispose();
            client.Close();
        }
    }

    /// <summary>断言失败即终止隔离测试，不伪造成功回执。</summary>
    /// <param name="ok">实际结果。</param>
    /// <param name="name">验收项。</param>
    private static void Check(bool ok, string name)
    {
        if (!ok) { Console.WriteLine("FAIL " + name); throw new InvalidOperationException(name); }
        passed++;
        Console.WriteLine("PASS " + name);
    }

    /// <summary>从实例字典读取正式宿主，反射仅存在于测试。</summary>
    /// <param name="id">服务端实例号。</param>
    private static SceneWaveSpawner Scene(ulong id)
    {
        // 测试只读正式实例索引，可验证最后一名成员离开后的注销。
        IDictionary instances = (IDictionary)typeof(TeamManager).GetField("_instances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(TeamManager.Instance);
        if (!instances.Contains(id)) return null;
        return (SceneWaveSpawner)instances[id].GetType().GetField("WaveSpawner").GetValue(instances[id]);
    }

    /// <summary>读取实际入场快照。</summary>
    /// <param name="scene">独立场景实例。</param>
    private static TeamEnterDungeonNtf Snapshot(SceneWaveSpawner scene)
    {
        TeamEnterDungeonNtf result = new TeamEnterDungeonNtf();
        scene.FillEnterScene(result);
        return result;
    }

    /// <summary>核对完整快照只包含当前Section，以及该目标真实事件次数与完成标记。</summary>
    /// <param name="scene">独立测试副本的正式实体宿主。</param>
    /// <param name="id">当前阶段应引用的原DungeonTargetTable行号。</param>
    /// <param name="current">当前目标应收到的事件次数，默认尚未完成。</param>
    private static bool Objective(SceneWaveSpawner scene, int id, int current = 0)
    {
        // 每次从业务入口创建独立快照，禁止借测试修改内部进度。
        DungeonFlowState state = Snapshot(scene).Flow;
        if (state.Objectives.Count != 1) return false;
        // 4060每个Section只有一个特殊事件目标，总次数取正式表定义。
        DungeonObjectiveData target = state.Objectives[0];
        if (target.Total != (int)LubanMgr.Instance.GetDungeonTargetTableById(id).Target[2]) return false;
        return target.Id == id && target.Current == current && target.Completed == (current == target.Total);
    }

    /// <summary>通过测试专用反射注入原AI事件，验证提前和错误通知；不添加正式业务调试入口。</summary>
    /// <param name="scene">接收目标事件的独立副本。</param>
    /// <param name="id">事件int_arg中的目标表行号。</param>
    /// <param name="eventName">原AI事件名，默认使用正式特殊目标事件。</param>
    private static void TargetEvent(SceneWaveSpawner scene, int id, string eventName = "dungeonfinishtarget")
    {
        // object[]仅作为Reflection.Invoke强制要求的调用边界，不作为业务数据载荷。
        typeof(MoshentaSceneFlow).GetMethod("DungeonTargetEvent", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scene.Flow, new object[] { eventName, id });
    }

    /// <summary>通过正式移动组件进入配置区域。</summary>
    /// <param name="peer">当前玩家。</param>
    /// <param name="x">目标X米数。</param>
    /// <param name="y">目标Y米数。</param>
    /// <param name="z">目标Z米数。</param>
    private static void Move(Peer peer, float x, float y, float z)
    {
        peer.Role.GetComponent<ROMoveComponent>().OnMoveStart(new MoveOperationReq { Dx = x, Dy = y, Dz = z, Time = 0.05f, IsMoving = true });
    }

    /// <summary>把 protobuf 请求送进正式会话处理器，响应走真实 TCP。</summary>
    /// <param name="peer">请求玩家。</param>
    /// <param name="objectId">物件编号。</param>
    /// <param name="waveId">采集波次，零为物件按钮。</param>
    private static void Interact(Peer peer, int objectId, int waveId = 0)
    {
        BasePackage packet = new BasePackage { GateSessionId = peer.Role.GateSessionId, UnitySessionId = peer.Role.UnitySessionId,
            ProtoCode = NetDefine.CMD_DungeonInteractCode, Data = new DungeonInteractReq { ObjectId = objectId, WaveId = waveId }.ToByteString() };
        new DungeonInteractHandler().OnCall(new GameMessageHandlerContext(GameCommand.From(GameCommandSource.GateServer, null, packet)));
        peer.Receive();
    }

    /// <summary>只从原 XML 取测试波次位置，通过正式碰撞扣血完成清波。</summary>
    /// <param name="peer">攻击者。</param>
    /// <param name="waveId">目标波次号。</param>
    private static void Kill(Peer peer, int waveId)
    {
        // 测试直接读取同份正式资源，不给业务添加击杀调试入口。
        System.Xml.Linq.XDocument config;
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Dungeons.4060.Waves.xml")) config = System.Xml.Linq.XDocument.Load(source);
        System.Xml.Linq.XElement wave = config.Descendants("MSceneWaveData").Single(row => (int)row.Element("base_info").Element("id") == waveId);
        System.Xml.Linq.XElement[] positions = wave.Descendants("MScenePositionData").ToArray();
        for (int index = 0; index < positions.Length; index++)
        {
            System.Xml.Linq.XElement point = positions[index].Element("position");
            peer.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", (float)point.Element("x"), (float)point.Element("y"), (float)point.Element("z") - 1.7f);
            TeamManager.Instance.OnMonsterHit(peer.Role, new Vector2(0, 1), int.MaxValue, new HashSet<ulong>());
        }
    }

    /// <summary>比较首领动作与原出生点，容许协议里朝向另行同步。</summary>
    /// <param name="action">待核对的真实权威动作。</param><param name="spawn">入场时的原始动作与坐标。</param>
    private static bool IdleAtSpawn(MonsterActionData action, MonsterActionData spawn)
    {
        // 三个轴均为服务器米制位置。
        bool samePoint = action.X == spawn.X && action.Y == spawn.Y && action.Z == spawn.Z;
        return samePoint && action.Phase == MonsterActionPhase.Idle && action.TargetRoleId == 0;
    }

    /// <summary>用正式入场、伤害、交互和按帧死亡检测回放一次双人首领战。</summary>
    /// <param name="a">首击和重新拉起首领的玩家。</param><param name="b">从墙外进场的队友。</param>
    /// <param name="other">用于核对实例隔离的另一队玩家。</param><param name="late">战斗中进入的观察者。</param>
    private static void BossArena(Peer a, Peer b, Peer other, Peer late)
    {
        // 测试使用两个独立实例，所有角色数据都在本进程的回环会话内。
        MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = 100, InstanceId = 70001, DungeonId = 4060 };
        order.MemberIds.Add(new ulong[] { a.Role.GetID(), b.Role.GetID() });
        TeamManager.Instance.EnterDungeon(order);
        MasterTeamDungeonOrder separate = new MasterTeamDungeonOrder { TeamId = 200, InstanceId = 70002, DungeonId = 4060 };
        separate.MemberIds.Add(other.Role.GetID());
        TeamManager.Instance.EnterDungeon(separate);
        a.Receive(); b.Receive(); other.Receive();
        // 初始出生号和位置来自正式的服务器入场消息。
        SceneWaveSpawner scene = Scene(70001);
        ulong boss = (34UL << 32) | 1;
        TeamEnterDungeonNtf initial = Snapshot(scene);
        MonsterActionData idle = initial.MonsterActions.Single(row => row.SpawnId == boss);
        Check(initial.Monsters.Any(row => row.SpawnId == boss) && initial.Flow.ActiveObjectIds.Contains(36) &&
            !initial.Flow.ActiveWallIds.Contains(23), "开本首领已就位，墙外阵可用，空气墙未升起");
        File.WriteAllBytes(Path.Combine(folder, "arena-idle.bin"), initial.ToByteArray());
        // 保留一个前置区域的阶段进度，以辨别团灭是否误重置了整个副本。
        Move(a, 133.14f, 63.89f, 121.74f);
        Check(Objective(scene, 138), "前置目标137已完成");
        Move(a, 77.5f, 80.31f, 591.49f);
        Interact(b, 36);
        Check(!b.Replies.Last().Success, "远处无法操作场地入口");
        Move(b, 77.62f, 78.87f, 585.9f);
        Interact(b, 36);
        Check(b.Replies.Last().Success && b.Role.mainRoleInfo.BaseInfo.Pos == "77.5_80.31_591.49", "按钮按配置将玩家送入墙内");
        TeamManager.Instance.CommandEvent(a.Role, "BossEnterCombat");
        // 贴身、剧情对话和足以触发旧延时升墙的心跳均不能拉起首领。
        a.Role.mainRoleInfo.BaseInfo.Pos = "77.36_80.32_620.49";
        TeamManager.Instance.UpdateMonsters(12000);
        TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 0, new HashSet<ulong>());
        TeamEnterDungeonNtf before = Snapshot(scene);
        MonsterActionData still = before.MonsterActions.Single(row => row.SpawnId == boss);
        Check(!before.Flow.ActiveWallIds.Contains(23) && IdleAtSpawn(still, idle), "贴身、交谈和零伤害均不拉怪或升墙");
        // 用实际伤害入口开战，队友在同一广播中看到实体墙。
        TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 5000, new HashSet<ulong>());
        a.Receive(); b.Receive();
        TeamEnterDungeonNtf fighting = Snapshot(scene);
        Check(fighting.Monsters.Single(row => row.SpawnId == boss).Hp < before.Monsters.Single(row => row.SpawnId == boss).Hp &&
            fighting.Flow.ActiveWallIds.Contains(23) && b.Notices.Any(row => row.Flow != null && row.Flow.ActiveWallIds.Contains(23)), "首笔真实伤害同次升墙并向队友广播");
        File.WriteAllBytes(Path.Combine(folder, "arena-fighting.bin"), fighting.ToByteArray());
        Check(!Snapshot(Scene(70002)).Flow.ActiveWallIds.Contains(23), "另一队的首领仍在待机");
        order.MemberIds.Add(late.Role.GetID());
        TeamManager.Instance.EnterDungeon(order);
        late.Receive();
        Check(late.Entry.Flow.ActiveWallIds.Contains(23) && late.Entry.Flow.ActiveObjectIds.Contains(36), "中途入场同时恢复墙和入口");
        Move(late, 77.62f, 78.87f, 585.9f);
        Check(!TeamManager.Instance.AllowDungeonMove(late.Role, new Vector3(77.5f, 80.31f, 591.49f)), "空气墙拒绝队员直接穿行");
        Interact(late, 36);
        Check(late.Replies.Last().Success && late.Role.mainRoleInfo.BaseInfo.Pos == "77.5_80.31_591.49", "战斗中墙外队员仍可通过单向传送阵进场");
        TeamManager.Instance.UpdateMonsters(1000);
        Check(Snapshot(scene).MonsterActions.Any(row => row.SpawnId == boss && row.TargetRoleId != 0), "开战后首领对攻击者建立仇恨");
        a.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        TeamManager.Instance.UpdateMonsters(50);
        Check(Snapshot(scene).Flow.ActiveWallIds.Contains(23), "一人死亡不撤墙");
        b.Role.mainRoleInfo.BaseInfo.CurrHp = 0; late.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        TeamManager.Instance.UpdateMonsters(50);
        a.Receive(); b.Receive(); late.Receive();
        TeamEnterDungeonNtf reset = Snapshot(scene);
        MonsterActionData home = reset.MonsterActions.Single(row => row.SpawnId == boss);
        Check(!reset.Flow.ActiveWallIds.Contains(23) && reset.Flow.ActiveObjectIds.Contains(36) &&
            reset.Monsters.Single(row => row.SpawnId == boss).Hp == initial.Monsters.Single(row => row.SpawnId == boss).Hp,
            "全队阵亡撤墙，入口可用，首领满血");
        Check(IdleAtSpawn(home, idle) && reset.Flow.ActiveWaveIds.Contains(18) && !Objective(scene, 137), "首领回出生点清仇恨，前置波次和目标进度保留");
        Check(b.Notices.Any(row => row.RemovedSpawnIds.Contains(boss)) && b.Notices.Last().Flow.Equals(reset.Flow), "队友先移除旧实体再收到重置快照");
        File.WriteAllBytes(Path.Combine(folder, "arena-reset.bin"), reset.ToByteArray());
        a.Role.mainRoleInfo.BaseInfo.CurrHp = StartingHp;
        TeamManager.Instance.UpdateMonsters(12000);
        Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23), "复活后旧计时器不会自行升墙");
        a.Role.mainRoleInfo.BaseInfo.Pos = "77.36_80.32_620.49";
        TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 5000, new HashSet<ulong>());
        Check(Snapshot(scene).Flow.ActiveWallIds.Contains(23), "重新伤害首领可二次开战");
        Kill(a, 34); Kill(a, 35); Kill(a, 46);
        Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23) && !Snapshot(scene).Flow.ActiveObjectIds.Contains(36),
            "击败全部首领后撤墙并收起战斗入口");
        // 原结算 NPC 的技能时间轴在演出后打开通关出口，期间只推进这个实例。
        for (int tick = 0; tick < 300; tick++) scene.Update(50, new MonsterAttrUpdateNtf());
        TeamEnterDungeonNtf victory = Snapshot(scene);
        Check(victory.Flow.ActiveObjectIds.Contains(35) && victory.Flow.Finished, "战后原通关出口和胜利结算仍可用");
    }

    /// <summary>通过正式复活处理器发送空请求，只返回本次收到的本人回包。</summary>
    /// <param name="peer">点击复活按钮的角色连接。</param>
    private static RoleReviveRet Revive(Peer peer)
    {
        // 请求前收到的帧数，用于区分历史广播与本次结果。
        int previous = peer.Revives.Count;
        // 正式空请求仅通过网关和客户端会话绑定角色。
        BasePackage packet = new BasePackage { GateSessionId = peer.Role.GateSessionId, UnitySessionId = peer.Role.UnitySessionId,
            ProtoCode = NetDefine.CMD_RoleReviveCode, Data = new RoleReviveReq().ToByteString() };
        new RoleReviveHandler().OnCall(new GameMessageHandlerContext(GameCommand.From(GameCommandSource.GateServer, null, packet)));
        peer.Receive();
        if (peer.Revives.Count <= previous) throw new InvalidOperationException("没有收到本次复活回包");
        return peer.Revives.Last();
    }

    /// <summary>验证三种首领坐标、玩家与队伍记录、团灭后重试，以及北森两个独立场景。</summary>
    /// <param name="a">攻击憎恨的队长。</param><param name="b">前往绝望区域的队员。</param>
    /// <param name="other">隔离实例中的角色。</param><param name="late">晚到后前往悲哀区域的队员。</param>
    private static void BossRevives(Peer a, Peer b, Peer other, Peer late)
    {
        // 同场三名首领在门外的独立落点，期望值直接与最终 11244 回包比对。
        const string hatred = "77.62_78.376_585.9";
        const string despair = "79.4_78.379_585.9";
        const string sorrow = "76.25_78.277_585.9";
        // 玩家经同队与另一实例两张正式 GS 入场单进场。
        MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = 301, InstanceId = 73001, DungeonId = 4060 };
        order.MemberIds.Add(new ulong[] { a.Role.GetID(), b.Role.GetID() }); TeamManager.Instance.EnterDungeon(order);
        MasterTeamDungeonOrder separate = new MasterTeamDungeonOrder { TeamId = 302, InstanceId = 73002, DungeonId = 4060 };
        separate.MemberIds.Add(other.Role.GetID()); TeamManager.Instance.EnterDungeon(separate);
        a.Receive(); b.Receive(); other.Receive();
        Check(a.Entry.Pos == ExpectedMoshentaSpawnPos && b.Entry.Pos == a.Entry.Pos, "首次进本仍使用副本入口");
        Check(!Revive(a).Success, "存活角色不能使用复活传送");
        a.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        Check(Revive(a).BaseInfo.Pos == ExpectedMoshentaSpawnPos, "未到首领区也未开战时在入口复活");
        Move(a, 77.36f, 80.32f, 620.49f);
        Move(b, 85.82f, 80.32f, 617.15f);
        TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 5000, new HashSet<ulong>());
        b.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        TeamManager.Instance.UpdateMonsters(50);
        // 验证由服务端决定生命、法力和可站立的坐标。
        RoleReviveRet right = Revive(b);
        Check(right.Success && right.BaseInfo.Pos == despair && Snapshot(Scene(73001)).Flow.ActiveWallIds.Contains(23),
            "战斗中阵亡回绝望复活点，空气墙继续生效");
        Check(right.BaseInfo.CurrHp == right.BaseInfo.MaxHp && right.BaseInfo.CurrMp == right.BaseInfo.MaxMp, "复活照常恢复生命与法力");
        a.Receive(); other.Receive();
        Check(a.Revives.Last().Equals(right) && other.Revives.Count == 0, "同队观察者收到相同坐标，其他实例没有复活广播");
        Check(!TeamManager.Instance.AllowDungeonMove(b.Role, new Vector3(77.5f, 80.31f, 591.49f)), "复活点位于空气墙外");
        Interact(b, 36);
        Check(b.Replies.Last().Success && b.Role.mainRoleInfo.BaseInfo.Pos == "77.5_80.31_591.49", "复活后可以用传送阵回到墙内");
        Move(b, 85.82f, 80.32f, 617.15f);
        order.MemberIds.Add(late.Role.GetID()); TeamManager.Instance.EnterDungeon(order); late.Receive();
        late.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        Check(Revive(late).BaseInfo.Pos == hatred, "无个人记录的队员使用队伍交战首领的复活点");
        Interact(late, 36); Move(late, 69.27f, 80.32f, 617.45f);
        a.Role.mainRoleInfo.BaseInfo.CurrHp = 0; b.Role.mainRoleInfo.BaseInfo.CurrHp = 0; late.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        TeamManager.Instance.UpdateMonsters(50);
        Check(!Snapshot(Scene(73001)).Flow.ActiveWallIds.Contains(23) && late.Role.mainRoleInfo.BaseInfo.Pos == "69.27_80.32_617.45",
            "团灭只重置首领，角色尸体等待本人复活");
        // 团灭后各人分别收到自己的落点，服务器不会用最后一次复活覆盖其他成员。
        RoleReviveRet center = Revive(a);
        right = Revive(b);
        RoleReviveRet left = Revive(late);
        Check(center.BaseInfo.Pos == hatred && right.BaseInfo.Pos == despair && left.BaseInfo.Pos == sorrow, "团灭后三名队员回三个不同的首领复活点");
        File.WriteAllBytes(Path.Combine(folder, "revive-hatred.bin"), center.ToByteArray());
        File.WriteAllBytes(Path.Combine(folder, "revive-despair.bin"), right.ToByteArray());
        File.WriteAllBytes(Path.Combine(folder, "revive-sorrow.bin"), left.ToByteArray());
        other.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
        Check(Revive(other).BaseInfo.Pos == ExpectedMoshentaSpawnPos, "另一支队伍不会继承首领复活点");
        // 用正式技能强制位移入口到达北森首领附近，再离开区域并团灭。
        MethodInfo relocate = typeof(TeamManager).GetMethod("ForceDungeonPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int map = 4050; map <= 4051; map++)
        {
            // 同一角色切入全新实例应重新开始积累复活点。
            MasterTeamDungeonOrder forest = new MasterTeamDungeonOrder { TeamId = 302, InstanceId = (ulong)(74000 + map), DungeonId = map };
            forest.MemberIds.Add(other.Role.GetID()); TeamManager.Instance.EnterDungeon(forest); other.Receive();
            other.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
            Check(Revive(other).BaseInfo.Pos == "193.09_22.34_42.05", map + " 新实例使用自己的入口");
            // object[] 仅用于调用非公开正式位移方法的反射边界。
            relocate.Invoke(TeamManager.Instance, new object[] { other.Role, new Vector3(123.59f, 22.38f, 247.72f) });
            relocate.Invoke(TeamManager.Instance, new object[] { other.Role, new Vector3(193.09f, 22.34f, 42.05f) });
            other.Role.mainRoleInfo.BaseInfo.CurrHp = 0; TeamManager.Instance.UpdateMonsters(50);
            Check(Revive(other).BaseInfo.Pos == "151.53_22.4_240.31", map + " 到达凯思利亚斯后离开场地和团灭均保留记录");
        }
    }

    /// <summary>隔离测试主入口，输出目录由测试启动器明确提供。</summary>
    /// <param name="args">临时证据目录。</param>
    public static int Main(string[] args)
    {
        folder = args[0];
        LubanMgr.Instance.Init();
        using (Peer a = new Peer(985001))
        using (Peer b = new Peer(985002))
        using (Peer other = new Peer(985003))
        using (Peer late = new Peer(985004))
        {
            if (args.Length > 1 && args[1] == "boss-revive")
            {
                BossRevives(a, b, other, late);
                return 0;
            }
            if (args.Length > 1 && args[1] == "boss-arena")
            {
                BossArena(a, b, other, late);
                return 0;
            }
            // 模拟 MS 已批准的入场单，GS 内使用正式实例创建流程。
            MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = 100, InstanceId = 70001, DungeonId = 4060 };
            order.MemberIds.Add(new ulong[] { a.Role.GetID(), b.Role.GetID() });
            TeamManager.Instance.EnterDungeon(order);
            MasterTeamDungeonOrder separate = new MasterTeamDungeonOrder { TeamId = 200, InstanceId = 70002, DungeonId = 4060 };
            separate.MemberIds.Add(other.Role.GetID());
            TeamManager.Instance.EnterDungeon(separate);
            a.Receive(); b.Receive(); other.Receive();
            SceneWaveSpawner scene = Scene(70001);
            Check(a.Entry.Flow.Equals(b.Entry.Flow) && a.Entry.Monsters.Any(row => row.SpawnId == ((34UL << 32) | 1)) &&
                !a.Entry.Flow.ActiveWallIds.Contains(23), "同队入场可见待机首领，墙尚未升起");
            Check(scene.Flow.SpawnPosition == new Vector3(132.97f, 58.89f, 60.95f) && ExpectedMoshentaSpawnPos == a.Entry.Pos, "单一XML入口与协议位置一致");
            Check(Objective(scene, 137), "初始只同步原表137且进度为0");
            Check(a.Entry.Flow.Objectives[0].Description == LubanMgr.Instance.GetDungeonTargetTableById(137).TargetDes, "目标文案直接来自正式Luban表");
            TargetEvent(scene, 138);
            TargetEvent(scene, 140);
            TargetEvent(scene, 142);
            TargetEvent(scene, 143);
            Check(Objective(scene, 137), "提前目标事件不推进或预完成后续Section");
            TargetEvent(scene, 999999);
            TargetEvent(scene, 137, "错误事件名");
            Check(Objective(scene, 137), "错误目标ID和事件名不能推进当前Section");
            File.WriteAllBytes(Path.Combine(folder, "wire-entry.bin"), a.Entry.ToByteArray());
            Interact(a, 5);
            Check(!a.Replies.Last().Success && a.Role.mainRoleInfo.BaseInfo.Pos == a.Entry.Pos, "未解锁门户拒绝且位置不变");
            Move(a, 133.14f, 63.89f, 121.74f);
            a.Receive(); b.Receive(); other.Receive();
            Check(Snapshot(scene).Monsters.Any(row => row.SpawnId == ((18UL << 32) | 1)), "前线区域激活第18波");
            Check(Objective(scene, 138), "原区域18完成137并进入138且不补提前事件");
            TargetEvent(scene, 137);
            Check(Objective(scene, 138), "已完成137重复事件不会跨阶段推进");
            int count = Snapshot(scene).Monsters.Count;
            Move(a, 133.14f, 63.89f, 121.74f);
            Check(Snapshot(scene).Monsters.Count == count, "重复区域上报不重复生成");
            Check(a.Notices.SequenceEqual(b.Notices) && other.Notices.Count == 0, "事件快照同队一致且不串实例");
            File.WriteAllBytes(Path.Combine(folder, "wire-frontline.bin"), a.Notices.Last().ToByteArray());
            Move(a, 116.1f, 63.89f, 179.02f);
            Check(Objective(scene, 139), "原区域19完成138并进入139");
            // 保留原18波巡逻与当前位置命中结算。
            for (int frame = 0; frame < 100; frame++) TeamManager.Instance.UpdateMonsters(50);
            // 同一实例同时存在待机首领和场景 NPC，按第 18 波自己的出生号查巡逻者。
            MonsterActionData orc = Snapshot(scene).MonsterActions.Single(row => row.SpawnId == ((18UL << 32) | 1));
            Check(orc.Phase == MonsterActionPhase.Move, "事件生成后第18波仍沿原行为树巡逻");
            a.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", orc.X, orc.Y, orc.Z - 1.7f);
            // 以本次入场的实际生命核对伤害，沿用当前难度的怪物属性。
            int orcHp = Snapshot(scene).Monsters.Single(row => row.SpawnId == orc.SpawnId).Hp;
            TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 5000, new HashSet<ulong>());
            Check(Snapshot(scene).Monsters.Single(row => row.SpawnId == orc.SpawnId).Hp < orcHp, "第18波移动后按当前坐标命中");
            // 让原行为树在下一次心跳选择刚刚造成伤害的攻击者。
            for (int frame = 0; frame < 20; frame++) TeamManager.Instance.UpdateMonsters(50);
            // 门0带原战斗限制，兽人仍锁定攻击者时必须拒绝。
            a.Role.mainRoleInfo.BaseInfo.Pos = "35.69_72.06_212.74";
            Interact(a, 0);
            Check(!a.Replies.Last().Success, "门0处于兽人战斗时拒绝传送");
            b.Role.mainRoleInfo.BaseInfo.Pos = "35.69_72.06_212.74";
            Interact(b, 0);
            Check(b.Replies.Last().Success && b.Role.mainRoleInfo.BaseInfo.Pos != "35.69_72.06_212.74", "门0脱战玩家使用原配置目标传送");
            a.Role.mainRoleInfo.BaseInfo.Pos = "55.3_65.71_195.14";
            Interact(a, 1);
            Check(!a.Replies.Last().Success, "门1处于兽人战斗时拒绝传送");
            b.Role.mainRoleInfo.BaseInfo.Pos = "55.3_65.71_195.14";
            Interact(b, 1);
            Check(b.Replies.Last().Success && b.Role.mainRoleInfo.BaseInfo.Pos == "75.3_65.9_106", "门1脱战请求采用原配置目标");
            Move(a, 44.57f, 72.06f, 212.91f);
            Check(Snapshot(scene).Flow.ActiveWaveIds.Contains(41), "二层区域解锁法阵采集波次");
            Check(Objective(scene, 140), "原区域20完成139并进入140且不补提前事件");
            Interact(a, 0, 41);
            Check(!a.Replies.Last().Success, "远距离法阵采集被拒绝");
            Move(a, 80.68f, 72.06f, 244.38f);
            Check(Objective(scene, 141), "原区域8完成140并进入141");
            Move(a, 80.76f, 72.06f, 289.66f);
            Interact(a, 0, 41);
            Check(a.Replies.Last().Success && Snapshot(scene).Flow.ActiveWaveIds.Contains(42), "真实采集请求完成41并召唤42");
            Check(Objective(scene, 141), "采集41仅召唤42且不完成原目标141");
            Kill(a, 42);
            Check(Snapshot(scene).Flow.CompletedWaveIds.Contains(62) && Snapshot(scene).Flow.ActiveObjectIds.Contains(5), "42真实死亡自动执行62并创建上塔门户5");
            Check(Objective(scene, 142), "42真实全灭完成141并进入142且不补提前事件");
            Move(a, 108.99f, 72.06f, 274.9f);
            Interact(a, 5);
            Check(a.Replies.Last().Success && a.Role.mainRoleInfo.BaseInfo.Pos == "107.01_62_612.23", "上塔传送采用XML目标且服务端位置已更新");
            // 原ResetDungeons仅刷新Boss AI/采集视层，不允许玩家进入区域25就重刷全本。
            Move(a, 77.62f, 78.87f, 586.55f);
            Check(Snapshot(scene).Flow.CompletedWaveIds.Contains(42), "原区域25不会把已清波次误重置");
            Move(a, 77.5f, 80.31f, 591.49f);
            Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23), "走进首领区域不升墙");
            a.Role.mainRoleInfo.BaseInfo.Pos = "77.36_80.32_620.49";
            TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 5000, new HashSet<ulong>());
            Check(Snapshot(scene).Flow.ActiveWallIds.Contains(23), "首领实际失血创建权威空气墙23");
            Check(Objective(scene, 143), "原区域21完成142并进入143且不补提前事件");
            a.Receive(); b.Receive();
            Check(a.Notices.Last().Flow.Equals(b.Notices.Last().Flow), "空气墙及Boss目标同步所有同实例队员");
            File.WriteAllBytes(Path.Combine(folder, "wire-boss.bin"), a.Notices.Last().ToByteArray());
            order.MemberIds.Add(late.Role.GetID());
            string before = a.Role.mainRoleInfo.BaseInfo.Pos;
            TeamManager.Instance.EnterDungeon(order);
            late.Receive(); a.Receive(); b.Receive();
            Check(late.Entry.Flow.ActiveWallIds.Contains(23) && late.Entry.Flow.Equals(a.Entry.Flow), "中途加入取得相同墙门任务完整快照");
            Check(late.Entry.Flow.Objectives.Single().Id == 143 && late.Entry.Flow.Objectives.Single().Current == 0, "晚加入只收到当前143阶段及真实计数");
            Check(a.Role.mainRoleInfo.BaseInfo.Pos == before && late.Entry.Pos == ExpectedMoshentaSpawnPos, "晚加入不移动已在场队员");
            Move(a, 77.5f, 80.31f, 580f);
            a.Receive();
            Check(a.Role.mainRoleInfo.BaseInfo.Pos == before && !a.Replies.Last().Success && a.Replies.Last().Pos == before, "跨过薄墙的大步移动被拒绝并收到11241权威校正");
            Kill(a, 34);
            Check(Objective(scene, 143), "只死亡Boss波34不完成143");
            Kill(a, 35);
            Check(Objective(scene, 143), "只死亡Boss波34和35不完成143");
            Kill(a, 46);
            // 让原结算演出执行到打开胜利出口的时刻。
            for (int tick = 0; tick < 300; tick++) scene.Update(50, new MonsterAttrUpdateNtf());
            Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23) && Snapshot(scene).Flow.ActiveObjectIds.Contains(35), "Boss实际全灭执行63释放墙并生成出口");
            Check(Snapshot(scene).Flow.Finished && Objective(scene, 143, 1), "三Boss实际全灭完成143并保留现有胜利出口链");
            TargetEvent(scene, 143);
            Check(Objective(scene, 143, 1), "末目标重复事件保持143完成且不增计数");
            a.Receive();
            File.WriteAllBytes(Path.Combine(folder, "wire-victory.bin"), a.Notices.Last().ToByteArray());
            Check(!Snapshot(Scene(70002)).Flow.ActiveWallIds.Contains(23) && !Snapshot(Scene(70002)).Flow.ActiveWaveIds.Contains(18), "另一个副本的怪物和流程始终独立");
            Check(Objective(Scene(70002), 137), "另一副本目标保持137且不串当前Section");
            Move(late, 77.46f, 80.32f, 604.5f);
            // 测试角色从入口跨越已释放墙，可直接到达出口。
            Interact(late, 35);
            Check(late.Replies.Last().Success && late.Replies.Last().Exited && late.Role.mainRoleInfo.BaseInfo.InstanceId == 0, "胜利出口通过真实请求回城并释放该成员");
            a.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
            b.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
            late.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
            TeamManager.Instance.UpdateMonsters(50);
            a.Receive(); b.Receive(); late.Receive();
            Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23) && Snapshot(scene).Flow.Finished, "胜利后全队阵亡仍保留通关");
            Check(Objective(scene, 143, 1), "团灭不会撤销已经完成的首领目标");
            TeamManager.Instance.LeaveDungeon(a.Role.GetID());
            Interact(a, 5);
            Check(!a.Replies.Last().Success && a.Role.mainRoleInfo.BaseInfo.InstanceId == 0, "离开副本后的交互请求不能误入实例");
            TeamManager.Instance.LeaveDungeon(b.Role.GetID());
            TeamManager.Instance.LeaveDungeon(late.Role.GetID());
            Check(Scene(70001) == null && scene.Flow == null, "最后队员离开实际释放怪物世界");
        }
        Console.WriteLine("MOSHENTA_EVENT_FLOW: PASS " + passed);
        return 0;
    }
}
