

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Google.Protobuf;

/// <summary>
/// 游戏世界的广播，同步
/// </summary>
public class GameWorldBCd:Singleton<GameWorldBCd>
{
    
    /// <summary>
    /// 角色进入世界，进行同步给其他玩家
    /// </summary>
    public void RoleEnterWroldBC(MainRoleInfo currentRole)
    {
        if (currentRole == null || currentRole.BaseInfo == null)
        {
            return;
        }

        //获取所有在线玩家
        Dictionary<int, OnlineRole> onlineRoles = GameGlobal.Instance.GetAllOlineRole();
        LogMsg.Info("OnEnterWroldHandle::" + onlineRoles.Count.ToString());
        foreach (OnlineRole role in onlineRoles.Values)
        {
            if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
            {
                continue;
            }

            //不许同步自己给自己
            if (role.mainRoleInfo.BaseInfo.RoleId == currentRole.BaseInfo.RoleId)
            {
                continue;
            }
            //判断是不是在同一个地图
            if (role.mainRoleInfo.BaseInfo.MapId != currentRole.BaseInfo.MapId)
            { 
                continue;
            }
            //判断一下距离AOC
            if (Vector3.Distance(role.mainRoleInfo.BaseInfo.Pos.ToVector3(), currentRole.BaseInfo.Pos.ToVector3()) >
                100)
            {
                continue;
            }

            Session gateSession = SessionMgr.Instance.GetSession(role.GateSessionId);
            if (gateSession == null)
            {
                continue;
            }
            gateSession.SendData(role.UnitySessionId,NetDefine.CMD_SyncotherOnlineCode,currentRole.BaseInfo.ToByteString());
        }
        
    }
    /// <summary>
    /// 角色进入世界，进行同步
    /// </summary>
    public void OtherOnlineWroldBC(ServerBase serverBase, BasePackage basePackage,MainRoleInfo currentRole)
    {
        if (serverBase == null || basePackage == null || currentRole == null || currentRole.BaseInfo == null)
        {
            return;
        }

        //获取所有在线玩家
        Dictionary<int, OnlineRole> onlineRoles = GameGlobal.Instance.GetAllOlineRole();
        LogMsg.Info("OnEnterWroldHandle::" + onlineRoles.Count.ToString());
        foreach (OnlineRole role in onlineRoles.Values)
        {
            if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
            {
                continue;
            }

            //不许同步自己给自己
            if (role.mainRoleInfo.BaseInfo.RoleId == currentRole.BaseInfo.RoleId)
            {
                continue;
            }
            //判断是不是在同一个地图
            if (role.mainRoleInfo.BaseInfo.MapId != currentRole.BaseInfo.MapId)
            { 
                continue;
            }
            //判断一下距离AOC
            if (Vector3.Distance(role.mainRoleInfo.BaseInfo.Pos.ToVector3(), currentRole.BaseInfo.Pos.ToVector3()) >
                100)
            {
                continue;
            }
            
            serverBase.SendData(basePackage,NetDefine.CMD_SyncotherOnlineCode,role.mainRoleInfo.BaseInfo.ToByteString());
        }
        
    }    
}
