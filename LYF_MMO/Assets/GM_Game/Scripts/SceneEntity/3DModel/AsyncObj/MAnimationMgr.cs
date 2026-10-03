using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using YooAsset;

/// <summary>
/// RO 风格动画剪辑管理器。
/// 负责维护动画短名到 YooAsset Location 的解析表，以及按 Location 共享的异步剪辑加载、引用计数和回调队列。
/// MAnimator 只持有本类的 ClipInfo，不直接创建或释放 YooAsset 操作句柄。
/// </summary>
public sealed class MAnimationMgr
{
    /// <summary>RO 命名风格的单例访问入口。</summary>
    public static MAnimationMgr singleton
    {
        get { return _singleton; }
    }
    /// <summary>动画剪辑完成加载后的强类型回调。</summary>
    /// <param name="clipInfo">本次加载对应的共享剪辑信息；加载失败时其 Clip 为 null。</param>
    public delegate void LoadClipCallback(ClipInfo clipInfo);

    /// <summary>动画索引文本异步预加载完成后的强类型回调。</summary>
    /// <param name="isSucceed">为 true 表示二维 JSON 已完整解析并建立动画索引。</param>
    public delegate void PreloadCallback(bool isSucceed);

    /// <summary>YooAsset 中动画索引文本的固定 Location，文件内容使用 RO 同款二维 JSON。</summary>
    private const string AnimInfoLocation = "Assets/GM_Game/Resources/ResInfo/animInfo.txt";

    /// <summary>空闲剪辑 LRU 的最大保留数量，达到阈值时按批量回收。</summary>
    private const int IdleCacheCapacity = 64;

    /// <summary>单次 LRU 回收的最大剪辑数量，保持 RO 的 32 条批量淘汰语义。</summary>
    private const int IdleCacheReleaseCount = 32;

    /// <summary>空闲缓存执行淘汰检查的周期，单位为秒。</summary>
    private const float IdleCacheRecycleInterval = 60f;

    /// <summary>进程内唯一的动画管理器实例。</summary>
    private static readonly MAnimationMgr _singleton = new MAnimationMgr();

    /// <summary>动画短名到资源 Location 的映射，键为配置中的 clipName。</summary>
    private readonly Dictionary<string, string> _nameToPathDict =
        new Dictionary<string, string>();

    /// <summary>资源 Location 到共享 ClipInfo 的映射，条目在资源释放后保留元数据以供下次复用。</summary>
    private readonly Dictionary<string, ClipInfo> _clipInfoDict =
        new Dictionary<string, ClipInfo>();

    /// <summary>空闲剪辑的 LRU 路径队列，首节点为最久未使用的剪辑。</summary>
    private readonly LinkedList<string> _idleClipPathList =
        new LinkedList<string>();

    /// <summary>空闲剪辑路径到 LRU 节点的索引，用于 O(1) 取消空闲状态。</summary>
    private readonly Dictionary<string, LinkedListNode<string>> _idleClipNodeDict =
        new Dictionary<string, LinkedListNode<string>>();

    /// <summary>等待动画索引文本异步加载完成的调用方回调队列。</summary>
    private readonly List<PreloadCallback> _preloadCallbacks =
        new List<PreloadCallback>();

    /// <summary>下一次允许执行 LRU 批量淘汰检查的实时启动秒数。</summary>
    private float _nextRecycleTimestamp;

    /// <summary>当前是否正在通过 YooAsset 异步加载动画索引文本。</summary>
    private bool _isPreloading;

    /// <summary>当前动画索引是否已经完成一次成功预加载。</summary>
    private bool _isPreloaded;

    /// <summary>当前动画索引 YooAsset 加载句柄，用于退出或重试时完整归还资源租约。</summary>
    private AssetOperationHandle _preloadHandle;

    /// <summary>动画索引加载会话版本；反初始化后到达的旧回调不得改写新生命周期状态。</summary>
    private int _preloadVersion;

    /// <summary>私有构造函数，禁止外部重复创建管理器。</summary>
    private MAnimationMgr()
    {
    }




    /// <summary>当前项目 PascalCase 风格的单例访问入口。</summary>
    public static MAnimationMgr Instance
    {
        get { return _singleton; }
    }

    /// <summary>
    /// 单条共享动画剪辑信息。
    /// 一个 ClipInfo 只对应一个资源 Location；多个实体动画覆盖同一剪辑时共用一次 YooAsset 加载和同一资源句柄。
    /// </summary>
    public sealed class ClipInfo
    {
        /// <summary>所属动画管理器，用于读取运行时资源包和释放共享句柄。</summary>
        private readonly MAnimationMgr _manager;

        /// <summary>等待当前异步加载完成的动画信息回调列表。</summary>
        private readonly List<LoadClipCallback> _loadCallbacks =
            new List<LoadClipCallback>();

