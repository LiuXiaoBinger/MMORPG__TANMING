using System;
using Google.Protobuf;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>RO同名导航到达和施法发送模块；当前无目标技能直接到达，实际寻路尚未实现。</summary>
public class MNavigationComponent : EntityComponentBase
{
    /// <summary>使用项目已有导航组件编号20，作为本地主角普通组件统一挂载和回收。</summary>
    public override EntityComponentType ComponentType { get { return EntityComponentType.Navigation; } }
    /// <summary>导航指令处理器，随实体组件挂载订阅、卸载时由基类解除。</summary>
    private readonly MEventHandler _onGetEvent;

    /// <summary>创建实体导航模块并绑定当前生命周期内的事件入口。</summary>
    /// <param name="owner">拥有导航和已学技能的本地主角实体。</param>
    public MNavigationComponent(ROObject owner) : base(owner) 
    { _onGetEvent = new MEventHandler(OnGetEvent); }

    /// <summary>沿RO导航事件入口接收技能到达指令。</summary>
    protected override void EventSubscribe()
    {
        RegisterEvent(MEventType.MEvent_Navigation, _onGetEvent);
    }

    /// <summary>RO中断导航接口；本项目未实现导航移动，当前无需停止路径或切待机。</summary>
    /// <param name="toIdle">未来导航中断时是否切待机；现阶段不执行导航状态切换。</param>
    public void Interrupt(bool toIdle = true)
    {
        // 寻路尚未实现，保留RO入口，不修改普通移动与技能状态。
    }

    /// <summary>当前全部正式技能为无目标释放，导航不等待空的寻路功能，立即进入到达动作。</summary>
    /// <param name="e">具名导航指令，同步派发结束后由实体回收。</param>
    protected void OnGetEvent(IEventArg e)
    {
        // 本次技能导航指令，当前接口不保存到组件字段或延后消费。
        MNavigationEventArgs args = e as MNavigationEventArgs;
        closeTargetAfter(args.NavAction);
    }

    /// <summary>RO同名到达分发入口；位置同步及寻路到达未接入，当前直接进入施法发送。</summary>
    /// <param name="skillArgs">到达后执行的本次技能指令。</param>
    private void closeTargetAfter(MSkillEventArgs skillArgs)
    {
        // 沿 RO 到达后先上报玩家已有位置同步，再进入原技能发送流程。
        PlayerEntity player = Entity as PlayerEntity;
        player.UpdateMove();
        OnAfterAction(skillArgs);
    }

    /// <summary>沿RO到达回调发送技能请求，成功发送后立即开始原本地预表现，不增加确认等待。</summary>
    /// <param name="skillArgs">本次输入的技能主表ID、释放点和可空目标。</param>
    protected void OnAfterAction(MSkillEventArgs skillArgs)
    {
        // 导航模块所属本地主角；当前无目标协议不上传尚未接入的目标或施法点。
        PlayerEntity localRole = Entity as PlayerEntity;
        // 原施法输入时钟，单位为不缩放秒；保留统一跳跃、特殊滑步、MP与CD许可。
        float realtimeSeconds = Time.realtimeSinceStartup;
        if (!localRole.Skill.
                CanStart(skillArgs.Identify, realtimeSeconds,
                    localRole.BaseInfo.CurrMp))
        {
            return;
        }
        // RO实体施法UUID在1至9999循环，请求与本地预表现共用这一笔身份。
        ulong skillUuid = localRole.NextSkillUUID;
        if (NetSocketMgr.Client != null)
        {
            try
            {
                // 沿原无目标协议，只发送UUID与主表ID，不扩导航或目标字段。
                SkillCastReq request = new SkillCastReq { SkillUuid = skillUuid, SkillId = skillArgs.Identify };
                NetSocketMgr.Client.SendData(NetDefine.CMD_SkillCastCode, request.ToByteString());
            }
            catch (Exception exception)
            {
                // 原发送失败语义保持：请求未提交时不启动本次预表现。
                Debug.LogError("技能施法请求发送失败：" + exception);
                return;
            }
        }
        // 只有已通过原技能准入的攻击辅助转向；目标只是选择辅助，不改变无目标技能的施法点。
        if (skillArgs.Target != null)
        {
            // 当前主表类别，Type 三的滑步不自动转向目标。
            cfg.SkillInfo skillInfo = LubanMgr.Instance.GetSkillInfoById(skillArgs.Identify);
            if (skillInfo.Type == 1 || skillInfo.Type == 2)
            {
                // 只使用水平朝向，单位米；目标与自己重合时保持原旋转。
                Vector3 direction = skillArgs.Target.Position - localRole.Position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0f)
                {
                    localRole.Model.Rotation = Quaternion.LookRotation(direction);
                }
            }
        }
        localRole.Skill.BeginPrediction(skillArgs.Identify, skillUuid,
            Time.realtimeSinceStartup, localRole.BaseInfo.CurrMp);
    }
}
