using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

/// <summary>
/// 以串行 latest-wins 规则管理场景加载、旧场景卸载和业务根启用，隔离过期场景生命周期副作用。
/// </summary>
public class SceneMgr : Singleton<SceneMgr>
{
    /// <summary>受管理场景中默认禁用的业务根名称；根内脚本只在请求最终生效时启用。</summary>
    private const string SceneBusinessRootName = "SceneBusinessRoot";

    /// <summary>单个 Unity 场景卸载操作允许创建的最大次数，耗尽后进入显式阻断态。</summary>
    private const int MaxUnityUnloadStartAttempts = 3;

    /// <summary>保存一个强类型场景快照及其有限次数 Unity 卸载状态。</summary>
    private sealed class UnitySceneUnloadState
    {
        /// <summary>在 YooAsset 句柄失效前保存的场景值，兜底卸载只读取该快照。</summary>
        public Scene SceneSnapshot;

        /// <summary>当前一次 Unity 卸载操作，非空时必须等待其 isDone。</summary>
        public AsyncOperation Operation;

        /// <summary>已尝试创建 Unity 卸载操作的次数，范围为零到最大尝试次数。</summary>
        public int StartAttemptCount;
    }

    /// <summary>保存单次场景请求的代次、资源句柄、卸载操作和独立更新订阅。</summary>
    private sealed class SceneLoadRequest
    {
        /// <summary>请求递增代次，用于区分快速切换中相同场景类型的不同实例。</summary>
        public long Generation;

        /// <summary>目标业务场景类型，成功启用后才提交为当前场景。</summary>
        public SceneType SceneType;

        /// <summary>最终场景业务根启用后执行一次的回调；过期或失败请求不执行。</summary>
        public Action CompletedCallback;

        /// <summary>本请求独占的 YooAsset Additive 场景句柄，持续到生效或卸载完成。</summary>
        public SceneOperationHandle Handle;

        /// <summary>加载成功后立即保存的目标场景值，句柄卸载失效后仍用于确认场景销毁。</summary>
        public Scene LoadedSceneSnapshot;

        /// <summary>本请求独占的逐帧状态订阅，任何路径均只释放自身订阅。</summary>
        public IDisposable UpdateSubscription;

        /// <summary>过期、失败或被替换场景的 YooAsset 卸载操作，必须完成后才允许后续请求启动。</summary>
        public UnloadSceneOperation RequestUnloadOperation;

        /// <summary>请求自身场景的 Unity 兜底卸载状态，负责有限重试和销毁确认。</summary>
        public UnitySceneUnloadState RequestUnityUnloadState;

        /// <summary>上一活动场景的 YooAsset 卸载操作，必须完成后才允许新业务根启用。</summary>
        public UnloadSceneOperation PreviousManagedUnloadOperation;

        /// <summary>调用上一活动句柄卸载前保存的强类型场景值，禁止卸载后再读取失效句柄。</summary>
        public Scene PreviousManagedSceneSnapshot;

        /// <summary>上一托管场景的 Unity 兜底卸载状态，YooAsset 失败或未销毁时使用。</summary>
        public UnitySceneUnloadState PreviousManagedUnityUnloadState;

        /// <summary>上一托管场景是否已确认销毁；确认前禁止启用新业务根。</summary>
        public bool IsPreviousManagedSceneUnloadComplete;

        /// <summary>上一活动场景以外的普通 Unity 场景卸载状态列表，用于有限重试和销毁确认。</summary>
        public List<UnitySceneUnloadState> PreviousUnityUnloadStates;

        /// <summary>是否已解除 Unity 场景加载的 0.9 进度挂起，保证同一句柄只接受一次成功操作。</summary>
        public bool IsLoadReleased;

        /// <summary>是否已开始清理上一活动场景，防止逐帧回调重复创建卸载操作。</summary>
        public bool IsPreviousSceneUnloadStarted;

