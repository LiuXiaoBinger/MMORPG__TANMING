extern alias Server;
using System;
using System.Collections.Generic;
using System.IO;
using MMoRpgCommon.SceneEntity;
using GeneAttributeType = Server::GeneAttributeType;
using GeneComponent = Server::GeneComponent;
using GeneEffectType = Server::GeneEffectType;
using GeneInstance = Server::GeneInstance;
using GenePulse = Server::GenePulse;
using GeneRuntime = Server::GeneRuntime;
using GeneState = Server::GeneState;

/// <summary>使用真实 Luban 表和已编译服务端模块进行独立契约测试，不进入正式场景或预制体。</summary>
public static class GeneRuntimeTests
{
    /// <summary>本轮从真实二进制表加载的基因定义。</summary>
    private static Dictionary<int, cfg.Gene> definitions;
    /// <summary>本轮通过的断言数，用于输出可核对的测试结果。</summary>
    private static int passed;
    /// <summary>组件生命周期测试接收到的周期通知数。</summary>
    private static int componentPulses;

    /// <summary>读取表后检查配置和运行行为；任何断言失败都使测试进程失败。</summary>
    /// <param name="args">第一个参数是项目根目录。</param>
    /// <returns>所有检查通过时返回 0。</returns>
    public static int Main(string[] args)
    {
        // 指定的数据文件即正式共享层消费的 Gene 二进制表。
        string tablePath = Path.Combine(args[0], "LYF_Server/Network/LubanManage/Tb/tbgene.bytes");
        definitions = new cfg.TbGene(new Luban.ByteBuf(File.ReadAllBytes(tablePath))).DataMap;
        VerifyConfiguration();
        VerifyAttributesAndSources();
        VerifyRefreshAndStacking();
        VerifyPeriodicResults();
        VerifyComposites();
        VerifyStatesAndScenarios();
        VerifyServerComponent();
        Console.WriteLine("PASS Gene：" + passed + " 项断言，" + definitions.Count + " 条真实配置。");
        return 0;
    }

