

using System;
using System.Collections.Generic;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 存储服务端的在线玩家
/// </summary>
public class OnlineRole
{
    /// <summary>与场景 PlayerEntity 的运行时绑定；OnlineRole 本身仍然只是持久角色数据根。</summary>
    private ulong _sceneUid;

    /// <summary>当前角色在场景中的实体 ID，无效值表示尚未进入场景。</summary>
    public ulong SceneUid
    {
        get { return _sceneUid; }
    }

    /// <summary>判断在线角色是否已绑定场景实体。</summary>
    public bool HasSceneEntity
    {
        get { return _sceneUid > 0UL; }
    }

    /// <summary>绑定场景实体 ID，不把场景实体组件混入角色持久化组件。</summary>
    public void BindSceneEntity(ulong sceneUid)
    {
        if (sceneUid == 0UL)
        {
            throw new ArgumentException("场景实体 UID 必须有效", "sceneUid");
        }
        _sceneUid = sceneUid;
    }

    /// <summary>离开场景或下线时清除实体绑定。</summary>
    public void ClearSceneEntityBinding()
    {
        _sceneUid = 0UL;
    }

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
    
    // 按模块 ID 记录最近修改时间（Unix 秒），用于模块级脏数据版本跟踪。
    private Dictionary<int,ModuleTime>  _module_modify_time = new Dictionary<int, ModuleTime>();
    // 好友关系表（好友 uid -> 好友度），当前未接入好友业务，预留字段。
    private Dictionary<UInt64, int> _friends = new Dictionary<ulong, int>();

    // 角色持久化组件容器，按注册顺序装载背包/次数/商店/技能等模块。
    private readonly RoleComponentContainer _components;
    // 角色级保存定时器，置位后由 UpdatePerSecond 触发组包发送。
    private readonly SaveRoleDataTimer _saveRoleDataTimer;
    // 角色数据发送器，负责把脏字段组包并发往 CenterServer 并处理 ACK 重试。
    private readonly RoleNetSender _roleNetSender;
    // 组件是否已完成首次初始化，防止重复创建组件。
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
        // 技能学习快照、MP 与 CD 只属于当前在线角色，离线后由组件释放。
        _components.SetComponent(new RoleSkillComponent(this));
        // 移动上报沿用当前在线角色的正式组件容器，不使用未绑定世界的历史占位组件。
        _components.SetComponent(new ROMoveComponent(this));
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
        // 离线先中断当前施法，防止旧会话的结束事件作用于重连后的角色。
        RoleSkillComponent skillComponent;
        if (TryGetComponent(out skillComponent))
        {
            skillComponent.InterruptActiveCast(true);
        }
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

    // 角色运行时开关表，保存/重连等状态标志按位记录。
    private Dictionary<RoleStateFlag, bool> _flags;
    // 背包明细和容量由 RoItemComponent/BagTransition 管理。


    #endregion


    /// <summary>
    /// 查询角色持有某物品的数量；当前背包计数逻辑尚未接入，固定返回 0。
    /// </summary>
    /// <param name="ItemId">物品配置 ID。</param>
    /// <returns>当前恒返回 0，待 RoItemComponent 计数查询接入后补全。</returns>
    public int GetItemCount(int ItemId)
    {
        if (GetComponent<RoItemComponent>() != null)
        {
            //历史移植代码：跨容器统计物品数量，待背包模块接入。
            //return GetComponent<RoItemComponent>().GetItemNum(item_id, calc_bind_item, calc_all_container);
        }
        return 0;
    }

    /// <summary>
    /// 向角色背包添加物品的统一入口，转发 RoItemComponent 执行权威发放。
    /// </summary>
    /// <param name="itemId">物品配置 ID。</param>
    /// <param name="ItemCount">发放数量。</param>
    /// <param name="reason">变更原因，用于日志与脏数据分类。</param>
    /// <param name="itemSign">物品绑定/非绑定标记。</param>
    /// <param name="money_type">货币类型占位（未来支持代金券抵扣）。</param>
    /// <param name="Price">涉及货币时的单价。</param>
    /// <param name="ntf_now">是否立即全量通知客户端背包。</param>
    /// <param name="idipParam">出参，订单记账参数，未使用时为 default。</param>
    /// <returns>新增的物品实例；数据未就绪或组件缺失时返回 null。</returns>
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
    
    
    
    
    /// <summary>获取角色组件，未注册时返回 null。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        return _components.GetComponent<T>();
    }

    /// <summary>尝试获取角色组件，成功时输出组件引用。</summary>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        return _components.TryGetComponent(out component);
    }

    /// <summary>向角色注册组件。</summary>
    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        _components.SetComponent(component);
    }

    /// <summary>
    /// 记录指定模块的最近修改时间为当前 UTC 秒，供模块级脏数据版本跟踪。
    /// </summary>
    /// <param name="module">发生变更的业务模块。</param>
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

    /// <summary>
    /// 校验角色是否持有足够数量的物品；虚拟货币不足与物品不足返回不同错误码。
    /// </summary>
    /// <param name="iteItemId">物品配置 ID。</param>
    /// <param name="iteItemCount">需要校验的数量。</param>
    /// <param name="iteBindOptionForTake">扣除时的绑定选项。</param>
    /// <returns>校验结果错误码。</returns>
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
                    //待补：虚拟货币不足的专用提示。
                    //todo先货币不足
                    return CmdCode.NotEnoughCurrency;
                }
                return CmdCode.InsufficientItems;
            }
        }
        return CmdCode.Succeed;
    }

    /// <summary>读取角色开关标志，未记录时按 false 处理。</summary>
    public bool GetFlag(RoleStateFlag e)
    {
        if (_flags.ContainsKey(e))
        {
            return _flags[e];
        }
        return false;
    }

    /// <summary>写入或更新角色开关标志。</summary>
    public void SetFlag(RoleStateFlag e, bool flag)
    {
        _flags[e] = flag;
    }

    /// <summary>
    /// 扣除物品的统一入口；一级货币（钻石）禁止在此直接扣除。
    /// </summary>
    /// <param name="descItemId">物品配置 ID。</param>
    /// <param name="descItemCount">扣除数量。</param>
    /// <param name="getReason">变更原因。</param>
    /// <param name="descBindOptionForTake">扣绑定时的绑定选项。</param>
    /// <returns>扣除结果错误码。</returns>
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
