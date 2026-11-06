using System;
using UniRx;
using UnityEngine;
using YooAsset;

/// <summary>加载目标场景时显示进度；成功后更新当前场景、读取导航数据、启用业务根节点，再调用回调。</summary>
public class SceneMgr : Singleton<SceneMgr>
{
    /// <summary>已成功加载的业务场景类型，失败时保留原值。</summary>
    public SceneType _currentSceneType;

    /// <summary>UIRoot持有的跨场景加载界面，允许为空。</summary>
    private LoadingView _loadingView;

    /// <summary>当前场景加载的逐帧进度订阅，加载完成后释放。</summary>
    private IDisposable _obs;

    /// <summary>设置场景切换共用的加载界面。</summary>
    /// <param name="loadingView">UIRoot创建的加载界面，可为空。</param>
    public void Init(LoadingView loadingView)
    {
        _loadingView = loadingView;
    }

    /// <summary>Single加载目标场景，由Unity卸载旧场景，成功后启用业务并回调。</summary>
    /// <param name="sceneType">目标场景类型，枚举名称对应项目资源Location。</param>
    /// <param name="callback">目标场景加载成功后执行一次的业务回调。</param>
    public void LoadSceneMode(SceneType sceneType, Action callback = null)
    {
        LoadSceneLocation(sceneType, ConstDefine.ScenePath + sceneType, callback);
    }

    /// <summary>按权威 SceneID 加载已登记的副本资源，缺资源时保留当前场景并报告实际原因。</summary>
    /// <param name="sceneId">服务器已确认进入的场景编号。</param>
    public void LoadDungeonScene(int sceneId)
    {
        // 资源登记只定位已迁入资产，不承担副本规则或目标配置。
        DungeonSceneResource resource = DungeonSceneResource.Load(sceneId);
        if (resource == null || string.IsNullOrEmpty(resource.sceneLocation))
        {
            Debug.LogError("副本场景 " + sceneId + " 加载失败：客户端场景资源尚未迁入。");
            TipsMgr.Instance.ShowSystemTips("副本场景资源尚未迁入（" + sceneId + "）");
            return;
        }
        LoadSceneLocation(SceneType.Scene_Dungeon, resource.sceneLocation, null);
    }

    /// <summary>复用现有加载进度和成功回调处理，副本地址由 SceneID 对应资源登记提供。</summary>
    /// <param name="sceneType">加载成功后保存的业务场景类别。</param>
    /// <param name="sceneLocation">YooAsset 可直接定位的场景资源地址。</param>
    /// <param name="callback">场景成功加载后的业务回调，允许为空。</param>
    private void LoadSceneLocation(SceneType sceneType, string sceneLocation, Action callback)
    {
        PrepareLocalRoleForSceneChange(sceneType);
        // 本次场景加载句柄；Single模式自动卸载旧场景并激活目标场景。
        SceneOperationHandle handle = Global.Instance.YooPackage.LoadSceneAsync(
            sceneLocation,
            UnityEngine.SceneManagement.LoadSceneMode.Single);
        if (_loadingView != null)
        {
            _loadingView.transform.SetAsLastSibling();
            _loadingView.Show();
        }

        // updateFrame为UniRx累计帧序号；逐帧读取本次加载进度及完成结果。
        _obs = Observable.EveryUpdate().Subscribe(updateFrame =>
        {
            if (_loadingView != null)
            {
                _loadingView.RefreshUI(handle.Progress,
                    $"加载场景中::{handle.Progress * 100f:0}");
            }
            if (!handle.IsDone)
            {
                return;
            }
            _obs.Dispose();
            if (_loadingView != null)
            {
                _loadingView.Show(false);
            }
            if (handle.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"场景 {sceneType} 加载失败：{handle.LastError}");
                return;
            }

            // 沿用RO桌面档位(VeryHigh)的运行时Shader上限500，排除仅用于编辑模式的LOD600；
            // 本项目未迁移RO画质档位入口，这里每次成功加载场景都设置为 500，不恢复之前的值。
            Shader.globalMaximumLOD = 500;
            _currentSceneType = sceneType;
            // 先读取与场景同名的导航文件，之后业务回调才可以查询路线。
            MNavigationMgr.singleton.OnSceneLoaded(handle.SceneObject.name);
            if (sceneType == SceneType.Scene_Dungeon)
            {
                // 出生根属于当前副本场景，Single 切走时自动注销该副本的实体。
                GameObject dungeonRoot = new GameObject("[RO 副本实体]");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dungeonRoot, handle.SceneObject);
                dungeonRoot.AddComponent<MSceneDungeon>();
            }
            // 目标场景根对象；创角业务根默认禁用，加载成功后才启用其场景脚本。
            GameObject[] roots = handle.SceneObject.GetRootGameObjects();
            // 根数组的零基索引；已启用的主城业务根保持现状。
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == "SceneBusinessRoot" && !roots[index].activeSelf)
                {
                    roots[index].SetActive(true);
                }
            }
            if (callback != null)
            {
                callback.Invoke();
            }
        });
    }

    /// <summary>玩法场景切换前停止旧路线和方向输入；原主角自首次创建起始终常驻。</summary>
    /// <param name="sceneType">主城和副本携带主角；登录与创角沿用原有角色清理流程。</param>
    private static void PrepareLocalRoleForSceneChange(SceneType sceneType)
    {
        if (sceneType != SceneType.Scene_MainCity && sceneType != SceneType.Scene_Dungeon)
        {
            return;
        }
        if (Global.Instance.SceneWorld == null)
        {
            return;
        }
        // 当前角色及其已加载模型，首次进城尚无模型时仍走原创建流程。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null || !localRole.Model.IsLoaded || !localRole.Model.GameObject.IsValid)
        {
            return;
        }
        // 旧场景的路线与方向输入不应在目标场景继续推进。
        localRole.NavComp.Interrupt();
        localRole.MoveComp.StopHorizontalMovement();
    }

}
