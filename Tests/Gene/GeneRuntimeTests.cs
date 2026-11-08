extern alias Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MMoRpgCommon.SceneEntity;
using GeneComponent = Server::GeneComponent;

/// <summary>使用真实 Luban 表和已编译服务端模块进行独立契约测试，不进入正式场景或预制体。</summary>
public static class GeneRuntimeTests
{
    /// <summary>本轮从真实二进制表加载的基因定义。</summary>
    private static Dictionary<int, cfg.Gene> definitions;
    /// <summary>本轮通过的断言数，用于输出可核对的测试结果。</summary>
    private static int passed;

    /// <summary>读取表后检查配置和运行行为；任何断言失败都使测试进程失败。</summary>
    /// <param name="args">第一个参数是项目根目录。</param>
    /// <returns>所有检查通过时返回 0。</returns>
    public static int Main(string[] args)
    {
        // 指定的数据文件即正式共享层消费的 Gene 二进制表。
        string tablePath = Path.Combine(args[0], "LYF_Server/Network/LubanManage/Tb/tbgene.bytes");
        definitions = new cfg.TbGene(new Luban.ByteBuf(File.ReadAllBytes(tablePath))).DataMap;
        // 配置快照仅用于测试，确认整轮运行没有修改任何原始字段。
        Dictionary<int, string> originalRows = new Dictionary<int, string>();
        // 按稳定键保存生成类型的完整字段输出，不把运行数值写回表行。
        List<int> originalIds = new List<int>(definitions.Keys);
        for (int index = 0; index < originalIds.Count; index++) originalRows.Add(originalIds[index], definitions[originalIds[index]].ToString());
        VerifyConfiguration();
        VerifyNodeFactory();
        VerifyDirectNodes();
        VerifyMultipleAttributes();
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
        // 整轮覆盖刷新、次数消费和周期推进后，所有配置字段仍保持原值。
        for (int index = 0; index < originalIds.Count; index++) Check(originalRows[originalIds[index]] == definitions[originalIds[index]].ToString(), "配置行保持只读 " + originalIds[index]);
        Console.WriteLine("PASS Gene：" + passed + " 项断言，" + definitions.Count + " 条真实配置。");
        return 0;
    }

