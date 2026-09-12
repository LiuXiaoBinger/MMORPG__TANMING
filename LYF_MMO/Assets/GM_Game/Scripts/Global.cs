using System;
using UnityEngine;
using YooAsset;

/**
 * Title:
 * Description:
 */



public class Global : MonoBehaviour {

    /// <summary>全局 Unity 生命周期入口。</summary>
    public static Global Instance;

    /// <summary>YooAsset 默认资源包。</summary>
    private ResourcePackage _package;
    /// <summary>获取 YooAsset 默认资源包。</summary>
    public ResourcePackage YooPackage { get => _package; }

    /// <summary>当前账号登录信息。</summary>
    [HideInInspector] public LoginRet LoginInfo { get; set; }

    /// <summary>客户端唯一角色运行时容器。</summary>
    public RoleWorldSystem RoleWorld { get; private set; }

    /// <summary>当前主角表现控制器，与纯数据角色模型保持分离。</summary>
    [HideInInspector] public RoleCtrlBase roleCtrlBase;

    /// <summary>初始化全局客户端模块。</summary>
    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(this);
        RoleWorld = new RoleWorldSystem();
        _package = YooAssets.GetPackage("DefaultPackage");
        NetSocketMgr.Instance.Init();
        PlayStateMgr.Instance.init();
        LubanMgr.Instance.Init();
    }

    /// <summary>推进客户端角色模型组件。</summary>
    private void Update()
    {
        if (RoleWorld == null)
        {
            return;
        }
        int deltaMilliseconds = Mathf.RoundToInt(Time.deltaTime * 1000f);
        RoleWorld.Update(deltaMilliseconds);
    }

    /// <summary>退出客户端时断开连接并清理角色运行时数据。</summary>
    private void OnApplicationQuit()
    {
        NetSocketMgr.Instance.Disconnect();
        if (RoleWorld != null)
        {
            RoleWorld.Clear();
        }
    }
}