        /// <summary>当前 YooAsset 异步加载操作句柄；加载成功后作为剪辑资源租约持有。</summary>
        private AssetOperationHandle _assetHandle;

        /// <summary>当前异步加载会话版本，忽略已被释放或重新发起的旧回调。</summary>
        private int _loadVersion;

        /// <summary>是否已有未完成的 YooAsset 加载请求。</summary>
        private bool _isLoading;

        /// <summary>使用该剪辑的 MAnimator.MAnimInfo 数量；大于零时资源句柄必须保持有效。</summary>
        private int _referenceCount;

        /// <summary>YooAsset 动画资源 Location，创建后不变。</summary>
        public string ClipPath { get; private set; }

        /// <summary>已加载 AnimationClip 的实际时长，单位为秒；加载前可能为配置值或零。</summary>
        public float Length { get; private set; }

        /// <summary>动画是否循环；加载成功后以 AnimationClip.isLooping 为准。</summary>
        public bool IsLooping { get; private set; }

        /// <summary>已加载的 Unity AnimationClip；加载中、失败或无引用释放后为 null。</summary>
        public AnimationClip Clip { get; private set; }

        /// <summary>当前使用该资源的动画信息数量，仅供调试和生命周期核对。</summary>
        internal int ReferenceCount { get { return _referenceCount; } }

        /// <summary>
        /// 创建指定 Location 的共享剪辑信息。
        /// </summary>
        /// <param name="manager">所属动画管理器实例。</param>
        /// <param name="clipPath">YooAsset 资源 Location。</param>
        /// <param name="length">配置预估时长，单位为秒；未知时传零。</param>
        /// <param name="isLooping">配置循环标记；加载成功后会被实际资源属性覆盖。</param>
        internal ClipInfo(MAnimationMgr manager, string clipPath, float length,
            bool isLooping)
        {
            _manager = manager;
            ClipPath = clipPath;
            Length = length;
            IsLooping = isLooping;
        }

        /// <summary>
        /// 增加一个动画覆盖使用者，并确保剪辑已通过 YooAsset 发起异步加载。
        /// 已加载资源会在当前调用栈同步通知，加载中资源只会登记回调而不会重复创建句柄。
        /// </summary>
        /// <param name="callback">该使用者的加载完成回调，释放前会从等待列表移除。</param>
        internal void RetainClip(LoadClipCallback callback)
        {
            _referenceCount++;
            // 剪辑重新被实体使用时必须从空闲 LRU 移除，避免后续批量淘汰误释放正在播放的资源。
            _manager.RemoveClipFromIdleCache(ClipPath);
            // 即使本次剪辑已被重新占用，也可顺带按周期淘汰其他长期空闲的资源。
            _manager.UpdateCache();
            if (Clip != null)
            {
                if (callback != null)
                {
                    callback(this);
                }
                return;
            }

            if (callback != null)
            {
                _loadCallbacks.Add(callback);
            }

            if (_isLoading)
            {
                return;
            }

            StartLoad();
        }

        /// <summary>
        /// 释放一个动画覆盖使用者。
        /// 引用归零且资源已加载时进入空闲 LRU；加载中的句柄保留到完成回调以安全结束异步请求。
        /// </summary>
        /// <param name="callback">需要从等待列表移除的加载完成回调。</param>
        internal void ReleaseClip(LoadClipCallback callback)
        {
            if (_referenceCount > 0)
            {
                _referenceCount--;
            }

            RemoveCallback(callback);

            if (_referenceCount == 0 && Clip != null && !_isLoading)
            {
                // RO 语义：无使用者的剪辑先缓存，达到 LRU 淘汰条件才真正归还 YooAsset 句柄。
                _manager.AddClipToIdleCache(ClipPath);
            }
        }

        /// <summary>
        /// 注册后首次使用时发起 YooAsset 异步加载。
        /// 无有效 Global、资源包或 Location 时按失败完成回调，避免等待者永久滞留。
        /// </summary>
        private void StartLoad()
        {
            if (Global.Instance == null || Global.Instance.YooPackage == null ||
                !Global.Instance.YooPackage.CheckLocationValid(ClipPath))
            {
                CompleteLoad(_loadVersion, null, null);
                return;
            }

            _isLoading = true;
            _loadVersion++;
            int loadVersion = _loadVersion;
            _assetHandle = Global.Instance.YooPackage.LoadAssetAsync<AnimationClip>(ClipPath);
            _assetHandle.Completed += handle => OnLoadCompleted(loadVersion, handle);
        }

