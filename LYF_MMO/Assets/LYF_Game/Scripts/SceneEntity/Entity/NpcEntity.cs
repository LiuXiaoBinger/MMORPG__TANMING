using MMoRpgCommon.SceneEntity;

/// <summary>
/// 保存 NPC 的名称、地图、模型、对话和商店等配置，供场景与交互代码读取。
/// 对象池复用前清空这些数据，避免上一个 NPC 的信息留到新 NPC 上。
/// </summary>
public sealed class NpcEntity : MEntity
{
    /// <summary>
    /// 构造 NPC 实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">NPC 配置表 ID</param>
    public NpcEntity(ulong uid, int configId)
        : base(uid, EntityType.Npc, configId)
    {
    }

    /// <summary>NPC 编号字段之一；与 NpcID 独立存储，赋值入口需要分别设置</summary>
    public int NpcId { get; set; }

    /// <summary>NPC 配置编号；修改它不会自动同步 NpcId</summary>
    public int NpcID { get; set; }

    /// <summary>NPC 类型（功能分类：对话、商店、任务等）</summary>
    public int NpcType { get; set; }

    /// <summary>NPC 显示名称</summary>
    public string Name { get; set; }

    /// <summary>所在地图 ID</summary>
    public int MapID { get; set; }

    /// <summary>NPC 模型预制体路径</summary>
    public string PrefabPath { get; set; }

    /// <summary>配置中的 Think 文本；本类只保存原值，不解释或执行其中内容</summary>
    public string Think { get; set; }

    /// <summary>NPC 对话文本</summary>
    public string Talk { get; set; }

    /// <summary>关联商店 ID（0 表示无商店）</summary>
    public int ShopId { get; set; }

    /// <summary>NPC 出生位置坐标字符串</summary>
    public string Position { get; set; }

    /// <summary>
    /// 对象池复用时清空所有 NPC 配置数据
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        NpcId = 0;
        NpcID = 0;
        NpcType = 0;
        Name = null;
        MapID = 0;
        PrefabPath = null;
        Think = null;
        Talk = null;
        ShopId = 0;
        Position = null;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
