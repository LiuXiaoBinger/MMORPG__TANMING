extern alias Client;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using ClientRole = Client::RoleEntity;
using MEntity = Client::MEntity;
using MObject = Client::MObject;
using MGeneComponent = Client::MGeneComponent;
using ClientGeneRuntime = Client::ClientGeneRuntime;
using EntityAttributesComponent = Client::EntityAttributesComponent;
using MEventDispatcher = Client::MEventDispatcher;
using MEventType = Client::MEventType;

/// <summary>用正式客户端组件接收服务器测试输出的字节；隔离进程不代替 Unity 联机实机。</summary>
public static class ClientGeneTests
{
    /// <summary>已通过的真实客户端行为断言数。</summary>
    private static int passed;
    /// <summary>接收权威属性与节点，验证重复同步、时间、状态、次数和换装。</summary>
    /// <param name="args">首项为 GeneSyncTests 生成的协议样本目录。</param>
    /// <returns>全部通过返回零，失败保留真实异常退出。</returns>
    public static int Main(string[] args)
    {
        // 反射只绕开 Unity 模型构造，实际属性、角色、基因仍用正式类型。
        ClientRole entity = (ClientRole)FormatterServices.GetUninitializedObject(typeof(ClientRole));
        typeof(MObject).GetField("_eventDispatcher", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entity, new MEventDispatcher());
        // 真正属性组件接收服务器最终值。
        EntityAttributesComponent attributes = new EntityAttributesComponent(entity);
        typeof(MEntity).GetProperty("AttrComp").SetValue(entity, attributes);
        // 真正专用组件和客户端派生运行器持有具体 GeneBase 节点。
        MGeneComponent genes = new MGeneComponent(entity);
        typeof(MEntity).GetProperty("Gene").SetValue(entity, genes);
        typeof(MGeneComponent).GetProperty("Runtime").SetValue(genes, new ClientGeneRuntime(entity, null));
        genes.OnAttachToHost(entity);
        entity.UpdateBaseInfo(new RoleBaseInfo { RoleId = 94001, MaxHp = 100, CurrHp = 80, MaxMp = 100, CurrMp = 50 });
        attributes.Apply(100, 80, 100, 50);
        // 用可观测事件计数验证普通帧与时间校正不触发全列表刷新。
        int changes = 0;
        genes.Changed += delegate { changes++; };
        Check(entity.ApplyAttributeUpdate(Read(args[0], "initial")), "正式角色入口接收服务器完整通知");
        Check(genes.Runtime.ActiveGenes.Count == 5 && attributes.MaxHp == 110, "100加10的服务器110不会客户端再加一次");
        Check(genes.Runtime.FindGene(-11, 1001, 2) is MaxHPGene, "属性映射保留真实具体类型");
        Check(genes.Runtime.ActiveGenes[1] is AtkExternalMinGene, "真实魔神塔无配置动态节点重建具体类型");
        Check(!genes.Runtime.CanMove && !genes.Runtime.CanCast && (attributes.States & GeneState.Stun) != 0, "状态同步进入现有移动施法与属性状态入口");
        // 客户端时间到零不移除权威节点，也不执行周期伤害和次数事件。
        for (int index = 0; index < 300; index++) { genes.Update(16); attributes.Update(16); }
        Check(attributes.CurrentHp == 80 && attributes.MaxHp == 110, "300帧不重复数值结算或周期扣血");
        Check(genes.Runtime.FindGene(-14).RemainingMilliseconds == 0 && genes.Runtime.ActiveGenes.Count == 5, "本地到零只显示零不移除");
        Check(genes.Runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit)) == 0 && ((CountGene)genes.Runtime.FindGene(-12)).RemainingTriggers == 2, "客户端不执行权威次数事件");
        Check(changes == 1, "普通帧不刷新基因列表");
        // 相同完整列表只校正时间，保留根引用和UI刷新计数。
        GeneBase original = genes.Runtime.FindGene(-11, 1001, 2);
        entity.ApplyAttributeUpdate(Read(args[0], "initial"));
        Check(changes == 1 && ReferenceEquals(original, genes.Runtime.FindGene(-11, 1001, 2)), "重复完整同步保留节点且不重建列表");
        entity.ApplyAttributeUpdate(Read(args[0], "triggered"));
        Check(((CountGene)genes.Runtime.FindGene(-12)).RemainingTriggers == 1 && changes == 2, "服务端次数变化刷新一次");
        entity.ApplyAttributeUpdate(Read(args[0], "damaged"));
        Check(attributes.CurrentHp == 79 && changes == 2, "服务器周期结果更新血量但不重建基因列表");
        entity.ApplyAttributeUpdate(Read(args[0], "unstunned"));
        Check(genes.Runtime.CanMove && genes.Runtime.CanCast && attributes.States == GeneState.None, "服务器删除状态恢复原消费入口");
        entity.ApplyAttributeUpdate(Read(args[0], "expired"));
        Check(genes.Runtime.FindGene(-14) == null, "仅服务器删除确认使到期节点离开列表");
        entity.ApplyAttributeUpdate(Read(args[0], "stacked"));
        Check(original.StackCount == 2 && attributes.MaxHp == 120, "层数更新而最终120不重复累加");
        entity.ApplyAttributeUpdate(Read(args[0], "replaced"));
        Check(genes.Runtime.FindGene(-11, 1001, 2) == null && genes.Runtime.FindGene(-16, 1002, 2) != null && attributes.MaxHp == 105, "同数量替换和来源更新正确");
        entity.ApplyAttributeUpdate(Read(args[0], "removed"));
        Check(attributes.MaxHp == 100 && genes.Runtime.FindGene(-16, 1002, 2) == null, "移除不会从服务器100再减5");
        entity.ApplyAttributeUpdate(Read(args[0], "exhausted"));
        Check(genes.Runtime.ActiveGenes.Count == 1 && genes.Runtime.FindGene(-12) == null, "次数耗尽移除但保留动态节点");
        // 旧协议缺少新增字段时等同空增量，不能把已有列表误清空。
        RoleAttributeUpdateNtf empty = new RoleAttributeUpdateNtf { BaseInfo = entity.BaseInfo.Clone() };
        entity.ApplyAttributeUpdate(empty);
        Check(genes.Runtime.ActiveGenes.Count == 1, "旧协议空增量保持当前节点");
        empty.GenesFull = true;
        entity.ApplyAttributeUpdate(empty);
        Check(genes.Runtime.ActiveGenes.Count == 0, "新协议空完整列表明确清空");
        entity.ApplyAttributeUpdate(Read(args[0], "initial"));
        entity.DispatchEvent(MEventType.Destroying, null);
        Check(genes.Runtime.ActiveGenes.Count == 0 && attributes.MaxHp == 110 && attributes.States == GeneState.None, "销毁清状态和节点而不回滚权威最终属性");
        genes.Shutdown();
        attributes.Shutdown();
        Console.WriteLine("PASS Client Gene Sync：" + passed + " 项真实组件断言；未启动联机场景。");
        return 0;
    }

    /// <summary>直接解析服务器正式序列化结果，不包装解析异常。</summary>
    /// <param name="directory">本轮协议字节样本目录。</param>
    /// <param name="name">业务步骤样本名。</param>
    /// <returns>正式生成协议类型。</returns>
    private static RoleAttributeUpdateNtf Read(string directory, string name) { return RoleAttributeUpdateNtf.Parser.ParseFrom(File.ReadAllBytes(Path.Combine(directory, name + ".bin"))); }

    /// <summary>断言失败直接保留异常和非零退出码。</summary>
    /// <param name="condition">真实组件行为是否满足要求。</param>
    /// <param name="description">当前业务验证说明。</param>
    private static void Check(bool condition, string description) { if (!condition) throw new InvalidOperationException(description); passed++; }
}
