using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YooAsset;
using static UnityEngine.Rendering.ReloadAttribute;

/// <summary>主包启动入口，按资源初始化、联网更新、代码装载和登录场景的顺序启动游戏。</summary>

public class Load : MonoBehaviour {

    #region 字段与属性
    // 启动资源模式；正式主包使用联网热更，编辑器可使用模拟资源。
    [SerializeField, Header("运行模式")] private EPlayMode _playMode = EPlayMode.EditorSimulateMode;

    // 联网更新时优先访问的资源服务器地址，由启动场景配置。
    [SerializeField, Header("资源系统地址")] private string defaultHostServer;

    // 主地址不可用时由 YooAsset 使用的备用资源服务器地址。
    [SerializeField, Header("备用地址")] private string fallbackHostServer;

    // 启动进度界面，资源初始化、下载和代码装载都通过它显示状态。
    [SerializeField, Header("热更新View")] private HotUpdateView _hotUpdateView;

    // 本次启动使用的 DefaultPackage；初始化成功后继续用于加载代码和登录场景。
    private ResourcePackage _package;

    // 按程序集文件名保存已下载的 DLL 字节，随后用于加载热更代码或补充 AOT 元数据。
    private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();

    // 需要补充元数据的 AOT 程序集文件名，包含 .dll 后缀；实际资源由 YooAsset 地址定位。
    public static List<string> AOTMetaAssemblyNames { get; } = new List<string>()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
        //"Assembly-CSharp.dll"
    };
    #endregion

    // 启动组件创建后先初始化资源系统，再由协程串起后续步骤。
    private void Awake() {

        //初始化YooAsset
        InitYooAsset();
    }

    /// <summary>加载热更与补充元数据文件，检查失败后停止进入登录。</summary>
    private IEnumerator InitCode() {
        _hotUpdateView.RefreshUI(1, "正在加载游戏代码..");
        // 热更程序集在前，随后加载既有补充元数据列表，文件名带dll后缀。
        List<string> assets = new List<string> { "GM_Game.dll" };
        assets.AddRange(AOTMetaAssemblyNames);
        // 顺序加载的文件索引，范围为本次程序集列表。
        for (int index = 0; index < assets.Count; index++) {
            // 当前程序集文件名，映射到已有YooAsset地址。
            string asset = assets[index];
            // 当前代码资源异步加载句柄，失败时不读取AssetObject。
            AssetOperationHandle dllHandle = _package.LoadAssetAsync<TextAsset>("Assets/GM_Game/Dlls/" + asset);
            yield return dllHandle;
            if (dllHandle.Status != EOperationStatus.Succeed) {
                ShowStartupError("游戏代码加载失败，请检查安装包和资源。", dllHandle.LastError);
                yield break;
            }
            // 实际程序集二进制资源，必须为有效的非空TextAsset。
            TextAsset textAsset = dllHandle.AssetObject as TextAsset;
            if (textAsset == null || textAsset.bytes.Length == 0) {
                ShowStartupError("游戏代码文件无效，请检查安装包和资源。", asset);
                yield break;
            }
            s_assetDatas[asset] = textAsset.bytes;
            Debug.Log($"dll:{asset} size:{textAsset.bytes.Length}");
        }

        if (!TryLoadCode()) {
            yield break;
        }
        yield return EnterGame();
    }

    /// <summary>启动失败时替换阶段提示，并将具体错误写入Player日志。</summary>
    /// <param name="message">面向玩家的中文失败提示及处理方向。</param>
    /// <param name="detail">资源系统或运行时返回的诊断内容，只用于日志。</param>
    private void ShowStartupError(string message, string detail) {
        _hotUpdateView.RefreshUI(0, message);
        Debug.LogError(message + " 详情：" + detail);
    }

    #region 事件回调
    // 记录刚开始下载的文件名和文件总字节数，日志中换算成 KB 显示。
    private void OnStartDownloadFileCallback(string fileName, long sizeBytes) {
        Debug.Log($"开始下载：{fileName}  大小：{sizeBytes / 1024f}KB");
    }

    /// <summary>依据下载器累计字节显示真实资源下载百分比。</summary>
    /// <param name="totalDownloadCount">本次需要下载的文件总数。</param>
    /// <param name="currentDownloadCount">本次已经完成的文件数。</param>
    /// <param name="totalDownloadBytes">本次需要下载的总字节数。</param>
    /// <param name="currentDownloadBytes">本次已经下载的累计字节数。</param>
    private void OnDownloadProgressCallback(int totalDownloadCount, int currentDownloadCount,
        long totalDownloadBytes, long currentDownloadBytes) {

        // 字节进度范围为0至1；总量为空时不伪造下载百分比。
        float prgs = 0;
        if (totalDownloadBytes > 0) {
            prgs = Mathf.Clamp01(currentDownloadBytes * 1.0f / totalDownloadBytes);
        }

        _hotUpdateView.RefreshUI(prgs,
            $"下载进度::{currentDownloadBytes / 1024 / 1024}M / {totalDownloadBytes / 1024 / 1024}M 【{prgs * 100}%】 ");

        Debug.Log($"文件总数:: {totalDownloadCount} 已下载文件数::{currentDownloadCount} 总大小::{totalDownloadBytes / 1024.0f / 1024} M " +
           $"  已下载大小::{currentDownloadBytes / 1024}KB");
    }

    // 记录下载失败的文件名及 YooAsset 返回的错误，最终界面由 Download 统一更新。
    private void OnDownloadErrorCallback(string fileName, string error) {
        Debug.Log($"下载失败::{fileName}  Error::{error}");
    }

    /// <summary>记录下载器最终结果，失败界面由Download统一设置。</summary>
    /// <param name="isSucceed">全部资源是否下载成功。</param>
    private void OnDownloadOverCallback(bool isSucceed) {
        if (isSucceed) {
            Debug.Log("下载成功..");
        }
        else {
            Debug.Log("下载失败..");
        }
    }
    #endregion

    /// <summary>安装补充元数据及热更程序集；主包与热更不匹配时显示失败原因。</summary>
    private bool TryLoadCode() {
        try {
            LoadMetadataForAOTAssemblies();
#if !UNITY_EDITOR
            // 程序集反射加载属于第三方运行时边界，保留既有二进制装载方式。
            Assembly.Load(s_assetDatas["GM_Game.dll"]);
#endif
            return true;
        }
        // 第三方运行时抛出的加载异常，仅记录诊断并终止启动，不回退旧资源。
        catch (Exception error) {
            ShowStartupError("游戏代码启动失败，请检查主程序和资源版本是否配套。", error.ToString());
            return false;
        }
    }

    // 把主包中已编译程序集的类型信息交给 HybridCLR，使热更代码能够使用所需的 AOT 泛型。
    private static void LoadMetadataForAOTAssemblies() {

        // AOT 是随安装包提前编译好的代码；这里只给这些程序集补类型信息，热更 DLL 不需要这一步。
        // SuperSet 允许补充程序集的元数据多于主包裁剪后保留下来的内容。
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        // 依次处理需要补元数据的程序集；aotDllName 是缓存中的 .dll 文件名。
        foreach (var aotDllName in AOTMetaAssemblyNames) {
            // InitCode 已加载的程序集字节，此处不再访问磁盘或网络。
            byte[] dllBytes = s_assetDatas[aotDllName];
            // 返回值用于记录是否补充成功；缺少预编译泛型实现时，HybridCLR 可借助这些元数据解释执行。
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }

    #region 核心逻辑
    /// <summary>
    /// 创建并设置默认资源包，然后启动资源检查和更新流程。
    /// </summary>
    private void InitYooAsset() {
        // 初始化资源系统
        YooAssets.Initialize();

        // 创建默认的资源包
        _package = YooAssets.CreatePackage("DefaultPackage");

        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(_package);

        StartCoroutine(InitPackage());
    }

    /// <summary>按实际操作推进资源初始化、版本查询及清单更新；失败后停止启动。</summary>
    private IEnumerator InitPackage() {

        _hotUpdateView.RefreshUI(0, "正在初始化资源..");
        // 当前运行模式返回的初始化操作；只用于判断初始化结果。
        InitializationOperation operation = null;

        switch (_playMode) {
            case EPlayMode.EditorSimulateMode:
                // 编辑器模拟资源所需参数，不访问远端版本。
                EditorSimulateModeParameters editorParameters = new EditorSimulateModeParameters();
                editorParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                operation = _package.InitializeAsync(editorParameters);
                break;

            case EPlayMode.HostPlayMode:
                // 联网运行参数，使用场景中配置的主备资源服务地址。
                HostPlayModeParameters hostParameters = new HostPlayModeParameters();
                hostParameters.BuildinQueryServices = new GameQueryServices(); // 查询安装包 StreamingAssets/yoo 中是否已有对应资源。
                hostParameters.DeliveryQueryServices = new GameDeliveryQueryServices();
                hostParameters.DecryptionServices = new GameDecryptionServices();
                hostParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                operation = _package.InitializeAsync(hostParameters);

                break;
        }

        if (operation == null) {
            ShowStartupError("资源模式配置错误，请检查安装包。", "不支持的资源模式：" + _playMode);
            yield break;
        }

        //等待初始化完成..
        yield return operation;

        Debug.Log("operation::" + operation.Status);
        if (operation.Status != EOperationStatus.Succeed) {
            ShowStartupError("资源初始化失败，请检查安装包。", operation.Error);
            yield break;
        }

        //初始化完成后，获取包版本信息
        _hotUpdateView.RefreshUI(0, "正在连接资源服务器..");
        // 版本请求的无新增数据超时为15秒；失败不能沿用初始化操作的状态。
        UpdatePackageVersionOperation versionOperation = _package.UpdatePackageVersionAsync(timeout: 15);
        yield return versionOperation;
        if (versionOperation.Status != EOperationStatus.Succeed) {
            ShowStartupError("无法获取资源版本，请检查资源服务器后重新启动。", versionOperation.Error);
            yield break;
        }

        // 编辑器模拟模式的版本为空是YooAsset合法行为，联网版本必须有效。
        if (_playMode == EPlayMode.HostPlayMode && string.IsNullOrWhiteSpace(versionOperation.PackageVersion)) {
            ShowStartupError("资源版本为空，请检查资源服务器后重新启动。", "联网资源版本文件没有有效内容。");
            yield break;
        }

        _hotUpdateView.RefreshUI(0, "正在检查资源更新..");
        // 清单请求同样使用15秒无新增数据超时；大文件下载仍保留原参数。
        UpdatePackageManifestOperation manifestOperation = _package.UpdatePackageManifestAsync(versionOperation.PackageVersion, timeout: 15);
        yield return manifestOperation;

        // 资源清单更新成功后，才能知道本机还缺哪些文件。
        if (manifestOperation.Status != EOperationStatus.Succeed) {
            ShowStartupError("资源清单更新失败，请检查资源服务器后重新启动。", manifestOperation.Error);
            yield break;
        }

        //开始下载
        yield return Download();
    }

    /// <summary>仅以下载器真实字节更新下载进度；无需更新时直接加载游戏代码。</summary>
    private IEnumerator Download() {
        _hotUpdateView.RefreshUI(0, "正在检查需要下载的资源..");
        // 同时下载文件上限，保留现有10个并发设置。
        int downloadingMaxNum = 10;
        // 单文件失败后沿用原有3次重试，不另建启动重试流程。
        int failedTryAgain = 3;
        // 已初始化的默认资源包。
        var package = YooAssets.GetPackage("DefaultPackage");
        // 依据当前清单和本机资源计算的实际下载器。
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0) {

            _hotUpdateView.RefreshUI(1, "没有资源更新..");
            yield return InitCode();
            yield break;
        }

        _hotUpdateView.RefreshUI(0, "正在下载资源..");

        //注册回调方法
        downloader.OnDownloadOverCallback = OnDownloadOverCallback; //当下载器结束（无论成功或失败）
        downloader.OnDownloadErrorCallback = OnDownloadErrorCallback; //当下载器发生错误
        downloader.OnDownloadProgressCallback = OnDownloadProgressCallback; //当下载进度发生变化
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileCallback; //当开始下载某个文件

        //开启下载
        downloader.BeginDownload();
        yield return downloader;

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeed) {
            //下载成功
            yield return InitCode();
        }
        else {
            //下载失败
            ShowStartupError("资源下载失败，请检查资源服务器后重新启动。", downloader.Error);
            yield break;
        }
    }

    /// <summary>加载登录场景，只有句柄成功才记录已进入登录。</summary>
    private IEnumerator EnterGame() {
        _hotUpdateView.RefreshUI(1, "正在进入登录..");
        // 默认资源包中的正式登录场景异步加载句柄。
        SceneOperationHandle handle = _package.LoadSceneAsync("Assets/GM_Game/Scenes/Scene_Login");
        yield return handle;
        if (handle.Status != EOperationStatus.Succeed) {
            ShowStartupError("登录界面加载失败，请检查安装包和资源。", handle.LastError);
            yield break;
        }
        Debug.Log("加载场景完成..");
    }
    #endregion

}

