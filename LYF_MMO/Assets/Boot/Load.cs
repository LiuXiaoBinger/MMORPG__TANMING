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

    // 启动资源模式；正式主包使用联网热更，编辑器可使用模拟资源。
    [SerializeField, Header("运行模式")] private EPlayMode _playMode = EPlayMode.EditorSimulateMode;
    [SerializeField, Header("资源系统地址")] private string defaultHostServer;
    [SerializeField, Header("备用地址")] private string fallbackHostServer;

    [SerializeField, Header("热更新View")] private HotUpdateView _hotUpdateView;

    private ResourcePackage _package;


    //获取资源二进制
    private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();


    //补充元数据dll的列表，Yooasset中不需要带后缀
    public static List<string> AOTMetaAssemblyNames { get; } = new List<string>()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
        //"Assembly-CSharp.dll"
    };

    private void Awake() {

        //初始化YooAsset
        InitYooAsset();
    }

    /// <summary>
    /// 初始化YooAsset
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
                hostParameters.BuildinQueryServices = new GameQueryServices(); //太空战机DEMO的脚本类，详细见StreamingAssetsHelper
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



        //检测下载结果
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

    /// <summary>启动失败时替换阶段提示，并将具体错误写入Player日志。</summary>
    /// <param name="message">面向玩家的中文失败提示及处理方向。</param>
    /// <param name="detail">资源系统或运行时返回的诊断内容，只用于日志。</param>
    private void ShowStartupError(string message, string detail) {
        _hotUpdateView.RefreshUI(0, message);
        Debug.LogError(message + " 详情：" + detail);
    }

    private static void LoadMetadataForAOTAssemblies() {

        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyNames) {
            byte[] dllBytes = s_assetDatas[aotDllName];
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }

    //当开始下载某个文件
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

    //当下载器发生错误
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

}




internal class GameDecryptionServices : IDecryptionServices {
    public ulong LoadFromFileOffset(DecryptFileInfo fileInfo) {
        return 32;
    }

    public byte[] LoadFromMemory(DecryptFileInfo fileInfo) {
        throw new NotImplementedException();
    }

    public Stream LoadFromStream(DecryptFileInfo fileInfo) {
        return new FileStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read); ;
    }

    public uint GetManagedReadBufferSize() {
        return 1024;
    }
}

internal class RemoteServices : IRemoteServices {

    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer) {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }


    public string GetRemoteFallbackURL(string fileName) {
        return $"{_fallbackHostServer}/{fileName}";
    }

    public string GetRemoteMainURL(string fileName) {
        return $"{_defaultHostServer}/{fileName}";
    }
}

internal class GameDeliveryQueryServices : IDeliveryQueryServices {
    public DeliveryFileInfo GetDeliveryFileInfo(string packageName, string fileName) {
        throw new NotImplementedException();
    }

    public bool QueryDeliveryFiles(string packageName, string fileName) {
        return false;
    }
}



internal class GameQueryServices : IBuildinQueryServices {

    public bool QueryStreamingAssets(string packageName, string fileName) {
        string filePath = Path.Combine(Application.streamingAssetsPath, "yoo", packageName, fileName);
        return File.Exists(filePath);
    }
}