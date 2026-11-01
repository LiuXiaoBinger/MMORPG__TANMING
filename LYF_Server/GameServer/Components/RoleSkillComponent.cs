using System.Collections.Generic;
using Google.Protobuf;

/// <summary>对应 RO 角色技能扩展；中心服负责学习等级，释放、动作和冷却统一继承 RO 技能组件。</summary>
public sealed class RoleSkillComponent : ROSkillComponent, IROEventListener
{
    /// <summary>中心服装载的稳定技能 ID 到学习等级；零级表示尚未学习。</summary>
    private Dictionary<int, int> skill_map_ = new Dictionary<int, int>();
    /// <summary>当前在线生命周期是否已成功接收完整中心服学习快照。</summary>
    private bool skills_loaded_;

    /// <summary>为现有在线角色容器创建唯一技能释放组件，不另建场景宿主。</summary>
    /// <param name="owner">持有权威角色快照及网关会话的在线角色。</param>
    public RoleSkillComponent(OnlineRole owner) : base(owner) { }

    /// <summary>组件初始化时向角色派发器登记吟唱事件监听。</summary>
    public override void Initialize()
    {
        base.Initialize();
        Owner.GetEventDispatcher().AddEventListener(
            ROEventDefine.kActionSinging, this, OnSingingRequest);
    }

    /// <summary>
    /// 处理吟唱事件：用事件字段还原施法请求，执行既有释放结算并回结果、广播开始表现。
    /// 拒绝分支沿用 TryCast 的返回码与 MP/CD 快照，回包仍先于开始广播。
    /// </summary>
    /// <param name="args">派发器传入的吟唱事件。</param>
    private void OnSingingRequest(ROEvent args)
    {
        // 事件类型由监听表保证，这里只做载荷类型确认。
        ROSingingArgs singing_args = args as ROSingingArgs;
        if (singing_args == null)
        {
            return;
        }

        // 现有协议只携带技能 ID 与单次施法标识，其余校验与结算全部交给 TryCast。
        SkillCastReq request = new SkillCastReq();
        request.SkillId = singing_args.skill_id_;
        request.SkillUuid = singing_args.skill_uuid_;

        // 权威施法结果，成功与拒绝都会填充，必须原样回给客户端。
        SkillCastRet result;
        // 成功时的开始表现通知，拒绝分支保持为空。
        SkillCastStartNtf start_notice;
        // TryCast 的释放判定结果，只有成功才允许继续广播开始表现。
        bool accepted = TryCast(request, out result, out start_notice);
        if (result != null)
        {
            // 权威结果按原 Gate 会话回到客户端，时序与入口直调时一致。
            Owner.SendToClient(NetDefine.CMD_SkillCastCode, result.ToByteString());
        }
        if (!accepted)
        {
            return;
        }
        if (start_notice == null)
        {
            return;
        }
        RoleManager.Instance.BroadcastSkillStart(Owner, start_notice);
    }

    /// <summary>完整中心服学习快照已装载后允许释放校验。</summary>
    public override bool SkillsLoaded { get { return skills_loaded_; } }

    /// <summary>验证并原子替换中心服下发的已学等级快照，不清除现有 CD。</summary>
    /// <param name="response">中心服 11080 技能列表；只有成功结果可装载。</param>
    /// <returns>全部行有效且没有重复技能 ID 时返回 true。</returns>
    public bool LoadLearnedSkills(RoleSkillInfoRet response)
    {
        if (response == null || response.CmdCode != CmdCode.Succeed)
        {
            return false;
        }
        // 临时索引完整验证后才替换，保持原中心服学习装载语义。
        Dictionary<int, int> levels = new Dictionary<int, int>();
        // 当前技能行下标，范围为零到中心服列表数量减一。
        for (int index = 0; index < response.RoleSkillInfoList.Count; index++)
        {
            // 一行对应角色对一个稳定技能 ID 的当前学习等级。
            RoleSkillInfo skill = response.RoleSkillInfoList[index];
            // 技能行缺失、技能 ID 不合法或等级为负时视为装载失败。
            if (skill == null || skill.SkillId <= 0 || skill.Level < 0)
            {
                return false;
            }
            // 同一技能 ID 在本次列表中重复出现时视为装载失败。
            if (levels.ContainsKey(skill.SkillId))
            {
                return false;
            }
            levels.Add(skill.SkillId, skill.Level);
        }
        skill_map_ = levels;
        skills_loaded_ = true;
        return true;
    }

    /// <summary>查询中心服已确认的等级，客户端请求不提供等级或效果 ID。</summary>
    /// <param name="skill_id">释放组件查询的稳定技能主表 ID。</param>
    /// <returns>已学正整数等级，未记录或零级为零。</returns>
    protected override int GetLearnedLevel(int skill_id)
    {
        // 当前技能对应的权威学习等级，字典缺项时保持未学习。
        int level;
        if (skill_map_.TryGetValue(skill_id, out level))
        {
            return level;
        }
        return 0;
    }

    /// <summary>先解除吟唱事件监听，再由基类结束活动技能和冷却缓存，最后释放角色学习快照。</summary>
    public override void Dispose()
    {
        if (Owner != null)
        {
            Owner.GetEventDispatcher().RemoveEventListener(
                ROEventDefine.kActionSinging, this);
        }
        base.Dispose();
        skill_map_.Clear();
        skills_loaded_ = false;
    }
}
