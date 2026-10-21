using System;
using UniRx;
using UnityEngine;
using YooAsset;

/// <summary>沿项目原流程加载单个场景，显示进度并在成功后执行业务回调。</summary>
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
        // 本次场景加载句柄；Single模式自动卸载旧场景并激活目标场景。
        SceneOperationHandle handle = Global.Instance.YooPackage.LoadSceneAsync(
            $"{ConstDefine.ScenePath}{sceneType}",
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

            _currentSceneType = sceneType;
            // 沿RO场景加载入口先准备同名公共导航数据，再执行创建玩家等原业务回调。
            MNavigationMgr.singleton.OnSceneLoaded(handle.SceneObject.name);
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
}
