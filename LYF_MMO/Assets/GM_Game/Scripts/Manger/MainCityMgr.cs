using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class MainCityMgr : MonoBehaviour
{
   public static MainCityMgr Instance;

   private MainRoleInfo _mainRoleInfo;
   void Awake()
   {
      Instance = this;
      _mainRoleInfo  = Global.Instance.mainRoleInfo;
      RegisCommand();
   }
   private void RegisCommand()
   {
      SocketDispatcher.Instance.AddEventHandler
         (NetDefine.CMD_SyncotherOnlineCode,SyncotherOnlineHandle);
   }

   private void SyncotherOnlineHandle(ByteString data)
   {
      RoleBaseInfo roleBaseInfo = RoleBaseInfo.Parser.ParseFrom(data);
      if (roleBaseInfo != null)
      {
         Debug.Log("同步其他玩家数据::"+roleBaseInfo.ToString());
         //生成其他玩家
         CreateRole(RoleType.OtherRole,roleBaseInfo,"Role/Role_MJS_Other");
      }
   }


   private void Start()
   {
      //创建主角
      if (_mainRoleInfo != null)
      {
         if (_mainRoleInfo.BaseInfo.JobId == (int)RoleJobtype.MJS)
         {
            CreateRole(RoleType.MainRole,_mainRoleInfo.BaseInfo,"Role/Role_MJS");
         }
         
      }
      else
      {
         CreateRole(RoleType.MainRole,_mainRoleInfo.BaseInfo,"Role/Role_JX");
      }
      
      //向服务器发送主角已经进入场景 服务器端将主角同步给其他玩家 其他玩家也同步给主角
      EnterWroldReq req = new EnterWroldReq()
      {
         RoleId = _mainRoleInfo.BaseInfo.RoleId,
      };
      NetSocketMgr.Client.SendData(NetDefine.CMD_EnterWroldCode,req.ToByteString());
   }
   /// <summary>
   /// 创建角色
   /// </summary>
   /// <param name="mainRole">角色类型</param>
   /// <param name="baseInfo">角色基础信息</param>
   /// <param name="roleRoleJx">资源路径</param>
   /// <exception cref="NotImplementedException"></exception>
   private  void CreateRole(RoleType roleType, RoleBaseInfo baseInfo, string prefabPath)
   {
      ResourceMgr.Instance.LoadPrefabAsync(prefabPath, (GameObject obj) =>
      {
         if (obj != null)
         {
            //todo
            obj.transform.position = new Vector3(62.1493416f, 19.4139996f, 80.6689758f);
            
            RoleCtrlBase roleCtrlBase = obj.GetComponent<RoleCtrlBase>();
            if (roleCtrlBase != null)
            {
               roleCtrlBase.InitCtrl(roleType,baseInfo);
            }
         }
      });
   }
}
