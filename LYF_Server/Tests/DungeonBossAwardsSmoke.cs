using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;

/// <summary>在独立游戏进程内通过正式波次、角色存档、背包和回环网关验收逐首领奖励。</summary>
public static class DungeonBossAwardsSmoke
{
    /// <summary>测试反射仅用于进入真实的非公开波次和伤害方法。</summary>
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    /// <summary>三名测试角色所在的达纳托斯实例编号。</summary>
    private const int Instance = 76101;
    /// <summary>本次发布的首领奖励道具魔物硬币。</summary>
    private const int Coin = 503;

    /// <summary>核对实际观察值，首个失败直接令进程返回错误。</summary>
    /// <param name="condition">待验证的业务结果。</param><param name="label">报告中的验收项。</param>
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Console.WriteLine("PASS " + label);
    }

    /// <summary>创建有背包、保存版本和路由会话的隔离在线角色。</summary>
    /// <param name="id">本进程内的角色及 Unity 会话号。</param><param name="link">接收此角色下行的网关连接。</param>
    private static OnlineRole Role(int id, LoopbackLink link)
    {
        // 所有实际奖励均写入这个在线角色自己的正式组件。
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo { RoleId = id, Nickname = "首领测试" + id,
            MapId = 4060, InstanceId = Instance, Level = 20, JobId = 1, CurrHp = 10000, MaxHp = 10000, Pos = "0_0_0" } };
        role.GateSessionId = link.SessionId;
        role.UnitySessionId = id;
        role.InitSerializes();
        role.InitializeSaveVersion(1);
        role.CompleteRoleDataLoad();
        RoleManager.Instance.AddOnlineRole(id, role);
        return role;
    }

    /// <summary>按正式难度与层数构造真实场景，把现有在线角色绑定到该实例。</summary>
    /// <param name="degree">难度配置主键。</param><param name="layer">终绝层数，普通难度为零。</param><param name="roles">已批准的队员编号。</param>
    private static SceneWaveSpawner Scene(int degree, int layer, List<ulong> roles)
    {
        // 反射适配游戏服内部的难度选择对象，不在生产代码增加测试入口。
        Type settings = typeof(SceneWaveSpawner).Assembly.GetType("DungeonDifficultySettings", true);
        // 构造器参数只在此处穿越测试反射边界。
        object selected = Activator.CreateInstance(settings, Hidden, null, new object[] { 4060, degree, layer }, null);
        // 与游戏服创建副本时相同的场景宿主和成员绑定。
        SceneWaveSpawner scene = (SceneWaveSpawner)Activator.CreateInstance(typeof(SceneWaveSpawner), Hidden, null, new[] { selected }, null);
        typeof(SceneWaveSpawner).GetMethod("BindInstance", Hidden).Invoke(scene, new object[] { (ulong)Instance, roles });
        return scene;
    }

    /// <summary>在真实场景创建指定 XML 波次，对活首领造成致命伤害。</summary>
    /// <param name="scene">正式实例宿主。</param><param name="wave">原始波次号。</param><param name="entity">该波的实际首领模板。</param>
    private static void Kill(SceneWaveSpawner scene, int wave, int entity)
    {
        typeof(SceneWaveSpawner).GetMethod("ActivateWave", Hidden).Invoke(scene, new object[] { wave });
        // 场景实体为游戏服内部类型，在测试反射边界按索引取出新实例。
        IList actors = (IList)typeof(SceneWaveSpawner).GetProperty("Actors", Hidden).GetValue(scene);
        // 同一波可用于再次挑战，从末尾找到本次新出生的活实体。
        for (int index = actors.Count - 1; index >= 0; index--)
        {
            // 当前真实 AI 实体，反射读取模板并调用游戏服实际扣血入口。
            object actor = actors[index];
            if ((int)actor.GetType().GetProperty("EntityId", Hidden).GetValue(actor) != entity) continue;
            typeof(SceneWaveSpawner).GetMethod("ChangeActorHp", Hidden).Invoke(scene, new object[] { actor, -int.MaxValue, null });
            return;
        }
        throw new InvalidOperationException("波次缺少模板 " + entity);
    }

    /// <summary>取一次测试动作后发给指定角色的真实首领回包。</summary>
    /// <param name="link">该角色的回环网关。</param><param name="role">预期的接收者。</param>
    private static BasePackage Packet(LoopbackLink link, OnlineRole role)
    {
        // 同一次击杀可能先发背包增量，按业务协议筛选最终的个人结算。
        List<DecodedFrame> frames = link.WaitFrames(NetDefine.CMD_DungeonBossProgress, role.UnitySessionId, 1500);
        // 恢复真实协议号、会话及完整消息体。
        for (int index = 0; index < frames.Count; index++)
            if (frames[index].ProtoCode == NetDefine.CMD_DungeonBossProgress && frames[index].UnitySessionId == role.UnitySessionId)
                return frames[index].Package;
        throw new InvalidOperationException("缺少个人首领通知 " + role.UnitySessionId);
    }

    /// <summary>从已加载的官方背包计算实际拥有的掉落数量。</summary>
    /// <param name="role">正在检查的角色。</param>
    private static long Coins(OnlineRole role)
    {
        return role.GetComponent<RoItemComponent>().GetVirtualItemCount(Coin);
    }

    /// <summary>让真实波次消费编辑器导出的三种物品，并从网关回读个人进度和清单。</summary>
    private static void MultiDrop()
    {
        // 单独的登录会话只接收本角色实际产生的协议。
        LoopbackLink link = new LoopbackLink(true);
        try
        {
            // 在新角色上检验整组发放和同周期重杀。
            OnlineRole role = Role(97604, link);
            List<ulong> members = new List<ulong> { role.GetID() };
            SceneWaveSpawner scene = Scene(1, 0, members);
            RoItemComponent bag = role.GetComponent<RoItemComponent>();
            // 两种虚拟货币需要两格，第三种物品使用普通背包。
            RoItemContainer currency = bag.GetContainer(KnapsackType.RoleVirtualItemPack);
            currency.SetContainerSize(1);
            Kill(scene, 34, 5501);
            DungeonBossProgressNtf result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(link, role).Data);
            Check(result.BagFull && result.Drops.Count == 0 && result.KilledBosses.Count == 1, "整组空间不足仍然记录首领进度");
            Check(Coins(role) == 0 && bag.GetVirtualItemCount(501) == 0 && bag.GetItemNum(2001) == 0, "背包容量不足时三种物品均不发放");
            currency.SetContainerSize(2);
            Kill(scene, 34, 5501);
            result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(link, role).Data);
            Check(result.Drops.Count == 3 && result.ItemId == 503 && result.ItemCount == 4, "客户端收到全清单，旧单项字段也记录首件");
            // 各项编号、数量和展示顺序均来自刚保存的编辑器数据。
            int[] ids = { 503, 501, 2001 };
            int[] counts = { 4, 2, 3 };
            for (int index = 0; index < ids.Length; index++)
                Check(result.Drops[index].ItemId == ids[index] && result.Drops[index].Count == counts[index], "掉落显示顺序及数量 " + ids[index]);
            Check(Coins(role) == 4 && bag.GetVirtualItemCount(501) == 2 && bag.GetItemNum(2001) == 3, "两种货币和普通道具全部进包");
            Kill(scene, 34, 5501);
            result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(link, role).Data);
            Check(result.Drops.Count == 0 && result.ItemCount == 0 && bag.GetItemNum(2001) == 3, "同周期重杀不重复发整组奖励");
            Kill(Scene(2, 0, members), 35, 5502);
            result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(link, role).Data);
            Check(result.NoDropsConfigured && result.Drops.Count == 0 && result.KilledBosses.Count == 1, "另一难度的空掉落只记录击杀进度");
        }
        finally { link.Close(); }
    }

    /// <summary>运行首领名单、多人掉落、资格持久化、周日刷新和终绝跨层回归。</summary>
    /// <param name="args">传入 multi 时读取隔离编辑器导出的多物品配置。</param>
    public static void Main(string[] args)
    {
        LubanMgr.Instance.Init();
        UIDHelper.ConfigureServerInstanceId(1);
        if (args.Length > 0 && args[0] == "multi") { MultiDrop(); return; }
        // 各角色独立连接，避免一次读取提前消费另一个人的通知。
        LoopbackLink firstLink = new LoopbackLink(true);
        LoopbackLink secondLink = new LoopbackLink(true);
        LoopbackLink outsideLink = new LoopbackLink(true);
        LoopbackLink unity = new LoopbackLink(false);
        try
        {
            // 甲已参战、乙稍后进入，丙始终处于同地图的另一实例。
            OnlineRole first = Role(97601, firstLink);
            OnlineRole second = Role(97602, secondLink);
            OnlineRole outside = Role(97603, outsideLink);
            second.mainRoleInfo.BaseInfo.InstanceId++;
            outside.mainRoleInfo.BaseInfo.InstanceId++;
            // 同一份成员表刻意包含未在本实例的角色，检验出席筛选。
            List<ulong> roles = new List<ulong> { first.GetID(), second.GetID(), outside.GetID() };
            // 首领一、二、三来自同一个正式副本。
            SceneWaveSpawner rookie = Scene(1, 0, roles);
            Kill(rookie, 34, 5501);
            // 捕获真实 GS 字节，通过正式 Gate 回包检查后发送到模拟客户端。
            BasePackage drop = Packet(firstLink, first);
            GateClientResponseForwarder.ForwardToUnity(SessionMgr.Instance.GetSession(unity.SessionId), drop);
            // 客户端最后收到的内容才能作为可展示掉落的证据。
            DungeonBossProgressNtf result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(unity, first).Data);
            Check(result.ItemId == Coin && result.ItemCount == 1 && result.KilledBosses.Count == 1, "新秀第一首领自动入包，网关透传个人掉落");
            Check(result.BossTotal == 3 && Coins(first) == 1 && Coins(outside) == 0, "三首领总数及跨实例成员隔离");
            second.mainRoleInfo.BaseInfo.InstanceId = Instance;
            Kill(rookie, 34, 5501);
            Check(DungeonBossProgressNtf.Parser.ParseFrom(Packet(firstLink, first).Data).ItemCount == 0 && Coins(first) == 1, "同日重杀同一首领不重复发放");
            Check(DungeonBossProgressNtf.Parser.ParseFrom(Packet(secondLink, second).Data).ItemCount == 1 && Coins(second) == 1, "同一队中首次参战的队员仍获得首领掉落");
            Kill(rookie, 35, 5502);
            Kill(rookie, 46, 5503);
            Check(Coins(first) == 3 && Coins(second) == 3, "第二和第三首领独立结算");
            // 角色存档序列化回读后，再用次数组件的正式加载重建本周期记录。
            SaveRoleDataReq saved = SaveRoleDataReq.Parser.ParseFrom(first.SaveAllMoudle().ToByteArray());
            Check(saved.RoleCountSnapshot && saved.ItemDataLists.Count > 0 && saved.RoleCountInfoList.Count == 6, "同一角色保存请求同时包含背包和逐首领的击杀及入包记录");
            // 将传输后的字段还原为登录加载所需的次数类型。
            List<RoleCountItemInfo> restored = new List<RoleCountItemInfo>();
            // 每条存档记录对应一个独立行为和稳定配置键。
            for (int index = 0; index < saved.RoleCountInfoList.Count; index++)
            {
                // 一条持久化次数。
                RoleCountInfo row = saved.RoleCountInfoList[index];
                restored.Add(new RoleCountItemInfo(row.Action, row.CountKey, row.Count));
            }
            first.GetComponent<RoleCountComponent>().Load(restored);
            firstLink.ClearFrames();
            typeof(SceneWaveSpawner).GetMethod("SendBossProgress", Hidden).Invoke(rookie, new object[] { first });
            result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(firstLink, first).Data);
            Check(result.KilledBosses.Count == 3 && result.ItemCount == 0, "重新入场从角色存档恢复三只首领名字且不补发");
            // 同一副本的另一难度可以在同一天发另一份奖励。
            SceneWaveSpawner weekly = Scene(2, 0, roles);
            Kill(weekly, 34, 5501);
            Kill(weekly, 34, 5501);
            Check(Coins(first) == 5 && Coins(second) == 5, "初心独立于新秀，同周仅获得两枚一次");
            // 两个新实例复用终绝难度的同一份角色资格。
            Kill(Scene(6, 1, roles), 34, 5501);
            Kill(Scene(6, 10, roles), 34, 5501);
            Check(Coins(first) == 11 && Coins(outside) == 0, "终绝第一层与第十层共用单个首领的周掉落");
            // 用真实业务方法验证每日与周日起点，覆盖跨年同周和周末换周。
            DateTime saturday = new DateTime(2027, 1, 2, 23, 59, 59, DateTimeKind.Utc);
            MethodInfo period = typeof(SceneWaveSpawner).GetMethod("BossPeriod", Hidden);
            Check(!period.Invoke(rookie, new object[] { saturday }).Equals(period.Invoke(rookie, new object[] { saturday.AddSeconds(1) })), "新秀在 UTC 零点刷新");
            Check(period.Invoke(weekly, new object[] { saturday }).Equals(period.Invoke(weekly, new object[] { saturday.AddDays(-2) })), "跨年仍在同一周时保持资格");
            Check(!period.Invoke(weekly, new object[] { saturday }).Equals(period.Invoke(weekly, new object[] { saturday.AddSeconds(1) })), "其他难度在 UTC 周日零点刷新");
            // 将该首领的入包记录置于上个实际周期，验证跨周期重新进包而不影响其他首领。
            RoleCountComponent counts = first.GetComponent<RoleCountComponent>();
            counts.SetCount(RoleCountAction.DungeonBossDrop, 550101, counts.GetCount(RoleCountAction.DungeonBossDrop, 550101) - 1);
            Kill(rookie, 34, 5501);
            Check(Coins(first) == 12 && Coins(second) == 11, "跨周期重新发放，仅影响资格过期的角色");
            // 用真实虚拟背包制造无空格情形，检查失败时仍记录击杀、清出空间后重新击杀可入包。
            RoItemComponent bag = outside.GetComponent<RoItemComponent>();
            RoItemContainer container = bag.GetContainer(KnapsackType.RoleVirtualItemPack);
            outside.mainRoleInfo.BaseInfo.InstanceId = Instance;
            container.SetContainerSize(1);
            bag.AddItem(101, 1, new ChangeReason(ItemChangeReason.ITEM_REASON_DUNGEONS_RESULT), ItemSign.IsNoSign, 0, 0, UpdateType.eUT_Update_All, default(IdipParam));
            Kill(rookie, 35, 5502);
            result = DungeonBossProgressNtf.Parser.ParseFrom(Packet(outsideLink, outside).Data);
            Check(result.BagFull && result.KilledBosses.Count == 1 && Coins(outside) == 0, "满包仍记录击杀并保留本周期掉落资格");
            container.SetContainerSize(2);
            Kill(rookie, 35, 5502);
            Check(Coins(outside) == 1, "清出空间后重新击杀自动入包");
        }
        finally
        {
            firstLink.Close();
            secondLink.Close();
            outsideLink.Close();
            unity.Close();
        }
    }
}
