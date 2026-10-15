using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using UnityEngine;
using YooAsset;

/// <summary>
/// 专用测试场景的 YooAsset 启动入口。
/// 该组件在默认资源包初始化成功后启用场景内预置的 Global，等待场景实体世界就绪后初始化测试 NPC 并模拟主角角色包。
/// </summary>
public class test : MonoBehaviour
{
    /// <summary>测试场景使用的 YooAsset 默认资源包名称，必须与资源收集配置中的包名一致。</summary>
    private const string DefaultPackageName = "DefaultPackage";

    /// <summary>场景中预置且默认禁用的全局组件；资源包就绪后才启用，避免其 Awake 提前读取 YooAsset。</summary>
    [SerializeField]
    private Global _sceneGlobal;

    /// <summary>测试场景中负责按配置创建 NPC 实体的管理器；必须在 Global.SceneWorld 就绪后调用。</summary>
    [SerializeField]
    private NpcManager _npcManager;

    /// <summary>测试模拟角色所属账号 ID，必须为正数并与测试场景登录会话一致。</summary>
    [SerializeField]
    private int _mockAccountId = 10001;

    /// <summary>测试模拟角色持久化 ID，必须为正数且在一次场景会话内保持不变。</summary>
    [SerializeField]
    private int _mockRoleId = 10001;

    /// <summary>测试模拟角色在角色信息界面和头顶名称中显示的昵称。</summary>
    [SerializeField]
    private string _mockRoleNickname = "测试主角";

    /// <summary>本对象生命周期内是否已启动初始化协程，防止重复创建资源包或重复启用 Global。</summary>
    private bool _hasStarted;

    /// <summary>是否已经发起过 NPC 初始化；用于阻止场景重复加载时重复创建 NPC。</summary>
    private bool _npcInitializationStarted;

    /// <summary>是否已经提交过本次模拟角色包，避免测试组件重复驱动同一主角创建。</summary>
    private bool _mockRolePacketSubmitted;

    /// <summary>Unity 场景加载时启动一次资源包初始化流程。</summary>
    private void Awake()
    {
        if (_hasStarted)
        {
            return;
        }

        _hasStarted = true;
        StartCoroutine(InitializeYooAsset());
    }

    /// <summary>
    /// 获取或创建默认资源包，完成编辑器模拟模式初始化后启用场景 Global。
    /// </summary>
    /// <returns>用于等待 YooAsset 初始化操作完成的 Unity 协程。</returns>
    private IEnumerator InitializeYooAsset()
    {
        // 默认资源包引用在 YooAssets 已初始化时直接复用，避免重复 CreatePackage。
        ResourcePackage package = GetOrCreateDefaultPackage();
        if (package == null)
        {
            yield break;
        }

        // Global.Awake 通过包名取得资源包，因此在异步初始化前设置默认包。
        YooAssets.SetDefaultPackage(package);

        // 编辑器模拟构建生成的清单路径；为空或文件缺失时不能调用 InitializeAsync。
        string manifestFilePath = GetSimulateManifestFilePath();
        if (string.IsNullOrEmpty(manifestFilePath) || !File.Exists(manifestFilePath))
        {
            Debug.LogError("YooAsset 编辑器模拟清单生成失败，未获得有效清单路径。请修正 DefaultPackage 的资源收集配置后重试。", this);
            yield break;
        }

        // 编辑器模拟模式参数只承载已验证存在的清单文件路径。
        EditorSimulateModeParameters initializeParameters =
            new EditorSimulateModeParameters();
        initializeParameters.SimulateManifestFilePath = manifestFilePath;

        // 默认资源包的异步初始化句柄，完成后读取状态决定是否启用 Global。
        InitializationOperation initializeOperation;
        try
        {
            initializeOperation = package.InitializeAsync(initializeParameters);
        }
        // 异常对象记录 YooAsset 参数校验或运行时初始化失败的具体原因。
        catch (Exception exception)
        {
            Debug.LogError("YooAsset 初始化操作创建失败：" + exception.Message, this);
            yield break;
        }

        if (initializeOperation == null)
        {
            Debug.LogError("YooAsset 未创建初始化操作，场景 Global 保持禁用。", this);
            yield break;
        }

        yield return initializeOperation;

        if (initializeOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError("YooAsset 默认资源包初始化失败：" + initializeOperation.Error, this);
            yield break;
        }

        EnableSceneGlobal();
        yield return InitializeAfterSceneWorldReady();
    }

