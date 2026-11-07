extern alias Server;
using System;
using System.Collections.Generic;
using System.IO;
using MMoRpgCommon.SceneEntity;
using GeneAttributeType = Server::GeneAttributeType;
using GeneComponent = Server::GeneComponent;
using GeneEffectType = Server::GeneEffectType;
using GeneEventType = Server::GeneEventType;
using GeneInstance = Server::GeneInstance;
using GenePulse = Server::GenePulse;
using GeneRuntime = Server::GeneRuntime;
using GeneState = Server::GeneState;
using GeneTriggerArgs = Server::GeneTriggerArgs;

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
        VerifyAnyTypeComposites();
        VerifyEventsAndCounts();
        VerifyNestedScenariosAndCycles();
        VerifyEquipmentTriggers();
        VerifyServerComponent();
        Console.WriteLine("PASS Gene：" + passed + " 项断言，" + definitions.Count + " 条真实配置。");
        return 0;
    }

    /// <summary>验证原有编号、周期单位和组合引用等表格与程序之间的契约。</summary>
    private static void VerifyConfiguration()
    {
        Check(definitions.Count == 107, "原80条及本轮27条装备特技基因完整导出");
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
            if (row.EffectType == (int)GeneEffectType.Event) Check(row.TriggerEvent > 0, "事件类型必须指定 " + row.Id);
            if (row.EffectType == (int)GeneEffectType.Count) Check(row.TriggerCount > 0, "次数必须为正 " + row.Id);
            // 所有类型都允许跨 Sheet 引用，包含继续引用组合的嵌套情况。
            int[] children = { row.AttachGeneId0, row.AttachGeneId1, row.AttachGeneId2, row.AttachGeneId3, row.AttachGeneId4 };
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j] == 0) continue;
                Check(definitions.ContainsKey(children[j]), "组合引用存在 " + row.Id);
            }
            Check(new GeneRuntime(definitions, row.Scenario).AddGene(row.Id) != null, "完整配置可展开 " + row.Id);
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

    /// <summary>验证属性、周期、状态均可携带关联基因，组合也能再次嵌套组合。</summary>
    private static void VerifyAnyTypeComposites()
    {
        // 属性根同时携带速度效果，覆盖只影响根自身的数值。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(1022, 7, 20);
        Equal(120, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "属性根数值覆盖");
        Equal(6.5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "属性携带移速");
        runtime.RemoveGene(1022, 7);
        Equal(5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "关联效果一起移除");
        runtime.AddGene(1205);
        Equal(3.5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "周期携带减速");
        Equal(20, runtime.Update(1000)[0].ResolveAmount(1000, 100), "周期根继续结算自身");
        runtime.AddGene(1305);
        Check(!runtime.CanMove, "状态根提供定身");
        // 第二个更新包含寒毒的两次伤害与状态关联中毒的一次伤害。
        List<GenePulse> pulses = runtime.Update(2000);
        Equal(3, pulses.Count, "状态携带周期效果");
        runtime.Clear();
        runtime.AddGene(1507);
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "多层组合外功");
        Equal(220, runtime.CalculateAttribute(GeneAttributeType.AtkInternalMax, 200), "多层组合内功");
        Check((runtime.GetStates() & GeneState.SuperArmor) != 0, "组合包含状态");
        Equal(1, runtime.Update(2000).Count, "组合包含持续恢复");
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "组合包含事件基因");
        Check(runtime.FindGene(1201) != null, "嵌套事件生成独立回春");
        runtime.RemoveGene(1507);
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "移除根清除普通组合效果");
        Check(runtime.FindGene(1201) != null, "事件已生成的基因独立存续");
    }

    /// <summary>验证事件过滤、次数消耗与补满、来源隔离和事件嵌套次数的持久状态。</summary>
    private static void VerifyEventsAndCounts()
    {
        // 事件的关联项在发生匹配事件前必须保持未激活。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(1401, 11);
        Equal(0, runtime.Update(1000).Count, "事件前没有周期结果");
        Equal(0, runtime.Trigger(GeneEventType.SkillCast), "不同事件不触发");
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "受击事件触发");
        Check(runtime.FindGene(1201, 11) != null, "事件结果保留来源");
        Equal(1, runtime.Update(2000).Count, "事件产生周期基因");
        runtime.Clear();
        // 根次数基因用完即移除，重复施加同来源会补满次数。
        GeneInstance count = runtime.AddGene(1601, 1);
        Equal(0, runtime.Trigger(GeneEventType.None), "空事件不消费次数");
        Equal(0, runtime.Trigger(GeneEventType.AttackHit), "不匹配事件不消费次数");
        Equal(3, count.Effects[0].RemainingTriggers, "初始次数保留");
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "第一次次数触发");
        Equal(2, count.Effects[0].RemainingTriggers, "触发扣一次");
        runtime.AddGene(1601, 1);
        Equal(3, count.Effects[0].RemainingTriggers, "刷新补满次数");
        // 三次成功触发都必须执行效果，最后一次执行后移除根。
        for (int i = 0; i < 3; i++) Equal(1, runtime.Trigger(GeneEventType.BeHit), "三次均生效");
        Check(runtime.FindGene(1601, 1) == null, "次数用完移除根");
        Check(runtime.FindGene(1201, 1) != null, "最后一次结果保留");
        Equal(0, runtime.Trigger(GeneEventType.BeHit), "耗尽后不再响应");
        runtime.Clear();
        runtime.AddGene(1601, 1);
        runtime.AddGene(1601, 2);
        Equal(2, runtime.Trigger(GeneEventType.BeHit), "不同来源独立触发");
        Equal(2, runtime.FindGene(1601, 1).Effects[0].RemainingTriggers, "来源一次数");
        Equal(2, runtime.FindGene(1601, 2).Effects[0].RemainingTriggers, "来源二次数");
        runtime.Clear();
        runtime.AddGene(1403, 9);
        // 事件直接关联次数节点，次数必须在多次事件之间保留，不能每次重建。
        for (int i = 0; i < 3; i++)
        {
            Equal(1, runtime.Trigger(GeneEventType.BeHit), "事件与次数组合");
            Check(runtime.FindGene(1503, 9) != null, "次数关联嵌套组合");
            runtime.RemoveGene(1503, 9);
        }
        Equal(0, runtime.Trigger(GeneEventType.BeHit), "事件中的次数已耗尽");
        Check(runtime.FindGene(1403, 9) != null, "子次数耗尽不移除事件父项");
        runtime.AddGene(1403, 9);
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "刷新事件恢复子次数");
        runtime.Clear();
        runtime.AddGene(1506);
        // 根组合的其他属性不应因为它持有的次数节点耗尽而消失。
        for (int i = 0; i < 3; i++) Equal(1, runtime.Trigger(GeneEventType.SkillCast), "组合中的施法次数");
        Equal(0, runtime.Trigger(GeneEventType.SkillCast), "组合子次数耗尽");
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "其他组合属性仍存在");
    }

    /// <summary>验证嵌套父节点的场景限制和循环配置的明确拒绝。</summary>
    private static void VerifyNestedScenariosAndCycles()
    {
        // 测试专用配置副本，将中间组合限制为副本场景，不改正式数据。
        Dictionary<int, cfg.Gene> adjusted = new Dictionary<int, cfg.Gene>(definitions);
        adjusted[1506] = WithChild(definitions[1506], 1007, 1);
        // 外层 1507 为全局，内层 1506 的场景约束必须传递给 1007。
        GeneRuntime runtime = new GeneRuntime(adjusted);
        runtime.AddGene(1507);
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "中间组合场景阻止子属性");
        runtime.SetScenario(1);
        Equal(115, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "切换副本激活嵌套属性");
        runtime.SetScenario(2);
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "离开副本停止嵌套属性");
        adjusted[1003] = WithChild(definitions[1003], 1501, 0);
        // 1501 引用 1003，1003 又引用 1501，必须在入容器前失败。
        bool rejected = false;
        try { runtime.AddGene(1501); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "检测间接循环引用");
        Check(runtime.FindGene(1501) == null, "无半完成的循环实例");
        adjusted[1601] = WithChild(definitions[1601], 1401, 0);
        adjusted[1401] = WithChild(definitions[1401], 1601, 0);
        rejected = false;
        try { runtime.AddGene(1401); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "事件次数子树同样拒绝循环");
    }

    /// <summary>在测试内按真实 Luban 字段顺序生成单个替换配置，不写正式表格。</summary>
    /// <param name="source">保持其他字段不变的原定义。</param>
    /// <param name="childId">替换第一个关联槽，其余槽位清空。</param>
    /// <param name="scenario">本测试需要的场景限制。</param>
    /// <returns>仅供当前测试配置副本使用的定义。</returns>
    /// <param name="chance">本测试采用的万分比触发概率。</param>
    /// <param name="cooldown">本测试采用的冷却毫秒数。</param>
    /// <param name="target">0 继承宿主，1 事件目标。</param>
    private static cfg.Gene WithChild(cfg.Gene source, int childId, int scenario, int chance = 10000, int cooldown = 0, int target = 0)
    {
        // 这是 Luban 测试序列化边界，字段顺序与生成解析器一致。
        Luban.ByteBuf buffer = new Luban.ByteBuf();
        buffer.WriteInt(source.Id);
        buffer.WriteString(source.EffectDesc);
        buffer.WriteInt(scenario);
        buffer.WriteInt(source.ValueType);
        buffer.WriteInt(childId);
        // 清空其余四个关联槽，确保只测试指定的循环或条件路径。
        for (int i = 0; i < 4; i++) buffer.WriteInt(0);
        buffer.WriteInt(source.IsStack);
        buffer.WriteInt(source.GeneTime);
        buffer.WriteInt(source.EffectType);
        buffer.WriteInt(source.AttributeId);
        buffer.WriteFloat(source.Value);
        buffer.WriteInt(source.Interval);
        buffer.WriteInt(source.MaxStack);
        buffer.WriteInt(source.State);
        buffer.WriteInt(source.TriggerEvent);
        buffer.WriteInt(source.TriggerCount);
        buffer.WriteInt(chance);
        buffer.WriteInt(cooldown);
        buffer.WriteInt(target);
        return new cfg.Gene(buffer);
    }

    /// <summary>验证装备同来源独立槽、目标破甲、概率/冷却/次数和即时回复。</summary>
    private static void VerifyEquipmentTriggers()
    {
        // 同装备两个相同基因槽必须累计，其他来源临时加成不随卸装删除。
        GeneRuntime runtime = new GeneRuntime(definitions);
        runtime.AddGene(3, 300, 20, 1, 7);
        runtime.AddGene(3, 300, 30, 7, 7);
        runtime.AddGene(3, 999, 10);
        Equal(160, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "同装备重复编号按槽累计");
        runtime.RemoveGenesBySource(300);
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "卸装移除全槽并保留其他来源");
        // 仅测试替换概率和冷却，不改正式配置：两次受击触发间隔一秒，目标缺失不消耗次数。
        Dictionary<int, cfg.Gene> adjusted = new Dictionary<int, cfg.Gene>(definitions);
        adjusted[1601] = WithChild(definitions[1601], 82101, 0, 10000, 1000, 1);
        runtime = new GeneRuntime(adjusted);
        // 对方独立容器中的实际外防，作为破甲后续伤害的属性输入。
        GeneRuntime target = new GeneRuntime(adjusted);
        // 原次数实例用于直接核对失败和成功分支的消费。
        GeneInstance counter = runtime.AddGene(1601, 300, null, 1, 7);
        Equal(0, runtime.Trigger(GeneEventType.BeHit), "没有事件目标不能触发");
        Equal(3, counter.Effects[0].RemainingTriggers, "目标缺失不吞次数");
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "存在目标后成功触发");
        Equal(2, counter.Effects[0].RemainingTriggers, "成功才消费一次");
        Equal(90, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲作用实际目标");
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲不作用攻击者");
        Check(target.FindGene(82101, 300).FirerId == 7, "施加角色与装备来源独立保留");
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "成功触发开始冷却");
        Equal(2, counter.Effects[0].RemainingTriggers, "冷却拒绝不吞次数");
        runtime.Update(999);
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "冷却前一毫秒不提前触发");
        runtime.Update(1);
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "冷却到点允许触发");
        target.Update(7999);
        Equal(90, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲持续八秒前仍有效");
        target.Update(1);
        Equal(100, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "八秒到期恢复原防御");
        adjusted[1601] = WithChild(definitions[1601], 82101, 0, 0, 1000, 1);
        runtime = new GeneRuntime(adjusted);
        counter = runtime.AddGene(1601);
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "零概率永不触发");
        Equal(3, counter.Effects[0].RemainingTriggers, "概率失败不吞次数");
        // 原始30%概率使用固定种子，真实随机流应同时出现成功与失败。
        runtime = new GeneRuntime(definitions, 0, new Random(19));
        runtime.AddGene(82001);
        // 本轮一百次命中的实际成功数，不把概率当必定触发。
        int successes = 0;
        for (int i = 0; i < 100; i++) successes += runtime.Trigger(new GeneTriggerArgs(GeneEventType.AttackHit, target));
        Check(successes > 0 && successes < 100, "正式3000万分概率产生成功和失败");
        // 击败立即产生资源结果，不等待 Update 或伪装成持续治疗。
        runtime = new GeneRuntime(definitions);
        // 本测试捕获即时结果，确认没有额外常驻资源节点。
        List<GenePulse> instant = new List<GenePulse>();
        runtime.Pulse += instant.Add;
        runtime.AddGene(84001, 300, null, 0, 7);
        runtime.AddGene(85001, 301, null, 0, 7);
        Equal(2, runtime.Trigger(GeneEventType.Kill), "击败同时触发即时回血与回蓝");
        Equal(2, instant.Count, "不推进时间即得到两个资源结果");
        Equal(2, runtime.ActiveGenes.Count, "即时效果不残留常驻实例");
        // 查找两种类型的换算结果，百分比法力不能误用生命上限。
        int heal = 0;
        // 法力结果使用独立的100点上限。
        int mana = 0;
        for (int i = 0; i < instant.Count; i++)
        {
            if (instant[i].EffectType == GeneEffectType.InstantHeal) heal = instant[i].ResolveAmount(1000, 100);
            if (instant[i].EffectType == GeneEffectType.InstantMana) mana = instant[i].ResolveAmount(1000, 100);
        }
        Equal(100, heal, "击败立即恢复10%最终生命");
        Equal(10, mana, "击败立即恢复10%最终法力");
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
