using System.Collections.Generic;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>
/// 本地玩家场景实体，同时承载本地角色组件 API。
/// 继承自 RoleEntity，额外管理本地玩家的业务组件容器、脏标记和角色数据。
/// 是客户端唯一的本地玩家场景表现根对象。
/// </summary>
public sealed class PlayerEntity : RoleEntity
{
    /// <summary>RO 同名当前选中实体，点击、瞄准与技能自动选敌共用；离场和复用时清空。</summary>
    public MEntity Target { get; set; }

    /// <summary>本地玩家上次生成的施法 UUID，零表示该实体尚未释放技能。</summary>
    private ulong _nextSkillUuid;

    /// <summary>上次已上报的水平移动状态，用于发送松键后的最后位置。</summary>
    private bool _isMoving;

    /// <summary>上次上报的跳跃段号；零表示落地，1 至 3 表示本轮滞空已起跳次数。</summary>
    private int _jumpCount;

    /// <summary>按 RO 规则生成本地玩家下一次施法 UUID，取值范围为 1 至 9999。</summary>
    public ulong NextSkillUUID
    {
        get
        {
            _nextSkillUuid++;
            if (_nextSkillUuid >= 10000)
            {
                _nextSkillUuid = 1;
            }
            return _nextSkillUuid;
        }
    }

    /// <summary>按 RO 同名发送入口上传当前位置、朝向、耗时和移动状态，并归零同步计时。</summary>
    public void UpdateMove()
    {
        // 主城实例和角色模型对应 RO 已进入场景；创角阶段不发送世界移动。
        if (MainCityMgr.Instance == null || !Model.IsLoaded)
        {
            return;
        }
        // 普通移动保留原有正耗时发送规则；起跳或落地状态变化允许零耗时即时上报。
        if (MoveComp.CostTime <= 0f && _jumpCount == MoveComp.JumpCount)
        {
            return;
        }
        _face = Model.Rotation.eulerAngles.y;
        _isMoving = IsMoving;
        _jumpCount = MoveComp.JumpCount;
        // 本时间段是否实际发生位移；跳跃与技能根运动也需要远端追赶最终坐标。
        bool isMoved = Position != sysnOldPos;
        sysnOldPos = Position;
        // 本次位置上报的强类型消息：位置为米、耗时为秒，沿 RO Dx/Dy/Dz/Time 语义发送。
        MoveOperationReq ptc = new MoveOperationReq();
        ptc.Dx = sysnOldPos.x;
        ptc.Dy = sysnOldPos.y;
        ptc.Dz = sysnOldPos.z;
        ptc.Face = _face;
        ptc.Time = MoveComp.CostTime;
        ptc.IsMoving = IsMoving;
        ptc.IsMoved = isMoved || MoveComp.HasDirectionalInput;
        ptc.JumpCount = _jumpCount;
        MoveComp.CostTime = 0f;
        NetSocketMgr.Client.SendData(NetDefine.CMD_MoveOperationCode, ptc.ToByteString());
        _lastSyncTime = 0f;
    }

    /// <summary>移动朝向使用的主相机缓存；Single切场景销毁旧相机后重新获取。</summary>
    private Transform _mainCameraTransform = null;

    /// <summary>本地角色业务组件容器（背包、计数等）</summary>
    private readonly RoleComponentContainer _roleComponents;

    /// <summary>角色运行时状态标记字典</summary>
    private readonly Dictionary<RoleStateFlag, bool> _flags =
        new Dictionary<RoleStateFlag, bool>();

    /// <summary>角色组件是否已完成初始化</summary>
    private bool _componentsInitialized;

    /// <summary>角色数据是否已从服务器加载完成</summary>
    private bool _roleDataReady;

    /// <summary>是否已释放角色数据资源</summary>
    private bool _disposed;

    /// <summary>是否已通知"加入世界"事件</summary>
    private bool _addedToWorldNotified;

    /// <summary>客户端脏标记跟踪器，记录本地修改待同步</summary>
    public ClientDirtyTracker DirtyTracker { get; private set; }

    /// <summary>本地玩家完整角色信息（含基础信息和扩展数据）</summary>
    public MainRoleInfo RoleInfo { get; private set; }

    /// <summary>是否为本地玩家（true 表示当前登录账号的角色）</summary>
    public bool IsLocal { get; private set; }

    /// <summary>
    /// 由实体 Builder 创建场景玩家。
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="configId">配置表 ID</param>
    public PlayerEntity(ulong uid, int configId)
        : base(uid, EntityType.Role | EntityType.Player, configId)
    {
        IsLocal = false;
        DirtyTracker = new ClientDirtyTracker();
        _roleComponents = new RoleComponentContainer(this);
        // 直接沿用 MEntity 已有 RO 同步字段；玩家第一次上报从当前世界位置开始计时。
        sysnOldPos = Position;
        _lastSyncTime = 0f;
        CacheMainCameraTransform();
    }

