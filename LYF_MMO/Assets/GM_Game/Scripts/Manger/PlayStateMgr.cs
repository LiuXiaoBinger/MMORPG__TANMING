using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class PlayStateMgr : Singleton< PlayStateMgr>
{
    public PlayerStats stats = null;

    public void init()
    {
        if(stats==null)
            stats = new PlayerStats();
    }
}
