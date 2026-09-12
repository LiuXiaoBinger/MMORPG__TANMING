using System.Collections.Generic;

/// <summary>
/// 客户端角色运行时模型，只保存角色数据和客户端组件，不承担网络会话与持久化职责。
/// </summary>
public sealed class ClientRole
{
    /// <summary>角色级脏标记聚合器，供各组件记录延迟消费的变化。</summary>
    public ClientDirtyTracker DirtyTracker { get; private set; }
    /// <summary>角色组件容器，仅本地角色注册完整业务组件。</summary>
    private readonly RoleComponentContainer _components;

    /// <summary>角色状态标记。</summary>
    private readonly Dictionary<RoleStateFlag, bool> _flags =
        new Dictionary<RoleStateFlag, bool>();

    /// <summary>角色组件是否已经初始化。</summary>
    private bool _componentsInitialized;

    /// <summary>本地角色的背包和次数快照是否已经装载。</summary>
    private bool _roleDataReady;

    /// <summary>角色是否已经释放。</summary>
    private bool _disposed;

    /// <summary>角色协议数据，只能由角色模型内部替换。</summary>
    public MainRoleInfo RoleInfo { get; private set; }

    /// <summary>当前角色是否为本机玩家。</summary>
    public bool IsLocal { get; private set; }

    /// <summary>获取角色基础信息。</summary>
    public RoleBaseInfo BaseInfo
    {
        get
        {
            if (RoleInfo == null)
            {
                return null;
            }
            return RoleInfo.BaseInfo;
        }
    }

    /// <summary>使用完整角色数据创建本地角色模型。</summary>
    /// <param name="roleInfo">开始游戏回包中的角色数据。</param>
    public ClientRole(MainRoleInfo roleInfo)
    {
        IsLocal = true;
        DirtyTracker = new ClientDirtyTracker();
        _components = new RoleComponentContainer(this);
        SetRoleInfo(roleInfo);
        InitSerializes();
    }

    /// <summary>使用基础信息创建其他玩家模型。</summary>
    /// <param name="baseInfo">其他玩家的基础数据。</param>
    public ClientRole(RoleBaseInfo baseInfo)
    {
        IsLocal = false;
        DirtyTracker = new ClientDirtyTracker();
        _components = new RoleComponentContainer(this);
        UpdateBaseInfo(baseInfo);
    }

    /// <summary>按固定顺序创建本地角色组件。</summary>
    private void InitSerializes()
    {
        if (!IsLocal || _componentsInitialized)
        {
            return;
        }

        _components.SetComponent(new RoleCountComponent(this));
        _components.SetComponent(new RoItemComponent(this));
        _components.Initialize();
        _componentsInitialized = true;
    }

    /// <summary>装载本地角色的背包和次数完整快照。</summary>
    /// <param name="result">角色背包完整回包。</param>
    /// <returns>装载成功返回 true。</returns>
    public bool LoadRoleData(RoleKanpsackInfoRet result)
    {
        if (!IsLocal || !_componentsInitialized || result == null ||
            result.CmdCode != CmdCode.Succeed || result.RoleKanpsackInfo == null)
        {
            return false;
        }

        RoItemComponent itemComponent = GetComponent<RoItemComponent>();
        RoleCountComponent countComponent = GetComponent<RoleCountComponent>();
        itemComponent.LoadKnapsack(result);
        countComponent.Load(result.RoleKanpsackInfo.RoleCountInfoList);
        _roleDataReady = true;
        _components.AfterLoad();
        return true;
    }

    /// <summary>通知角色及组件已经加入客户端角色世界。</summary>
    public void AfterAddedToWorld()
    {
        _components.AfterAddedToWorld();
    }

    /// <summary>推进角色客户端组件。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public void Update(int deltaMilliseconds)
    {
        if (_disposed)
        {
            return;
        }
        _components.Update(deltaMilliseconds);
    }

