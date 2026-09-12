

using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 存储服务端的在线玩家
/// </summary>
public class OnlineRole
{
    /// <summary>模块时间记录：记录模块最近修改时间与上次落库时间。</summary>
    public struct ModuleTime
    {
        /// <summary>模块最近修改时间（Unix 时间戳，秒）。</summary>
        public long modify_time;

        /// <summary>上次落库时间（毫秒级时间戳）。</summary>
        public ulong save_db_time;

        public ModuleTime(long m_time=0, ulong s_time=0)
        {
            modify_time = m_time;
            save_db_time = s_time;
        }
    }
    
    private Dictionary<int,ModuleTime>  _module_modify_time = new Dictionary<int, ModuleTime>();//控制版本时间戳.
    private Dictionary<UInt64, int> _friends = new Dictionary<ulong, int>();//好友列表（好友uid->好友度）
    
    private readonly RoleComponentContainer _components;
    private readonly SaveRoleDataTimer _saveRoleDataTimer;
    private readonly RoleNetSender _roleNetSender;
    private bool _componentsInitialized;
    /// <summary>中心服角色数据是否已经完成完整装载。</summary>
    private bool _roleDataLoaded;
    /// <summary>是否已经接收并记录中心服权威保存版本。</summary>
    private bool _saveVersionInitialized;
    /// <summary>角色每秒计时累计值，达到一秒后触发保存定时器消费。</summary>
    private int _perSecondElapsedMilliseconds;
    /// <summary>角色级持久化版本，所有组件共用且每次新快照递增。</summary>
    private long _saveVersion;

    public OnlineRole()
    {
        _components = new RoleComponentContainer(this);
        _saveRoleDataTimer = new SaveRoleDataTimer();
        _saveRoleDataTimer.SetRole(this);
        _roleNetSender = new RoleNetSender(this);
    }

    /// <summary>
    /// 向当前角色对应的本端 Unity 客户端发送协议数据。
    /// </summary>
    /// <param name="protoCode">需要发送的协议号，必须大于零。</param>
    /// <param name="data">协议序列化后的数据；无协议体时允许为空。</param>
    /// <returns>成功提交到网关会话时返回 true；参数或会话无效时返回 false。</returns>
    public bool SendToClient(int protoCode, ByteString data)
    {
        if (protoCode <= 0)
        {
            LogMsg.Info("角色客户端发送失败：协议号无效，协议号=" + protoCode, LogMsgType.Error);
            return false;
        }

        if (GateSessionId <= 0)
        {
            LogMsg.Info("角色客户端发送失败：网关会话 ID 无效，角色=" + GetID(), LogMsgType.Error);
            return false;
        }

        if (UnitySessionId <= 0)
        {
            LogMsg.Info("角色客户端发送失败：Unity 会话 ID 无效，角色=" + GetID(), LogMsgType.Error);
            return false;
        }

        Session gateSession = SessionMgr.Instance.GetSession(GateSessionId);
        if (gateSession == null)
        {
            LogMsg.Info("角色客户端发送失败：找不到网关会话，角色=" + GetID() +
                "，网关会话=" + GateSessionId, LogMsgType.Error);
            return false;
        }

        gateSession.SendData(UnitySessionId, protoCode, data);
        return true;
    }

    /// <summary>按固定顺序创建角色组件，并完成首次初始化。</summary>
    public void InitSerializes()
    {
        if (_componentsInitialized)
        {
            return;
        }


        _components.SetComponent(new RoleCountComponent(this));
        _components.SetComponent(new RoItemComponent(this));
        _components.SetComponent(new RoleShopRecord(this));
        _components.Initialize();
        _componentsInitialized = true;
    }

    /// <summary>持久化数据装载完成后调用组件钩子。</summary>
    public void AfterLoad()
    {
        _components.AfterLoad();
    }

    /// <summary>角色加入在线索引后调用组件钩子。</summary>
    public void AfterAddedToRole()
    {
        _components.AfterAddedToRole();
    }

   

    /// <summary>角色固定帧更新和组件更新均只在逻辑线程执行。</summary>
    public void Update(int deltaMilliseconds)
    {
        _components.Update(deltaMilliseconds);
        _roleNetSender.Update(deltaMilliseconds);
        _perSecondElapsedMilliseconds += deltaMilliseconds;
        if (_perSecondElapsedMilliseconds >= 1000)
        {
            _perSecondElapsedMilliseconds %= 1000;
            UpdatePerSecond();
        }
    }

