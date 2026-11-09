using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>当前副本的战斗资源入口；每个场景实例拥有自己的技能缓存和导航查询器。</summary>
internal sealed class DungeonBattleConfig
{
    /// <summary>当前配置选定的原表、动作、公式与曲线目录。</summary>
    internal readonly MoshentaSkillCatalog Catalog;
    /// <summary>当前地图的导航查询器，实例内所有实体共享。</summary>
    internal readonly MNavigation Navigation;
    /// <summary>当前副本行为树XML所在绝对目录，引用树也从此处解析。</summary>
    internal readonly string TreeDirectory;
    /// <summary>当前场景树名；空值表示沿用场景XML的事件规则。</summary>
    internal readonly string SceneTree;
    /// <summary>原时间轴Lua脚本与公共helper所在绝对目录。</summary>
    internal readonly string LuaDirectory;
    /// <summary>代码时间轴导出的计划XML绝对路径。</summary>
    internal readonly string TimeAxesPath;
    /// <summary>本实例选择的Lua或Code执行模式。</summary>
    internal readonly string TimelineMode;
    /// <summary>NPC配置号到EntityTable号的本副本绑定。</summary>
    private readonly Dictionary<int, int> npcEntities = new Dictionary<int, int>();
    /// <summary>本副本实际登记的实体时间轴，未登记的实体不创建时间轴。</summary>
    private readonly Dictionary<int, DungeonTimelineBinding> timelines = new Dictionary<int, DungeonTimelineBinding>();

    /// <summary>读取所选战斗配置并准备该实例共用的只读资源。</summary>
    /// <param name="path">相对发布目录或绝对路径的正式配置文件。</param>
    internal DungeonBattleConfig(string path)
    {
        // XML属性只选择现有资源，不改变原RO树、运行表和技能数据。
        XElement config = XElement.Load(Resolve(path));
        Catalog = new MoshentaSkillCatalog(Resolve((string)config.Attribute("SkillDirectory")));
        Navigation = new MNavigation(MNavigationMapSerializer.Load(Resolve((string)config.Attribute("NavigationPath"))));
        TreeDirectory = Resolve((string)config.Attribute("TreeDirectory"));
        SceneTree = (string)config.Attribute("SceneTree") ?? "";
        LuaDirectory = Resolve((string)config.Attribute("LuaDirectory"));
        TimeAxesPath = Resolve((string)config.Attribute("TimeAxesPath"));
        TimelineMode = (string)config.Attribute("TimelineMode");
        // NPC索引与剧情实体号分离，映射由各副本明确提供。
        XElement[] npcs = config.Elements("Npc").ToArray();
        for (int index = 0; index < npcs.Length; index++)
            npcEntities.Add((int)npcs[index].Attribute("npcId"), (int)npcs[index].Attribute("entityId"));
        // 同一实体最多一个时间轴绑定，切换模式仍共用原实例时钟。
        XElement[] bindings = config.Elements("Timeline").ToArray();
        for (int index = 0; index < bindings.Length; index++)
        {
            // 当前实体明确选择的Lua名称及已实现代码配置。
            DungeonTimelineBinding binding = new DungeonTimelineBinding
            {
                EntityId = (int)bindings[index].Attribute("entityId"),
                Script = (string)bindings[index].Attribute("script"),
                CodeProfile = (string)bindings[index].Attribute("codeProfile")
            };
            timelines.Add(binding.EntityId, binding);
        }
    }

    /// <summary>将配置路径解析到发布目录；未声明的时间轴可选路径保持为空。</summary>
    /// <param name="path">配置登记的资源路径。</param>
    private static string Resolve(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
    }

    /// <summary>读取NPC的实体绑定，未登记时保持旧流程忽略NPC的行为。</summary>
    /// <param name="npcId">原场景NPC表编号。</param>
    internal int NpcEntity(int npcId)
    {
        // 未登记返回零，零不代表任何实际实体。
        int entityId;
        if (npcEntities.TryGetValue(npcId, out entityId)) return entityId;
        return 0;
    }

    /// <summary>查询实体的显式时间轴，普通怪物返回空。</summary>
    /// <param name="entityId">原EntityTable编号。</param>
    internal DungeonTimelineBinding Timeline(int entityId)
    {
        // 字典只保存本配置声明的绑定，不推断其他副本的剧情身份。
        DungeonTimelineBinding binding;
        timelines.TryGetValue(entityId, out binding);
        return binding;
    }
}
