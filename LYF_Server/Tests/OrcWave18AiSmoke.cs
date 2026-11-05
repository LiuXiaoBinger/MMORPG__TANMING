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

/// <summary>在独立进程中按正式游戏服逻辑帧和 TCP 数据包验证第 18 波兽人的战斗闭环。</summary>
public static class OrcWave18AiSmoke
{
    /// <summary>跨端身份由波次 18 和本波出生序号 1 组成。</summary>
    private const ulong SpawnId = (18UL << 32) | 1UL;
    /// <summary>本例中的兽人普攻配置号。</summary>
    private const int OrcAttack = 1000118401;
    /// <summary>GameServer 固定逻辑帧，单位毫秒。</summary>
    private const int FrameMs = 50;
    /// <summary>高血量受测角色用于经历多次原表攻击，不把每刀伤害固定成测试数值。</summary>
    private const int StartingHp = 100000000;
    /// <summary>ASPD 79 将基础 0.333333343 秒结果点缩放为约 807 毫秒。</summary>
    private const int ImpactMs = 807;
    /// <summary>ASPD 79 将 1 秒基础攻击周期缩放为 2420 毫秒。</summary>
    private const int AttackCycleMs = 2420;
    /// <summary>原副本 1 号巡逻路径，使用世界 X、Z 米制坐标。</summary>
    private static readonly Vector2[] Patrol = {
        new Vector2(150.29f, 179.48f), new Vector2(150.15f, 163.75f), new Vector2(145.28f, 159.48f)
    };
    /// <summary>本进程当前已推进的游戏时间，单位毫秒。</summary>
    private static int clock;
    /// <summary>成功通过的独立验收条目数。</summary>
    private static int passed;

    /// <summary>给原伤害公式输入可复现的命中、暴击和浮动百分位。</summary>
    private sealed class FixedRoll : Random
    {
        /// <summary>所有抽样使用的固定百分位，取值位于 [0, 1)。</summary>
        private readonly double sample;
        /// <summary>保存本组对照使用的固定掷骰输入。</summary>
        /// <param name="sample">与 RO Lua 基准探针相同的输入百分位。</param>
        public FixedRoll(double sample) { this.sample = sample; }
        /// <summary>每次调用返回同一百分位，避免验收取决于运气。</summary>
        public override double NextDouble() { return sample; }
    }