    /// <summary>角色每秒逻辑；定时器只置位，由这里触发角色数据组包与发送。</summary>
    public void UpdatePerSecond()
    {
        if (!_roleDataLoaded)
        {
            return;
        }
        if (_saveRoleDataTimer.ConsumeNeedSave())
        {
            _roleNetSender.SendRoleDataToDB();
        }
    }

    /// <summary>按组件注册顺序收集脏字段，对齐参考项目的 SaveAllMoudle。</summary>
    public SaveRoleDataReq SaveAllMoudle()
    {
        // 背包和次数完整回包到达前禁止生成快照，避免默认版本覆盖中心服数据。
        if (!_roleDataLoaded || !_saveVersionInitialized)
        {
            return null;
        }
        if (mainRoleInfo == null ||
            mainRoleInfo.BaseInfo == null)
        {
            return null;
        }
        if (!_components.NeedSave())
        {
            return null;
        }
        if (_saveVersion == long.MaxValue)
        {
            LogMsg.Info("角色保存版本已达到 long.MaxValue，拒绝生成新快照，角色=" + GetID(), LogMsgType.Error);
            return null;
        }
        _saveVersion++;
        long version = _saveVersion;
        RoleDataFieldManager fieldManager = 
            new RoleDataFieldManager(mainRoleInfo.BaseInfo.RoleId, version);
        _components.Save(fieldManager, version);
        return fieldManager.GetRequest();
    }

    /// <summary>获取角色唯一的持久化快照版本。</summary>
    public long GetSaveVersion()
    {
        return _saveVersion;
    }

    /// <summary>从中心服登录数据初始化角色持久化版本基线。</summary>
    /// <param name="persistedVersion">中心服已确认的角色快照版本。</param>
    public bool InitializeSaveVersion(long persistedVersion)
    {
        // 运行中的角色不得被重复登录回包覆盖版本基线。
        if (_roleDataLoaded)
        {
            LogMsg.Info("角色数据已经完成加载，拒绝再次覆盖保存版本，角色=" + GetID(), LogMsgType.Warn);
            return false;
        }
        if (persistedVersion < 0L)
        {
            persistedVersion = 0L;
        }
        if (_saveVersionInitialized)
        {
            if (_saveVersion != persistedVersion)
            {
                LogMsg.Info("角色保存版本基线冲突，拒绝覆盖本地版本，角色=" + GetID() +
                    "，本地版本=" + _saveVersion + "，回包版本=" + persistedVersion, LogMsgType.Error);
                return false;
            }
            return true;
        }
        _saveVersion = persistedVersion;
        _saveVersionInitialized = true;
        return true;
    }

    /// <summary>在背包和次数数据全部装载后解除保存门闩并启动保存定时器。</summary>
    /// <returns>首次完成加载时返回 true；条件不满足或重复调用时返回 false。</returns>
    public bool CompleteRoleDataLoad()
    {
        if (_roleDataLoaded || !_componentsInitialized || !_saveVersionInitialized)
        {
            return false;
        }

        _roleDataLoaded = true;
        _saveRoleDataTimer.Start();
        return true;
    }

    /// <summary>判断角色是否已经完成中心服数据装载并允许业务修改。</summary>
    /// <returns>数据已就绪返回 true，否则返回 false。</returns>
    public bool IsRoleDataReady()
    {
        return _roleDataLoaded;
    }

    /// <summary>CenterServer 事务成功后才按版本清除组件脏状态。</summary>
    /// <summary>处理中心服成功 ACK，并按版本通知各组件清理脏数据。</summary>
    /// <param name="version">中心服确认的角色保存版本。</param>
    public void OnRoleDataSaved(long version)
    {
        _components.OnSaveAcknowledged(version);
    }

    /// <summary>将角色保存响应交给发送器校验在途版本并处理重试。</summary>
    /// <param name="result">中心服保存响应。</param>
    public void OnSaveRoleDataResult(SaveRoleDataRet result)
    {
        _roleNetSender.OnSaveRoleDataResult(result);
    }

    /// <summary>进入离线保存阶段；角色仍保留在角色索引中，以便接收 ACK 和执行重试。</summary>
    public void BeginOffline()
    {
        State = OnlineRoleState.Offline;
        _saveRoleDataTimer.Stop();
        _roleNetSender.SendRoleDataToDB();
    }

    /// <summary>没有脏数据且没有在途请求时，角色才允许从内存中最终释放。</summary>
    public bool CanDispose()
    {
        return !_roleNetSender.HasInFlightRequest() && !_components.NeedSave();
    }

