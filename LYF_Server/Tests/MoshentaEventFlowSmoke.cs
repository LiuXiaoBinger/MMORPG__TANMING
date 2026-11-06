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
                if (packet.ProtoCode == NetDefine.CMD_TeamEnterDungeonCode) Entry = TeamEnterDungeonNtf.Parser.ParseFrom(packet.Data);
                if (packet.ProtoCode == NetDefine.CMD_MonsterAttrUpdateCode) Notices.Add(MonsterAttrUpdateNtf.Parser.ParseFrom(packet.Data));
            }
        }

        /// <summary>关闭本机回环和测试进程里的在线角色，保持正式游戏服务、数据库及场景独立。</summary>
        public void Dispose()
        {
            RoleManager.Instance.RemoveOnlineRole(Role.mainRoleInfo.BaseInfo.RoleId);
            Role.Dispose();
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
        IDictionary instances = (IDictionary)typeof(TeamManager).GetField("_instances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(TeamManager.Instance);
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
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Moshenta.Waves.xml")) config = System.Xml.Linq.XDocument.Load(source);
        System.Xml.Linq.XElement wave = config.Descendants("MSceneWaveData").Single(row => (int)row.Element("base_info").Element("id") == waveId);
        System.Xml.Linq.XElement[] positions = wave.Descendants("MScenePositionData").ToArray();
        for (int index = 0; index < positions.Length; index++)
        {
            System.Xml.Linq.XElement point = positions[index].Element("position");
            peer.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", (float)point.Element("x"), (float)point.Element("y"), (float)point.Element("z") - 1.7f);
            TeamManager.Instance.OnMonsterHit(peer.Role, new Vector2(0, 1), int.MaxValue, new HashSet<ulong>());
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
            // 模拟 MS 已批准的入场单，GS 内使用正式实例创建流程。
            MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = 100, InstanceId = 70001, DungeonId = 4060 };
            order.MemberIds.Add(new ulong[] { a.Role.GetID(), b.Role.GetID() });
            TeamManager.Instance.EnterDungeon(order);
            MasterTeamDungeonOrder separate = new MasterTeamDungeonOrder { TeamId = 200, InstanceId = 70002, DungeonId = 4060 };
            separate.MemberIds.Add(other.Role.GetID());
            TeamManager.Instance.EnterDungeon(separate);
            a.Receive(); b.Receive(); other.Receive();
            SceneWaveSpawner scene = Scene(70001);
            Check(a.Entry.Equals(b.Entry) && a.Entry.Monsters.Count == 0, "同队入场快照一致且无战斗怪物");
            Check(a.Entry.Pos == "132.97_58.89_60.95" && TeamManager.MoshentaSpawnPos == a.Entry.Pos, "单一XML入口与协议位置一致");
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
            MonsterActionData orc = Snapshot(scene).MonsterActions.Single();
            Check(orc.Phase == MonsterActionPhase.Move, "事件生成后第18波仍沿原行为树巡逻");
            a.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", orc.X, orc.Y, orc.Z - 1.7f);
            TeamManager.Instance.OnMonsterHit(a.Role, new Vector2(0, 1), 1, new HashSet<ulong>());
            Check(Snapshot(scene).Monsters.Single(row => row.SpawnId == orc.SpawnId).Hp == 239, "第18波移动后按当前坐标命中");
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
            Check(Snapshot(scene).Flow.ActiveWallIds.Contains(23), "Boss入口事件创建权威空气墙23");
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
            Check(a.Role.mainRoleInfo.BaseInfo.Pos == before && late.Entry.Pos == TeamManager.MoshentaSpawnPos, "晚加入不移动已在场队员");
            Move(a, 77.5f, 80.31f, 580f);
            a.Receive();
            Check(a.Role.mainRoleInfo.BaseInfo.Pos == before && !a.Replies.Last().Success && a.Replies.Last().Pos == before, "跨过薄墙的大步移动被拒绝并收到11241权威校正");
            Kill(a, 34);
            Check(Objective(scene, 143), "只死亡Boss波34不完成143");
            Kill(a, 35);
            Check(Objective(scene, 143), "只死亡Boss波34和35不完成143");
            Kill(a, 46);
            Check(!Snapshot(scene).Flow.ActiveWallIds.Contains(23) && Snapshot(scene).Flow.ActiveObjectIds.Contains(35), "Boss实际全灭执行63释放墙并生成出口");
            Check(Snapshot(scene).Flow.Finished && Objective(scene, 143, 1), "三Boss实际全灭完成143并保留现有胜利出口链");
            TargetEvent(scene, 143);
            Check(Objective(scene, 143, 1), "末目标重复事件保持143完成且不增计数");
            a.Receive();
            File.WriteAllBytes(Path.Combine(folder, "wire-victory.bin"), a.Notices.Last().ToByteArray());
            Check(Snapshot(Scene(70002)).Monsters.Count == 0, "另一个副本的怪物和流程始终独立");
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
            Check(Snapshot(scene).Monsters.Count == 0 && Snapshot(scene).Flow.ActiveWallIds.Count == 0 && !Snapshot(scene).Flow.ActiveObjectIds.Contains(35), "全队死亡重置实体空气墙和出口");
            Check(Objective(scene, 137) && !Snapshot(scene).Flow.Finished, "全灭目标进度回到原表137初始状态");
            Check(a.Notices.Last().Flow.Equals(b.Notices.Last().Flow) && a.Role.mainRoleInfo.BaseInfo.Pos == TeamManager.MoshentaSpawnPos, "全灭重置快照同队一致且回权威入口");
            File.WriteAllBytes(Path.Combine(folder, "wire-reset.bin"), a.Notices.Last().ToByteArray());
            a.Role.mainRoleInfo.BaseInfo.CurrHp = StartingHp;
            TeamManager.Instance.UpdateMonsters(50);
            Move(a, 133.14f, 63.89f, 121.74f);
            Check(Snapshot(scene).Monsters.Count == count, "重置后区域能够重新激活且无旧实体重叠");
            TeamManager.Instance.LeaveDungeon(a.Role.GetID());
            Interact(a, 5);
            Check(!a.Replies.Last().Success && a.Role.mainRoleInfo.BaseInfo.InstanceId == 0, "离开副本后的交互请求不能误入实例");
            TeamManager.Instance.LeaveDungeon(b.Role.GetID());
            TeamManager.Instance.LeaveDungeon(late.Role.GetID());
            Check(Snapshot(scene).Monsters.Count == 0, "最后队员离开实际释放怪物世界");
        }
        Console.WriteLine("MOSHENTA_EVENT_FLOW: PASS " + passed);
        return 0;
    }
}