        /// <summary>
        /// YooAsset 异步操作完成回调，读取实际剪辑并交由统一完成逻辑处理。
        /// </summary>
        /// <param name="loadVersion">发起本次加载时记录的会话版本。</param>
        /// <param name="handle">YooAsset 回调返回的操作句柄。</param>
        private void OnLoadCompleted(int loadVersion, AssetOperationHandle handle)
        {
            AnimationClip clip = null;
            if (handle != null && handle.Status == EOperationStatus.Succeed)
            {
                clip = handle.GetAssetObject<AnimationClip>();
            }

            CompleteLoad(loadVersion, handle, clip);
        }

        /// <summary>
        /// 结束一次异步加载并通知仍持有引用的动画信息。
        /// 旧会话回调只释放自身句柄，不能修改当前 ClipInfo 的状态。
        /// </summary>
        /// <param name="loadVersion">完成回调所属的加载版本。</param>
        /// <param name="handle">YooAsset 操作句柄；资源包不可用时可为 null。</param>
        /// <param name="clip">成功取得的 AnimationClip；失败时为 null。</param>
        private void CompleteLoad(int loadVersion, AssetOperationHandle handle,
            AnimationClip clip)
        {
            if (loadVersion != _loadVersion)
            {
                if (handle != null && ReferenceEquals(_assetHandle, handle))
                {
                    handle.Release();
                    _assetHandle = null;
                }
                return;
            }

            _isLoading = false;
            if (clip == null)
            {
                if (handle != null)
                {
                    handle.Release();
                }
                _assetHandle = null;
                NotifyLoadCallbacks();
                return;
            }

            Clip = clip;
            Length = clip.length;
            IsLooping = clip.isLooping;
            _assetHandle = handle;
            NotifyLoadCallbacks();

            if (_referenceCount == 0)
            {
                // 异步加载完成前全部使用者已释放时，资源同样进入空闲缓存而非立即释放。
                _manager.AddClipToIdleCache(ClipPath);
            }
        }

        /// <summary>
        /// 使用快照顺序通知等待加载完成的所有动画信息。
        /// 回调执行期间允许重新覆盖或释放同一 ClipInfo，因此先清空原队列避免重复通知。
        /// </summary>
        private void NotifyLoadCallbacks()
        {
            List<LoadClipCallback> callbacks =
                new List<LoadClipCallback>(_loadCallbacks);
            _loadCallbacks.Clear();
            for (int index = 0; index < callbacks.Count; index++)
            {
                LoadClipCallback callback = callbacks[index];
                if (callback != null)
                {
                    callback(this);
                }
            }
        }

