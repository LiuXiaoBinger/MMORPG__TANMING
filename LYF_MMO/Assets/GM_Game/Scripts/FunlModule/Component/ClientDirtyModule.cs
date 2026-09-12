/// <summary>客户端可延迟消费的角色模块脏标记类型。</summary>
public enum ClientDirtyModule
{
    /// <summary>未知模块。</summary>
    None = 0,
    /// <summary>角色物品和背包页。</summary>
    Knapsack = 1,
    /// <summary>商城、副本和任务使用的次数数据。</summary>
    RoleCount = 2,
    /// <summary>装备模块。</summary>
    Equipment = 3,
    /// <summary>任务模块。</summary>
    Task = 4,
    /// <summary>副本模块。</summary>
    Dungeon = 5,
    /// <summary>拍卖行模块。</summary>
    Auction = 6,
    /// <summary>Buff 模块。</summary>
    Buff = 7
}
