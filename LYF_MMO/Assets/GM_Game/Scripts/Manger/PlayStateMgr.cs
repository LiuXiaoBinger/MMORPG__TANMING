using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>
/// 玩家玩法运行态管理器，集中持有玩家数值配置（PlayerStats）等运行期共享数据。
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
