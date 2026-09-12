using Google.Protobuf;
using UnityEngine;

/// <summary>主城角色表现管理器，负责把角色模型转换为 Unity 表现对象。</summary>
public class MainCityMgr : MonoBehaviour
{
   /// <summary>主城管理器单例。</summary>
   public static MainCityMgr Instance;

   /// <summary>注册主城网络消息。</summary>
   private void Awake()
   {
      Instance = this;
      SocketDispatcher.Instance.AddEventHandler(
         NetDefine.CMD_SyncotherOnlineCode, SyncotherOnlineHandle);
   }

   /// <summary>创建本地角色表现并通知服务端进入场景。</summary>
   private void Start()
   {
      ClientRole localRole = null;
      if (Global.Instance != null && Global.Instance.RoleWorld != null)
      {
         localRole = Global.Instance.RoleWorld.LocalRole;
      }
      if (localRole == null || localRole.BaseInfo == null)
      {
         return;
      }

      string prefabPath = "Role/Role_JX";
      if (localRole.BaseInfo.JobId == (int)RoleJobtype.MJS)
      {
         prefabPath = "Role/Role_MJS";
      }
      CreateRole(RoleType.MainRole, localRole.BaseInfo, prefabPath);

      EnterWroldReq request = new EnterWroldReq
      {
         RoleId = localRole.BaseInfo.RoleId
      };
      NetSocketMgr.Client.SendData(NetDefine.CMD_EnterWroldCode, request.ToByteString());
   }

   /// <summary>处理其他玩家同步消息并更新角色世界模型。</summary>
   private void SyncotherOnlineHandle(ByteString data)
   {
      RoleBaseInfo roleBaseInfo = RoleBaseInfo.Parser.ParseFrom(data);
      if (roleBaseInfo == null || Global.Instance == null || Global.Instance.RoleWorld == null)
      {
         return;
      }
      ClientRole existingRole;
      bool alreadyExists = Global.Instance.RoleWorld.TryGetRole(
         roleBaseInfo.RoleId, out existingRole);
      Global.Instance.RoleWorld.AddOrUpdateOtherRole(roleBaseInfo);
      if (!alreadyExists)
      {
         CreateRole(RoleType.OtherRole, roleBaseInfo, "Role/Role_MJS_Other");
      }
   }

   /// <summary>异步加载角色预制体并初始化表现控制器。</summary>
   private void CreateRole(RoleType roleType, RoleBaseInfo baseInfo, string prefabPath)
   {
      ResourceMgr.Instance.LoadPrefabAsync(prefabPath, obj =>
      {
         if (obj == null)
         {
            return;
         }
         obj.transform.position = new Vector3(62.1493416f, 19.4139996f, 80.6689758f);
         RoleCtrlBase roleCtrlBase = obj.GetComponent<RoleCtrlBase>();
         if (roleCtrlBase != null)
         {
            roleCtrlBase.InitCtrl(roleType, baseInfo);
         }
      });
   }

   /// <summary>注销主城网络消息并释放单例引用。</summary>
   private void OnDestroy()
   {
      SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_SyncotherOnlineCode);
      if (ReferenceEquals(Instance, this))
      {
         Instance = null;
      }
   }
}