        /// <summary>加载完成后校验到的默认禁用业务根，旧场景卸载前必须确认有效。</summary>
        public GameObject BusinessRoot;
    }

    /// <summary>最后一次成功启用业务根的场景类型；加载和卸载期间保持旧值。</summary>
    public SceneType _currentSceneType;

    /// <summary>跨场景复用的加载界面实例，只由最新请求更新或关闭。</summary>
    private LoadingView _loadingView;

    /// <summary>请求代次递增种子，同一应用生命周期内单调递增。</summary>
    private long _requestGenerationSeed;

    /// <summary>调用方最后一次提交的有效请求；等待期间只保留这一项。</summary>
    private SceneLoadRequest _latestRequest;

    /// <summary>当前唯一执行加载或卸载状态机的请求；非空时禁止启动另一个底层加载。</summary>
    private SceneLoadRequest _runningRequest;

    /// <summary>当前由管理器启用的 Additive 场景句柄，下一次切换必须先完成其卸载。</summary>
    private SceneOperationHandle _activeSceneHandle;

    /// <summary>当前拥有共享 Loading 显示权的请求代次，零表示没有请求持有。</summary>
    private long _loadingOwnerGeneration;

    /// <summary>场景无法确认销毁后进入的终止状态；为 true 时拒绝后续切场景以防叠加污染。</summary>
    private bool _isSceneCleanupBlocked;

    /// <summary>终止场景调度的具体清理失败原因，用于后续拒绝请求时提供诊断。</summary>
    private string _sceneCleanupBlockedReason;

    /// <summary>注入场景切换期间使用的 Loading 界面。</summary>
    /// <param name="loadingView">应用生命周期内复用的 Loading 实例，允许为空以支持无界面启动。</param>
    public void Init(LoadingView loadingView)
    {
        _loadingView = loadingView;
    }

    /// <summary>提交场景切换请求；运行请求完成隔离清理后只启动最后一次调用意图。</summary>
    /// <param name="sceneType">目标业务场景类型，其名称必须匹配 YooAsset 场景 Location 后缀。</param>
    /// <param name="callback">场景业务根成功启用后的可选一次性回调。</param>
    public void LoadSceneMode(SceneType sceneType, Action callback = null)
    {
        if (_isSceneCleanupBlocked)
        {
            Debug.LogError($"场景调度已阻断，拒绝加载 {sceneType}：{_sceneCleanupBlockedReason}");
            return;
        }
        // request 是只属于本次调用的强类型请求，等待槽被覆盖时不会触碰其他请求资源。
        SceneLoadRequest request = new SceneLoadRequest
        {
            Generation = ++_requestGenerationSeed,
            SceneType = sceneType,
            CompletedCallback = callback
        };
        _latestRequest = request;
        ShowLoadingView(request);

        if (_runningRequest != null)
        {
            TryReleaseSceneLoad(_runningRequest);
            return;
        }
        StartLatestRequestIfIdle();
    }

