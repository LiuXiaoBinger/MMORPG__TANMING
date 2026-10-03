using System.Collections;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 主城其他玩家同步测试组件。
/// 该组件仅在专用测试场景或临时手动挂载时使用，通过 SocketDispatcher 投递正式同步协议，验证其他玩家实体和表现创建链路。
/// </summary>
public sealed class MainCityOtherRoleMock : MonoBehaviour
{
    /// <summary>测试未建立实际网络连接时使用的正数派发代次；只在本次同步调用栈内传递给分发器，不写入网络管理器状态。</summary>
    private const long TestDispatchGeneration = 1L;

    /// <summary>是否在组件启动后自动模拟一次其他玩家上线同步；正式场景和预制体不得挂载本组件。</summary>
    [SerializeField, Header("启动模拟")]
    private bool _simulateOnStart;

    /// <summary>自动模拟前等待的秒数，范围大于等于零，用于等待 Global 和主城管理器初始化完成。</summary>
    [SerializeField, Header("模拟延迟秒"), Min(0f)]
    private float _delaySeconds = 0.5f;

    /// <summary>模拟的其他玩家持久化角色 ID；同一主城内必须保持唯一，重复 ID 用于验证不会重复实例化。</summary>
    [SerializeField, Header("角色 ID")]
    private int _roleId = 900001;

    /// <summary>模拟角色显示昵称，用于验证网络同步基础数据是否写入场景角色实体。</summary>
    [SerializeField, Header("角色昵称")]
    private string _nickname = "主城模拟玩家";

    /// <summary>模拟角色职业编号，对应 RoleBaseInfo.JobId；当前默认值 1 为项目已有剑修职业编号。</summary>
    [SerializeField, Header("职业编号")]
    private int _jobId = 1;

    /// <summary>本组件生命周期内是否已成功投递模拟协议；用于避免自动启动和手动菜单重复创建同一测试角色。</summary>
    private bool _packetDispatched;

    /// <summary>
    /// 在启动阶段按配置延迟模拟同步。
    /// Unity 协程由组件生命周期管理，组件销毁时会自动停止。
    /// </summary>
    /// <returns>延迟结束后的协程迭代器。</returns>
    private IEnumerator Start()
    {
        // 配置未启用自动测试时不执行任何角色创建操作。
        if (!_simulateOnStart)
        {
            yield break;
        }

        // 配置延迟仅用于让测试日志更易辨识，不作为业务就绪判定。
        if (_delaySeconds > 0f)
        {
            yield return new WaitForSeconds(_delaySeconds);
        }

        // 正式接收处理依赖 Global、实体世界和 MainCityMgr 的 Awake 注册；三者缺一时继续等待。
        while (!IsReadyForSync())
        {
            yield return null;
        }

        SimulateOtherRoleOnline();
    }

    /// <summary>
    /// 通过正式协议号模拟服务器通知其他玩家上线。
    /// 可从 Inspector 组件菜单手动调用，用于验证首次创建和重复角色 ID 不重复实例化。
    /// </summary>
    [ContextMenu("模拟其他玩家进入主城")]
    public void SimulateOtherRoleOnline()
    {
        // 同一组件生命周期只投递一次，避免测试入口自身制造重复同步。
        if (_packetDispatched)
        {
            Debug.Log("MainCityOtherRoleMock: 同一测试协议已投递，忽略重复模拟。", this);
            return;
        }

        // 主城管理器在 Awake 中注册协议处理委托；未注册时投递没有业务效果。
        MainCityMgr mainCityManager = MainCityMgr.Instance;
        if (mainCityManager == null || !mainCityManager.isActiveAndEnabled)
        {
            Debug.LogWarning("MainCityOtherRoleMock: MainCityMgr 尚未就绪，无法执行主城角色同步测试。", this);
            return;
        }

        // 角色 ID 小于等于零不符合服务器角色数据约束，避免向场景世界写入无效索引。
        if (_roleId <= 0)
        {
            Debug.LogWarning("MainCityOtherRoleMock: 角色 ID 必须大于零。", this);
            return;
        }

        // 使用与服务器 CMD_SyncotherOnlineCode 完全相同的强类型 Protobuf 载荷。
        RoleBaseInfo roleBaseInfo = CreateRoleBaseInfo();
        // 真实收包路径传入当前连接代次；测试未连接时仅提供正数临时代次，不伪造网络连接状态。
        long dispatchGeneration = NetSocketMgr.Instance.CurrentConnectionGeneration;
        if (dispatchGeneration <= 0)
        {
            dispatchGeneration = TestDispatchGeneration;
        }

        // 以正式协议号和序列化字节投递，必经 MainCityMgr.SyncotherOnlineHandle 的 ParseFrom 与 HandleOtherRoleOnline。
        SocketDispatcher.Instance.DispatchEvent(NetDefine.CMD_SyncotherOnlineCode,
            roleBaseInfo.ToByteString(), dispatchGeneration);

        // 分发器没有返回值，测试只查询正式世界结果确认同步是否已经落地，不参与角色创建或删除。
        SceneWorldSystem sceneWorld = Global.Instance.SceneWorld;
        RoleEntity syncedRole;
        if (!sceneWorld.TryGetRole(_roleId, out syncedRole) || syncedRole == null)
        {
            Debug.LogWarning("MainCityOtherRoleMock: 正式协议已投递但未生成其他角色，请检查主城同步处理链。", this);
            return;
        }

        // 仅在正式处理链已将角色写入世界后记录成功，后续自动或手动调用均不会再次投递。
        _packetDispatched = true;
        Debug.Log("MainCityOtherRoleMock: 已通过 CMD_SyncotherOnlineCode 正式协议链同步角色，角色 ID：" + syncedRole.GetID(), this);
    }

    /// <summary>构造模拟服务器下发的角色基础同步数据。</summary>
    /// <returns>包含角色 ID、昵称和职业编号的强类型角色基础信息。</returns>
    private RoleBaseInfo CreateRoleBaseInfo()
    {
        // RoleBaseInfo 与真实网络协议使用同一类型，保证测试入口的数据结构与服务器同步一致。
        RoleBaseInfo roleBaseInfo = new RoleBaseInfo
        {
            RoleId = _roleId,
            Nickname = _nickname,
            JobId = _jobId
        };
        return roleBaseInfo;
    }

    /// <summary>判断正式主城同步处理依赖的场景对象是否全部完成初始化。</summary>
    /// <returns>Global、SceneWorld 和已启用的 MainCityMgr 均可用时返回 true。</returns>
    private bool IsReadyForSync()
    {
        // 当前全局生命周期对象；为空表示 YooAsset 或场景启动流程尚未完成。
        Global currentGlobal = Global.Instance;
        if (currentGlobal == null || currentGlobal.SceneWorld == null)
        {
            return false;
        }

        // 主城管理器必须已激活，以确保 Awake 已将同步协议注册到 SocketDispatcher。
        MainCityMgr mainCityManager = MainCityMgr.Instance;
        if (mainCityManager == null || !mainCityManager.isActiveAndEnabled)
        {
            return false;
        }
        return true;
    }
}