    /// <summary>以同一非暴击的普通命中，确认装备外防生效、面板展示的外防变化值不会被叠加第二次。</summary>
    private static void CheckDefense()
    {
        // 初始样本为 50 级无外防角色，随后按原 Lua 对照组分别改变面板属性。
        RoleBaseInfo target = new RoleBaseInfo { Level = 50 };
        int low = OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.5));
        target.DefExternal = 150;
        Check(OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.5)) == 3256 && low == 3256, "对照原 Lua，无外防及 150 外防都结算 3256 点");
        target.DefExternal = 1324;
        target.CritResistPoint = 2598;
        int high = OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.5));
        target.DefExternalDelta = 450;
        int panelDelta = OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.5));
        Check(high == 1053 && high == panelDelta, "对照原 Lua，1324 外防/2598 暴抗结算 1053 点，面板差值不重复叠加");
        target.Level = 300;
        Check(OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.6)) == 0, "原 Lua 高等级目标闪避样本无伤害");
        target = new RoleBaseInfo { Level = 50 };
        Check(OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.005)) == 4739, "原 Lua 暴击样本为 4739 点");
        target.CritDamageReduce = 20;
        Check(OrcBasicAttackDamage.Calculate(target, new FixedRoll(0.005)) == 3791, "原 Lua 20% 暴伤减免样本为 3791 点");
    }

    /// <summary>单个玩家的游戏服角色和真实回环网络出口，两名队员和另一实例玩家分别各有一组。</summary>
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

    /// <summary>驱动一次正式怪物逻辑更新而不等待墙钟时间。</summary>
    private static void Tick()
    {
        MonsterWaveManager.Instance.Update(FrameMs);
        clock += FrameMs;
    }

    /// <summary>记录断言并带上可复现的模拟毫秒时间，失败使测试进程以非零退出。</summary>
    /// <param name="ok">本条业务结果是否与验收约定一致。</param>
    /// <param name="name">测试输出中用于定位失败项的名称。</param>
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException("t=" + clock + "ms " + name);
        passed++;
        Console.WriteLine("PASS t=" + clock + "ms " + name);
    }

    /// <summary>在不改变玩家位置与队伍状态的情况下获取与入场完全相同的快照。</summary>
    /// <param name="scene">当前队伍私有的场景实例。</param>
    private static MonsterActionData Snapshot(SceneWaveSpawner scene)
    {
        // 独立的协议目标避免读取上一帧序列化缓存。
        TeamEnterDungeonNtf data = new TeamEnterDungeonNtf();
        scene.FillEnterScene(data);
        return data.MonsterActions.Single(action => action.SpawnId == SpawnId);
    }

    /// <summary>按 MS 已分配的实例 ID 定位 GS 真实场景。测试反射私有实例字典，避免添加业务调试入口。</summary>
    /// <param name="instanceId">进入副本后 GS 写入角色的实例号。</param>
    private static SceneWaveSpawner GetScene(ulong instanceId)
    {
        // Dungeon 为 GS 嵌套私有类型，反射只用于测试读取真实存在的实例。
        IDictionary teams = (IDictionary)typeof(TeamManager).GetField("_instances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(TeamManager.Instance);
        return (SceneWaveSpawner)teams[instanceId].GetType().GetField("WaveSpawner").GetValue(teams[instanceId]);
    }

    /// <summary>按真实普攻面向，在兽人当前中心或指定旧位置进行一次命中判定。</summary>
    /// <param name="role">同实例内的施害者。</param>
    /// <param name="x">希望覆盖的怪物世界 X 中心，单位米。</param>
    /// <param name="y">希望覆盖的怪物世界高度，单位米。</param>
    /// <param name="z">希望覆盖的怪物世界 Z 中心，单位米。</param>
    /// <param name="damage">本次测试攻击的整数伤害，沿用正式碰撞窗口结算接口。</param>
    private static void StrikeAt(OnlineRole role, float x, float y, float z, int damage)
    {
        SetPosition(role, x, y, z - 1.7f);
        TeamManager.Instance.OnMonsterHit(role, new Vector2(0, 1), damage, new HashSet<ulong>());
    }

    /// <summary>送入业务读取的服务器角色位置，采用当前协议中的米制 XYZ 字符串。</summary>
    /// <param name="role">要摆放的测试玩家。</param>
    /// <param name="x">世界坐标 X，单位米。</param>
    /// <param name="y">世界坐标 Y，单位米。</param>
    /// <param name="z">世界坐标 Z，单位米。</param>
    private static void SetPosition(OnlineRole role, float x, float y, float z)
    {
        role.mainRoleInfo.BaseInfo.Pos = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", x, y, z);
    }

    /// <summary>还原移动 30 米的回归场景，直接更改测试实例内存中的 Transform。</summary>
    private static void CheckMovedHit()
    {
        // 独立于随后真实队伍的实例，用于单独确认生命碰撞的空间来源。
        SceneWaveSpawner scene = new SceneWaveSpawner();
        // 入场血量按出生序号和实体集合一一对应。
        TeamEnterDungeonNtf entry = new TeamEnterDungeonNtf();
        scene.FillEnterScene(entry);
        // 从快照定位第 18 波，不以模板号混淆其他波次同模型。
        int index = entry.Monsters.ToList().FindIndex(row => row.SpawnId == SpawnId);
        // 反射边界仅用于把怪物移至原点以外的指定位置，不产生调试接口。
        IList entities = (IList)typeof(SceneWaveSpawner).GetField("monsters_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scene);
        // 通过私有集合得到的实体实例仅在反射边界转出其强类型 Transform。
        MMoRpgCommon.SceneEntity.EntityTransformSnapshot transform = (MMoRpgCommon.SceneEntity.EntityTransformSnapshot)
            entities[index].GetType().GetProperty("Transform").GetValue(entities[index], null);
        // 普攻中心前移 1.7 米，旧施害位置对应原出生点。
        Vector3 oldOrigin = new Vector3(transform.X, transform.Y, transform.Z - 1.7f);
        transform.X += 30f;
        // 两个判定分别属于独立窗口，不受命中去重掩盖。
        MonsterAttrUpdateNtf oldHit = new MonsterAttrUpdateNtf();
        scene.ChangeHP(oldHit, oldOrigin, new Vector2(0, 1), 1, new HashSet<ulong>());
        // 新位置使用相同施害朝向和碰撞盒规格。
        MonsterAttrUpdateNtf newHit = new MonsterAttrUpdateNtf();
        scene.ChangeHP(newHit, oldOrigin + new Vector3(30, 0, 0), new Vector2(0, 1), 1, new HashSet<ulong>());
        Check(!oldHit.Monsters.Any(row => row.SpawnId == SpawnId) && newHit.Monsters.Any(row => row.SpawnId == SpawnId && row.Hp == 239),
            "移动 30 米后出生点打空、当前位置命中 240→239");
        scene.Clear();
    }

    /// <summary>依次观察 1→2→3→2→1，并让一个未造成伤害的玩家站在巡逻点附近。</summary>
    /// <param name="scene">待验的实际队伍实例。</param>
    /// <param name="bystander">仅站在巡逻路线附近的玩家。</param>
    private static void CheckPatrol(SceneWaveSpawner scene, OnlineRole bystander)
    {
        SetPosition(bystander, Patrol[0].X, 64.39f, Patrol[0].Y);
        // 期望的点序列由场景的 PingPong 决定，末点不会跳回起点。
        int[] sequence = { 0, 1, 2, 1, 0 };
        // 已按正确顺序经过的原配置点数量。
        int visited = 0;
        // 记录兽人第一次离开起点附近的时间，验证首点停留配置 3 秒。
        int firstArrival = -1, firstDeparture = -1;
        // 完整往返预留两分钟模拟时间，不引入墙钟等待。
        for (int frame = 0; frame < 2400 && visited < sequence.Length; frame++)
        {
            Tick();
            // 读取服务端当前坐标，不用目的地冒充实际抵达。
            MonsterActionData action = Snapshot(scene);
            if (action.TargetRoleId != 0 || action.Phase == MonsterActionPhase.Attack) throw new Exception("巡逻怪主动攻击旁观者");
            // 本帧的水平位置使用与导航相同的 X、Z 平面。
            Vector2 here = new Vector2(action.X, action.Z);
            if (firstArrival >= 0 && firstDeparture < 0 && Vector2.Distance(here, Patrol[0]) > 1f) firstDeparture = clock;
            if (Vector2.Distance(here, Patrol[sequence[visited]]) > 0.7f) continue;
            if (visited == 0) firstArrival = clock;
            visited++;
        }
        Check(visited == sequence.Length, "实际当前位置经过巡逻点 1→2→3→2→1");
        Check(firstDeparture - firstArrival >= 3000 && bystander.mainRoleInfo.BaseInfo.CurrHp == StartingHp, "首点等待至少 3 秒，邻近玩家未挨打");
    }

    /// <summary>等待兽人真实进入指定玩家的攻击状态，期间可要求观察到追击位移。</summary>
    /// <param name="scene">该兽人所在的游戏场景。</param>
    /// <param name="target">已受仇恨锁定的玩家。</param>
    /// <param name="requireChase">本次攻击之前是否要求追近至少 2 米。</param>
    private static MonsterActionData WaitAttack(SceneWaveSpawner scene, OnlineRole target, bool requireChase)
    {
        // 记录追击起点，避免命令写入目标但实体实际原地不动。
        MonsterActionData start = Snapshot(scene);
        // 在有仇恨以后发生过达到阈值的实际位移。
        bool moved = false;
        for (int frame = 0; frame < 800; frame++)
        {
            Tick();
            // 怪物生命和目标由游戏服务端权威更新。
            MonsterActionData a = Snapshot(scene);
            if (Vector2.Distance(new Vector2(start.X, start.Z), new Vector2(a.X, a.Z)) > 2f) moved = true;
            if (a.Phase != MonsterActionPhase.Attack || a.TargetRoleId != target.mainRoleInfo.BaseInfo.RoleId) continue;
            Check(!requireChase || moved, "出现指定目标的挥刀请求，追击位移符合预期");
            Check(a.SkillId == OrcAttack, "使用 RO 普攻 1000118401");
            return a;
        }
        throw new Exception("40 秒内没有向指定角色起刀 " + target.GetID());
    }

    /// <summary>逐帧观察实际扣血，允许原表闪避。用当前这次起刀的已过时间核验真正命中点。</summary>
    /// <param name="scene">兽人正在攻击目标的权威副本。</param>
    /// <param name="role">保持在射程内、未离线的目标。</param>
    /// <returns>第一次出现实际扣血的逻辑时间，单位毫秒。</returns>
    private static int WaitDamage(SceneWaveSpawner scene, OnlineRole role)
    {
        // 保存入参时的真实血量，未命中或闪避不算一次成功结算。
        int startedHp = role.mainRoleInfo.BaseInfo.CurrHp;
        // 最多观察 60 秒模拟时间，覆盖多次连续闪避。
        for (int frame = 0; frame < 1200; frame++)
        {
            Tick();
            if (role.mainRoleInfo.BaseInfo.CurrHp >= startedHp) continue;
            // 已命中这一刀的权威动作快照，允许最后一个逻辑帧取整误差。
            MonsterActionData action = Snapshot(scene);
            Check(action.ElapsedMs >= ImpactMs - 1 && action.ElapsedMs <= ImpactMs + FrameMs,
                "真实扣血发生在攻速换算后的结果点，已过 " + action.ElapsedMs + " 毫秒");
            return clock;
        }
        throw new Exception("保持在射程与当前目标内达 60 秒，仍未产生有效伤害");
    }

    /// <summary>将同一实例中收到的怪物通知按协议原样写给客户端重放，仅在运行者指定输出目录时保留。</summary>
    /// <param name="folder">由测试命令指定的临时输出目录，可以为空。</param>
    /// <param name="name">本条真实回包对应的动作。</param>
    /// <param name="bytes">业务协议原样序列化的字节，不包含网络压缩头。</param>
    private static void Evidence(string folder, string name, byte[] bytes)
    {
        if (!string.IsNullOrEmpty(folder)) File.WriteAllBytes(Path.Combine(folder, "test-wire-" + name + ".bin"), bytes);
    }

    /// <summary>构建两名观察同一只兽人的玩家以及另一个实例，测试从建队到出刀的完整调用链。</summary>
    /// <param name="folder">协议样本的可选输出目录。</param>
    private static void CheckInstances(string folder)
    {
        using (Peer a = new Peer(981001))
        using (Peer b = new Peer(981002))
        using (Peer other = new Peer(981003))
        {
            // 通过真实 MS 建队，由 GS 分别运行两份副本。
            MasterTeamProbe teams = MasterTeamProbe.Instance;
            // 正式接口分配的两个独立队伍号。
            ulong firstId = teams.RequestCreateTeam(a.Role, 1000);
            Check(teams.InviteJoinTeam(a.Role, b.Role.GetID()) && teams.AcceptTeamInvatation(b.Role, firstId, a.Role.GetID(), true), "双人组队");
            ulong otherId = teams.RequestCreateTeam(other.Role, 1000);
            Check(!teams.CaptainRequestEnterFBPtc(a.Role, 4060), "双人队伍等待队员确认");
            Check(teams.MemberReplyEnterFBPtc(b.Role, ReplyType.Agree) && teams.CaptainRequestEnterFBPtc(other.Role, 4060), "两支队伍分别进入 4060");
            a.Receive(); b.Receive(); other.Receive();
            Check(a.Entry.Equals(b.Entry) && a.Entry.InstanceId != other.Entry.InstanceId, "真实入场协议同队一致、跨队实例不同");
            Check(a.Entry.MonsterActions.Count == 1 && a.Entry.MonsterActions[0].SpawnId == SpawnId && a.Entry.Monsters.Count == 68,
                "68 个开场战斗怪中仅第 18 波第 1 只接入行为树");
            Check(a.Entry.Monsters.Single(x => x.SpawnId == SpawnId).Hp == 240 && other.Entry.Monsters.Single(x => x.SpawnId == SpawnId).Hp == 240,
                "两个实例的第 18 波均以配置的 240 点生命独立出生");
            Evidence(folder, "entry", a.Entry.ToByteArray());
            Evidence(folder, "other-entry", other.Entry.ToByteArray());
            // 各自队伍从正式入场流程取得的服务端副本对象。
            SceneWaveSpawner scene = GetScene((ulong)a.Role.mainRoleInfo.BaseInfo.InstanceId), separate = GetScene((ulong)other.Role.mainRoleInfo.BaseInfo.InstanceId);
            CheckPatrol(scene, a.Role);
            // 必须在移动段中途重入，避免起点坐标掩盖快照错误。
            MonsterActionData move = Snapshot(scene);
            for (int frame = 0; frame < 200 && move.Phase != MonsterActionPhase.Move; frame++) { Tick(); move = Snapshot(scene); }
            Check(move.Phase == MonsterActionPhase.Move && !teams.CaptainRequestEnterFBPtc(a.Role, 4060), "在巡逻移动中发起重新入场确认");
            Check(teams.MemberReplyEnterFBPtc(b.Role, ReplyType.Agree), "队员确认后重新进入同一副本");
            a.Receive(); b.Receive();
            Check(a.Entry.MonsterActions.Single().Equals(move) && a.Entry.Equals(b.Entry), "重入带回当前运动段和位置");
            Evidence(folder, "reentry-move", a.Entry.ToByteArray());
            StrikeAt(a.Role, move.X, move.Y, move.Z, 10);
            // 将已造成伤害的角色放到另一巡逻点，要求兽人实际追到它附近。
            Vector2 far = Patrol[1];
            if (Vector2.Distance(new Vector2(move.X, move.Z), far) < 5f) far = Patrol[0];
            SetPosition(a.Role, far.X, move.Y, far.Y);
            // 重要目标已有较高伤害，第二玩家较低伤害不会取代它。
            b.Role.mainRoleInfo.BaseInfo.DefExternal = 1324;
            b.Role.mainRoleInfo.BaseInfo.CritResistPoint = 2598;
            StrikeAt(b.Role, move.X, move.Y, move.Z, 1);
            MonsterActionData swing = WaitAttack(scene, a.Role, true);
            Check(a.Role.mainRoleInfo.BaseInfo.CurrHp == StartingHp, "发出挥刀动作时目标尚未扣血");
            // 检查首刀结果点前仍保持满血，允许 50 毫秒固定帧向上取整。
            int started = clock - swing.ElapsedMs;
            while (clock + FrameMs < started + ImpactMs) Tick();
            Check(a.Role.mainRoleInfo.BaseInfo.CurrHp == StartingHp, "约 807 毫秒的原表结果点前保持满血");
            // 使用两次真正造成伤害的时间，命中和暴击由业务公式独立掷骰。
            int firstHit = WaitDamage(scene, a.Role);
            int nextHit = WaitDamage(scene, a.Role);
            Check(nextHit - firstHit >= AttackCycleMs - FrameMs, "相邻伤害遵守换算后的 2420 毫秒攻击周期");
            a.Receive(); b.Receive(); other.Receive();
            // 比较每一条实际怪物通知，包含两个队员先后对兽人的命中和兽人反击。
            Check(a.Notices.SequenceEqual(b.Notices), "两名玩家收到完全相同的实例动作与伤害广播");
            Check(other.Notices.All(n => n.InstanceId == other.Entry.InstanceId && n.PlayerHits.Count == 0), "其他实例没有收到本队玩家伤害或运动数据");
            Check(a.Notices.All(n => n.InstanceId == a.Entry.InstanceId), "本队广播也没有混入另一实例");
            // 选取有明确语义的真实包，供 Unity 端独立重放。
            Evidence(folder, "move", a.Notices.First(n => n.MonsterActions.Any(x => x.Phase == MonsterActionPhase.Move)).ToByteArray());
            Evidence(folder, "chase", a.Notices.First(n => n.MonsterActions.Any(x => x.Phase == MonsterActionPhase.Move && x.TargetRoleId > 0)).ToByteArray());
            Evidence(folder, "orc-hit", a.Notices.First(n => n.Monsters.Any(x => x.SpawnId == SpawnId && x.Hp > 0 && x.Damage > 0)).ToByteArray());
            Evidence(folder, "attack", a.Notices.First(n => n.MonsterActions.Any(x => x.Phase == MonsterActionPhase.Attack)).ToByteArray());
            Evidence(folder, "player-hit", a.Notices.First(n => n.PlayerHits.Any(x => x.TargetRoleId == a.Role.mainRoleInfo.BaseInfo.RoleId)).ToByteArray());
            // 下一刀足以杀死首要目标，应观察到其真正的零血结果，再转向第二仇恨来源。
            a.Role.mainRoleInfo.BaseInfo.CurrHp = 1;
            for (int frame = 0; frame < 1200 && a.Role.mainRoleInfo.BaseInfo.CurrHp != 0; frame++) Tick();
            Check(a.Role.mainRoleInfo.BaseInfo.CurrHp == 0, "普攻致死后玩家血量钳制到零");
            WaitAttack(scene, b.Role, false);
            WaitDamage(scene, b.Role);
            a.Receive(); b.Receive();
            // 护甲角色的首次真实命中和未防御目标的首次真实命中均来自下行通知，含 RO 随机浮动。
            MonsterPlayerHitData armoredHit = b.Notices.SelectMany(n => n.PlayerHits).First(h => h.TargetRoleId == b.Role.mainRoleInfo.BaseInfo.RoleId && h.Damage > 0);
            MonsterPlayerHitData lightHit = a.Notices.SelectMany(n => n.PlayerHits).First(h => h.TargetRoleId == a.Role.mainRoleInfo.BaseInfo.RoleId && h.Damage > 0);
            Check(armoredHit.Damage < lightHit.Damage && armoredHit.Hp == b.Role.mainRoleInfo.BaseInfo.CurrHp,
                "真实包中重甲目标伤害 " + armoredHit.Damage + " 低于无防目标 " + lightHit.Damage + "，血量与服务器一致");
            // 致死前目标只有 1 点生命，实际扣血字段应为 1。
            MonsterAttrUpdateNtf lethal = a.Notices.First(n => n.PlayerHits.Any(h => h.TargetRoleId == a.Role.mainRoleInfo.BaseInfo.RoleId && h.Hp == 0));
            Check(lethal.PlayerHits.Any(h => h.TargetRoleId == a.Role.mainRoleInfo.BaseInfo.RoleId && h.Damage == 1), "玩家致死包实际扣血为 1，最终为零");
            Evidence(folder, "player-lethal", lethal.ToByteArray());
            Check(teams.LeaveTeam(b.Role.GetID()), "正在被攻击的第二目标退出该副本");
            // 已离开本次实例的角色不应再被在途结果点命中。
            int hp = b.Role.mainRoleInfo.BaseInfo.CurrHp;
            for (int frame = 0; frame < 70; frame++) Tick();
            Check(Snapshot(scene).TargetRoleId == 0 && b.Role.mainRoleInfo.BaseInfo.CurrHp == hp, "死亡或退出玩家均被清出仇恨，离开后没有补扣");
            // 另一实例的兽人仍是完整的 240 血，独立建立仇恨后在挥刀前摇期间杀死它。
            MonsterActionData fresh = Snapshot(separate);
            StrikeAt(other.Role, fresh.X, fresh.Y, fresh.Z, 1);
            SetPosition(other.Role, fresh.X, fresh.Y, fresh.Z + 0.5f);
            MonsterActionData pending = WaitAttack(separate, other.Role, false);
            StrikeAt(other.Role, pending.X, pending.Y, pending.Z, 500);
            hp = other.Role.mainRoleInfo.BaseInfo.CurrHp;
            for (int frame = 0; frame < 70; frame++) Tick();
            other.Receive();
            Check(Snapshot(separate).Phase == MonsterActionPhase.Dead && other.Role.mainRoleInfo.BaseInfo.CurrHp == hp,
                "前摇中死亡取消本刀，后续帧不再移动或出刀");
            Check(other.Notices.Any(n => n.Monsters.Any(x => x.SpawnId == SpawnId && x.Hp == 239)), "相同出生号的第二实例保有独立的初始生命");
            Evidence(folder, "monster-dead", other.Notices.First(n => n.Monsters.Any(x => x.SpawnId == SpawnId && x.Hp == 0)).ToByteArray());
            Check(teams.LeaveTeam(a.Role.GetID()) && teams.LeaveTeam(other.Role.GetID()), "两个实例的最后一位成员均已正式退出");
            a.Receive(); other.Receive();
            // 检查已解散实例的帧订阅真正结束，不再向离开的角色广播动作。
            int priorCount = a.Notices.Count + other.Notices.Count;
            for (int frame = 0; frame < 80; frame++) Tick();
            a.Receive(); other.Receive();
            Check(priorCount == a.Notices.Count + other.Notices.Count && teams.GetInstanceId(a.Role.GetID()) == 0 && teams.GetInstanceId(other.Role.GetID()) == 0,
                "两个实例均回收，随后四秒未下发旧兽人通知");
        }
    }

    /// <summary>测试入口，应从单独生成的 GameServer 输出目录运行。</summary>
    /// <param name="args">可选首参数指定已存在的临时目录，用来保存真实协议样本。</param>
    public static int Main(string[] args)
    {
        LubanMgr.Instance.Init();
        CheckDefense();
        CheckMovedHit();
        // 测试输出位置由命令明确指定，不向当前工作区的正式资源目录写样本。
        string folder = string.Empty;
        if (args.Length > 0) folder = args[0];
        // 独立 MS 使用默认服务端口，隔离启动器可通过第二个参数覆盖。
        int port = NetDefine.MasterServerPort;
        if (args.Length > 1) port = int.Parse(args[1]);
        using (MasterTeamProbe probe = new MasterTeamProbe(port))
        {
            MasterTeamProbe.Instance = probe;
            CheckInstances(folder);
        }
        Console.WriteLine("ORC_WAVE18_AI: PASS " + passed);
        return 0;
    }
}