    /// <summary>
    /// 等待 Global 创建场景实体世界后初始化测试 NPC 和模拟角色包，避免实体工厂访问空世界。
    /// </summary>
    /// <returns>等待场景世界就绪并完成测试初始化的 Unity 协程。</returns>
    private IEnumerator InitializeAfterSceneWorldReady()
    {
        // Global.Awake 先启动动画索引，SceneWorld 只会在索引成功后创建。
        while (Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            yield return null;
        }

        InitializeNpcAfterSceneWorldReady();
        SimulateMainRolePacket();
    }

    /// <summary>
    /// 初始化测试场景中的 NPC 管理器；该流程与主角模拟包相互独立。
    /// </summary>
    private void InitializeNpcAfterSceneWorldReady()
    {
        if (_npcManager == null)
        {
            Debug.LogWarning("测试场景未绑定 NpcManager，跳过 NPC 测试流程。", this);
            return;
        }

        // 该标记保证本对象生命周期内最多发起一轮 NPC 初始化。
        if (_npcInitializationStarted)
        {
            return;
        }
        _npcInitializationStarted = true;
        _npcManager.Initialize();
    }

    /// <summary>
    /// 公开测试入口：模拟收到开始游戏角色包，并通过 Global 正式入口创建本地玩家。
    /// 可由测试按钮、Inspector ContextMenu 或自动初始化流程调用。
    /// </summary>
    [ContextMenu("模拟收到主角角色包")]
    public void SimulateMainRolePacket()
    {
        if (_mockRolePacketSubmitted)
        {
            Debug.Log("已提交过同一测试主角角色包，忽略重复模拟。", this);
            return;
        }

        // 当前有效全局生命周期实例，负责消费测试角色包并创建场景主角。
        Global currentGlobal = Global.Instance;
        if (currentGlobal == null || currentGlobal.SceneWorld == null)
        {
            Debug.LogWarning("Global 或 SceneWorld 尚未就绪，暂不模拟主角角色包。", this);
            return;
        }

        if (_mockAccountId <= 0 || _mockRoleId <= 0)
        {
            Debug.LogError("测试主角账号 ID 和角色 ID 必须为正数。", this);
            return;
        }

        // 当前世界已加载的本地玩家；测试包不得覆盖真实登录角色，避免 LoadLocalRole 清空其模型。
        PlayerEntity existingRole = currentGlobal.SceneWorld.LocalRole;
        if (existingRole != null)
        {
            _mockRolePacketSubmitted = true;
            Debug.Log("本地角色已经存在，测试角色包不覆盖当前登录角色。", this);
            return;
        }

        // 与正式开始游戏回包载荷同类型的测试主角数据，仅在测试脚本中构造。
        MainRoleInfo mockRolePacket = CreateMockMainRolePacket();
        // 正式 Global 角色提交结果，用于决定是否记录本次测试包已被接受。
        Global.LocalRoleLoadSubmitResult submitResult =
            currentGlobal.SubmitLocalRoleLoad(mockRolePacket);
        if (submitResult == Global.LocalRoleLoadSubmitResult.Rejected)
        {
            Debug.LogWarning("模拟主角角色包被 Global 拒绝，未创建本地玩家。", this);
            return;
        }

        _mockRolePacketSubmitted = true;
        Debug.Log("模拟主角角色包已提交，Global 状态：" + submitResult, this);

        StartCoroutine(LoadMockLearnedSkills());
    }

