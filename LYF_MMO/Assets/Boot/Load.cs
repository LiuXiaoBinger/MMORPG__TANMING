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

/**
 * Title:
 * Description:
 */


public class Load : MonoBehaviour {

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


    private IEnumerator InitPackage() {


        InitializationOperation operation = null;

        switch (_playMode) {
            case EPlayMode.EditorSimulateMode:
                //编辑器模式
                EditorSimulateModeParameters editorParameters = new EditorSimulateModeParameters();
                editorParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                operation = _package.InitializeAsync(editorParameters);
                break;

            case EPlayMode.HostPlayMode:
                //联机运行模式
                HostPlayModeParameters hostParameters = new HostPlayModeParameters();
                hostParameters.BuildinQueryServices = new GameQueryServices(); //太空战机DEMO的脚本类，详细见StreamingAssetsHelper
                hostParameters.DeliveryQueryServices = new GameDeliveryQueryServices();
                hostParameters.DecryptionServices = new GameDecryptionServices();
                hostParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                operation = _package.InitializeAsync(hostParameters);

                break;
        }

        //等待初始化完成..
        yield return operation;

        Debug.Log("operation::" + operation.Status);
        if (operation.Status != EOperationStatus.Succeed) {
            Debug.Log($"{operation.Error}");
            yield break;
        }


        //初始化完成后，获取包版本信息
        var versionOperation = _package.UpdatePackageVersionAsync();
        yield return versionOperation;
        if (operation.Status != EOperationStatus.Succeed) {
            Debug.LogError("RequestVersion Error::" + operation.Error);
            yield break;
        }

        //更新资源清单
        var manifestOperation = _package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
        yield return manifestOperation;



        //检测下载结果
        if (manifestOperation.Status != EOperationStatus.Succeed) {
            Debug.LogError("UpdateManifest Error::" + manifestOperation.Error);
            yield break;
        }

        //开始下载
        yield return Download();
    }

    private IEnumerator Download() {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var package = YooAssets.GetPackage("DefaultPackage");
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);


        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0) {

            _hotUpdateView.RefreshUI(1, "没有资源更新..");
            yield return InitCode();
            yield break;
        }


        //需要下载的文件总数和总大小 
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;

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
            Debug.Log("下载失败...");
            yield break;
        }
    }

    /// <summary>
    /// 初始化补充元数据dll
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitCode() {

        var assets = new List<string> {
               "GM_Game.dll"
        }.Concat(AOTMetaAssemblyNames);

        foreach (var asset in assets) {
            var dllHandle = _package.LoadAssetAsync<TextAsset>("Assets/GM_Game/Dlls/" + asset);
            yield return dllHandle;
            TextAsset textAsset = dllHandle.AssetObject as TextAsset;
            s_assetDatas[asset] = textAsset.bytes;
            Debug.Log($"dll:{asset} size:{textAsset.bytes.Length}");
        }

        LoadMetadataForAOTAssemblies();
#if !UNITY_EDITOR
        // Editor环境下，GM_Game.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
        Assembly.Load(s_assetDatas["GM_Game.dll"] );
#endif

        yield return EnterGame();
    }

    private IEnumerator EnterGame() {

        SceneOperationHandle handle = _package.LoadSceneAsync("Assets/GM_Game/Scenes/Scene_Login");
        yield return handle;
        Debug.Log("加载场景完成..");

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

    //当下载进度发生变化
    private void OnDownloadProgressCallback(int totalDownloadCount, int currentDownloadCount,
        long totalDownloadBytes, long currentDownloadBytes) {

        float prgs = currentDownloadBytes * 1.0f / totalDownloadBytes;

        _hotUpdateView.RefreshUI(prgs,
            $"下载进度::{currentDownloadBytes / 1024 / 1024}M / {totalDownloadBytes / 1024 / 1024}M 【{prgs * 100}%】 ");

        Debug.Log($"文件总数:: {totalDownloadCount} 已下载文件数::{currentDownloadCount} 总大小::{totalDownloadBytes / 1024.0f / 1024} M " +
           $"  已下载大小::{currentDownloadBytes / 1024}KB");
    }

    //当下载器发生错误
    private void OnDownloadErrorCallback(string fileName, string error) {
        Debug.Log($"下载失败::{fileName}  Error::{error}");
    }

    //当下载器结束（无论成功或失败）
    private void OnDownloadOverCallback(bool isSucceed) {
        Debug.Log("下载" + (isSucceed ? " 成功 " : "失败") + " ....");
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