    /// <summary>验证具体类型映射、每次创建独立状态、配置边界及运行模型的程序集依赖。</summary>
    private static void VerifyNodeFactory()
    {
        // 属性编号 1 至 26 均应返回具有实际属性语义的具体类型。
        Type[] attributeTypes = { typeof(MaxHPGene), typeof(MaxMPGene), typeof(AtkExternalMinGene), typeof(AtkExternalMaxGene),
            typeof(AtkInternalMinGene), typeof(AtkInternalMaxGene), typeof(DefExternalGene), typeof(DefExternalDeltaGene),
            typeof(DefInternalGene), typeof(DefInternalDeltaGene), typeof(HitPointGene), typeof(BlockPointGene),
            typeof(ZhenJiGene), typeof(WuGuGene), typeof(CritPointGene), typeof(CritResistPointGene), typeof(CritDamageGene),
            typeof(CritDamageReduceGene), typeof(HealStrengthGene), typeof(HealBonusGene), typeof(ZhuXinGene), typeof(JianRenGene),
            typeof(QiGene), typeof(MoveSpeedGene), typeof(DamageTakenGene), typeof(HealingReceivedGene) };
        // 所有非属性 EffectType 的实际节点类型，属性槽由上面的具体属性表判断。
        Type[] effectTypes = { typeof(AttributeGeneBase), typeof(PeriodicDamageGene), typeof(PeriodicHealGene), typeof(PeriodicManaGene),
            typeof(StateGene), typeof(CompositeGene), typeof(EventGene), typeof(CountGene), typeof(CurrHPGene), typeof(CurrMPGene) };
        // 工厂连接原索引，创建结果只持有复制后的运行字段。
        LubanGeneFactory factory = new LubanGeneFactory(definitions);
        // 全部正式配置覆盖真实类型与父子树，不使用手写运行替身。
        List<cfg.Gene> rows = new List<cfg.Gene>(definitions.Values);
        for (int index = 0; index < rows.Count; index++)
        {
            // 当前只读配置用于核对一次类型转换。
            cfg.Gene row = rows[index];
            // 工厂应每次创建新的具体运行对象。
            GeneBase node = factory.CreateGene(row.Id);
            if (row.EffectType == 0) Check(node.GetType() == attributeTypes[row.AttributeId - 1], "具体属性类型 " + row.Id);
            else Check(node.GetType() == effectTypes[row.EffectType], "具体效果类型 " + row.Id);
            Check(node.GetType().Assembly == typeof(GeneBase).Assembly, "所有节点来自公共程序集 " + row.Id);
        }
        // 同一编号与工厂两次创建的整个子树及父引用都必须独立。
        GeneBase first = factory.CreateGene(1507, 321);
        // 第二棵树不能继承第一次数值覆盖或子树变更。
        GeneBase second = factory.CreateGene(1507);
        VerifyIndependentTrees(first, second);
        first.Children[0].Value = 987;
        Check(second.Children[0].Value != 987, "子节点数值不跨实例共享");
        Equal(definitions[1507].Value, second.Value, "根数值覆盖不写回配置");
        // 两个宿主共享同一工厂但保存各自的周期、冷却与次数。
        GeneRuntime firstRuntime = CreateRuntime(factory);
        // 第二宿主不会消费第一宿主的运行状态。
        GeneRuntime secondRuntime = CreateRuntime(factory);
        // 相同持续治疗节点分别记时。
        TimeBuffGeneBase firstPeriodic = (TimeBuffGeneBase)firstRuntime.AddGene(1201);
        // 未推进的节点仍保持零周期进度。
        TimeBuffGeneBase secondPeriodic = (TimeBuffGeneBase)secondRuntime.AddGene(1201);
        firstRuntime.Update(333);
        Equal(333, firstPeriodic.ElapsedMilliseconds, "首实例周期独立推进");
        Equal(0, secondPeriodic.ElapsedMilliseconds, "第二实例周期未被推进");
        // 复制专用事件配置仅用于验证同工厂两次创建的冷却与次数独立。
        Dictionary<int, cfg.Gene> adjusted = new Dictionary<int, cfg.Gene>(definitions);
        adjusted[1601] = WithChild(definitions[1601], 1201, 0, 10000, 1000);
        factory = new LubanGeneFactory(adjusted);
        firstRuntime = CreateRuntime(factory);
        secondRuntime = CreateRuntime(factory);
        // 保存两个真实次数节点以核对消费和冷却的原对象。
        CountGene firstCount = (CountGene)firstRuntime.AddGene(1601);
        // 第二节点仍使用相同配置总量，状态独立。
        CountGene secondCount = (CountGene)secondRuntime.AddGene(1601);
        firstRuntime.Trigger(GeneEventType.BeHit);
        Equal(2, firstCount.RemainingTriggers, "首节点消费一次");
        Equal(3, secondCount.RemainingTriggers, "第二节点次数未消费");
        Equal(1000, firstCount.CooldownUntilMilliseconds, "首节点开启自己的冷却");
        Equal(0, secondCount.CooldownUntilMilliseconds, "第二节点冷却未开启");
        // 工厂建好后新增配置仍可读取，覆盖魔神塔内部配置晚注册的真实使用方式。
        adjusted.Remove(23);
        factory = new LubanGeneFactory(adjusted);
        adjusted.Add(23, definitions[23]);
        Check(factory.CreateGene(23) is QiGene, "工厂读取晚注册的原索引");
        // 属性编号 25、26 与移动速度同样必须有具体运行类型。
        Check(attributeTypes.Length == 26, "全部已支持属性都有具体类型");
        // 公共运行程序集绝不能反向引用包含 cfg 的 Network 程序集。
        AssemblyName[] references = typeof(GeneBase).Assembly.GetReferencedAssemblies();
        for (int index = 0; index < references.Length; index++) Check(references[index].Name != "Network", "Common 无 Network 反向依赖");
        // 检查公共节点层及容器的所有实例字段和属性，包括私有自动属性后备字段。
        Type[] runtimeTypes = typeof(GeneBase).Assembly.GetTypes();
        for (int index = 0; index < runtimeTypes.Length; index++)
        {
            // 只检查本任务运行模型，不扩大为公共程序集其他历史类型的审计。
            Type type = runtimeTypes[index];
            if (!typeof(GeneBase).IsAssignableFrom(type) && !type.Name.StartsWith("Gene")) continue;
            // 反射是测试边界，只读取成员签名，不读取私有业务值。
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++) Check(!ReferencesConfiguration(fields[fieldIndex].FieldType), "运行字段无 cfg " + type.Name + "." + fields[fieldIndex].Name);
            // 公共属性同样不能把 cfg.Gene 暴露为运行模型。
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++) Check(!ReferencesConfiguration(properties[propertyIndex].PropertyType), "运行属性无 cfg " + type.Name + "." + properties[propertyIndex].Name);
        }
    }

    /// <summary>验证集合直接持有节点，并且手工组合与事件施加均不依赖配置索引。</summary>
    private static void VerifyDirectNodes()
    {
        // 省略配置工厂，证明具体节点能独立进入正式运行模块。
        GeneRuntime runtime = CreateRuntime();
        // 两秒寿命的生命节点，其运行状态应保存在传入对象本身。
        MaxHPGene health = new MaxHPGene { GeneID = -1, Value = 120, DurationSeconds = 2 };
        Check(ReferenceEquals(health, runtime.AddGene(health, 900, 2, 7)), "添加返回原基因节点");
        Check(ReferenceEquals(health, runtime.ActiveGenes[0]), "角色集合直接保存原节点");
        Equal(220, runtime.CalculateAttribute(GeneAttributeType.MaxHp, 100), "直接节点参与属性计算");
        runtime.Update(500);
        Equal(1500, health.RemainingMilliseconds, "剩余寿命直接保存在节点上");
        Check(health.SourceId == 900 && health.FirerId == 7, "节点直接保存来源与施加者");
        Check(runtime.RemoveGene(health), "按原节点引用移除");
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.MaxHp, 100), "移除节点恢复基础属性");

        // 事件节点直接关联无表格编号的周期节点，实际触发时必须使用节点自身的参数。
        CountGene trigger = new CountGene { GeneID = -2, TriggerEvent = GeneEventType.BeHit,
            TriggerChance = 10000, TriggerLimit = 2, CooldownMilliseconds = 1000, TargetOpponent = true };
        // 每秒恢复 25 点生命、持续三秒；事件产生的每次施加应有独立周期进度。
        PeriodicHealGene heal = new PeriodicHealGene { GeneID = -3, Value = 25,
            IntervalMilliseconds = 1000, DurationSeconds = 3 };
        trigger.AddChild(heal);
        runtime.AddGene(trigger, 901, 0, 7);
        // 两个目标没有配置工厂，分别接收两次独立施加。
        GeneRuntime firstTarget = CreateRuntime();
        // 第二目标的周期不能继承第一目标已经经过的时间。
        GeneRuntime secondTarget = CreateRuntime();
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, firstTarget)), "直接组合第一次触发");
        // 第一目标得到实际类型的独立对象，原事件子树仍保留在施加者上。
        TimeBuffGeneBase firstHeal = (TimeBuffGeneBase)firstTarget.ActiveGenes[0];
        Check(!ReferenceEquals(heal, firstHeal), "跨宿主施加不共享原子节点");
        firstTarget.Update(400);
        Equal(400, firstHeal.ElapsedMilliseconds, "首目标独立推进周期");
        Equal(0, heal.ElapsedMilliseconds, "原事件子节点没有被推进");
        // 修改原子节点的数值，下一次触发应保留此运行参数，不回读初始表值。
        heal.Value = 40;
        runtime.Update(1000);
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, secondTarget)), "直接组合第二次触发");
        Equal(0, trigger.RemainingTriggers, "次数直接保存在原节点上");
        Equal(0, runtime.ActiveGenes.Count, "根次数耗尽移除原节点");
        secondTarget.Update(1000);
        Equal(40, Attributes(secondTarget).LastHpDelta, "事件施加采用当前节点参数");
        firstTarget.Update(600);
        Equal(25, Attributes(firstTarget).LastHpDelta, "前一次施加保留自己的数值与周期");

        // 反射仅用于访问副本内部适配边界，不在正式业务新增测试入口。
        Type bridgeType = typeof(GeneComponent).Assembly.GetType("MoshentaGeneBridge");
        // 原副本实例只需要编号，空施加者表示真实场景来源。
        Type buffType = typeof(GeneComponent).Assembly.GetType("MoshentaBuffInstance");
        // 此处 object 是反射调用边界，业务集合仍严格为 List<GeneBase>。
        object buff = Activator.CreateInstance(buffType, true);
        buffType.GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(buff, 3606001);
        LubanMgr.Instance.Init();
        // 记录正式配置索引大小，桥接添加节点不能往里面注册伪造配置。
        int definitionCount = LubanMgr.Instance.GetGeneInfos().Count;
        // 取实际桥接方法，参数数组仅用于反射调用已有强类型方法。
        MethodInfo add = bridgeType.GetMethod("Add", BindingFlags.Static | BindingFlags.NonPublic);
        add.Invoke(null, new object[] { runtime, buff, 1, GeneAttributeType.MaxHp, 80f, false, GeneState.None });
        Equal(180, runtime.CalculateAttribute(GeneAttributeType.MaxHp, 100), "副本桥接直接创建属性节点");
        add.Invoke(null, new object[] { runtime, buff, 1, GeneAttributeType.MaxHp, 130f, false, GeneState.None });
        Equal(230, runtime.CalculateAttribute(GeneAttributeType.MaxHp, 100), "同一内部编号使用本次数值");
        Equal(definitionCount, LubanMgr.Instance.GetGeneInfos().Count, "副本桥接不修改配置索引");
    }

    /// <summary>递归核对两棵真实工厂结果不存在共享节点，且各自父引用正确。</summary>
    /// <param name="first">第一次创建的运行树。</param>
    /// <param name="second">同编号第二次创建的运行树。</param>
    private static void VerifyIndependentTrees(GeneBase first, GeneBase second)
    {
        Check(!ReferenceEquals(first, second), "节点对象独立 " + first.GeneID);
        Equal(first.Children.Count, second.Children.Count, "两次展开的子项数量一致");
        // 按配置槽顺序逐层核对原运行引用。
        for (int index = 0; index < first.Children.Count; index++)
        {
            Check(ReferenceEquals(first.Children[index].Parent, first), "首树父引用正确");
            Check(ReferenceEquals(second.Children[index].Parent, second), "第二树父引用正确");
            VerifyIndependentTrees(first.Children[index], second.Children[index]);
        }
    }

    /// <summary>检查成员签名是否直接或通过数组、泛型容器保留生成配置类型。</summary>
    /// <param name="type">仅用于测试反射的成员声明类型。</param>
    /// <returns>签名包含 cfg 命名空间类型时返回 true。</returns>
    private static bool ReferencesConfiguration(Type type)
    {
        if (type.Namespace == "cfg") return true;
        if (type.HasElementType) return ReferencesConfiguration(type.GetElementType());
        // Dictionary 或列表的泛型参数也不能藏有配置行。
        Type[] arguments = type.GetGenericArguments();
        for (int index = 0; index < arguments.Length; index++)
        {
            if (ReferencesConfiguration(arguments[index])) return true;
        }
        return false;
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
            Check(CreateRuntime(new LubanGeneFactory(definitions), row.Scenario).AddGene(row.Id) != null, "完整配置可展开 " + row.Id);
        }
    }

    /// <summary>验证固定值与百分比计算、独立来源和移除后的无损恢复。</summary>
    private static void VerifyAttributesAndSources()
    {
        // 当前容器只用于这一组属性场景，不影响后续测试。
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
        // 记录首个实例引用，重复施加应复用它。
        GeneBase original = runtime.AddGene(1001, 1);
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
        int calls = Tick(runtime, 500);
        Equal(1, calls, "刷新后保留周期进度");
        Equal(-60, Attributes(runtime).LastHpDelta, "周期数值按层数合计");
        Check(runtime.FindGene(1203, 7).SourceId == 7, "周期节点保留真实来源");
    }

    /// <summary>验证周期节点直接修改宿主资源，并保留补齐、到期截断与百分比行为。</summary>
    private static void VerifyPeriodicResults()
    {
        // 真实公共节点驱动测试角色的实际属性组件。
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
        runtime.AddGene(1201);
        Equal(5, Tick(runtime, 25000), "大帧补齐且不到期外触发");
        Equal(650, Attributes(runtime).Hp, "五次治疗直接增加150生命");
        Equal(30, Attributes(runtime).LastHpDelta, "固定治疗值");
        Check(runtime.FindGene(1201) == null, "到期删除节点");
        Equal(0, Tick(runtime, 10000), "到期后不再修改资源");
        runtime.AddGene(1204);
        Equal(0, Tick(runtime, 1999), "周期前不提前触发");
        runtime.Update(1);
        Equal(-10, Attributes(runtime).LastHpDelta, "中毒按目标生命上限百分比");
        runtime.Clear();
        runtime.AddGene(1202);
        runtime.Update(2000);
        Equal(10, Attributes(runtime).Mp, "回蓝节点直接增加当前法力");
        Equal(0, Tick(runtime, 0), "零时间不产生周期");
    }

    /// <summary>验证组合共用寿命，父项移除不会误删另行施加的同编号叶子。</summary>
    private static void VerifyComposites()
    {
        // 同一实体同时持有一个组合和一个独立叶子。
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
        runtime.AddGene(1022, 7, 20);
        Equal(120, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "属性根数值覆盖");
        Equal(6.5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "属性携带移速");
        runtime.RemoveGene(1022, 7);
        Equal(5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "关联效果一起移除");
        runtime.AddGene(1205);
        Equal(3.5, runtime.CalculateAttribute(GeneAttributeType.MoveSpeed, 5), "周期携带减速");
        runtime.Update(1000);
        Equal(-20, Attributes(runtime).LastHpDelta, "周期根继续结算自身");
        runtime.AddGene(1305);
        Check(!runtime.CanMove, "状态根提供定身");
        // 第二个更新包含寒毒的两次伤害与状态关联中毒的一次伤害。
        int calls = Tick(runtime, 2000);
        Equal(3, calls, "状态携带周期效果");
        runtime.Clear();
        runtime.AddGene(1507);
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "多层组合外功");
        Equal(220, runtime.CalculateAttribute(GeneAttributeType.AtkInternalMax, 200), "多层组合内功");
        Check((runtime.GetStates() & GeneState.SuperArmor) != 0, "组合包含状态");
        Equal(1, Tick(runtime, 2000), "组合包含持续恢复");
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
        runtime.AddGene(1401, 11);
        Equal(0, Tick(runtime, 1000), "事件前没有周期结果");
        Equal(0, runtime.Trigger(GeneEventType.SkillCast), "不同事件不触发");
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "受击事件触发");
        Check(runtime.FindGene(1201, 11) != null, "事件结果保留来源");
        Equal(1, Tick(runtime, 2000), "事件产生周期基因");
        runtime.Clear();
        // 根次数基因用完即移除，重复施加同来源会补满次数。
        GeneBase count = runtime.AddGene(1601, 1);
        Equal(0, runtime.Trigger(GeneEventType.None), "空事件不消费次数");
        Equal(0, runtime.Trigger(GeneEventType.AttackHit), "不匹配事件不消费次数");
        Equal(3, ((CountGene)count).RemainingTriggers, "初始次数保留");
        Equal(1, runtime.Trigger(GeneEventType.BeHit), "第一次次数触发");
        Equal(2, ((CountGene)count).RemainingTriggers, "触发扣一次");
        runtime.AddGene(1601, 1);
        Equal(3, ((CountGene)count).RemainingTriggers, "刷新补满次数");
        // 三次成功触发都必须执行效果，最后一次执行后移除根。
        for (int i = 0; i < 3; i++) Equal(1, runtime.Trigger(GeneEventType.BeHit), "三次均生效");
        Check(runtime.FindGene(1601, 1) == null, "次数用完移除根");
        Check(runtime.FindGene(1201, 1) != null, "最后一次结果保留");
        Equal(0, runtime.Trigger(GeneEventType.BeHit), "耗尽后不再响应");
        runtime.Clear();
        runtime.AddGene(1601, 1);
        runtime.AddGene(1601, 2);
        Equal(2, runtime.Trigger(GeneEventType.BeHit), "不同来源独立触发");
        Equal(2, ((CountGene)runtime.FindGene(1601, 1)).RemainingTriggers, "来源一次数");
        Equal(2, ((CountGene)runtime.FindGene(1601, 2)).RemainingTriggers, "来源二次数");
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(adjusted));
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
        GeneRuntime runtime = CreateRuntime(new LubanGeneFactory(definitions));
        runtime.AddGene(3, 300, 20, 1, 7);
        runtime.AddGene(3, 300, 30, 7, 7);
        runtime.AddGene(3, 999, 10);
        Equal(160, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "同装备重复编号按槽累计");
        runtime.RemoveGenesBySource(300);
        Equal(110, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "卸装移除全槽并保留其他来源");
        // 仅测试替换概率和冷却，不改正式配置：两次受击触发间隔一秒，目标缺失不消耗次数。
        Dictionary<int, cfg.Gene> adjusted = new Dictionary<int, cfg.Gene>(definitions);
        adjusted[1601] = WithChild(definitions[1601], 82101, 0, 10000, 1000, 1);
        runtime = CreateRuntime(new LubanGeneFactory(adjusted));
        // 对方独立容器中的实际外防，作为破甲后续伤害的属性输入。
        GeneRuntime target = CreateRuntime(new LubanGeneFactory(adjusted));
        // 原次数实例用于直接核对失败和成功分支的消费。
        GeneBase counter = runtime.AddGene(1601, 300, null, 1, 7);
        Equal(0, runtime.Trigger(GeneEventType.BeHit), "没有事件目标不能触发");
        Equal(3, ((CountGene)counter).RemainingTriggers, "目标缺失不吞次数");
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "存在目标后成功触发");
        Equal(2, ((CountGene)counter).RemainingTriggers, "成功才消费一次");
        Equal(90, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲作用实际目标");
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲不作用攻击者");
        Check(target.FindGene(82101, 300).FirerId == 7, "施加角色与装备来源独立保留");
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "成功触发开始冷却");
        Equal(2, ((CountGene)counter).RemainingTriggers, "冷却拒绝不吞次数");
        runtime.Update(999);
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "冷却前一毫秒不提前触发");
        runtime.Update(1);
        Equal(1, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "冷却到点允许触发");
        target.Update(7999);
        Equal(90, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "破甲持续八秒前仍有效");
        target.Update(1);
        Equal(100, target.CalculateAttribute(GeneAttributeType.DefExternal, 100), "八秒到期恢复原防御");
        adjusted[1601] = WithChild(definitions[1601], 82101, 0, 0, 1000, 1);
        runtime = CreateRuntime(new LubanGeneFactory(adjusted));
        counter = runtime.AddGene(1601);
        Equal(0, runtime.Trigger(new GeneTriggerArgs(GeneEventType.BeHit, target)), "零概率永不触发");
        Equal(3, ((CountGene)counter).RemainingTriggers, "概率失败不吞次数");
        // 原始30%概率使用固定种子，真实随机流应同时出现成功与失败。
        runtime = CreateRuntime(new LubanGeneFactory(definitions), 0, new Random(19));
        runtime.AddGene(82001);
        // 本轮一百次命中的实际成功数，不把概率当必定触发。
        int successes = 0;
        for (int i = 0; i < 100; i++) successes += runtime.Trigger(new GeneTriggerArgs(GeneEventType.AttackHit, target));
        Check(successes > 0 && successes < 100, "正式3000万分概率产生成功和失败");
        // 击败事件立即让具体子节点修改角色资源，不等待 Update。
        runtime = CreateRuntime(new LubanGeneFactory(definitions));
        runtime.AddGene(84001, 300, null, 0, 7);
        runtime.AddGene(85001, 301, null, 0, 7);
        Equal(2, runtime.Trigger(GeneEventType.Kill), "击败同时触发即时回血与回蓝");
        Equal(2, Attributes(runtime).Calls, "不推进时间即完成两个资源操作");
        Equal(2, runtime.ActiveGenes.Count, "即时节点不残留常驻集合");
        Equal(600, Attributes(runtime).Hp, "击败直接恢复10%最终生命");
        Equal(10, Attributes(runtime).Mp, "击败直接恢复10%最终法力");
        Check(Attributes(runtime).LastFirerId == 7, "直接资源操作保留实际施加角色");
    }

    /// <summary>验证正式服务端工厂装配的组件能够计时、通知和随实体释放。</summary>
    private static void VerifyServerComponent()
    {
        LubanMgr.Instance.Init();
        // 使用正式工厂创建玩家，仍在独立测试进程中，不进入线上场景。
        Server::GameServer.SceneEntity.ROObject entity = Server::GameServer.SceneEntity.EntityFactory.Create(EntityType.Player, 1);
        entity.MaxHp = 1000;
        entity.CurrentHp = 500;
        entity.MaxMp = 100;
        entity.Initialize();
        // 组件编号 4 保持不变，工厂已经改为具体 GeneComponent。
        GeneComponent component = entity.Components.Get<GeneComponent>(EntityComponentType.Gene);
        component.Runtime.AddGene(1201);
        entity.Update(10000);
        Equal(650, entity.CurrentHp, "实际场景实体通过属性组件完成五次治疗");
        component.Runtime.Clear();
        // 直接资源出口必须保留共享生命接管与实际施加者归因。
        Server::GameServer.SceneEntity.AttributesComponent attributes = entity.Components.Get<Server::GameServer.SceneEntity.AttributesComponent>(EntityComponentType.Attributes);
        // 本次共享生命收到的最终伤害，已经计算承伤倍率。
        int sharedDelta = 0;
        // 真正施加角色，与装备或效果来源 ID 不同。
        ulong sharedFirer = 0;
        // 测试反射边界只安装现有内部委托，业务不增加测试入口。
        typeof(Server::GameServer.SceneEntity.AttributesComponent).GetField("SharedHpChange", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(attributes, new Func<ulong, int, bool>((firer, delta) =>
            {
                sharedFirer = firer;
                sharedDelta = delta;
                entity.CurrentHp += delta;
                return true;
            }));
        component.Runtime.AddGene(new DamageTakenGene { GeneID = -81, Value = 50 });
        component.Runtime.AddGene(new PeriodicDamageGene { GeneID = -82, Value = 20, IntervalMilliseconds = 1000 }, 99, 0, 77);
        component.Runtime.Update(1000);
        Equal(-30, sharedDelta, "共享生命收到子类已结算的伤害");
        Check(sharedFirer == 77 && component.LastDamageFirerId == 77, "真实来源透传到共享生命与死亡归因");
        Equal(620, entity.CurrentHp, "共享生命接管后不重复扣血");
        component.Runtime.AddGene(1302);
        entity.Destroy();
        Equal(0, component.Runtime.ActiveGenes.Count, "销毁实体清空基因");
    }

    /// <summary>验证同一具体基因在一次 OnCall 中修改多属性，并正确叠加与撤销。</summary>
    private static void VerifyMultipleAttributes()
    {
        // 无配置工厂，证明实际行为来自子类函数而非表内属性编号。
        GeneRuntime runtime = CreateRuntime();
        // 一个子类同时修改生命与外攻，支持三层并在两秒后到期。
        MultiAttributeTestGene gene = new MultiAttributeTestGene { GeneID = -8, Value = 10,
            AllowsStacking = true, MaxStack = 3, DurationSeconds = 2 };
        runtime.AddGene(gene, 8);
        Equal(1010, Attributes(runtime).MaxHp, "一个OnCall直接增加生命上限");
        Equal(120, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "同一个OnCall采用独立规则增加外攻");
        runtime.AddGene(-8, 8);
        Equal(1020, Attributes(runtime).MaxHp, "叠层先撤旧修正再执行新修正");
        Equal(140, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "两项属性同时正确叠加");
        runtime.Update(2000);
        Equal(1000, Attributes(runtime).MaxHp, "到期由子类OnRemove撤销生命修正");
        Equal(100, runtime.CalculateAttribute(GeneAttributeType.AtkExternalMin, 100), "到期完整撤销第二项属性");
    }

    /// <summary>为每个独立运行器提供真实的测试宿主与属性组件。</summary>
    /// <param name="factory">配置节点工厂，直接节点测试可省略。</param>
    /// <param name="scenario">测试环境分类。</param>
    /// <param name="random">事件概率使用的确定随机源。</param>
    /// <returns>绑定独立宿主的正式公共运行器。</returns>
    private static GeneRuntime CreateRuntime(GeneFactory factory = null, int scenario = 0, Random random = null)
    {
        return new GeneTestOwner(factory, scenario, random).GeneRuntime;
    }

    /// <summary>取得当前测试宿主实际承受效果的属性组件。</summary>
    /// <param name="runtime">测试角色的正式公共运行器。</param>
    /// <returns>当前宿主的原属性组件。</returns>
    private static GeneTestAttributes Attributes(GeneRuntime runtime) { return ((GeneTestOwner)runtime.Owner).Attributes; }

    /// <summary>推进正式运行器并统计这段时间实际执行的资源操作，仅用于独立测试。</summary>
    /// <param name="runtime">需要推进的运行器。</param>
    /// <param name="milliseconds">逻辑时间跨度。</param>
    /// <returns>本次推进实际执行的资源操作次数。</returns>
    private static int Tick(GeneRuntime runtime, int milliseconds)
    {
        // 从测试属性组件统计直接调用，业务代码不持有测试计数。
        int before = Attributes(runtime).Calls;
        runtime.Update(milliseconds);
        return Attributes(runtime).Calls - before;
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
