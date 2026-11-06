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
public static class DungeonConfigProgressSmoke
{
    /// <summary>测试角色生命上限，避免巡逻测试时偶然全灭。</summary>
    private const int StartingHp = 100000000;
    /// <summary>成功断言数量。</summary>
    private static int passed;
    /// <summary>本轮协议证据输出目录。</summary>
    private static string folder;
    /// <summary>隔离测试玩家，通过真实TCP观察正式业务回包。</summary>
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
        /// <summary>真实网络收到的业务错误，验证资源缺失不会破坏原场景。</summary>
        public readonly List<ErrMsg> Errors = new List<ErrMsg>();
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
                if (packet.ProtoCode == NetDefine.CMD_ErrCode) Errors.Add(ErrMsg.Parser.ParseFrom(packet.Data));
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
        // 服务端当前实例字典，只读反射用于验证旧世界是否真实释放。
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
    /// <param name="pointLimit">本次最多攻击的原出生点数，用于保留部分击杀进度。</param>
    private static void Kill(Peer peer, int waveId, int pointLimit = int.MaxValue)
    {
        // 测试直接读取同份正式资源，不给业务添加击杀调试入口。
        System.Xml.Linq.XDocument config;
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Dungeons." + peer.Entry.SceneId + ".Waves.xml")) config = System.Xml.Linq.XDocument.Load(source);
        // 按原波次号取得正式XML定义，测试不伪造出生点。
        System.Xml.Linq.XElement wave = config.Descendants("MSceneWaveData").Single(row => (int)row.Element("base_info").Element("id") == waveId);
        // 目标波次全部原始位置，保持场景展开顺序。
        System.Xml.Linq.XElement[] positions = wave.Descendants("MScenePositionData").ToArray();
        // 按所选出生点数量触发实际攻击，允许只杀第一只怪物。
        for (int index = 0; index < positions.Length && index < pointLimit; index++)
        {
            // 当前受击出生点的世界坐标，使用正式碰撞结算。
            System.Xml.Linq.XElement point = positions[index].Element("position");
            peer.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", (float)point.Element("x"), (float)point.Element("y"), (float)point.Element("z") - 1.7f);
            TeamManager.Instance.OnMonsterHit(peer.Role, new Vector2(0, 1), int.MaxValue, new HashSet<ulong>());
        }
    }

    /// <summary>读取该实例正式场景源，仅用于测试选取交互位置。</summary>
    /// <param name="sceneId">协议下发的原场景编号。</param>
    private static System.Xml.Linq.XDocument Config(int sceneId)
    {
        using (Stream source = typeof(SceneWaveSpawner).Assembly.GetManifestResourceStream("Dungeons." + sceneId + ".Waves.xml")) return System.Xml.Linq.XDocument.Load(source);
    }

    /// <summary>通过正式请求处理器采集原XML中的真实波次。</summary>
    /// <param name="peer">发起采集的实例内玩家。</param>
    /// <param name="waveId">原采集波次编号。</param>
    private static void Collect(Peer peer, int waveId)
    {
        // 原场景波次与采集坐标，不改任何目标或采集完成状态。
        System.Xml.Linq.XElement wave = Config(peer.Entry.SceneId).Descendants("MSceneWaveData").Single(row => (int)row.Element("base_info").Element("id") == waveId);
        // 正式采集点世界坐标，只用于把测试角色放在合法交互范围。
        System.Xml.Linq.XElement position = wave.Descendants("MScenePositionData").First().Element("position");
        peer.Role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", (float)position.Element("x"), (float)position.Element("y"), (float)position.Element("z"));
        Interact(peer, 0, waveId);
    }