    /// <summary>
    /// 场景世界更新时同时推进角色组件。
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    public override void Update(int deltaMilliseconds)
    {
        base.Update(deltaMilliseconds);
        UpdateRoleData(deltaMilliseconds);
        if (!Model.IsLoaded || MainCityMgr.Instance == null)
        {
            return;
        }
        // 与 RO 一样，在本帧运动完成后比较上次位置，厘米以下变化不触发位置包。
        Vector3 destPos = Position;
        // 当前点相对上次上报点的三维位移，单位为米。
        Vector3 sub = destPos - sysnOldPos;
        // 世界 X 轴变化的整数厘米数。
        int subX = (int)(sub.x * 100f);
        // 世界 Y 轴变化的整数厘米数；用于同步本项目已有跳跃及落地位置。
        int subY = (int)(sub.y * 100f);
        // 世界 Z 轴变化的整数厘米数。
        int subZ = (int)(sub.z * 100f);
        // 当前模型水平朝向角，单位为度。
        float ang = Model.Rotation.eulerAngles.y;
        _lastSyncTime += deltaMilliseconds * 0.001f;
        // 起跳、追加段和落地立即上报；即使尚无厘米位移或累计时间为零，也不能漏掉表现状态。
        if (_jumpCount != MoveComp.JumpCount)
        {
            UpdateMove();
            return;
        }
        // 移动结算时间与同步间隔均满足上报节拍。
        bool movementTimingValid = MoveComp.CostTime > 0f && _lastSyncTime > 0.1f;
        // 移动状态标志发生变化。
        bool moveStateChanged = _isMoving != IsMoving;
        // 三维位置任一轴向上有整数厘米级位移。
        bool positionShifted = Mathf.Abs(subX) > 0 || Mathf.Abs(subZ) > 0 || Mathf.Abs(subY) > 0;
        // 水平朝向角相对上次上报偏转超过 1 度。
        bool faceChanged = Mathf.Abs(ang - _face) > 1f;
        // 姿态发生实质变化（状态标志、位置或朝向任一成立）。
        bool poseChanged = moveStateChanged || positionShifted || faceChanged;
        if (movementTimingValid && poseChanged)
        {
            _face = ang;
            UpdateMove();
        }
    }

