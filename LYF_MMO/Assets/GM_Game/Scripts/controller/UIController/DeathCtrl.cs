using Google.Protobuf;
using MMoRpgCommon;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>本地主角死亡窗口与权威复活协议的业务入口，不承担道具或付费复活。</summary>
public class DeathCtrl : CtrlBase
{
    /// <summary>由主视图持有并初始化的正式死亡窗口。</summary>
    private readonly DeathWindow window;
    /// <summary>跨场景常驻的实体世界，负责本地主角变更通知。</summary>
    private readonly SceneWorldSystem world;
    /// <summary>可解除的生命变化监听，跟随当前本地主角绑定。</summary>
    private readonly MEventHandler healthChangedHandler;
    /// <summary>当前订阅生命变化的本地主角；切角色时先解绑旧实体。</summary>
    private PlayerEntity localRole;

    /// <summary>接入主视图、角色生命事件及服务端复活结果。</summary>
    /// <param name="view">已经完成窗口注册与控件初始化的主视图。</param>
    public DeathCtrl(MainView view) : base(view)
    {
        window = view.DeathWindow;
        world = Global.Instance.SceneWorld;
        healthChangedHandler = new MEventHandler(OnHealthChanged);
        window.ReviveRequested += RequestRevive;
        world.LocalRoleChanged += BindRole;
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_RoleReviveResultCode, OnReviveResult);
        BindRole(world.LocalRole);
    }

    /// <summary>主视图销毁时解除实体、窗口和协议监听。</summary>
    public override void Dispose()
    {
        window.ReviveRequested -= RequestRevive;
        world.LocalRoleChanged -= BindRole;
        SocketDispatcher.Instance.RemoveEventHandler(NetDefine.CMD_RoleReviveResultCode, OnReviveResult);
        if (localRole != null) localRole.RemoveEventListener(MEventType.HpChanged, healthChangedHandler);
    }

    /// <summary>更换本地主角时改绑生命变化，并恢复当前死亡界面状态。</summary>
    /// <param name="role">新本地主角，离开世界时为空。</param>
    private void BindRole(PlayerEntity role)
    {
        if (localRole != null) localRole.RemoveEventListener(MEventType.HpChanged, healthChangedHandler);
        localRole = role;
        if (localRole != null) localRole.AddEventListener(MEventType.HpChanged, healthChangedHandler);
        RefreshWindow();
    }

    /// <summary>生命变化只读取实体当前状态，不在展示事件中移动或恢复角色。</summary>
    /// <param name="eventArg">现有生命事件无额外载荷。</param>
    private void OnHealthChanged(IEventArg eventArg)
    {
        RefreshWindow();
    }

    /// <summary>副本角色死亡时显示窗口，存活或离开角色世界时关闭。</summary>
    private void RefreshWindow()
    {
        if (localRole == null || localRole.BaseInfo == null)
        {
            window.CloseWindow();
            return;
        }
        if (localRole.BaseInfo.CurrHp <= 0 && localRole.BaseInfo.InstanceId != 0)
        {
            if (!window.gameObject.activeSelf) ShowMainWindow(WindowType.DeathWindow);
            return;
        }
        window.CloseWindow();
    }

    /// <summary>按钮只发送无目标、无位置的本人复活请求，活人拒绝由服务器执行。</summary>
    private void RequestRevive()
    {
        MNetClient.Instance.Send(NetDefine.CMD_RoleReviveCode, new RoleReviveReq().ToByteString());
    }

    /// <summary>确认服务端复活后同步位置、资源及死亡动作；同场景观察者使用相同结果。</summary>
    /// <param name="data">服务端复活结果的原始Protobuf消息。</param>
    private void OnReviveResult(ByteString data)
    {
        // 直接解析协议，不包装或吞掉格式异常。
        RoleReviveRet notice = RoleReviveRet.Parser.ParseFrom(data);
        if (!notice.Success)
        {
            TipsMgr.Instance.ShowSystemTips(notice.Reason);
            return;
        }
        // 只应用当前所在实例的权威结果，避免切场景后移动另一实例角色。
        RoleBaseInfo info = notice.BaseInfo;
        if (localRole == null || info.InstanceId != localRole.BaseInfo.InstanceId) return;
        // 本人或同实例远端角色，由持久角色编号定位原实体。
        RoleEntity role = localRole;
        if (localRole.GetID() != info.RoleId)
        {
            if (!world.TryGetRole(info.RoleId, out role)) return;
        }
        role.UpdateBaseInfo(info);
        role.NavComp.Interrupt(false);
        role.MoveComp.StopHorizontalMovement();
        role.MoveComp.EndJump();
        // 服务器坐标沿用三分量米制文本，组件与模型同时定位，不重发进场。
        System.Numerics.Vector3 position = info.Pos.ToVector3();
        role.Position = new Vector3(position.X, position.Y, position.Z);
        // 保留当前朝向，更新实体位置快照供后续移动同步使用。
        EntityTransformComponent transformComponent;
        role.Components.TryGet(EntityComponentType.Scene, out transformComponent);
        transformComponent.Apply(new EntityTransformSnapshot
        {
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Yaw = role.Model.GameObject.NativeObject.transform.eulerAngles.y
        });
        role.AttrComp.Apply(info.MaxHp, info.CurrHp, info.MaxMp, info.CurrMp);
        // 旧控制器仍用于角色存活检查，同步它持有的基础属性引用。
        RoleCtrlBase controller = role.GetEntityCtrl();
        if (controller != null) controller.InitCtrl(controller._roleType, role.BaseInfo);
        role.DispatchEvent(MEventType.Revived, null);
        RefreshWindow();
    }
}