    /// <summary>仅测试组件补发尚未移植的北森NPC时间轴SceneAI输入，目标计数仍由正式事件链完成。</summary>
    /// <param name="scene">接收事件的真实副本实例。</param>
    /// <param name="name">原RO时间轴或SceneAI事件名。</param>
    private static void ScriptFixture(SceneWaveSpawner scene, string name)
    {
        // object[]只处于Reflection.Invoke要求的反射边界，不作为业务载荷。
        typeof(MoshentaSceneFlow).GetMethod("SceneAIEvent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scene.Flow, new object[] { name });
    }

    /// <summary>把当前正式Flow脏状态经TeamManager与真实TCP发出，再保存实际收到的协议体。</summary>
    /// <param name="peer">接收本实例通知的在线玩家。</param>
    /// <param name="name">测试证据的文件名，不含扩展名。</param>
    private static void SaveFlow(Peer peer, string name)
    {
        TeamManager.Instance.OnDungeonMove(peer.Role);
        peer.Receive();
        // 此处保存实际已接收的包，禁止用构造快照替代网络结果。
        MonsterAttrUpdateNtf notice = peer.Notices.Last();
        File.WriteAllBytes(Path.Combine(folder, name + ".bin"), notice.ToByteArray());
    }

    /// <summary>完成当前单个目标所需的原事件，用于无场景资产的树结构单元验收。</summary>
    /// <param name="progress">独立目标组件，不声称存在正式NPC脚本源。</param>
    private static void FinishSection(DungeonTargetProgress progress)
    {
        // 固定本次目标列表，事件推进后不继续消费下一阶段。
        DungeonFlowState before = new DungeonFlowState();
        progress.FillSnapshot(before);
        // 逐一验证当前目标列表，不向后续阶段补发提前事件。
        for (int index = 0; index < before.Objectives.Count; index++)
        {
            // 当前待验证目标的原表定义，按真实类型提供对应事件。
            DungeonTargetTable row = LubanMgr.Instance.GetDungeonTargetTableById(before.Objectives[index].Id);
            // 按原处理器定义的总次数发事件，保留目标配置语义。
            for (int count = 0; count < before.Objectives[index].Total; count++)
            {
                switch ((int)row.Target[0])
                {
                    case 1: progress.NpcScript("dungeontargetnpctalk", (int)row.Target[1], 0); break;
                    case 2: progress.MonsterDied((int)row.Target[1]); break;
                    case 3: progress.Collected((int)row.Target[1]); break;
                    case 5: progress.EnterTrigger((int)row.Target[1], (int)row.Target[2]); break;
                    case 7: progress.SceneEvent("dungeonfinishtarget", row.ID); break;
                    case 8:
                        // 向波次目标逐个发送原表要求的完成波次。
                        for (int wave = 1; wave < row.Target.Length; wave++) progress.WaveFinished((int)row.Target[wave]);
                        break;
                    default: throw new InvalidOperationException("未经验证的目标类型" + row.Target[0]);
                }
            }
        }
    }

    /// <summary>取得独立组件当前阶段的第一个目标，仅供分支拓扑断言。</summary>
    /// <param name="progress">独立的原表目标组件。</param>
    private static int First(DungeonTargetProgress progress)
    {
        // 独立组件生成的当前阶段快照，读取其原目标身份。
        DungeonFlowState state = new DungeonFlowState();
        progress.FillSnapshot(state);
        return state.Objectives[0].Id;
    }

    /// <summary>构造AI选择器的已清波条件，不写目标计数，也不声称这些隔离条件来自真实战斗。</summary>
    /// <param name="scene">专用于选择器语义的独立场景。</param>
    /// <param name="waveIds">原AI需要检查的已清空波次。</param>
    private static void CompletedWavesFixture(SceneWaveSpawner scene, params int[] waveIds)
    {
        // 仅测试反射设置源事件前提，随后仍调用正式SceneAI输入和目标处理器。
        HashSet<int> completed = (HashSet<int>)typeof(MoshentaSceneFlow).GetField("complete", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scene.Flow);
        // 按原波次ID建立已完成条件，避免移植整段Boss剧情来验证选择器。
        for (int index = 0; index < waveIds.Length; index++) completed.Add(waveIds[index]);
    }

    /// <summary>读取原AI布尔黑板的实际状态，不替业务代码设置完成标记。</summary>
    /// <param name="scene">持有该副本私有黑板的实例。</param>
    /// <param name="name">原EntryTask的布尔变量名。</param>
    private static bool SceneBool(SceneWaveSpawner scene, string name)
    {
        // 测试只读真实黑板，确认一次性语义来自原条件及置值。
        Dictionary<string, bool> flags = (Dictionary<string, bool>)typeof(MoshentaSceneFlow).GetField("sceneAiFlags", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scene.Flow);
        return flags[name];
    }

    /// <summary>在隔离AI语义测试中调用正式采集目标处理器，不伪造Section下标或次数。</summary>
    /// <param name="scene">本次测试副本的真实目标组件宿主。</param>
    private static void CollectTargetFixture(SceneWaveSpawner scene)
    {
        // 此边界仅检查早到事件与当前Section的关系；正式采集链已经由上方TCP用例覆盖。
        DungeonTargetProgress targets = (DungeonTargetProgress)typeof(MoshentaSceneFlow).GetField("targets", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scene.Flow);
        targets.Collected(512051);
    }

    /// <summary>验证魔神塔Selector优先级、可重复事件以及北森原布尔门控之间的区别。</summary>
    private static void CheckSceneAISelection()
    {
        // 两个清波条件都满足；原Selector中143分支先于141且没有一次性布尔条件。
        SceneWaveSpawner tower = new SceneWaveSpawner(4060);
        CompletedWavesFixture(tower, 34, 35, 46, 42);
        // 只发送当前Section对应的原目标事件，将测试推进到141，不直接写计数。
        for (int target = 137; target <= 140; target++) TargetEvent(tower, target);
        ScriptFixture(tower, "monster_die");
        Check(Objective(tower, 141), "Selector两组清波满足时只发送首分支143而不会顺带完成141");
        TargetEvent(tower, 141); TargetEvent(tower, 142);
        ScriptFixture(tower, "monster_die");
        Check(Objective(tower, 143, 1), "魔神塔提前143未匹配阶段不会消耗后续monster_die的143事件");
        tower.Clear();
        // 北森235原分支必须同时满足Spawn=true和Finish=false，不能只依据清波。
        SceneWaveSpawner forest = new SceneWaveSpawner(4050);
        CompletedWavesFixture(forest, 0, 1);
        CollectTargetFixture(forest);
        ScriptFixture(forest, "monster_die");
        Check(Objective(forest, 235), "北森清波满足但原WaveSpawn01=false时不执行235分支");
        Check(!SceneBool(forest, "WaveSpawn01") && !SceneBool(forest, "Wave01Finshi"), "北森布尔初值直接保留原EntryTask的false");
        ScriptFixture(forest, "FuWenShi01");
        Check(SceneBool(forest, "WaveSpawn01"), "原FuWenShi01结果按顺序设置WaveSpawn01=true");
        ScriptFixture(forest, "monster_die");
        Check(Objective(forest, 236) && SceneBool(forest, "Wave01Finshi"), "北森原条件通过后发送235并设置Wave01Finshi=true");
        forest.Clear();
        // 北森一次性状态确实由原AISetBool消耗；即使目标早到未匹配，也不会重复发该分支。
        SceneWaveSpawner earlyForest = new SceneWaveSpawner(4050);
        CompletedWavesFixture(earlyForest, 0, 1);
        ScriptFixture(earlyForest, "FuWenShi01"); ScriptFixture(earlyForest, "monster_die");
        Check(Objective(earlyForest, 234) && SceneBool(earlyForest, "Wave01Finshi"), "北森早到235不预计数但按原AI设置一次性完成布尔");
        CollectTargetFixture(earlyForest); ScriptFixture(earlyForest, "monster_die");
        Check(Objective(earlyForest, 235), "北森原Finish=true阻止后续死亡重复执行235分支");
        earlyForest.Clear();
    }

    /// <summary>隔离验证真实入场、击杀采集协议与原表阶段语义，不连接正式进程。</summary>
    /// <param name="args">原始网络证据的临时输出目录。</param>
    public static int Main(string[] args)
    {
        folder = args[0];
        LubanMgr.Instance.Init();
        // 定向返修可单独运行AI语义，不重复网络和资源选择验收。
        if (args.Length > 2 && args[2] == "scene-ai")
        {
            CheckSceneAISelection();
            Console.WriteLine("DUNGEONS_SCENE_AI_SELECTION: PASS " + passed);
            return 0;
        }
        using (Peer a = new Peer(986001))
        using (Peer b = new Peer(986002))
        using (Peer late = new Peer(986003))
        using (Peer hard = new Peer(986004))
        {
            // 普通北森测试实例的真实GS入场命令。
            MasterTeamDungeonOrder order = new MasterTeamDungeonOrder { TeamId = 301, InstanceId = 71001, DungeonId = 4050 };
            order.MemberIds.Add(a.Role.GetID());
            TeamManager.Instance.EnterDungeon(order);
            // 隔离实例的入场命令，用于检查目标计数不会串队。
            MasterTeamDungeonOrder otherOrder = new MasterTeamDungeonOrder { TeamId = 302, InstanceId = 71002, DungeonId = 4050 };
            otherOrder.MemberIds.Add(b.Role.GetID());
            TeamManager.Instance.EnterDungeon(otherOrder);
            // 困难北森入场命令，验证不同SceneID和击杀目标。
            MasterTeamDungeonOrder hardOrder = new MasterTeamDungeonOrder { TeamId = 303, InstanceId = 71003, DungeonId = 4051 };
            hardOrder.MemberIds.Add(hard.Role.GetID());
            TeamManager.Instance.EnterDungeon(hardOrder);
            a.Receive(); b.Receive(); hard.Receive();
            // GS正式创建的普通北森实例，用于读取实际进度。
            SceneWaveSpawner scene = Scene(71001);
            Check(a.Entry.DungeonId == 4050 && a.Entry.SceneId == 4050 && a.Entry.MapId == 4050, "4050真实入口按所选表下发场景");
            Check(a.Entry.Pos == "193.09_22.34_42.05" && Objective(scene, 234), "北森原出生点与初始采集目标");
            Check(hard.Entry.DungeonId == 4051 && hard.Entry.SceneId == 4051 && Objective(Scene(71003), 234), "4051独立困难XML与目标配置");
            File.WriteAllBytes(Path.Combine(folder, "wire-4050-entry234.bin"), a.Entry.ToByteArray());
            TargetEvent(scene, 235); TargetEvent(scene, 246);
            Check(Objective(scene, 234), "未激活特殊事件不预计数");
            Collect(a, 26);
            Check(!a.Replies.Last().Success && Objective(scene, 234), "未激活采集波次不计数");
            Collect(a, 13);
            Check(a.Replies.Last().Success && Objective(scene, 235), "真实采集13完成234并激活初始怪物");
            Check(Snapshot(scene).Flow.ActiveWaveIds.Contains(0) && Snapshot(scene).Flow.ActiveWaveIds.Contains(1), "原采集事件FuWenShi01触发实际波0和1");
            Kill(a, 0); Kill(a, 1);
            Check(Objective(scene, 236), "实际波0和1清空经原AI完成235");
            ScriptFixture(scene, "AreaEnter01");
            Check(Objective(scene, 237), "原NPC时间轴fixture事件推进236到击杀237");
            Kill(a, 6, 1);
            Check(Objective(scene, 237, 1), "正式碰撞死亡计入237第一只");
            a.Receive();
            SaveFlow(a, "wire-4050-partial237");
            order.MemberIds.Add(late.Role.GetID());
            TeamManager.Instance.EnterDungeon(order);
            late.Receive(); a.Receive();
            Check(late.Entry.Flow.Objectives[0].Current == 1 && late.Entry.Flow.Objectives[0].Id == 237, "晚加入接收当前实例部分击杀进度");
            Check(Objective(Scene(71002), 234), "其他实例保持独立234");
            Kill(a, 6);
            Check(Objective(scene, 238), "237五次真实死亡完成后进入238");
            SaveFlow(a, "wire-4050-completed237");
            Kill(a, 7);
            Check(Objective(scene, 240), "真实波7死亡按原顺序完成238和239");
            ScriptFixture(scene, "AreaEnter02"); Collect(a, 14);
            Check(Objective(scene, 241), "真实采集14完成240");
            ScriptFixture(scene, "FaShiAppear"); Kill(a, 8);
            Check(Objective(scene, 243), "原时间轴输入与真实波8清空推进241和242");
            ScriptFixture(scene, "AreaEnter03"); Collect(a, 15);
            Check(Objective(scene, 245), "真实采集15按配置类型3完成244");
            Kill(a, 9); Kill(a, 10); Kill(a, 25);
            Check(Snapshot(scene).Flow.Objectives.Select(row => row.Id).SequenceEqual(new[] { 246, 247 }), "原246等号247保留同阶段双目标");
            SaveFlow(a, "wire-4050-both246247");
            ScriptFixture(scene, "JiaoGuoAppear");
            Check(Snapshot(scene).Flow.Objectives[0].Completed && !Snapshot(scene).Flow.Objectives[1].Completed, "246完成后仍等待247且保留两个目标");
            SaveFlow(a, "wire-4050-one-completed246247");
            ScriptFixture(scene, "XinAppear"); Collect(a, 18); Collect(a, 26);
            Check(Objective(scene, 248), "真实采集512057完成247才推进248");
            Kill(a, 11); Kill(a, 12);
            Check(Objective(scene, 249), "实际波11和12完成248");
            ScriptFixture(scene, "LaoLong"); Kill(a, 19);
            Check(Objective(scene, 250, 1), "最终真实9751死亡完成250且保留末阶段");
            SaveFlow(a, "wire-4050-last250");
            a.Role.mainRoleInfo.BaseInfo.CurrHp = 0; late.Role.mainRoleInfo.BaseInfo.CurrHp = 0;
            TeamManager.Instance.UpdateMonsters(50); a.Receive();
            Check(Objective(scene, 234), "全队死亡重建原234初始阶段");
            SaveFlow(a, "wire-4050-reset234");
            Collect(hard, 13); Kill(hard, 0); Kill(hard, 1); ScriptFixture(Scene(71003), "AreaEnter01");
            Check(Objective(Scene(71003), 251), "4051按其配置使用251而非普通237");
            Kill(hard, 6, 1);
            Check(Objective(Scene(71003), 251, 1), "困难怪物配置真实死亡计入251");
            File.WriteAllBytes(Path.Combine(folder, "wire-4051-entry234.bin"), hard.Entry.ToByteArray());
        }
        // 无正式NPC/区域资产的其他副本只验证原表组件，绝不标为实机场景事件。
        DungeonTargetProgress branch = new DungeonTargetProgress(LubanMgr.Instance.GetDungeonsTableById(2809));
        Check(First(branch) == 501, "2809原分支根组1引用目标501");
        // 按原配置顺序完成指定数量的前置阶段，停在待验证分支。
        for (int section = 0; section < 5; section++) FinishSection(branch);
        Check(branch.SectionFinished && First(branch) == 504, "2809多后继完成后保持组5等待");
        Check(branch.NpcScript("selecttargetbranch", 0, 9) && First(branch) == 509, "2809合法选择9进入第二分支");
        // 按原配置顺序完成指定数量的前置阶段，停在待验证分支。
        for (int section = 0; section < 3; section++) FinishSection(branch);
        Check(First(branch) == 507, "2809第二分支按9到15到16汇合组8");
        // 另一份原2809组件，单独覆盖无效选支随机回退。
        DungeonTargetProgress otherBranch = new DungeonTargetProgress(LubanMgr.Instance.GetDungeonsTableById(2809));
        // 按原配置顺序完成指定数量的前置阶段，停在待验证分支。
        for (int section = 0; section < 5; section++) FinishSection(otherBranch);
        Check(otherBranch.NpcScript("selecttargetbranch", 0, 999) && (First(otherBranch) == 505 || First(otherBranch) == 509), "无效显式选支沿RO随机真实后继");
        // 原2815分支图，覆盖三分支和后续汇合。
        DungeonTargetProgress complex = new DungeonTargetProgress(LubanMgr.Instance.GetDungeonsTableById(2815));
        Check(First(complex) == 615, "2815根组17正确");
        FinishSection(complex); FinishSection(complex);
        Check(complex.SectionFinished && First(complex) == 616, "2815三后继等待选择");
        Check(complex.NpcScript("selecttargetbranch", 0, 30) && First(complex) == 628, "2815正确选择组30");
        FinishSection(complex); FinishSection(complex);
        Check(complex.NpcScript("selecttargetbranch", 0, 32) && First(complex) == 630, "2815嵌套分支选择组32");
        FinishSection(complex);
        Check(First(complex) == 618, "2815分支32汇合既有组20");
        // 整个MS进本选择走独立进程TCP，再由正式GS入口产生实际客户端通知。
        using (Peer switching = new Peer(986010))
        using (MasterTeamProbe master = new MasterTeamProbe(int.Parse(args[1], CultureInfo.InvariantCulture)))
        {
            master.RequestCreateTeam(switching.Role, 1000);
            Check(master.CaptainRequestEnterFBPtc(switching.Role, 4060), "MS真实请求接受配置4060");
            switching.Receive();
            // 魔神塔首次实际分配的实例号，用于检查跨副本不会复用。
            ulong first = switching.Entry.InstanceId;
            Check(master.CaptainRequestEnterFBPtc(switching.Role, 4050), "MS真实请求接受配置4050");
            switching.Receive();
            // 切换北森后实际收到的新实例号。
            ulong second = switching.Entry.InstanceId;
            Check(first != second && switching.Entry.SceneId == 4050, "同队4060转4050获得新实例和正确场景");
            // 服务端当前实例字典，只读反射用于验证旧世界是否真实释放。
            IDictionary instances = (IDictionary)typeof(TeamManager).GetField("_instances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(TeamManager.Instance);
            Check(!instances.Contains(first), "同队转入其他副本释放旧空实例");
            Collect(switching, 13);
            master.CaptainRequestEnterFBPtc(switching.Role, 4050); switching.Receive();
            Check(switching.Entry.InstanceId == second && switching.Entry.Flow.Objectives[0].Id == 235, "同副本重入保留真实采集进度");
            Check(master.CaptainRequestEnterFBPtc(switching.Role, 4051), "MS真实请求接受配置4051");
            switching.Receive();
            Check(switching.Entry.SceneId == 4051 && switching.Entry.InstanceId != second && !instances.Contains(second), "同队转4051后清理普通副本并加载困难XML");
            Collect(switching, 13);
            // 缺资源请求前仍在运行的困难实例身份。
            ulong current = switching.Entry.InstanceId;
            // 缺资源请求前的权威位置，失败后必须保持。
            string position = switching.Role.mainRoleInfo.BaseInfo.Pos;
            Check(!master.CaptainRequestEnterFBPtc(switching.Role, 2809), "MS未登记资源在改实例前返回失败"); switching.Receive();
            Check(switching.Errors.Last().CmdCode == CmdCode.ServerError && (ulong)switching.Role.mainRoleInfo.BaseInfo.InstanceId == current && switching.Role.mainRoleInfo.BaseInfo.Pos == position, "未迁入场景通过现有错误协议失败且不改变角色实例");
            Check(instances.Contains(current), "资源缺失请求保留原实例及生命进度");
            master.CaptainRequestEnterFBPtc(switching.Role, 4051); switching.Receive();
            Check(switching.Entry.InstanceId == current && switching.Entry.Flow.Objectives[0].Id == 235, "失败后重入4051保持原MS实例及已完成采集进度");
        }
        CheckSceneAISelection();
        Console.WriteLine("DUNGEONS_CONFIG_PROGRESS: PASS " + passed);
        return 0;
    }
}
