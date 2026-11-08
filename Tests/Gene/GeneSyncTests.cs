extern alias Server;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Google.Protobuf;
using OnlineRole = Server::OnlineRole;

/// <summary>使用正式服务器组件产生协议字节，供真实客户端组件接收；不连接运行中的服务器。</summary>
public static class GeneSyncTests
{
    /// <summary>本轮已通过的服务器同步断言数。</summary>
    private static int passed;
    /// <summary>验证完整/增量、动态节点、事件次数与到期，并输出跨程序集协议样本。</summary>
    /// <param name="args">首项为当前任务临时协议样本目录。</param>
    /// <returns>全部成功返回零，失败保留真实异常退出。</returns>
    public static int Main(string[] args)
    {
        UIDHelper.ConfigureServerInstanceId(252);
        LubanMgr.Instance.Init();
        // 使用完整正式角色初始化，不伪造运行器宿主或属性组件。
        OnlineRole role = new OnlineRole { mainRoleInfo = new MainRoleInfo { BaseInfo = new RoleBaseInfo
            { RoleId = 94001, JobId = 1, Level = 10, MaxHp = 100, CurrHp = 80, MaxMp = 100, CurrMp = 50, Pos = "0_0_0" } } };
        role.InitSerializes();
        role.InitializeSaveVersion(0);
        role.CompleteRoleDataLoad();
        // 与真实装备使用的同一个角色基因容器。
        GeneRuntime runtime = role.Gene.Runtime;
        // 属性节点只在服务器加一次，客户端接收的是最终110上限。
        GeneBase hp = runtime.AddGene(new MaxHPGene { GeneID = -11, DisplayName = "装备生命", Value = 10 }, 1001, 2, 94001);
        // 真实动态魔神塔入口没有配置行；通过反射访问 internal 仅用于测试边界。
        Type buffType = typeof(OnlineRole).Assembly.GetType("MoshentaBuffInstance");
        // 序列化/反射测试边界使用 object，不作为业务载荷。
        object buff = Activator.CreateInstance(buffType, true);
        buffType.GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buff, 77);
        // 真实原配置行只在边界读取名称，节点不保存配置引用。
        Type catalog = typeof(OnlineRole).Assembly.GetType("MoshentaSkillCatalog");
        buffType.GetField("Row", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buff, catalog.GetMethod("Buff", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 3606301 }));
        typeof(OnlineRole).Assembly.GetType("MoshentaGeneBridge").GetMethod("Add", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { runtime, buff, 3, GeneAttributeType.AtkExternalMin, 9f, false, GeneState.None, new Func<long>(() => -1) });
        // 实际动态节点的编号由正式入口生成，客户端不能依赖同编号配置。
        GeneBase dynamicGene = runtime.ActiveGenes[1];
        Check(dynamicGene is AtkExternalMinGene && !LubanMgr.Instance.GetGeneInfos().ContainsKey(dynamicGene.GeneID), "真实魔神塔节点无需配置行");
        // 次数根的子树用于验证服务器消耗与客户端显示一致。
        CountGene count = new CountGene { GeneID = -12, DisplayName = "受击恢复", TriggerLimit = 2, TriggerEvent = GeneEventType.BeHit, TriggerChance = 10000 };
        count.AddChild(new CurrHPGene { GeneID = -13, Value = 2 });
        runtime.AddGene(count);
        // 有限周期节点只能由服务器造成真实伤害。
        runtime.AddGene(new PeriodicDamageGene { GeneID = -14, DisplayName = "灼烧", Value = 3, IntervalMilliseconds = 1000, DurationSeconds = 2 });
        // 同步状态必须沿既有 CanMove/CanCast 消费链生效。
        GeneBase state = runtime.AddGene(new StateGene { GeneID = -15, DisplayName = "眩晕", States = GeneState.Stun });
        // 首次通知包含登录恢复后的全部真实根。
        RoleAttributeUpdateNtf first = Packet(role);
        Check(first.GenesFull && first.Genes.Count == 5, "首次完整列表包含当前所有根");
        Check(first.BaseInfo.MaxHp == 110, "服务器最终属性100加10得到110");
        Save(args[0], "initial", first);
        Check(Packet(role).Genes.Count == 0, "发送完成后无变化不重复下发列表");
        runtime.Update(400);
        Check(Packet(role).Genes.Count == 0, "普通时间推进不产生节点增量");
        runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit));
        // 真实事件成功才扣次数，关联即时治疗不生成持久根。
        RoleAttributeUpdateNtf triggered = Packet(role);
        Check(triggered.Genes.Count == 1 && triggered.Genes[0].RemainingTriggers == 1, "次数减少产生单根增量");
        Save(args[0], "triggered", triggered);
        runtime.Update(600);
        // 周期资源变化只更新最终资源，不重复发送没有变化的节点。
        RoleAttributeUpdateNtf damaged = Packet(role);
        Check(damaged.BaseInfo.CurrHp == 79 && damaged.Genes.Count == 0, "周期伤害由服务器结算并同步最终79生命");
        Save(args[0], "damaged", damaged);
        runtime.RemoveGene(state);
        // 删除标记保留精确身份，客户端据此撤销状态缓存。
        RoleAttributeUpdateNtf unstunned = Packet(role);
        Check(unstunned.Genes.Count == 1 && unstunned.Genes[0].Removed, "状态移除只发一个删除项");
        Save(args[0], "unstunned", unstunned);
        runtime.Update(1000);
        // 寿命到期由服务器移除，客户端显示到零时不能自行提前移除。
        RoleAttributeUpdateNtf expired = Packet(role);
        Check(expired.Genes.Count == 1 && expired.Genes[0].Removed, "真实到期产生删除项");
        Save(args[0], "expired", expired);
        hp.AllowsStacking = true;
        runtime.AddGene(-11, 1001, null, 2, 94001);
        // 同身份刷新沿用原对象并同步层数和新的最终上限。
        RoleAttributeUpdateNtf stacked = Packet(role);
        Check(stacked.Genes[0].StackCount == 2 && stacked.BaseInfo.MaxHp == 120, "叠层同步120最终上限");
        Save(args[0], "stacked", stacked);
        runtime.RemoveGene(hp);
        runtime.AddGene(new MaxHPGene { GeneID = -16, DisplayName = "更换装备", Value = 5 }, 1002, 2, 94001);
        // 根数量未变也必须收到旧来源删除和新来源添加。
        RoleAttributeUpdateNtf replaced = Packet(role);
        Check(replaced.Genes.Count == 2 && replaced.Genes[0].Removed && replaced.Genes[1].GeneId == -16, "同数量装备替换传递增删");
        Save(args[0], "replaced", replaced);
        runtime.RemoveGenesBySource(1002);
        // 正式穿脱采用同一来源卸载路径。
        RoleAttributeUpdateNtf removed = Packet(role);
        Check(removed.BaseInfo.MaxHp == 100 && removed.Genes[0].Removed, "卸装同步裸上限和删除");
        Save(args[0], "removed", removed);
        runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit));
        // 次数用尽的根由服务器移除。
        RoleAttributeUpdateNtf exhausted = Packet(role);
        Check(exhausted.Genes.Count == 1 && exhausted.Genes[0].Removed, "次数耗尽发送删除");
        Save(args[0], "exhausted", exhausted);
        VerifyDynamicLifecycle(role);
        Console.WriteLine("PASS Server Gene Sync：" + passed + " 项断言，10份真实协议样本。");
        return 0;
    }

    /// <summary>通过真实魔神塔Buff添加/更新/移除验证外部End仍是唯一权威寿命。</summary>
    /// <param name="role">已初始化的正式在线角色。</param>
    private static void VerifyDynamicLifecycle(OnlineRole role)
    {
        // 反射只隔离场景怪物构造，实际Buff容器、配置和角色效果流程保持原实现。
        Server::SceneWaveSpawner scene = (Server::SceneWaveSpawner)FormatterServices.GetUninitializedObject(typeof(Server::SceneWaveSpawner));
        // 本测试所用原场景通知容器，与生产类型相同。
        BindingFlags internalMembers = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(Server::SceneWaveSpawner).GetProperty("Notice", internalMembers).SetValue(scene, new MonsterAttrUpdateNtf());
        // 访问 internal 原Buff容器的构造和函数，不重新实现其生命周期。
        Type runtimeType = typeof(OnlineRole).Assembly.GetType("MoshentaBuffRuntime");
        // 反射边界的对象仅在此测试方法存活，不是业务代理角色。
        object buffs = Activator.CreateInstance(runtimeType, internalMembers, null, new object[] { scene, null, role }, null);
        // 添加前的真实攻击用于验证只撤销一次。
        int before = role.AttrComp.CalculateAttribute().AtkExternalMin;
        runtimeType.GetMethod("Add", internalMembers).Invoke(buffs, new object[] { null, 3606301 });
        // 原嗜血Buff实际创建的四个属性叶子，其中第一个用于验证名称和寿命。
        GeneBase dynamicGene = role.Gene.Runtime.FindGene(-1057700817, 0, 1);
        Check(dynamicGene.DisplayName == "A7：嗜血buff" && dynamicGene.GetDisplayRemainingMilliseconds() == 30000, "真实动态Buff名称和30秒原寿命");
        Check(role.AttrComp.CalculateAttribute().AtkExternalMin == before + 1000, "真实动态属性仅加一次");
        role.Gene.Runtime.Update(35000);
        Check(ReferenceEquals(role.Gene.Runtime.FindGene(dynamicGene.GeneID, 0, 1), dynamicGene), "Gene时间推进不提前删除由外部End管理的叶子");
        typeof(Server::SceneWaveSpawner).GetProperty("Now", internalMembers).SetValue(scene, 5000L);
        Check(dynamicGene.GetDisplayRemainingMilliseconds() == 25000 && dynamicGene.RemainingMilliseconds == -1, "显示查询读取原场景时间而不另起到期流程");
        typeof(Server::SceneWaveSpawner).GetProperty("Now", internalMembers).SetValue(scene, 30000L);
        runtimeType.GetMethod("Update", internalMembers).Invoke(buffs, new object[] { 30000L });
        Check(role.Gene.Runtime.FindGene(dynamicGene.GeneID, 0, 1) == null && role.AttrComp.CalculateAttribute().AtkExternalMin == before, "原End到期移除并只撤销一次属性");
        role.Gene.Runtime.Update(35000);
        runtimeType.GetMethod("Update", internalMembers).Invoke(buffs, new object[] { 31000L });
        Check(role.AttrComp.CalculateAttribute().AtkExternalMin == before, "再次推进不会重复撤销属性");
    }

    /// <summary>直接调用正式属性计算和同步边界，不手写第二套节点转换。</summary>
    /// <param name="role">实际在线角色。</param>
    /// <returns>已包含权威属性和本次节点变化的正式协议。</returns>
    private static RoleAttributeUpdateNtf Packet(OnlineRole role)
    {
        // 与 NotifyAttributeUpdate 使用同一计算和追加方法。
        RoleAttributeUpdateNtf packet = new RoleAttributeUpdateNtf { BaseInfo = role.AttrComp.CalculateAttribute() };
        role.Gene.AppendSynchronization(packet);
        return packet;
    }

    /// <summary>保存服务器生成的 protobuf 字节，真实客户端测试直接解析。</summary>
    /// <param name="directory">本轮临时样本目录。</param>
    /// <param name="name">当前业务步骤。</param>
    /// <param name="packet">正式服务器输出。</param>
    private static void Save(string directory, string name, RoleAttributeUpdateNtf packet)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name + ".bin"), packet.ToByteArray());
    }

    /// <summary>实际结果不符合预期时保留异常退出。</summary>
    /// <param name="condition">真实行为是否满足要求。</param>
    /// <param name="description">失败时需要定位的业务条件。</param>
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        passed++;
    }
}
