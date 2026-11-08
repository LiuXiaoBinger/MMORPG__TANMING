extern alias Client;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using ClientMonsterEntity = Client::MonsterEntity;
using MEntity = Client::MEntity;
using MObject = Client::MObject;
using MGeneComponent = Client::MGeneComponent;
using EntityAttributesComponent = Client::EntityAttributesComponent;
using MEventDispatcher = Client::MEventDispatcher;
using MEventType = Client::MEventType;

/// <summary>隔离运行真实客户端属性和基因组件；不创建 Unity 模型，不代替场景实机测试。</summary>
public static class ClientGeneTests
{
    /// <summary>已经通过的行为断言数。</summary>
    private static int passed;

    /// <summary>验证资源上限刷新和销毁事件顺序。</summary>
    /// <returns>全部通过返回 0，失败返回 1。</returns>
    public static int Main()
    {
        try
        {
            // 反射仅在测试装配边界绕开模型构造，仍使用正式实体与组件类型。
            ClientMonsterEntity entity = (ClientMonsterEntity)FormatterServices.GetUninitializedObject(typeof(ClientMonsterEntity));
            typeof(MObject).GetField("_eventDispatcher", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(entity, new MEventDispatcher());
            // 正式属性组件，使用真实属性与资源实现。
            EntityAttributesComponent attributes = new EntityAttributesComponent(entity);
            typeof(MEntity).GetProperty("AttrComp").SetValue(entity, attributes);
            // 正式基因组件；测试只注入无配置工厂的运行容器，使用手工具体节点。
            MGeneComponent genes = new MGeneComponent(entity);
            typeof(MEntity).GetProperty("Gene").SetValue(entity, genes);
            typeof(MGeneComponent).GetProperty("Runtime").SetValue(genes, new GeneRuntime(entity));
            genes.OnAttachToHost(entity);
            attributes.Apply(1000, 1000, 100, 50);
            // 同一生命上限节点刷新前后，当前生命不应被中间撤销过程错误裁切。
            MaxHPGene maximum = new MaxHPGene { GeneID = -1, Value = 200 };
            genes.Runtime.AddGene(maximum);
            attributes.Update(1);
            attributes.ChangeHp(200, 0);
            Check(attributes.CurrentHp == 1200, "真实客户端属性接受基因生命上限");
            genes.Runtime.AddGene(-1);
            attributes.Update(1);
            Check(attributes.CurrentHp == 1200, "刷新生命上限基因不丢失当前生命");
            genes.Runtime.RemoveGene(maximum);
            attributes.Update(1);
            Check(attributes.MaxHp == 1000 && attributes.CurrentHp == 1000, "真正移除后裁切至最终上限");
            genes.Runtime.AddGene(new MaxHPGene { GeneID = -2, Value = 200 });
            genes.Runtime.AddGene(new StateGene { GeneID = -3, States = GeneState.Stun });
            // 真实 Destroy 先派发此事件，再回收普通属性，最后回收专用 Gene。
            entity.DispatchEvent(MEventType.Destroying, null);
            Check(genes.Runtime.ActiveGenes.Count == 0, "销毁事件在属性回收前撤销基因");
            Check(attributes.MaxHp == 1000 && attributes.States == GeneState.None, "销毁前撤销属性和状态");
            attributes.Shutdown();
            genes.Shutdown();
            attributes.Apply(1000, 500, 100, 50);
            Check(attributes.MaxHp == 1000 && attributes.States == GeneState.None, "组件再次使用无负修正残留");
            Console.WriteLine("PASS Client Gene：" + passed + " 项真实组件断言；未启动 Unity 场景。");
            return 0;
        }
        // 测试出口保留失败状态；这里不涉及网络协议解析。
        catch (Exception failure)
        {
            Console.WriteLine("FAIL Client Gene after " + passed + ": " + failure.GetType().Name + " " + failure.Message);
            return 1;
        }
    }

    /// <summary>失败时停止当前测试，成功时累计断言。</summary>
    /// <param name="condition">实际组件行为是否满足预期。</param>
    /// <param name="description">本条行为的中文说明。</param>
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        passed++;
    }
}