    /// <summary>缓存当前活动场景中带MainCamera标签的相机变换。</summary>
    private void CacheMainCameraTransform()
    {
        // 当前场景输出主相机；创角与主城各自持有独立实例。
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            _mainCameraTransform = mainCamera.transform;
        }
    }

    /// <summary>获取移动朝向使用的主相机，旧场景相机销毁后更新缓存。</summary>
    /// <returns>当前场景主相机的变换；场景尚无主相机时为空。</returns>
    public Transform GetMainCamera()
    {
        // 玩家实体先于主城创建并跨场景存活，不能继续使用已卸载的创角相机。
        if (_mainCameraTransform == null)
        {
            CacheMainCameraTransform();
        }
        return _mainCameraTransform;
    }

    /// <summary>
    /// 实体世界创建后装载本地角色数据并初始化角色组件。
    /// 仅在本地玩家实体上调用一次。
    /// </summary>
    /// <param name="roleInfo">从服务器获取的完整角色信息</param>
    public void InitializeLocalRole(MainRoleInfo roleInfo)
    {
        IsLocal = true;
        SetRoleInfo(roleInfo);
        InitializeRoleComponents();
    }

    /// <summary>
    /// 初始化本地角色业务组件（背包、计数等）。
    /// 仅初始化一次，重复调用直接忽略。
    /// </summary>
    private void InitializeRoleComponents()
    {
        if (!IsLocal || _componentsInitialized)
        {
            return;
        }
        // 注册本地角色业务组件
        _roleComponents.SetComponent(new RoleCountComponent(this));
        _roleComponents.SetComponent(new RoItemComponent(this));
        _roleComponents.Initialize();
        _componentsInitialized = true;
    }

    /// <summary>
    /// 从服务器背包回包加载角色数据到本地组件。
    /// </summary>
    /// <param name="result">背包信息回包结果</param>
    /// <returns>加载成功返回 true；非本地玩家、组件未初始化或回包失败返回 false</returns>
    public bool LoadRoleData(RoleKanpsackInfoRet result)
    {
        // 非本地玩家或业务组件尚未初始化，直接忽略回包。
        bool notLocalOrUninitialized = !IsLocal || !_componentsInitialized;
        // 回包无效：回包体为空、协议码非成功或背包信息缺失（内部短路保护空引用）。
        bool resultInvalid = result == null ||
            result.CmdCode != CmdCode.Succeed ||
            result.RoleKanpsackInfo == null;
        if (notLocalOrUninitialized || resultInvalid)
        {
            return false;
        }
        // 加载背包数据
        RoItemComponent itemComponent = GetRoleComponent<RoItemComponent>();
        RoleCountComponent countComponent = GetRoleComponent<RoleCountComponent>();
        itemComponent.LoadKnapsack(result);
        countComponent.Load(result.RoleKanpsackInfo.RoleCountInfoList);
        _roleDataReady = true;
        _roleComponents.AfterLoad();
        return true;
    }

    /// <summary>
    /// 实体加入世界后通知角色组件执行"入场后"逻辑。
    /// 仅通知一次，重复调用直接忽略。
    /// </summary>
    public void AfterAddedToWorld()
    {
        if (_addedToWorldNotified || !_componentsInitialized || _disposed)
        {
            return;
        }
        _roleComponents.AfterAddedToWorld();
        _addedToWorldNotified = true;
    }

    /// <summary>
    /// 驱动本地角色业务组件每帧更新。
    /// </summary>
    /// <param name="deltaMilliseconds">帧间隔毫秒数</param>
    public void UpdateRoleData(int deltaMilliseconds)
    {
        if (_disposed || LifecycleState != EntityLifecycleState.Active)
        {
            return;
        }
        _roleComponents.Update(deltaMilliseconds);
    }

    /// <summary>
    /// 释放本地角色数据资源，清空组件、脏标记和状态标记。
    /// 幂等操作，重复调用安全。
    /// </summary>
    public void DisposeRoleData()
    {
        if (_disposed)
        {
            return;
        }
        _roleComponents.Dispose();
        if (DirtyTracker != null)
        {
            DirtyTracker.ClearAll();
        }
        _flags.Clear();
        RoleInfo = null;
        _roleDataReady = false;
        _disposed = true;
    }

    /// <summary>释放角色数据并销毁实体。</summary>
    public void Dispose()
    {
        DisposeRoleData();
        Destroy();
    }

    /// <summary>
    /// 获取角色 ID（来自 BaseInfo）
    /// </summary>
    /// <returns>角色持久化 ID，未初始化时返回 0</returns>
    public override int GetID()
    {
        if (BaseInfo == null)
        {
            return 0;
        }
        return BaseInfo.RoleId;
    }

    /// <summary>
    /// 检查角色数据是否已加载完成
    /// </summary>
    /// <returns>数据就绪返回 true</returns>
    public bool IsRoleDataReady()
    {
        return _roleDataReady;
    }

    /// <summary>
    /// 设置本地玩家完整角色信息（深拷贝）。
    /// </summary>
    /// <param name="roleInfo">完整角色信息，null 时清空</param>
    public void SetRoleInfo(MainRoleInfo roleInfo)
    {
        if (roleInfo == null)
        {
            RoleInfo = null;
            base.UpdateBaseInfo(null);
            return;
        }
        // 深拷贝，防止外部引用被意外修改
        RoleInfo = roleInfo.Clone();
        base.UpdateBaseInfo(RoleInfo.BaseInfo);
    }

    /// <summary>
    /// 更新角色基础同步信息（重写以同步到 RoleInfo）。
    /// </summary>
    /// <param name="baseInfo">角色基础信息，null 时直接忽略</param>
    public override void UpdateBaseInfo(RoleBaseInfo baseInfo)
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
        base.UpdateBaseInfo(baseInfo);
    }

    /// <summary>应用服务器确认的当前 MP，并同步角色基础数据与属性组件。</summary>
    /// <param name="currentMp">服务器声明的当前 MP，单位为整数点数。</param>
    /// <returns>角色基础数据存在并完成同步时返回 true。</returns>
    public bool ApplyAuthoritativeMp(int currentMp)
    {
        if (BaseInfo == null)
        {
            return false;
        }
        // 服务器回包可能暂时超出客户端旧上限，先按本地可见范围钳制。
        if (currentMp < 0)
        {
            currentMp = 0;
        }
        if (currentMp > BaseInfo.MaxMp)
        {
            currentMp = BaseInfo.MaxMp;
        }
        // 克隆后回写，保持 BaseInfo 与 RoleInfo 的深拷贝约定。
        RoleBaseInfo updatedInfo = BaseInfo.Clone();
        updatedInfo.CurrMp = currentMp;
        UpdateBaseInfo(updatedInfo);
        if (AttrComp != null)
        {
            AttrComp.Apply(updatedInfo.MaxHp, updatedInfo.CurrHp,
                updatedInfo.MaxMp, updatedInfo.CurrMp);
        }
        return true;
    }

    /// <summary>
    /// 销毁前释放本地角色数据
    /// </summary>
    protected override void OnDestroying()
    {
        MSkillTargetMgr.singleton.ResetTarget(null);
        Target = null;
        DisposeRoleData();
        base.OnDestroying();
    }

    /// <summary>
    /// 对象池复用时重置本地玩家全部运行时状态。
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        Target = null;
        _componentsInitialized = false;
        _roleDataReady = false;
        _disposed = false;
        _addedToWorldNotified = false;
        IsLocal = false;
        _isMoving = false;
        _jumpCount = 0;
        RoleInfo = null;
        _flags.Clear();
        DirtyTracker.ClearAll();
        base.PrepareForReuse(uid, entityType, configId);
        // 基类已经清除朝向和上次位置，玩家恢复 0.1 秒同步计时起点。
        sysnOldPos = Position;
        _lastSyncTime = 0f;
    }

    /// <summary>
    /// 获取指定类型的角色业务组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>组件实例，不存在返回 null</returns>
    public T GetRoleComponent<T>() where T : RoleComponentBase
    {
        return _roleComponents.GetComponent<T>();
    }

    /// <summary>
    /// 获取指定类型的角色业务组件（别名，等价于 GetRoleComponent）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>组件实例，不存在返回 null</returns>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        return GetRoleComponent<T>();
    }

    /// <summary>
    /// 尝试获取指定类型的角色业务组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="component">输出参数，获取到的组件实例</param>
    /// <returns>存在返回 true，不存在返回 false</returns>
    public bool TryGetRoleComponent<T>(out T component) where T : RoleComponentBase
    {
        return _roleComponents.TryGetComponent(out component);
    }

    /// <summary>
    /// 尝试获取指定类型的角色业务组件（别名）
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="component">输出参数，获取到的组件实例</param>
    /// <returns>存在返回 true，不存在返回 false</returns>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        return TryGetRoleComponent(out component);
    }

    /// <summary>
    /// 查询指定物品的持有数量
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <returns>物品数量，组件不存在时返回 0</returns>
    public int GetItemCount(int itemId)
    {
        RoItemComponent itemComponent;
        if (!TryGetRoleComponent(out itemComponent))
        {
            return 0;
        }
        return itemComponent.GetItemNum(itemId, ItemBind.kItemBindNo, true);
    }

    /// <summary>
    /// 向背包添加物品
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="itemCount">添加数量</param>
    /// <param name="reason">变更原因</param>
    /// <param name="itemSign">物品标识</param>
    /// <param name="moneyType">货币类型</param>
    /// <param name="price">价格</param>
    /// <param name="notifyNow">是否立即通知 UI 刷新</param>
    /// <param name="idipParam">输出参数，变更后的物品参数</param>
    /// <returns>添加的物品实例，失败返回 null</returns>
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
        if (!TryGetRoleComponent(out itemComponent))
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

    /// <summary>
    /// 检查指定物品数量是否足够
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="itemCount">需要的数量</param>
    /// <param name="bindOption">绑定选项</param>
    /// <returns>检查结果码（Succeed 表示足够）</returns>
    public CmdCode CheckItem(int itemId, long itemCount, ItemBind bindOption)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }
        RoItemComponent itemComponent;
        if (!TryGetRoleComponent(out itemComponent))
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

    /// <summary>
    /// 从背包扣除物品
    /// </summary>
    /// <param name="itemId">物品 ID</param>
    /// <param name="itemCount">扣除数量</param>
    /// <param name="reason">变更原因</param>
    /// <param name="bindOption">绑定选项</param>
    /// <returns>操作结果码</returns>
    public CmdCode ReduceItem(int itemId, long itemCount, ItemChangeReason reason,
        ItemBind bindOption)
    {
        if (!IsRoleDataReady())
        {
            return CmdCode.InvalidSession;
        }
        // 钻石类物品不允许直接扣除
        if (ItemBase.IsDiamond(itemId))
        {
            return CmdCode.ItemNotExist;
        }
        RoItemComponent itemComponent;
        if (!TryGetRoleComponent(out itemComponent))
        {
            return CmdCode.ItemNotExist;
        }
        return itemComponent.ReduceItem(itemId, itemCount, reason, bindOption);
    }

    /// <summary>
    /// 获取角色状态标记值
    /// </summary>
    /// <param name="flag">状态标记枚举</param>
    /// <returns>标记值，未设置过返回 false</returns>
    public bool GetFlag(RoleStateFlag flag)
    {
        bool value;
        if (_flags.TryGetValue(flag, out value))
        {
            return value;
        }
        return false;
    }

    /// <summary>
    /// 设置角色状态标记值
    /// </summary>
    /// <param name="flag">状态标记枚举</param>
    /// <param name="value">标记值</param>
    public void SetFlag(RoleStateFlag flag, bool value)
    {
        _flags[flag] = value;
    }
}