    /// <summary>底层空闲时启动最新等待请求，保证加载和卸载全过程严格串行。</summary>
    private void StartLatestRequestIfIdle()
    {
        if (_isSceneCleanupBlocked || _runningRequest != null ||
            _latestRequest == null)
        {
            return;
        }
        if (Global.Instance == null || Global.Instance.YooPackage == null)
        {
            // unavailableRequest 是本轮无法启动的最新请求，用于统一失败收口和日志。
            SceneLoadRequest unavailableRequest = _latestRequest;
            FailRequest(unavailableRequest, "YooAsset 资源包尚未初始化");
            return;
        }

        // runningRequest 成为当前唯一底层状态机，后续调用仅覆盖 latest 等待槽。
        SceneLoadRequest runningRequest = _latestRequest;
        _runningRequest = runningRequest;
        // sceneLocation 按项目场景目录与枚举名称拼出完整 YooAsset Location。
        string sceneLocation = $"{ConstDefine.ScenePath}{runningRequest.SceneType}";
        try
        {
            // Additive 保留旧活动场景；suspendLoad 保证管理器有机会判断请求是否已经过期。
            runningRequest.Handle = Global.Instance.YooPackage.LoadSceneAsync(
                sceneLocation, UnityEngine.SceneManagement.LoadSceneMode.Additive, true);
        }
        catch (Exception exception)
        {
            FailRequest(runningRequest, $"创建场景加载句柄异常：{exception.Message}");
            return;
        }
        if (runningRequest.Handle == null)
        {
            FailRequest(runningRequest, "YooAsset 返回了空场景加载句柄");
            return;
        }

        // updateSubscription 只写入 runningRequest，旧闭包永远无法释放后续请求的订阅。
        IDisposable updateSubscription = Observable.EveryUpdate().Subscribe(updateFrame =>
        {
            // currentUpdateFrame 是 UniRx 累计帧序号，仅表达回调数据语义，不参与状态判断。
            long currentUpdateFrame = updateFrame;
            UpdateRunningRequest(runningRequest, currentUpdateFrame);
        });
        runningRequest.UpdateSubscription = updateSubscription;
    }

    /// <summary>逐帧推进请求的加载、过期卸载、旧场景卸载和最终提交阶段。</summary>
    /// <param name="request">订阅闭包绑定的请求实例，只允许管理自己的句柄和卸载操作。</param>
    /// <param name="updateFrame">UniRx 累计帧序号，仅用于明确回调参数的数据语义。</param>
    private void UpdateRunningRequest(SceneLoadRequest request, long updateFrame)
    {
        if (request.RequestUnloadOperation != null ||
            request.RequestUnityUnloadState != null)
        {
            // YooAsset 卸载会立即使 Handle 失效，卸载阶段必须在任何句柄属性读取之前分流。
            UpdateObsoleteRequest(request);
            return;
        }
        if (!ReferenceEquals(_runningRequest, request) || request.Handle == null)
        {
            DisposeRequestSubscription(request);
            return;
        }
        if (!request.Handle.IsValid)
        {
            FailRequest(request, "YooAsset 场景句柄在加载阶段提前失效");
            return;
        }

        // isLatest 决定本请求是否仍有权更新共享界面和最终启用场景业务根。
        bool isLatest = ReferenceEquals(_latestRequest, request);
        if (isLatest)
        {
            RefreshLoadingView(request, request.Handle.Progress);
        }
        if (!request.IsLoadReleased && (!isLatest || request.Handle.Progress >= 0.9f))
        {
            TryReleaseSceneLoad(request);
        }
        if (!request.Handle.IsDone)
        {
            return;
        }
        if (request.Handle.Status != EOperationStatus.Succeed)
        {
            FailRequest(request, request.Handle.LastError);
            return;
        }
        if (!request.LoadedSceneSnapshot.IsValid())
        {
            // loadedSceneSnapshot 必须在任何 UnloadAsync 调用前保存，后续禁止再从失效句柄取场景。
            Scene loadedSceneSnapshot = request.Handle.SceneObject;
            request.LoadedSceneSnapshot = loadedSceneSnapshot;
        }
        if (!request.LoadedSceneSnapshot.IsValid() ||
            !request.LoadedSceneSnapshot.isLoaded)
        {
            FailRequest(request, "YooAsset 报告成功但目标场景快照无效或未加载");
            return;
        }
        if (!isLatest)
        {
            UpdateObsoleteRequest(request);
            return;
        }

        UpdateLatestRequest(request);
    }

    /// <summary>尝试解除指定请求的加载挂起；过期场景业务根默认禁用，因此完成加载不会执行其脚本。</summary>
    /// <param name="request">需要解除挂起的运行请求，只修改该请求自己的释放标记。</param>
    private void TryReleaseSceneLoad(SceneLoadRequest request)
    {
        if (request == null || request.Handle == null || request.IsLoadReleased)
        {
            return;
        }
        // releaseSucceeded 表示 YooAsset 已接受解除挂起，失败时保留 false 供后续帧重试。
        bool releaseSucceeded = request.Handle.UnSuspend();
        if (releaseSucceeded)
        {
            request.IsLoadReleased = true;
        }
    }

