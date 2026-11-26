/// <summary>区服场景调度和地图分线协议编号，两条玩家指令经网关进入控制服。</summary>
public static class SceneLineCode
{
    /// <summary>查询当前地图可进入的线路。</summary>
    public const ushort List = 11300;
    /// <summary>申请进入本地图的指定线路。</summary>
    public const ushort Change = 11301;
    /// <summary>游戏节点向控制服登记本次可承载的场景。</summary>
    public const ushort Register = 11302;
    /// <summary>网关向控制服申请场景名额，结果含目标实例。</summary>
    public const ushort Allocate = 11303;
    /// <summary>原 GS 保存并移交在线角色。</summary>
    public const ushort Depart = 11304;
    /// <summary>目标 GS 接收角色，向网关提交接管回执。</summary>
    public const ushort Arrive = 11305;
    /// <summary>网关提交最终场景位置，释放原线路名额。</summary>
    public const ushort Commit = 11306;
    /// <summary>网关取消待转入名额，保留原场景。</summary>
    public const ushort Cancel = 11307;
    /// <summary>当前玩家连接关闭，回收其角色和分线人数。</summary>
    public const ushort Disconnect = 11308;
    /// <summary>GS 向本区 MS 更新角色宿主和队伍展示属性。</summary>
    public const ushort Presence = 11309;
    /// <summary>进本前由 MS 将异 GS 的队员移交给同一个实例宿主。</summary>
    public const ushort Gather = 11310;
    /// <summary>角色进图后告知客户端当前可选线状态，零表示正在独立副本。</summary>
    public const ushort State = 11311;
    /// <summary>原 GS 已完成离线保存，可以释放角色归属并允许再次登录。</summary>
    public const ushort Saved = 11312;
    /// <summary>网关向共享中心登记本区已经可以接入，载荷仅使用节点登记消息的 RealmId。</summary>
    public const ushort RealmOnline = 11313;
    /// <summary>网关撤下当前连接登记的区服入口。</summary>
    public const ushort RealmOffline = 11314;
    /// <summary>走入公共城市传送门，服务端回送已进入的场景或拒绝原因。</summary>
    public const ushort Portal = 11315;
    /// <summary>控制服要求 GS 建立公共场景；GS 用同一协议号回送建好的实例。</summary>
    public const ushort CreateStaticScene = 11316;
    /// <summary>控制服通知宿主回收已无玩家的弹性公共场景。</summary>
    public const ushort CloseStaticScene = 11317;
    /// <summary>场景宿主在评分模式下上报 CPU 占用和活跃副本数量。</summary>
    public const ushort HostLoad = 11318;
}
