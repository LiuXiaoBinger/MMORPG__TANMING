/// <summary>背包模型测试宿主，仅提供物品组件依赖的角色资料和真实脏标记。</summary>
public sealed class PlayerEntity
{
    /// <summary>测试角色资料，不连接正式玩家存档。</summary>
    public MainRoleInfo RoleInfo { get; private set; }
    /// <summary>真实客户端脏标记，用于验证旧格位与新格位刷新。</summary>
    public ClientDirtyTracker DirtyTracker { get; private set; }
    /// <summary>创建与场景和相机无关的隔离宿主。</summary>
    /// <param name="info">仅供当前模型回归使用的角色资料。</param>
    public PlayerEntity(MainRoleInfo info)
    {
        RoleInfo = info;
        DirtyTracker = new ClientDirtyTracker();
    }
    /// <summary>返回隔离角色编号，供物品日志引用。</summary>
    public int GetID() { return RoleInfo.BaseInfo.RoleId; }
}