    /// <summary>推进过期请求卸载，必须等旧场景彻底销毁后才释放串行槽并启动最新请求。</summary>
    /// <param name="request">已经完成 Additive 加载但不再是 latest 的请求。</param>
    private void UpdateObsoleteRequest(SceneLoadRequest request)
    {
        if (request.IsPreviousSceneUnloadStarted &&
            !ArePreviousSceneUnloadsDone(request))
        {
            // 请求在清理旧活动场景期间过期时，仍须等旧 OnDestroy 完成后再卸载本请求场景。
            return;
        }
        if (request.RequestUnityUnloadState != null)
        {
            if (!UpdateUnitySceneUnload(request.RequestUnityUnloadState, request,
                "请求场景兜底卸载"))
            {
                return;
            }
            FinishRequestAndStartLatest(request);
            return;
        }
        if (request.RequestUnloadOperation == null)
        {
            if (request.Handle == null || !request.Handle.IsValid)
            {
                // 句柄已由外部失效时不能再调用或读取它，直接使用预存快照进入 Unity 有限卸载。
                request.RequestUnityUnloadState = new UnitySceneUnloadState
                {
                    SceneSnapshot = request.LoadedSceneSnapshot
                };
                if (UpdateUnitySceneUnload(request.RequestUnityUnloadState,
                    request, "失效请求句柄场景兜底卸载"))
                {
                    FinishRequestAndStartLatest(request);
                }
                return;
            }
            // 过期场景的 SceneBusinessRoot 始终保持默认禁用，开始卸载前不触发生命周期副作用。
            // LoadedSceneSnapshot 已在句柄有效时保存，UnloadAsync 返回后不得再读取 Handle.SceneObject。
            request.RequestUnloadOperation = request.Handle.UnloadAsync();
            return;
        }
        if (!request.RequestUnloadOperation.IsDone)
        {
            return;
        }
        if (request.RequestUnloadOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"过期场景卸载失败，代次={request.Generation}，场景={request.SceneType}，原因={request.RequestUnloadOperation.Error}");
        }
        // requestUnityUnloadState 无论 YooAsset 成败都再次确认快照已销毁，仍加载时执行有限兜底。
        request.RequestUnityUnloadState = new UnitySceneUnloadState
        {
            SceneSnapshot = request.LoadedSceneSnapshot
        };
        if (UpdateUnitySceneUnload(request.RequestUnityUnloadState, request,
            "请求场景兜底卸载"))
        {
            FinishRequestAndStartLatest(request);
        }
    }

    /// <summary>推进最新请求的旧场景清理，并仅在清理完成后启用新场景业务根。</summary>
    /// <param name="request">当前同时等于 running 和 latest 的已加载请求。</param>
    private void UpdateLatestRequest(SceneLoadRequest request)
    {
        if (request.BusinessRoot == null)
        {
            request.BusinessRoot = FindBusinessRoot(request.LoadedSceneSnapshot);
            if (request.BusinessRoot == null)
            {
                FailRequest(request, $"场景缺少唯一的 {SceneBusinessRootName}");
                return;
            }
            if (request.BusinessRoot.activeSelf)
            {
                FailRequest(request, $"场景 {SceneBusinessRootName} 未保持默认禁用");
                return;
            }
        }
        if (!request.IsPreviousSceneUnloadStarted)
        {
            BeginPreviousSceneUnload(request);
            return;
        }
        if (!ArePreviousSceneUnloadsDone(request))
        {
            return;
        }
        if (!ReferenceEquals(_latestRequest, request))
        {
            // 卸载旧场景期间出现新请求时，本场景仍未启用业务根，转入无副作用的过期卸载。
            UpdateObsoleteRequest(request);
            return;
        }
        CommitLatestRequest(request);
    }

    /// <summary>开始卸载上一托管场景及初始普通场景，新场景业务根在此阶段保持禁用。</summary>
    /// <param name="request">已加载且仍为 latest 的请求。</param>
    private void BeginPreviousSceneUnload(SceneLoadRequest request)
    {
        request.IsPreviousSceneUnloadStarted = true;
        request.PreviousUnityUnloadStates = new List<UnitySceneUnloadState>();
        // newScene 是当前请求已加载但业务根尚未启用的目标场景，清理时必须保留。
        Scene newScene = request.LoadedSceneSnapshot;
        // previousManagedScene 用于避免同一旧托管场景同时走 YooAsset 和 Unity 两条卸载路径。
        Scene previousManagedScene = new Scene();
        if (_activeSceneHandle != null && _activeSceneHandle.IsValid)
        {
            previousManagedScene = _activeSceneHandle.SceneObject;
            // 快照必须先于 UnloadAsync 保存，因为 YooAsset 会在调用内立即使句柄 Provider 失效。
            request.PreviousManagedSceneSnapshot = previousManagedScene;
            request.PreviousManagedUnloadOperation = _activeSceneHandle.UnloadAsync();
        }
        else
        {
            request.IsPreviousManagedSceneUnloadComplete = true;
        }

        // previousScenes 是开始卸载时的场景值快照，防止遍历期间 Unity 场景列表变化。
        List<Scene> previousScenes = CaptureLoadedScenes();
        // sceneIndex 是场景快照的零基索引。
        for (int sceneIndex = 0; sceneIndex < previousScenes.Count; sceneIndex++)
        {
            // previousScene 是待检查的旧场景，目标场景和已交给 YooAsset 的旧场景必须跳过。
            Scene previousScene = previousScenes[sceneIndex];
            if (!previousScene.IsValid() || !previousScene.isLoaded ||
                previousScene == newScene || previousScene == previousManagedScene)
            {
                continue;
            }
            // unloadState 保存普通旧场景快照并由统一有限重试逻辑创建和等待卸载操作。
            UnitySceneUnloadState unloadState = new UnitySceneUnloadState
            {
                SceneSnapshot = previousScene
            };
            request.PreviousUnityUnloadStates.Add(unloadState);
        }
    }

    /// <summary>检查所有旧场景是否已经销毁，保证旧 OnDestroy 先于新业务根 Awake。</summary>
    /// <param name="request">持有本轮旧场景卸载操作的最新请求。</param>
    /// <returns>YooAsset 和 Unity 的全部卸载操作均结束时返回 true。</returns>
    private bool ArePreviousSceneUnloadsDone(SceneLoadRequest request)
    {
        if (!request.IsPreviousManagedSceneUnloadComplete &&
            request.PreviousManagedUnloadOperation != null &&
            !request.PreviousManagedUnloadOperation.IsDone)
        {
            return false;
        }
        if (!request.IsPreviousManagedSceneUnloadComplete)
        {
            if (request.PreviousManagedUnloadOperation != null &&
                request.PreviousManagedUnloadOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"上一托管场景卸载失败：{request.PreviousManagedUnloadOperation.Error}");
            }
            if (request.PreviousManagedUnityUnloadState == null)
            {
                // 兜底状态只读取 UnloadAsync 前的快照，绝不访问已经失效的活动场景句柄。
                request.PreviousManagedUnityUnloadState = new UnitySceneUnloadState
                {
                    SceneSnapshot = request.PreviousManagedSceneSnapshot
                };
            }
            if (!UpdateUnitySceneUnload(request.PreviousManagedUnityUnloadState,
                request, "上一托管场景兜底卸载"))
            {
                return false;
            }
            request.IsPreviousManagedSceneUnloadComplete = true;
        }
        if (request.PreviousUnityUnloadStates == null)
        {
            return true;
        }

        // stateIndex 是普通 Unity 旧场景卸载状态的零基索引。
        for (int stateIndex = 0;
            stateIndex < request.PreviousUnityUnloadStates.Count;
            stateIndex++)
        {
            // unloadState 持有一个普通旧场景快照，未确认销毁时继续阻塞新业务根。
            UnitySceneUnloadState unloadState = request.PreviousUnityUnloadStates[stateIndex];
            if (!UpdateUnitySceneUnload(unloadState, request, "普通旧场景卸载"))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>以有限次数创建并等待 Unity 场景卸载，完成后再次确认场景快照已不再加载。</summary>
    /// <param name="unloadState">保存场景快照、当前操作和已尝试次数的强类型卸载状态。</param>
    /// <param name="ownerRequest">拥有本次清理责任的运行请求，用于失败时终止其调度链。</param>
    /// <param name="operationName">写入诊断日志的卸载阶段名称。</param>
    /// <returns>场景已经无效或不再加载时返回 true；等待、重试或阻断时返回 false。</returns>
    private bool UpdateUnitySceneUnload(UnitySceneUnloadState unloadState,
        SceneLoadRequest ownerRequest, string operationName)
    {
        if (unloadState == null)
        {
            return true;
        }
        // sceneSnapshot 始终来自句柄失效前保存的值，禁止从已卸载句柄重新获取。
        Scene sceneSnapshot = unloadState.SceneSnapshot;
        if (!sceneSnapshot.IsValid() || !sceneSnapshot.isLoaded)
        {
            return true;
        }
        if (_isSceneCleanupBlocked)
        {
            return false;
        }
        if (unloadState.Operation != null)
        {
            if (!unloadState.Operation.isDone)
            {
                return false;
            }
            unloadState.Operation = null;
            if (!sceneSnapshot.IsValid() || !sceneSnapshot.isLoaded)
            {
                return true;
            }
        }
        if (unloadState.StartAttemptCount >= MaxUnityUnloadStartAttempts)
        {
            BlockSceneLoading(ownerRequest,
                $"{operationName}超过最大尝试次数，场景仍处于加载状态：{sceneSnapshot.name}");
            return false;
        }

        unloadState.StartAttemptCount++;
        // unloadOperation 是本次 Unity 兜底卸载操作；返回空时保留快照并在下一帧有限重试。
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneSnapshot);
        if (unloadOperation == null)
        {
            Debug.LogError($"{operationName}无法创建 Unity 卸载操作，第 {unloadState.StartAttemptCount} 次，场景={sceneSnapshot.name}");
            if (unloadState.StartAttemptCount >= MaxUnityUnloadStartAttempts)
            {
                BlockSceneLoading(ownerRequest,
                    $"{operationName}连续返回空操作，场景仍处于加载状态：{sceneSnapshot.name}");
            }
            return false;
        }
        unloadState.Operation = unloadOperation;
        return false;
    }

    /// <summary>清理无法确认完成时终止场景调度，取消等待请求并拒绝后续切换，避免继续叠加场景。</summary>
    /// <param name="request">触发不可恢复清理失败的运行请求。</param>
    /// <param name="failureReason">包含卸载阶段和场景名称的明确终止原因。</param>
    private void BlockSceneLoading(SceneLoadRequest request, string failureReason)
    {
        if (_isSceneCleanupBlocked)
        {
            return;
        }
        _isSceneCleanupBlocked = true;
        _sceneCleanupBlockedReason = failureReason;
        if (_latestRequest != null)
        {
            _latestRequest.CompletedCallback = null;
        }
        if (request != null)
        {
            request.CompletedCallback = null;
        }
        _latestRequest = null;
        if (ReferenceEquals(_runningRequest, request))
        {
            _runningRequest = null;
        }
        DisposeRequestSubscription(request);
        ClearLoadingViewForBlockedState();
        Debug.LogError($"场景调度进入阻断状态：{failureReason}");
    }

    /// <summary>最终激活场景并启用唯一业务根，随后提交状态和执行一次回调。</summary>
    /// <param name="request">旧场景已全部销毁且仍为 latest 的请求。</param>
    private void CommitLatestRequest(SceneLoadRequest request)
    {
        // activationSucceeded 表示目标场景已成为 Unity 活动场景，失败时不得启用业务根。
        bool activationSucceeded = request.Handle.ActivateScene();
        if (!activationSucceeded)
        {
            FailRequest(request, "已加载场景无法设置为 Unity 活动场景");
            return;
        }
        DisposeRequestSubscription(request);
        // completedCallback 是提交前取出的局部快照，字段清空后可抵御同步重入和重复执行。
        Action completedCallback = request.CompletedCallback;
        request.CompletedCallback = null;
        _activeSceneHandle = request.Handle;
        _currentSceneType = request.SceneType;
        _runningRequest = null;
        _latestRequest = null;
        // 旧请求在任何新生命周期或回调执行前关闭自己的 Loading；后续同步请求只会重新显示并取得所有权。
        HideLoadingView(request);
        request.BusinessRoot.SetActive(true);

        if (completedCallback == null)
        {
            return;
        }
        try
        {
            completedCallback.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError($"场景 {request.SceneType} 完成回调执行异常：{exception}");
        }
    }

    /// <summary>处理加载或提交失败；已完成加载的场景必须等待卸载结束后才能继续。</summary>
    /// <param name="request">发生失败的请求，可能尚未创建有效场景句柄。</param>
    /// <param name="failureReason">YooAsset 或管理器产生的可诊断失败原因。</param>
    private void FailRequest(SceneLoadRequest request, string failureReason)
    {
        // safeFailureReason 避免底层空错误信息造成无法定位的日志。
        string safeFailureReason = failureReason;
        if (string.IsNullOrWhiteSpace(safeFailureReason))
        {
            safeFailureReason = "未知错误";
        }
        // failedSceneType 在防御性空请求路径下使用当前场景，保证日志字段稳定。
        SceneType failedSceneType = _currentSceneType;
        // failedGeneration 在防御性空请求路径下使用零，表示没有有效请求代次。
        long failedGeneration = 0;
        if (request != null)
        {
            failedSceneType = request.SceneType;
            failedGeneration = request.Generation;
        }
        Debug.LogError($"场景请求失败，代次={failedGeneration}，场景={failedSceneType}，原因={safeFailureReason}");

        // failedLatest 表示失败请求仍是最后意图；只有它可以清空 latest 和隐藏 Loading。
        bool failedLatest = ReferenceEquals(_latestRequest, request);
        if (failedLatest)
        {
            _latestRequest = null;
            HideLoadingView(request);
        }
        if (request != null && request.LoadedSceneSnapshot.IsValid() &&
            request.LoadedSceneSnapshot.isLoaded)
        {
            // 已加载失败场景的业务根仍保持禁用，复用过期卸载状态机等待其完全销毁。
            UpdateObsoleteRequest(request);
            return;
        }
        FinishRequestAndStartLatest(request);
    }

    /// <summary>结束指定请求的串行占用，并在其清理完成后启动仍在等待的 latest 请求。</summary>
    /// <param name="request">已经完成加载失败或场景卸载的运行请求。</param>
    private void FinishRequestAndStartLatest(SceneLoadRequest request)
    {
        DisposeRequestSubscription(request);
        if (ReferenceEquals(_runningRequest, request))
        {
            _runningRequest = null;
        }
        StartLatestRequestIfIdle();
    }

    /// <summary>精确释放指定请求自己的逐帧订阅，并清空字段保证幂等。</summary>
    /// <param name="request">需要停止状态轮询的请求，允许为空。</param>
    private void DisposeRequestSubscription(SceneLoadRequest request)
    {
        if (request == null || request.UpdateSubscription == null)
        {
            return;
        }
        // requestSubscription 是请求订阅局部快照，先清字段再释放以抵御同步重入。
        IDisposable requestSubscription = request.UpdateSubscription;
        request.UpdateSubscription = null;
        requestSubscription.Dispose();
    }

    /// <summary>查找目标场景唯一的默认禁用业务根。</summary>
    /// <param name="scene">YooAsset 已完成 Additive 加载的目标场景。</param>
    /// <returns>找到唯一同名根对象时返回实例，否则返回 null。</returns>
    private GameObject FindBusinessRoot(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }
        // rootObjects 是 Unity 返回的场景根对象快照，受管理场景应只有业务根一项。
        GameObject[] rootObjects = scene.GetRootGameObjects();
        // matchingRoot 保存唯一名称匹配项；重复名称视为资源配置错误。
        GameObject matchingRoot = null;
        // rootIndex 是场景根对象数组的零基索引。
        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            // rootObject 是当前待核对的场景根对象。
            GameObject rootObject = rootObjects[rootIndex];
            if (rootObject == null || rootObject.name != SceneBusinessRootName)
            {
                continue;
            }
            if (matchingRoot != null)
            {
                return null;
            }
            matchingRoot = rootObject;
        }
        return matchingRoot;
    }

    /// <summary>捕获当前全部已加载场景，供旧场景卸载阶段稳定按索引遍历。</summary>
    /// <returns>按 Unity 场景索引顺序保存的值类型快照。</returns>
    private List<Scene> CaptureLoadedScenes()
    {
        // loadedScenes 容量等于当前场景数，避免收集期间发生多次扩容。
        List<Scene> loadedScenes = new List<Scene>(SceneManager.sceneCount);
        // sceneIndex 是 Unity 已加载场景列表的零基索引。
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            // loadedScene 是捕获时对应索引的场景值快照。
            Scene loadedScene = SceneManager.GetSceneAt(sceneIndex);
            loadedScenes.Add(loadedScene);
        }
        return loadedScenes;
    }

    /// <summary>显示并置顶共享 Loading，仅在收到新 latest 请求时调用。</summary>
    /// <param name="request">取得共享 Loading 显示权的最新请求。</param>
    private void ShowLoadingView(SceneLoadRequest request)
    {
        _loadingOwnerGeneration = request.Generation;
        if (_loadingView == null)
        {
            return;
        }
        _loadingView.transform.SetAsLastSibling();
        _loadingView.Show();
        _loadingView.RefreshUI(0f, "加载场景中::0");
    }

    /// <summary>使用最新有效请求的零到一进度刷新共享 Loading。</summary>
    /// <param name="progress">YooAsset 返回的归一化加载进度，正常范围为零到一。</param>
    /// <param name="request">尝试刷新 Loading 的请求，代次必须仍持有显示权。</param>
    private void RefreshLoadingView(SceneLoadRequest request, float progress)
    {
        if (request == null || _loadingOwnerGeneration != request.Generation ||
            !ReferenceEquals(_latestRequest, request) || _loadingView == null)
        {
            return;
        }
        _loadingView.RefreshUI(progress, $"加载场景中::{progress * 100f:0}");
    }

    /// <summary>隐藏共享 Loading；只有最终提交成功或最新请求失败时允许调用。</summary>
    /// <param name="request">尝试隐藏 Loading 的请求，只有当前显示权所有者可以成功。</param>
    private void HideLoadingView(SceneLoadRequest request)
    {
        if (request == null || _loadingOwnerGeneration != request.Generation)
        {
            return;
        }
        _loadingOwnerGeneration = 0;
        if (_loadingView == null)
        {
            return;
        }
        _loadingView.Show(false);
    }

    /// <summary>进入不可恢复清理阻断态时清除 Loading 所有权并强制隐藏界面。</summary>
    private void ClearLoadingViewForBlockedState()
    {
        _loadingOwnerGeneration = 0;
        if (_loadingView == null)
        {
            return;
        }
        _loadingView.Show(false);
    }
}