    /// <summary>验证原有编号、周期单位和组合引用等表格与程序之间的契约。</summary>
    private static void VerifyConfiguration()
    {
        Check(definitions.Count == 69, "配置数量应为原 23 条与新增 46 条");
        // 原编号不能重排，否则会改变既有装备词条的属性含义。
        for (int id = 1; id <= 23; id++)
        {
            Check(definitions[id].AttributeId == id, "保留原属性编号 " + id);
            Check(definitions[id].GeneTime == 0, "原装备属性仍为常驻 " + id);
        }
        // 通过可按索引访问的原对象列表检查每一条配置。
        List<cfg.Gene> rows = new List<cfg.Gene>(definitions.Values);
        for (int i = 0; i < rows.Count; i++)
        {
            // 当前真实表行，验证结果不修改配置对象。
            cfg.Gene row = rows[i];
            Check(row.GeneTime >= 0 && row.MaxStack >= 0, "寿命与层数范围 " + row.Id);
            if (row.EffectType >= 1 && row.EffectType <= 3)
            {
                Check(row.Interval > 0 && row.Value >= 0, "周期参数 " + row.Id);
            }
            if (row.EffectType != (int)GeneEffectType.Composite) continue;
            // 原表的五个组合槽位只能指向叶子，避免递归组合含义不明确。
            int[] children = { row.AttachGeneId0, row.AttachGeneId1, row.AttachGeneId2, row.AttachGeneId3, row.AttachGeneId4 };
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j] == 0) continue;
                Check(definitions.ContainsKey(children[j]), "组合引用存在 " + row.Id);
                Check(definitions[children[j]].EffectType != 5, "组合引用叶子 " + row.Id);
            }
        }
    }

    /// <summary>验证固定值与百分比计算、独立来源和移除后的无损恢复。</summary>
    private static void VerifyAttributesAndSources()
    {
        // 当前容器只用于这一组属性场景，不影响后续测试。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(3, 1, 20);
        runtime.AddGene(1003, 2);
        Equal(130, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "固定值与百分比合并");
        runtime.AddGene(1003, 3);
        Equal(140, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "不同来源独立生效");
        Check(runtime.RemoveGene(1003, 2), "按来源移除");
        Equal(130, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "保留另一个来源");
        runtime.RemoveGene(1003, 3);
        runtime.Update(200000);
        Equal(120, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "常驻基因不超时");
        Equal(240, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 220), "基础属性改变后重新求值");
        runtime.Clear();
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "移除后恢复基础值");
        runtime.AddGene(1013);
        Equal(160, runtime.CalculateAttribute(GeneAttributeType.CritDamage, 150), "暴伤固定值增加百分点");
        runtime.AddGene(1107);
        Equal(3.5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "减速采用负百分比");
    }

    /// <summary>验证不叠加时只刷新、允许叠加时封顶，并保留周期进度。</summary>
    private static void VerifyRefreshAndStacking()
    {
        // 刷新和叠加场景独立使用一个容器。
        GeneRuntime runtime = new GeneRuntime(definitions);
        // 记录首个实例引用，重复施加应复用它。
        GeneInstance original = runtime.AddGene(1001, 1);
        runtime.Update(4000);
        Equal(6000, original.RemainingMilliseconds, "寿命以毫秒推进");
        Check(ReferenceEquals(original, runtime.AddGene(1001, 1)), "同来源刷新同一实例");
        Equal(10000, original.RemainingMilliseconds, "重新施加刷新寿命");
        Equal(1, original.StackCount, "不可叠加保持一层");
        runtime.Clear();
        runtime.AddGene(1203, 7);
        runtime.Update(500);
        runtime.AddGene(1203, 7);
        runtime.AddGene(1203, 7);
        runtime.AddGene(1203, 7);
        Equal(3, runtime.FindGene(1203, 7).StackCount, "三层上限");
        // 重复施加没有清空此前已累计的半秒周期。
        List<GenePulse> pulses = runtime.Update(500);
        Equal(1, pulses.Count, "刷新后保留周期进度");
        Equal(60, pulses[0].ResolveAmount(1000, 100), "周期数值按层数合计");
        Check(pulses[0].SourceId == 7, "周期结果保留真实来源");
    }

    /// <summary>验证大帧补齐、到期截断、百分比换算及恢复资源类型。</summary>
    private static void VerifyPeriodicResults()
    {
        // 一个大帧跨过多次周期，但只能结算基因真实存续时间。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(1201);
        // 十秒回春每两秒一次，二十五秒更新也只能产生五次。
        List<GenePulse> pulses = runtime.Update(25000);
        Equal(5, pulses.Count, "大帧补齐且不到期外触发");
        Equal(30, pulses[0].ResolveAmount(1000, 100), "固定治疗值");
        Check(pulses[0].EffectType == GeneEffectType.PeriodicHeal, "治疗结果类型");
        Check(runtime.FindGene(1201) == null, "到期删除实例");
        Equal(0, runtime.Update(10000).Count, "到期后不再产生结果");
        runtime.AddGene(1204);
        Equal(0, runtime.Update(1999).Count, "周期前不提前触发");
        pulses = runtime.Update(1);
        Equal(10, pulses[0].ResolveAmount(1000, 50), "中毒按目标生命上限百分比");
        runtime.Clear();
        runtime.AddGene(1202);
        pulses = runtime.Update(2000);
        Check(pulses[0].EffectType == GeneEffectType.PeriodicMana, "回蓝结果类型");
        Equal(10, pulses[0].ResolveAmount(1000, 100), "固定回蓝值");
        Equal(0, runtime.Update(0).Count, "零时间不产生周期");
    }

    /// <summary>验证组合共用寿命，父项移除不会误删另行施加的同编号叶子。</summary>
    private static void VerifyComposites()
    {
        // 同一实体同时持有一个组合和一个独立叶子。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(1501, 1);
        runtime.AddGene(1003, 1);
        Equal(2, runtime.ActiveGenes.Count, "组合只登记父实例");
        Equal(120, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "组合与独立项合计");
        Equal(220, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMax, 200), "组合第二个叶子");
        runtime.RemoveGene(1501, 1);
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "组合移除保留独立叶子");
        Equal(200, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMax, 200), "组合全部叶子一起解除");
        runtime.AddGene(1504);
        Equal(80, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲外防修正");
        Equal(160, runtime.CalculateAttribute(GeneAttributeType.DefInternal, 200), "破甲内防修正");
        runtime.Update(10000);
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "父寿命到期解除叶子");
    }

    /// <summary>验证多来源控制、霸体屏蔽及场景专用效果。</summary>
    private static void VerifyStatesAndScenarios()
    {
        // 默认处于公共场景，只有全局配置可进入此容器。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(1301, 1);
        runtime.AddGene(1301, 2);
        runtime.RemoveGene(1301, 1);
        Check(!runtime.CanMove && !runtime.CanCast, "另一个来源仍保持眩晕");
        runtime.AddGene(1304);
        Check(runtime.CanMove && runtime.CanCast, "霸体屏蔽强控");
        runtime.AddGene(1303);
        Check(runtime.CanMove && !runtime.CanCast, "霸体不屏蔽沉默");
        runtime.Update(5000);
        Check(runtime.GetStates() == GeneState.None, "状态到期解除");
        Check(runtime.AddGene(1017) == null, "公共场景拒绝 PvP 专用基因");
        runtime.SetScenario(2);
        runtime.AddGene(1017);
        runtime.AddGene(1001);
        Equal(150, runtime.CalculateAttribute(GeneAttributeType.ZhuXin, 50), "PvP 诛心加成");
        runtime.SetScenario(1);
        Check(runtime.FindGene(1017) == null, "离开 PvP 移除专用效果");
        Check(runtime.FindGene(1001) != null, "全局效果跨环境保留");
    }

    /// <summary>验证正式服务端工厂装配的组件能够计时、通知和随实体释放。</summary>
    private static void VerifyServerComponent()
    {
        LubanMgr.Instance.Init();
        // 使用正式工厂创建玩家，仍在独立测试进程中，不进入线上场景。
        Server::GameServer.SceneEntity.ROObject entity = Server::GameServer.SceneEntity.EntityFactory.Create(EntityType.Player, 1);
        entity.Initialize();
        // 组件编号 4 保持不变，工厂已经改为具体 GeneComponent。
        GeneComponent component = entity.Components.Get<GeneComponent>(EntityComponentType.Gene);
        component.Pulse += OnComponentPulse;
        component.Runtime.AddGene(1201);
        entity.Update(10000);
        Equal(5, componentPulses, "工厂组件真实驱动周期通知");
        component.Runtime.AddGene(1302);
        entity.Destroy();
        Equal(0, component.Runtime.ActiveGenes.Count, "销毁实体清空基因");
    }

    /// <summary>统计组件实际派发的周期通知。</summary>
    /// <param name="pulse">此次治疗脉冲，必须保留其父配置编号。</param>
    private static void OnComponentPulse(GenePulse pulse)
    {
        Check(pulse.GeneId == 1201, "组件通知保留配置编号");
        componentPulses++;
    }

    /// <summary>检查数值结果，容忍浮点计算的微小误差。</summary>
    /// <param name="expected">业务约定的预期数值。</param>
    /// <param name="actual">实际运行模块计算结果。</param>
    /// <param name="message">失败时应能定位的行为描述。</param>
    private static void Equal(double expected, double actual, string message)
    {
        Check(Math.Abs(expected - actual) < 0.0001, message + "，实际=" + actual + "，期望=" + expected);
    }

    /// <summary>失败时中断测试进程，成功时累计断言数。</summary>
    /// <param name="condition">当前行为是否满足预期。</param>
    /// <param name="message">需要交付的检查描述。</param>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed++;
    }
}