    /// <summary>角色下线时按组件注册逆序释放资源。</summary>
    public void Dispose()
    {
        _saveRoleDataTimer.Stop();
        _components.Dispose();
    }

    /// <summary>获取角色 ID；角色基础数据尚未装载时返回 0。</summary>
    public uint GetID()
    {
        if (mainRoleInfo != null && mainRoleInfo.BaseInfo != null)
        {
            return (uint)mainRoleInfo.BaseInfo.RoleId;
        }
        return 0;
    }
    #region 成员变量
    // Unity 客户端在网关侧的会话 ID，用于向指定客户端回包。
    public int UnitySessionId;
    // GameServer 与 GateServer 连接的会话 ID，用于定位网关连接。
    public int GateSessionId;
    // 中心服返回的角色快照，进入世界和附近广播使用该数据。
    public MainRoleInfo mainRoleInfo;

    // 在线状态仅由 GameServer 游戏主循环更新，网络线程不得直接修改。
    public OnlineRoleState State = OnlineRoleState.Online;

    // 基础属性实体预留给战斗模块，伤害计算应基于服务端属性。
    private RoleBaseAttrEntity _roleBaseAttrEntity;

    private Dictionary<RoleStateFlag, bool> _flags;
    // 背包明细和容量由 RoItemComponent/BagTransition 管理。


    #endregion


    public int GetItemCount(int ItemId)
    {
        if (GetComponent<RoItemComponent>() != null)
        {
            //return GetComponent<RoItemComponent>().GetItemNum(item_id, calc_bind_item, calc_all_container);
        }
        return 0;
    }

    public ItemBase AddItem(int itemId, int ItemCount,
        ChangeReason reason, ItemSign itemSign, int money_type,
        long Price, bool ntf_now,out IdipParam idipParam)
    {
        if (!IsRoleDataReady())
        {
            idipParam = default;
            return null;
        }
        if (GetComponent<RoItemComponent>() != null)
        {
            //item_count = GSBackstageCtrlActMgr::Instance()->CheckScoreLimit(this, item_id, item_count, reason);

            UpdateType ntf_type = UpdateType.eUT_Normal;
            if (ntf_now)
            {
                ntf_type = UpdateType.eUT_Update_All;
            }

            idipParam = default;
            return GetComponent<RoItemComponent>().
                AddItem(itemId, ItemCount, reason, itemSign,
                    money_type, Price, ntf_type, idipParam);
        }

        idipParam = default;
        return null;
    }
    
    
    
    
    public T GetComponent<T>() where T : RoleComponentBase
    {
        return _components.GetComponent<T>();
    }

    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        return _components.TryGetComponent(out component);
    }

    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        _components.SetComponent(component);
    }

    public void SetModifyTime(RoleModuleType module)
    {
        
        if (_module_modify_time.ContainsKey((int)module))
        {
            var moduleTime = _module_modify_time[(int)module];
            moduleTime.modify_time =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else
        {
            _module_modify_time.Add((int)module, new ModuleTime(DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        }
    }

    public CmdCode CheckItem(int iteItemId, long iteItemCount, ItemBind iteBindOptionForTake)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }
        if (GetComponent<RoItemComponent>() != null)
        {
            if ((iteItemCount > GetComponent<RoItemComponent>().GetItemNum(iteItemId, iteBindOptionForTake)))
            {
                if (ItemDesc.IsVirtualItem(iteItemId))
                {
                    //todo先货币不足
                    return CmdCode.NotEnoughCurrency;
                }
                return CmdCode.InsufficientItems;
            }
        }
        return CmdCode.Succeed;
    }

    public bool GetFlag(RoleStateFlag e)
    {
        if (_flags.ContainsKey(e))
        {
            return _flags[e];
        }
        return false;
    }
    public void SetFlag(RoleStateFlag e, bool flag)
    {
        _flags[e] = flag;
    }

    //扣除虚拟物品 钱特殊钱财
    public CmdCode ReduceItem(int descItemId, long descItemCount, ItemChangeReason getReason, ItemBind descBindOptionForTake)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }
        if (ItemBase.IsDiamond(descItemId))
        {
            //LogMsg.Info("[道具系统] %s 扣除一级货币:%d_%" lld ", reason %d 不允许", MarkToString().c_str(), item_id, item_count,reason);
            return CmdCode.ItemNotExist;
        }
        if (GetComponent<RoItemComponent>() != null)
        {
            return GetComponent<RoItemComponent>().
                ReduceItem(descItemId, descItemCount, getReason, descBindOptionForTake);
        }
        return CmdCode.ItemNotExist;
    }
}