    /// <summary>等待模拟角色与正式主界面就绪后，经 protobuf 正式解析入口装载全学技能包。</summary>
    /// <returns>逐帧等待角色、主界面和模型资源完成初始化。</returns>
    private IEnumerator LoadMockLearnedSkills()
    {
        // 提交结果可能表示待处理，必须等场景世界真正创建本地主角。
        PlayerEntity role = null;
        while (true)
        {
            // 全局场景世界或本地主角尚未创建时继续等待。
            if (Global.Instance == null || Global.Instance.SceneWorld == null ||
                Global.Instance.SceneWorld.LocalRole == null)
            {
                yield return null;
                continue;
            }
            // 主城管理器或主界面根节点尚未就绪时继续等待。
            if (MainCityMgr.Instance == null || UIRoot.Instance == null)
            {
                yield return null;
                continue;
            }
            break;
        }
        role = Global.Instance.SceneWorld.LocalRole;
        MainCityMgr.Instance.EnsureLocalRolePresentation(role);
        if (UIRoot.Instance.MainCtrl == null)
        {
            UIRoot.Instance.InitMainCtrl();
        }
        if (UIRoot.Instance.MainCtrl == null)
        {
            Debug.LogError("测试场景正式主界面未能初始化，模拟全学技能包未装载。", this);
            yield break;
        }
        UIRoot.Instance.MainCtrl.ShowView();
        UIRoot.Instance.MainCtrl.RegisterMainInput();

        // 原技能表全部条目构成本地模拟回包的已学列表，无表现的剩余条目不占快捷键。
        List<cfg.SkillInfo> skills = new List<cfg.SkillInfo>(LubanMgr.Instance.GetSkillInfos().Values);
        skills.Sort((left, right) => left.Id.CompareTo(right.Id));
        // 六项有正式表现的技能；10060由原F移到2，10080滑行使用F。
        int[] animatedSkillIds = { 10010, 10040, 10050, 10060, 10070, 10080 };
        // 与技能 ID 索引一一对应，模拟11080后由正式控制器统一更新输入和图标。
        string[] bindKeys = { "Q", "E", "R", "2", "1", "F" };
        // 本地模拟的 11080 回复包只描述等级和键位，正式控制器负责装载核心与界面。
        RoleSkillInfoRet response = new RoleSkillInfoRet();
        response.CmdCode = CmdCode.Succeed;
        response.RoleId = role.GetID();
        for (int skillIndex = 0; skillIndex < skills.Count; skillIndex++)
        {
            // 未配置战斗表现的技能保留一级已学状态，但不占用战斗快捷键。
            string bindKey = string.Empty;
            for (int keyIndex = 0; keyIndex < animatedSkillIds.Length; keyIndex++)
            {
                if (skills[skillIndex].Id == animatedSkillIds[keyIndex])
                {
                    bindKey = bindKeys[keyIndex];
                }
            }
            response.RoleSkillInfoList.Add(new RoleSkillInfo
            {
                SkillId = skills[skillIndex].Id,
                Level = 1,
                BindKey = bindKey
            });
        }

        // 序列化边界使用 protobuf 强类型消息，正式解析入口复用线上回包校验。
        ByteString payload = response.ToByteString();
        if (!UIRoot.Instance.MainCtrl.LoadSkillResponse(payload))
        {
            Debug.LogError("模拟全学技能回包未被正式控制器接纳，角色 ID：" + response.RoleId, this);
            yield break;
        }
        // 旧模型动画事件接收器在此测试场景会访问缺失的网络战斗对象，仅在运行态关闭组件。
        while (true)
        {
            // 模型尚未加载完成时继续等待。
            if (role.Model == null || !role.Model.IsLoaded)
            {
                yield return null;
                continue;
            }
            // 模型游戏对象或其原生对象尚未就绪时继续等待。
            if (role.Model.GameObject == null || role.Model.GameObject.NativeObject == null)
            {
                yield return null;
                continue;
            }
            break;
        }
        RoleAnimaBehaviour[] receivers = role.Model.GameObject.NativeObject.GetComponentsInChildren<RoleAnimaBehaviour>(true);
        for (int index = 0; index < receivers.Length; index++)
        {
            if (receivers[index] != null)
            {
                receivers[index].enabled = false;
            }
        }
        Debug.Log("测试角色动画事件接收组件已禁用，数量：" + receivers.Length, this);
        Debug.Log("模拟全学技能回包已装载，技能数量：" + response.RoleSkillInfoList.Count + "，可手动按 Q/E/R/F/1 播放技能动画。", this);
    }

