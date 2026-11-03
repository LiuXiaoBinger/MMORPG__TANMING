using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 保存角色控制器共用的 PlayerStats；当前 init 在没有配置时直接创建默认数值对象。
/// </summary>
public class PlayStateMgr : Singleton< PlayStateMgr>
{
    /// <summary>玩家数值配置实例，未初始化时为 null，init 时若为空则新建默认配置。</summary>
    public PlayerStats stats = null;

    /// <summary>初始化玩法状态，确保 stats 存在；由 Global.Awake 启动时调用。</summary>
    public void init()
    {
        if(stats==null)
            stats = new PlayerStats();
    }
}