        /// <summary>
        /// 从等待列表中删除指定动画信息回调。
        /// 同一动画信息在快速二次覆盖时可能出现多个登记，因此从后向前移除全部匹配项。
        /// </summary>
        /// <param name="callback">需要删除的回调；空回调无需处理。</param>
        private void RemoveCallback(LoadClipCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            for (int index = _loadCallbacks.Count - 1; index >= 0; index--)
            {
                if (_loadCallbacks[index] == callback)
                {
                    _loadCallbacks.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// 释放当前已加载剪辑的 YooAsset 句柄并保留路径元数据。
        /// 下次 RetainClip 会以相同 ClipInfo 重新加载，因此不会产生路径信息分裂。
        /// </summary>
        private void ReleaseLoadedAsset()
        {
            if (_assetHandle != null)
            {
                _assetHandle.Release();
            }
            _assetHandle = null;
            Clip = null;
        }

        /// <summary>
        /// 用新索引行更新尚未加载剪辑的静态元数据。
        /// 已加载资源的真实时长和循环标记优先级更高，不在此处覆盖。
        /// </summary>
        /// <param name="length">索引记录的剪辑时长，单位为秒，必须不小于零。</param>
        /// <param name="isLooping">索引记录的循环标记。</param>
        internal void UpdateConfigInfo(float length, bool isLooping)
        {
            if (Clip != null)
            {
                return;
            }

            Length = length;
            IsLooping = isLooping;
        }

        /// <summary>
        /// 由管理器在 LRU 淘汰时归还空闲资源句柄。
        /// 仅引用为零且不存在未完成异步请求时可执行，避免释放正在使用或加载中的剪辑。
        /// </summary>
        internal void ReleaseIdleClip()
        {
            if (_referenceCount != 0 || _isLoading)
            {
                return;
            }

            ReleaseLoadedAsset();
        }

        /// <summary>
        /// 强制结束当前剪辑生命周期，取消全部等待回调并归还已加载或加载中的 YooAsset 句柄。
        /// 仅由 MAnimationMgr.Uninit 在客户端退出或 Global 被销毁时调用。
        /// </summary>
        internal void ClearClip()
        {
            _loadVersion++;
            _isLoading = false;
            _loadCallbacks.Clear();
            _referenceCount = 0;
            if (_assetHandle != null)
            {
                _assetHandle.Release();
            }
            _assetHandle = null;
            Clip = null;
        }
    }

    /// <summary>
    /// 当前动画索引是否已经成功加载完成。
    /// 为 false 时拒绝短名解析，仅允许当前资源包可验证的完整 YooAsset Location。
    /// </summary>
    public bool IsPreloaded
    {
        get { return _isPreloaded; }
    }

    /// <summary>
    /// 异步加载并解析 RO 格式的动画索引文本。
    /// 同一时刻的重复调用会共用一个 YooAsset TextAsset 加载请求，所有调用方在完成后收到相同结果。
    /// </summary>
    /// <param name="callback">预加载结果回调；传入 null 表示调用方只触发加载不接收结果。</param>
    public void Preload(PreloadCallback callback)
    {
        if (_isPreloaded)
        {
            if (callback != null)
            {
                callback(true);
            }
            return;
        }

        if (callback != null)
        {
            _preloadCallbacks.Add(callback);
        }

        if (_isPreloading)
        {
            return;
        }

        if (Global.Instance == null || Global.Instance.YooPackage == null ||
            !Global.Instance.YooPackage.CheckLocationValid(AnimInfoLocation))
        {
            Debug.LogError("动画索引文本地址无效：" + AnimInfoLocation);
            NotifyPreloadCallbacks(false);
            return;
        }

        _isPreloading = true;
        _preloadVersion++;
        // 当前动画索引加载会话编号，用于过滤退出或重新初始化后的旧回调。
        int preloadVersion = _preloadVersion;
        // 动画索引必须经 YooAsset 加载，不能使用 Resources.Load 绕开热更新资源包。
        _preloadHandle =
            Global.Instance.YooPackage.LoadAssetAsync<TextAsset>(AnimInfoLocation);
        _preloadHandle.Completed += handle =>
            OnAnimInfoLoadCompleted(preloadVersion, handle);
    }

    /// <summary>
    /// YooAsset 动画索引文本加载完成回调。
    /// 无论解析成功或失败都释放 TextAsset 操作句柄，索引内容已被复制进受管字典。
    /// </summary>
    /// <param name="preloadVersion">发起请求时记录的动画索引加载会话编号。</param>
    /// <param name="handle">本次 TextAsset 异步加载的 YooAsset 操作句柄。</param>
    private void OnAnimInfoLoadCompleted(int preloadVersion,
        AssetOperationHandle handle)
    {
        if (preloadVersion != _preloadVersion)
        {
            if (handle != null && ReferenceEquals(_preloadHandle, handle))
            {
                handle.Release();
                _preloadHandle = null;
            }
            return;
        }

        // 解析结果用于向全部等待预加载的调用方返回一致状态。
        bool isSucceed = false;
        // YooAsset 返回的文本资源，成功后仅读取其 UTF-8 字符串内容。
        TextAsset animInfoAsset = null;
        if (handle != null && handle.Status == EOperationStatus.Succeed)
        {
            animInfoAsset = handle.GetAssetObject<TextAsset>();
        }

        if (animInfoAsset != null)
        {
            isSucceed = ApplyAnimInfoText(animInfoAsset.text);
        }
        else
        {
            Debug.LogError("动画索引文本加载失败：" + AnimInfoLocation);
        }

        if (handle != null)
        {
            handle.Release();
        }
        _preloadHandle = null;

        _isPreloading = false;
        _isPreloaded = isSucceed;
        NotifyPreloadCallbacks(isSucceed);
    }

    /// <summary>
    /// 通知本轮动画索引预加载的全部等待者。
    /// 先清空原队列以允许回调中再次发起预加载而不会被旧回调重复触发。
    /// </summary>
    /// <param name="isSucceed">二维 JSON 是否完整解析并写入动画索引。</param>
    private void NotifyPreloadCallbacks(bool isSucceed)
    {
        // 回调快照保证调用方在回调内增删监听不会影响本轮通知顺序。
        List<PreloadCallback> callbacks =
            new List<PreloadCallback>(_preloadCallbacks);
        _preloadCallbacks.Clear();
        for (int index = 0; index < callbacks.Count; index++)
        {
            // 当前索引对应的一次异步预加载结果回调。
            PreloadCallback callback = callbacks[index];
            if (callback != null)
            {
                callback(isSucceed);
            }
        }
    }

    /// <summary>
    /// 解析二维 JSON 并以完整成功为前提替换短名索引。
    /// 已存在的 ClipInfo 不会被清空，确保预加载之后仍被实体引用的剪辑生命周期完整。
    /// </summary>
    /// <param name="animInfoText">RO 格式动画索引文本，每条结构为 [Location, 时长秒, 是否循环]。</param>
    /// <returns>所有条目均合法且至少包含一条动画记录时返回 true。</returns>
    private bool ApplyAnimInfoText(string animInfoText)
    {
        // 解析出的强类型动画配置行，全部成功后才写入运行时映射。
        List<AnimationConfigEntry> entries;
        if (!TryParseAnimInfo(animInfoText, out entries))
        {
            Debug.LogError("动画索引文本不是合法二维 JSON：" + AnimInfoLocation);
            return false;
        }

        if (entries.Count == 0)
        {
            Debug.LogError("动画索引文本不包含任何动作：" + AnimInfoLocation);
            return false;
        }

        _nameToPathDict.Clear();
        for (int index = 0; index < entries.Count; index++)
        {
            // 当前二维 JSON 行的完整 Location、时长和循环属性。
            AnimationConfigEntry entry = entries[index];
            // 短名由实际 Unity 资源文件名去除 .anim 后缀得到，匹配表现表中的 clipName。
            string clipName = GetClipNameFromPath(entry.ClipPath);
            if (string.IsNullOrEmpty(clipName) ||
                !RegisterClipPath(clipName, entry.ClipPath, entry.Length,
                    entry.IsLooping))
            {
                Debug.LogError("动画索引条目无效：" + entry.ClipPath);
                _nameToPathDict.Clear();
                return false;
            }
        }

        Debug.Log("动画索引加载完成，数量：" + entries.Count);
        return true;
    }

    /// <summary>
    /// 从完整 YooAsset Location 得到不含扩展名的动画短名。
    /// </summary>
    /// <param name="clipPath">Unity 工程内的完整动画资源 Location。</param>
    /// <returns>不带 .anim 扩展名的文件短名；路径非法时返回空字符串。</returns>
    private string GetClipNameFromPath(string clipPath)
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            return string.Empty;
        }

        // 最后一个正斜杠后是资源文件名；YooAsset Location 固定使用正斜杠。
        int separatorIndex = clipPath.LastIndexOf('/');
        // 仅移除文件名部分的最后一个扩展名，目录名中的点不参与判断。
        int extensionIndex = clipPath.LastIndexOf('.');
        if (extensionIndex <= separatorIndex)
        {
            extensionIndex = clipPath.Length;
        }

        int nameStartIndex = separatorIndex + 1;
        if (nameStartIndex >= extensionIndex)
        {
            return string.Empty;
        }

        return clipPath.Substring(nameStartIndex, extensionIndex - nameStartIndex);
    }