    /// <summary>
    /// 构造与开始游戏回包一致的强类型主角数据，字段值仅用于测试场景。
    /// </summary>
    /// <returns>可交给 Global.SubmitLocalRoleLoad 的完整主角数据。</returns>
    private MainRoleInfo CreateMockMainRolePacket()
    {
        // 待提交给正式 Global 角色加载入口的强类型主角数据。
        MainRoleInfo mockRolePacket = new MainRoleInfo();
        mockRolePacket.AccountId = _mockAccountId;
        mockRolePacket.Exp = 0;
        mockRolePacket.SkillUpPoint = 0;
        mockRolePacket.ServerId = 1;
        mockRolePacket.BaseInfo = new RoleBaseInfo();
        mockRolePacket.BaseInfo.RoleId = _mockRoleId;
        // 测试角色有足够生存值与法力值，正式本地施法仍会执行 MP 和状态检查。
        mockRolePacket.BaseInfo.MaxHp = 1000;
        mockRolePacket.BaseInfo.CurrHp = 1000;
        mockRolePacket.BaseInfo.MaxMp = 1000;
        mockRolePacket.BaseInfo.CurrMp = 1000;
        // 测试主角固定使用 MJS 职业，正式 MainCityMgr 会据此选择 Role_MJS 预制体。
        mockRolePacket.BaseInfo.JobId = (int)RoleJobtype.MJS;
        if (string.IsNullOrEmpty(_mockRoleNickname))
        {
            mockRolePacket.BaseInfo.Nickname = "测试主角";
        }
        else
        {
            mockRolePacket.BaseInfo.Nickname = _mockRoleNickname;
        }
        return mockRolePacket;
    }

    /// <summary>
    /// 复用已存在的默认资源包；仅在 YooAssets 尚未初始化或资源包不存在时执行必要创建。
    /// </summary>
    /// <returns>可用于默认包初始化的资源包；发生异常时返回空引用。</returns>
    private ResourcePackage GetOrCreateDefaultPackage()
    {
        // 尝试读取已有资源包；YooAssets 未初始化时该 API 会抛出异常。
        ResourcePackage package;
        try
        {
            package = YooAssets.TryGetPackage(DefaultPackageName);
        }
        // 异常对象仅用于判定资源系统尚不可访问，并输出首次初始化失败原因。
        catch (Exception)
        {
            try
            {
                YooAssets.Initialize();
                package = YooAssets.TryGetPackage(DefaultPackageName);
            }
            // 异常对象记录 YooAssets 驱动器创建失败等不可恢复错误。
            catch (Exception exception)
            {
                Debug.LogError("YooAssets 初始化失败：" + exception.Message, this);
                return null;
            }
        }

        if (package != null)
        {
            return package;
        }

        try
        {
            return YooAssets.CreatePackage(DefaultPackageName);
        }
        // 异常对象记录默认资源包创建失败的具体原因。
        catch (Exception exception)
        {
            Debug.LogError("DefaultPackage 创建失败：" + exception.Message, this);
            return null;
        }
    }

    /// <summary>
    /// 生成编辑器模拟清单并返回构建结果路径。
    /// </summary>
    /// <returns>模拟构建返回的清单路径；构建异常时返回空字符串。</returns>
    private string GetSimulateManifestFilePath()
    {
        try
        {
            return EditorSimulateModeHelper.SimulateBuild(DefaultPackageName);
        }
        // 异常对象记录资源收集配置无效等模拟构建失败原因。
        catch (Exception exception)
        {
            Debug.LogError("YooAsset 编辑器模拟构建失败：" + exception.Message, this);
            return string.Empty;
        }
    }

    /// <summary>
    /// 在资源包成功初始化后启用场景内预置的 Global，确保其 Awake 可读取 DefaultPackage。
    /// </summary>
    private void EnableSceneGlobal()
    {
        if (_sceneGlobal == null)
        {
            Debug.LogError("测试场景未绑定预置 Global，资源包已初始化但不会创建全局模块。", this);
            return;
        }

        // 已有持久化 Global 时复用它，禁止测试场景再启用第二个全局实例触发网络重连和场景清理。
        if (Global.Instance != null && Global.Instance != _sceneGlobal)
        {
            Debug.Log("检测到已有持久化 Global，跳过测试场景 Global 启用。", this);
            return;
        }

        if (_sceneGlobal.gameObject.activeSelf)
        {
            return;
        }

        _sceneGlobal.gameObject.SetActive(true);
    }
}