    /// <summary>释放角色组件和运行时引用。</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _components.Dispose();
        if (DirtyTracker != null)
        {
            DirtyTracker.ClearAll();
        }
        _flags.Clear();
        RoleInfo = null;
        _roleDataReady = false;
        _disposed = true;
    }

    /// <summary>获取角色编号，基础数据不存在时返回零。</summary>
    public int GetID()
    {
        if (BaseInfo == null)
        {
            return 0;
        }
        return BaseInfo.RoleId;
    }

    /// <summary>判断本地角色业务快照是否已完成装载。</summary>
    public bool IsRoleDataReady()
    {
        return _roleDataReady;
    }

    /// <summary>使用最新完整数据更新本地角色信息。</summary>
    /// <param name="roleInfo">最新角色数据。</param>
    public void SetRoleInfo(MainRoleInfo roleInfo)
    {
        if (roleInfo == null)
        {
            RoleInfo = null;
            return;
        }
        RoleInfo = roleInfo.Clone();
    }

    /// <summary>使用同步消息更新其他玩家基础数据。</summary>
    /// <param name="baseInfo">最新角色基础数据。</param>
    public void UpdateBaseInfo(RoleBaseInfo baseInfo)
    {
        if (baseInfo == null)
        {
            return;
        }
        if (RoleInfo == null)
        {
            RoleInfo = new MainRoleInfo();
        }
        RoleInfo.BaseInfo = baseInfo.Clone();
    }

    /// <summary>获取指定角色组件。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        return _components.GetComponent<T>();
    }

    /// <summary>尝试获取指定角色组件。</summary>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        return _components.TryGetComponent(out component);
    }

    /// <summary>查询角色持有的指定物品数量。</summary>
    public int GetItemCount(int itemId)
    {
        RoItemComponent itemComponent;
        if (!TryGetComponent(out itemComponent))
        {
            return 0;
        }
        return itemComponent.GetItemNum(itemId, ItemBind.kItemBindNo, true);
    }

    /// <summary>通过物品组件增加物品。</summary>
    public ItemBase AddItem(int itemId, int itemCount, ChangeReason reason,
        ItemSign itemSign, int moneyType, long price, bool notifyNow,
        out IdipParam idipParam)
    {
        idipParam = default;
        if (!IsRoleDataReady())
        {
            return null;
        }

        RoItemComponent itemComponent;
        if (!TryGetComponent(out itemComponent))
        {
            return null;
        }

        UpdateType updateType = UpdateType.eUT_Normal;
        if (notifyNow)
        {
            updateType = UpdateType.eUT_Update_All;
        }
        return itemComponent.AddItem(itemId, itemCount, reason, itemSign,
            moneyType, price, updateType, idipParam);
    }

    /// <summary>校验角色是否持有足量物品。</summary>
    public CmdCode CheckItem(int itemId, long itemCount, ItemBind bindOption)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }

        RoItemComponent itemComponent;
        if (!TryGetComponent(out itemComponent))
        {
            return CmdCode.ItemNotExist;
        }
        if (itemCount <= itemComponent.GetItemNum(itemId, bindOption))
        {
            return CmdCode.Succeed;
        }
        if (ItemDesc.IsVirtualItem(itemId))
        {
            return CmdCode.NotEnoughCurrency;
        }
        return CmdCode.InsufficientItems;
    }

    /// <summary>通过物品组件扣除角色物品。</summary>
    public CmdCode ReduceItem(int itemId, long itemCount, ItemChangeReason reason,
        ItemBind bindOption)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }
        if (ItemBase.IsDiamond(itemId))
        {
            return CmdCode.ItemNotExist;
        }

        RoItemComponent itemComponent;
        if (!TryGetComponent(out itemComponent))
        {
            return CmdCode.ItemNotExist;
        }
        return itemComponent.ReduceItem(itemId, itemCount, reason, bindOption);
    }

    /// <summary>读取角色状态标记。</summary>
    public bool GetFlag(RoleStateFlag flag)
    {
        bool value;
        if (_flags.TryGetValue(flag, out value))
        {
            return value;
        }
        return false;
    }

    /// <summary>设置角色状态标记。</summary>
    public void SetFlag(RoleStateFlag flag, bool value)
    {
        _flags[flag] = value;
    }
}