// YooAsset 要求的文件读取适配接口；文件方式跳过前 32 字节，流方式直接打开原文件。
internal class GameDecryptionServices : IDecryptionServices {
    // 返回文件读取起点，单位为字节；所有资源使用同一偏移，未读取 fileInfo 的内容。
    public ulong LoadFromFileOffset(DecryptFileInfo fileInfo) {
        return 32;
    }

    // 内存解密方式尚未实现，调用会抛出异常；fileInfo 是 YooAsset 提供的文件信息。
    public byte[] LoadFromMemory(DecryptFileInfo fileInfo) {
        throw new NotImplementedException();
    }

    // 按 fileInfo.FilePath 打开只读文件流，允许其他读取者同时打开，由资源加载方管理流的使用。
    public Stream LoadFromStream(DecryptFileInfo fileInfo) {
        return new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read); ;
    }

    // 流式读取使用的托管缓冲区大小，单位为字节。
    public uint GetManagedReadBufferSize() {
        return 1024;
    }
}

// 实现 YooAsset 的远端地址接口，把资源文件名拼到启动场景配置的主、备用地址后。
internal class RemoteServices : IRemoteServices {

    // 保存主资源地址 defaultHostServer 和备用地址 fallbackHostServer，供后续下载请求使用。
    public RemoteServices(string defaultHostServer, string fallbackHostServer) {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    // 优先下载地址的根路径，在创建此适配对象时确定。
    private readonly string _defaultHostServer;

    // 备用下载地址的根路径，在创建此适配对象时确定。
    private readonly string _fallbackHostServer;

    // 把要下载的 fileName 接到备用服务器地址后，返回完整 URL。
    public string GetRemoteFallbackURL(string fileName) {
        return $"{_fallbackHostServer}/{fileName}";
    }

    // 把要下载的 fileName 接到主服务器地址后，返回完整 URL。
    public string GetRemoteMainURL(string fileName) {
        return $"{_defaultHostServer}/{fileName}";
    }
}

// YooAsset 的外部交付资源查询接口；本项目当前不提供这类资源。
internal class GameDeliveryQueryServices : IDeliveryQueryServices {
    // 按资源包名 packageName 和文件名 fileName 查询交付文件；当前未实现，调用会抛出异常。
    public DeliveryFileInfo GetDeliveryFileInfo(string packageName, string fileName) {
        throw new NotImplementedException();
    }

    // 无论资源包名 packageName 和文件名 fileName 是什么，都返回没有外部交付文件。
    public bool QueryDeliveryFiles(string packageName, string fileName) {
        return false;
    }
}

// YooAsset 的安装包内置资源查询接口，按 StreamingAssets/yoo 下的目录结构查找。
internal class GameQueryServices : IBuildinQueryServices {

    // 检查资源包 packageName 中的 fileName 是否随安装包提供；这里只检查文件存在性。
    public bool QueryStreamingAssets(string packageName, string fileName) {
        // 安装包内资源的完整路径，目录层级与 YooAsset 的打包输出一致。
        string filePath = Path.Combine(Application.streamingAssetsPath, "yoo", packageName, fileName);
        return File.Exists(filePath);
    }
}