    /// <summary>
    /// 将引用归零的已加载剪辑放入 LRU 队尾，并按周期检查是否需要批量淘汰。
    /// </summary>
    /// <param name="clipPath">需要标记为空闲的 YooAsset 动画资源 Location。</param>
    private void AddClipToIdleCache(string clipPath)
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            return;
        }

        // 重复进入空闲态时先移除旧节点，再作为最新空闲项放到队尾。
        RemoveClipFromIdleCache(clipPath);
        // 新增节点表示该剪辑最近一次变为空闲的顺序。
        LinkedListNode<string> idleNode = _idleClipPathList.AddLast(clipPath);
        _idleClipNodeDict.Add(clipPath, idleNode);
        UpdateCache();
    }

    /// <summary>
    /// 将重新被使用的剪辑从空闲 LRU 中移除。
    /// </summary>
    /// <param name="clipPath">重新被实体引用的 YooAsset 动画资源 Location。</param>
    private void RemoveClipFromIdleCache(string clipPath)
    {
        // 当前路径在 LRU 中的节点，用于 O(1) 删除。
        LinkedListNode<string> idleNode;
        if (!_idleClipNodeDict.TryGetValue(clipPath, out idleNode))
        {
            return;
        }

        _idleClipNodeDict.Remove(clipPath);
        _idleClipPathList.Remove(idleNode);
    }

    /// <summary>
    /// 执行一次空闲剪辑 LRU 周期检查。
    /// 该入口可由全局帧循环驱动；同时 Retain/Release 时也会惰性触发，确保没有额外驱动时仍能回收。
    /// </summary>
    public void UpdateCache()
    {
        // Unity 启动后的实时时间不受游戏时间缩放影响，适合作为资源缓存周期时钟。
        float currentTimestamp = Time.realtimeSinceStartup;
        if (currentTimestamp < _nextRecycleTimestamp)
        {
            return;
        }

        _nextRecycleTimestamp = currentTimestamp + IdleCacheRecycleInterval;
        if (_idleClipPathList.Count < IdleCacheCapacity)
        {
            return;
        }

        // 达到容量阈值时最多释放 32 条最久未使用剪辑，保留 RO 的分批释放策略。
        int releasedCount = 0;
        while (releasedCount < IdleCacheReleaseCount &&
            _idleClipPathList.First != null)
        {
            // LRU 队头永远是最久未使用的空闲剪辑。
            LinkedListNode<string> idleNode = _idleClipPathList.First;
            // 被淘汰剪辑的完整 YooAsset Location。
            string clipPath = idleNode.Value;
            _idleClipPathList.RemoveFirst();
            _idleClipNodeDict.Remove(clipPath);

            // 资源信息可能被手动登记逻辑替换，找不到时只移除失效 LRU 节点。
            ClipInfo clipInfo;
            if (_clipInfoDict.TryGetValue(clipPath, out clipInfo))
            {
                clipInfo.ReleaseIdleClip();
            }
            releasedCount++;
        }
    }

    /// <summary>
    /// 结束动画资源管理器当前生命周期，归还索引及全部剪辑句柄并清空回调、引用和 LRU 状态。
    /// 单例对象本身继续存在，后续新的 Global 生命周期可再次调用 Preload 建立索引。
    /// </summary>
    public void Uninit()
    {
        _preloadVersion++;
        _isPreloading = false;
        _isPreloaded = false;
        _preloadCallbacks.Clear();
        if (_preloadHandle != null)
        {
            _preloadHandle.Release();
        }
        _preloadHandle = null;

        // 共享剪辑快照用于在清空字典前逐项释放可能仍在加载或被缓存的 YooAsset 句柄。
        List<ClipInfo> clipInfos = new List<ClipInfo>(_clipInfoDict.Values);
        for (int index = 0; index < clipInfos.Count; index++)
        {
            // 当前待结束生命周期的共享剪辑信息。
            ClipInfo clipInfo = clipInfos[index];
            if (clipInfo != null)
            {
                clipInfo.ClearClip();
            }
        }

        _clipInfoDict.Clear();
        _nameToPathDict.Clear();
        _idleClipPathList.Clear();
        _idleClipNodeDict.Clear();
        _nextRecycleTimestamp = 0f;
    }

    /// <summary>
    /// 登记动画配置短名与 YooAsset Location 的关系。
    /// 同名重复登记会覆盖旧映射，但不会销毁仍被实体引用的旧 ClipInfo。
    /// </summary>
    /// <param name="clipName">配置或表现表中的动画短名，不能为空。</param>
    /// <param name="clipPath">YooAsset 动画资源 Location，不能为空。</param>
    /// <param name="length">可选配置时长，单位秒；未知时为零。</param>
    /// <param name="isLooping">可选配置循环标记；加载后会被资源实际值覆盖。</param>
    /// <returns>登记成功返回 true；参数非法返回 false。</returns>
    public bool RegisterClipPath(string clipName, string clipPath, float length,
        bool isLooping)
    {
        if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(clipPath))
        {
            return false;
        }

        _nameToPathDict[clipName] = clipPath;
        ClipInfo clipInfo;
        if (!_clipInfoDict.TryGetValue(clipPath, out clipInfo))
        {
            clipInfo = new ClipInfo(this, clipPath, length, isLooping);
            _clipInfoDict.Add(clipPath, clipInfo);
        }
        else
        {
            // 索引文本重新加载时只刷新尚未加载资源的静态元数据，不影响正在使用的实际剪辑属性。
            clipInfo.UpdateConfigInfo(length, isLooping);
        }
        return true;
    }

    /// <summary>
    /// 登记没有预设时长的动画短名与资源 Location。
    /// 运行时资源成功加载后会自动填充时长和循环标记。
    /// </summary>
    /// <param name="clipName">配置或表现表中的动画短名。</param>
    /// <param name="clipPath">YooAsset 动画资源 Location。</param>
    /// <returns>登记成功返回 true；参数非法返回 false。</returns>
    public bool RegisterClipPath(string clipName, string clipPath)
    {
        return RegisterClipPath(clipName, clipPath, 0f, false);
    }

    /// <summary>
    /// 按动画短名解析 YooAsset Location。
    /// 未预登记时仅允许把本身可被当前资源包验证的完整 Location 作为直接路径使用，不会伪造资源路径。
    /// </summary>
    /// <param name="clipName">配置动画名或完整 YooAsset Location。</param>
    /// <returns>可加载的 ClipPath；没有映射或路径无效时返回空字符串。</returns>
    public string GetClipPath(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            return string.Empty;
        }

        if (!_isPreloaded)
        {
            if (Global.Instance != null && Global.Instance.YooPackage != null &&
                Global.Instance.YooPackage.CheckLocationValid(clipName))
            {
                RegisterClipPath(clipName, clipName);
                return clipName;
            }

            Debug.LogError("动画索引尚未加载成功，拒绝解析动画短名：" + clipName);
            return string.Empty;
        }

        string clipPath;
        if (_nameToPathDict.TryGetValue(clipName, out clipPath))
        {
            return clipPath;
        }

        if (Global.Instance != null && Global.Instance.YooPackage != null &&
            Global.Instance.YooPackage.CheckLocationValid(clipName))
        {
            RegisterClipPath(clipName, clipName);
            return clipName;
        }

        Debug.LogError("动画剪辑路径未登记或无效：" + clipName);
        return string.Empty;
    }

    /// <summary>
    /// 按完整资源 Location 获取共享剪辑信息。
    /// 只有已登记的路径才返回对象，防止错误的 clipName 被当作资源 Location 使用。
    /// </summary>
    /// <param name="clipPath">YooAsset 动画资源 Location。</param>
    /// <returns>共享 ClipInfo；未登记时返回 null。</returns>
    public ClipInfo GetClipInfo(string clipPath)
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            return null;
        }

        ClipInfo clipInfo;
        if (_clipInfoDict.TryGetValue(clipPath, out clipInfo))
        {
            return clipInfo;
        }
        return null;
    }

    /// <summary>
    /// 解析 RO 兼容二维 JSON 的动画配置。
    /// 每一行必须严格包含三个值：完整 YooAsset Location、秒为单位的时长、循环标记；数值与布尔值允许使用 RO 原始的字符串写法。
    /// </summary>
    /// <param name="animInfoText">从 YooAsset TextAsset 读取的 UTF-8 动画索引内容。</param>
    /// <param name="entries">解析成功时返回的强类型动画配置行；失败时返回空列表。</param>
    /// <returns>二维 JSON 结构和所有字段值均合法时返回 true。</returns>
    private bool TryParseAnimInfo(string animInfoText,
        out List<AnimationConfigEntry> entries)
    {
        // 无论解析是否成功都返回非空集合，调用方无需额外判空。
        entries = new List<AnimationConfigEntry>();
        if (string.IsNullOrEmpty(animInfoText))
        {
            return false;
        }

        // 字符级读取器只接受本索引所需的二维 JSON 数据，避免将弱类型 JSON 对象引入业务层。
        AnimationInfoReader reader = new AnimationInfoReader(animInfoText);
        if (!reader.TryConsume('['))
        {
            return false;
        }

        while (true)
        {
            if (reader.TryConsume(']'))
            {
                return reader.IsAtEnd();
            }

            if (!reader.TryConsume('['))
            {
                return false;
            }

            // 二维数组第 0 列：完整 YooAsset 动画资源 Location。
            string clipPath;
            if (!reader.TryReadValue(out clipPath) || !reader.TryConsume(','))
            {
                return false;
            }

            // 二维数组第 1 列：动画时长，单位秒，按不受本地化影响的浮点格式解析。
            string lengthText;
            if (!reader.TryReadValue(out lengthText) || !reader.TryConsume(','))
            {
                return false;
            }

            float length;
            if (!float.TryParse(lengthText, NumberStyles.Float,
                CultureInfo.InvariantCulture, out length) || length < 0f)
            {
                return false;
            }

            // 二维数组第 2 列：循环标记，兼容 RO 文本中的 True/False 字符串。
            string isLoopingText;
            if (!reader.TryReadValue(out isLoopingText))
            {
                return false;
            }

            bool isLooping;
            if (!bool.TryParse(isLoopingText, out isLooping) ||
                !reader.TryConsume(']'))
            {
                return false;
            }

            if (string.IsNullOrEmpty(clipPath))
            {
                return false;
            }

            // 当前行已完整校验，才能写入强类型配置集合。
            entries.Add(new AnimationConfigEntry(clipPath, length, isLooping));

            if (reader.TryConsume(']'))
            {
                return reader.IsAtEnd();
            }

            if (!reader.TryConsume(','))
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 单条动画索引的强类型数据。
    /// 配置从 TextAsset 解析后仅在预加载阶段存在，用于建立短名和共享 ClipInfo 映射。
    /// </summary>
    private sealed class AnimationConfigEntry
    {
        /// <summary>Unity 工程内可被 YooAsset 清单识别的完整动画资源 Location。</summary>
        public readonly string ClipPath;

        /// <summary>动作剪辑配置时长，单位为秒，加载实际 AnimationClip 后可被真实时长覆盖。</summary>
        public readonly float Length;

        /// <summary>动作是否按配置循环播放，加载实际 AnimationClip 后可被真实循环标记覆盖。</summary>
        public readonly bool IsLooping;

        /// <summary>
        /// 创建一条已解析的动画配置记录。
        /// </summary>
        /// <param name="clipPath">完整 YooAsset 动画资源 Location。</param>
        /// <param name="length">配置时长，单位为秒，必须不小于零。</param>
        /// <param name="isLooping">配置的循环播放标记。</param>
        public AnimationConfigEntry(string clipPath, float length, bool isLooping)
        {
            ClipPath = clipPath;
            Length = length;
            IsLooping = isLooping;
        }
    }

    /// <summary>
    /// 只服务动画二维 JSON 的轻量字符读取器。
    /// 它返回明确的字符串标量，避免通用 JSON 反序列化在业务边界产生弱类型载荷。
    /// </summary>
    private sealed class AnimationInfoReader
    {
        /// <summary>待解析的完整 UTF-8 文本内容。</summary>
        private readonly string _text;

        /// <summary>下一次读取的字符下标，范围为 0 到文本长度。</summary>
        private int _position;

        /// <summary>
        /// 创建动画二维 JSON 的字符读取器。
        /// </summary>
        /// <param name="text">必须由 YooAsset TextAsset 提供的原始动画索引文本。</param>
        public AnimationInfoReader(string text)
        {
            _text = text;
            _position = 0;
        }

        /// <summary>
        /// 消费下一个非空白字符，只有它与期望字符一致时才向前推进。
        /// </summary>
        /// <param name="expectedCharacter">期望读取到的 JSON 结构字符。</param>
        /// <returns>成功消费期望字符时返回 true。</returns>
        public bool TryConsume(char expectedCharacter)
        {
            SkipWhitespace();
            if (_position >= _text.Length ||
                _text[_position] != expectedCharacter)
            {
                return false;
            }

            _position++;
            return true;
        }

        /// <summary>
        /// 读取一个 JSON 标量。
        /// 双引号包围的字符串会处理标准 JSON 转义；未加引号的数值和布尔值读取到逗号、方括号或空白为止。
        /// </summary>
        /// <param name="value">成功读取到的标量文本。</param>
        /// <returns>标量格式合法且非空时返回 true。</returns>
        public bool TryReadValue(out string value)
        {
            value = string.Empty;
            SkipWhitespace();
            if (_position >= _text.Length)
            {
                return false;
            }

            if (_text[_position] == '"')
            {
                return TryReadString(out value);
            }

            // 未加引号的 JSON 标量起始位置。
            int startPosition = _position;
            while (_position < _text.Length)
            {
                // 当前字符决定未加引号标量的结束边界。
                char currentCharacter = _text[_position];
                if (currentCharacter == ',' || currentCharacter == ']' ||
                    currentCharacter == '[' || char.IsWhiteSpace(currentCharacter))
                {
                    break;
                }
                _position++;
            }

            if (_position == startPosition)
            {
                return false;
            }

            value = _text.Substring(startPosition, _position - startPosition);
            return true;
        }

        /// <summary>
        /// 判断当前读取位置之后仅剩空白字符。
        /// </summary>
        /// <returns>已读取完整 JSON 文本时返回 true。</returns>
        public bool IsAtEnd()
        {
            SkipWhitespace();
            return _position >= _text.Length;
        }

        /// <summary>
        /// 读取 JSON 双引号字符串，并转换常见转义字符和 Unicode 编码。
        /// </summary>
        /// <param name="value">成功时返回已反转义的字符串内容。</param>
        /// <returns>引号与转义格式完整时返回 true。</returns>
        private bool TryReadString(out string value)
        {
            value = string.Empty;
            if (_position >= _text.Length || _text[_position] != '"')
            {
                return false;
            }

            _position++;
            // JSON 字符串按字符逐段构造，避免转义字符被当作资源路径的一部分。
            StringBuilder builder = new StringBuilder();
            while (_position < _text.Length)
            {
                // 当前读取字符可能是字符串结束符、普通字符或转义前缀。
                char currentCharacter = _text[_position];
                _position++;
                if (currentCharacter == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (currentCharacter != '\\')
                {
                    builder.Append(currentCharacter);
                    continue;
                }

                if (_position >= _text.Length)
                {
                    return false;
                }

                // 反斜杠后的 JSON 转义类型。
                char escapeCharacter = _text[_position];
                _position++;
                if (escapeCharacter == '"' || escapeCharacter == '\\' ||
                    escapeCharacter == '/')
                {
                    builder.Append(escapeCharacter);
                    continue;
                }

                if (escapeCharacter == 'b')
                {
                    builder.Append('\b');
                    continue;
                }

                if (escapeCharacter == 'f')
                {
                    builder.Append('\f');
                    continue;
                }

                if (escapeCharacter == 'n')
                {
                    builder.Append('\n');
                    continue;
                }

                if (escapeCharacter == 'r')
                {
                    builder.Append('\r');
                    continue;
                }

                if (escapeCharacter == 't')
                {
                    builder.Append('\t');
                    continue;
                }

                if (escapeCharacter != 'u' || _position + 4 > _text.Length)
                {
                    return false;
                }

                // 四位十六进制 Unicode 转义值。
                string unicodeText = _text.Substring(_position, 4);
                int unicodeValue;
                if (!int.TryParse(unicodeText, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out unicodeValue))
                {
                    return false;
                }

                builder.Append((char)unicodeValue);
                _position += 4;
            }

            return false;
        }

        /// <summary>
        /// 跳过 JSON 允许的空白字符，使结构读取不受格式化缩进和换行影响。
        /// </summary>
        private void SkipWhitespace()
        {
            while (_position < _text.Length &&
                char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }
    }
}
