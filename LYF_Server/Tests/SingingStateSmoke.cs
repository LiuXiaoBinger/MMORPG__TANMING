using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 吟唱与技能动作状态测试：先验证正式技能零吟唱与连续施法，再覆盖合成读条到期、
/// 未学技能不读条、主动移动打断、死亡打断、角色销毁打断，以及吟唱期间的请求队列上限与后置帧出队。
/// 断言的是状态机真实链路 default → singing → skill → default，而不是只看"回没回默认态"。
/// 独立进程运行，不进入正式服务工程。
/// </summary>
public static class SingingStateSmoke
{
    /// <summary>合成读条夹具的时长，单位毫秒，不来自正式技能的 CastMs。</summary>
    private const int SkillCastMilliseconds = SingingStateFixture.DurationMilliseconds;

    /// <summary>一个逻辑帧的毫秒数。</summary>
    private const int FrameMilliseconds = 50;

    /// <summary>等待状态收敛的最大帧数，仅用于防止用例死循环。</summary>
    private const int MaxWaitFrames = 400;

    /// <summary>
    /// GameGlobal.LogicTick 的自动属性后备字段。
    /// 技能动作期限按逻辑 Tick 判定，独立测试进程不跑 GameGlobal.Update，
    /// 只能用反射推进这一计时基准；边界仅限本测试进程，不影响正式代码。
    /// </summary>
    private static readonly FieldInfo LogicTickField =
        typeof(GameGlobal).GetField("<LogicTick>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// SingingComponent.interval_ 私有字段：本次吟唱需要的总毫秒数。
    /// 生产代码不为测试暴露读取接口，测试侧只反射读取，边界仅限本测试进程。
    /// </summary>
    private static readonly FieldInfo SingingIntervalField =
        typeof(SingingComponent).GetField("interval_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// RoleSingingComponent.request_list_ 私有字段：等待执行的施法请求队列。
    /// 同样只在测试进程内反射读取，生产代码不保留计数门面。
    /// </summary>
    private static readonly FieldInfo RequestListField =
        typeof(RoleSingingComponent).GetField("request_list_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>读取吟唱组件当前的总读条毫秒数。</summary>
    /// <param name="singing">待读取的吟唱组件。</param>
    /// <returns>本次吟唱的总毫秒数；未开始吟唱时为零。</returns>
    private static int ReadSingingInterval(SingingComponent singing)
    {
        return (int)SingingIntervalField.GetValue(singing);
    }

    /// <summary>读取施法请求队列当前长度。</summary>
    /// <param name="singing">待读取的施法请求组件。</param>
    /// <returns>队列中尚未执行的请求数量。</returns>
    private static int ReadRequestCount(RoleSingingComponent singing)
    {
        // queue 是队列字段的实例；按 ICollection 取长度，避免依赖队列的泛型实参。
        object queue = RequestListField.GetValue(singing);
        return ((System.Collections.ICollection)queue).Count;
    }

    /// <summary>测试入口：全部用例通过返回 0，任一失败返回 1 并打印原因。</summary>
    /// <param name="args">命令行参数；本测试不接收参数。</param>
    public static int Main(string[] args)
    {
        try
        {
            LubanMgr.Instance.Init();
            if (LogicTickField == null)
            {
                return Fail("未找到 GameGlobal.LogicTick 后备字段，无法推进逻辑 Tick");
            }

            // result 承接当前用例返回码，非零即该用例断言失败，后面用例不再执行。
            int result = CheckNonSingingSkillSequence();
            if (result != 0)
            {
                return result;
            }
            result = CheckNoPresentationKeepsActionTime();
            if (result != 0)
            {
                return result;
            }
            result = CheckSingingStartsAndFinishes();
            if (result != 0)
            {
                return result;
            }
            result = CheckUnlearnedSkillDoesNotSing();
            if (result != 0)
            {
                return result;
            }
            result = CheckMoveInterruptsSinging();
            if (result != 0)
            {
                return result;
            }
            result = CheckDeathInterruptsSinging();
            if (result != 0)
            {
                return result;
            }
            result = CheckRequestQueueLimitAndLateUpdate();
            if (result != 0)
            {
                return result;
            }
            result = CheckDisposeStopsSinging();
            if (result != 0)
            {
                return result;
            }
            Console.WriteLine("SingingStateSmoke: all cases passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("SingingStateSmoke 异常: " + ex);
            return 1;
        }
    }

    /// <summary>
    /// 合成吟唱用例：由测试夹具准备正时长读条；到期后自然结束并真正切入技能动作态，
    /// 动作存活期间保持技能态且动作实例唯一，动作到期后回到默认态，全程只扣一次蓝。
    /// </summary>
    private static int CheckSingingStartsAndFinishes()
    {
        // role 是本用例专用角色（ID 910101），已装载技能 10010，不加入在线索引。
        OnlineRole role = CreateRole(910101);
        // singing 接收角色身上的吟唱组件，用于读取读条时长与结束标记。
        RoleSingingComponent singing;
        // machine 接收角色身上的状态机组件，用于读取当前状态定义。
        StateMachineComponent machine;
        // skill_component 接收技能组件，用于读取当前动作实例与技能表数据。
        RoleSkillComponent skill_component;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine) ||
            !role.TryGetComponent(out skill_component))
        {
            return Fail("角色未注册吟唱组件、状态机或技能组件");
        }

        // initial_mp 是进入吟唱前的权威 MP 值（角色初始 500），作为全程只扣一次蓝的基线。
        int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;
        SingingStateFixture.Start(role, 10010, 1UL);

        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("施法请求未把状态机切到吟唱，实际 " + machine.GetCurrentState());
        }
        if (ReadSingingInterval(singing) != SkillCastMilliseconds)
        {
            return Fail("合成吟唱时长应为 " + SkillCastMilliseconds + "，实际 " + ReadSingingInterval(singing));
        }
        if (!singing.IsStartSinging())
        {
            return Fail("吟唱未标记为已开始读条");
        }

        // 只推进到差一帧到期，吟唱必须仍在进行，且此时还没有技能动作实例。
        int frames_to_finish = (SkillCastMilliseconds + FrameMilliseconds - 1) / FrameMilliseconds;
        Advance(role, frames_to_finish - 1);
        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("未到合成吟唱期限前不应结束");
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
        {
            return Fail("吟唱期间不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }
        if (skill_component.CurrentSkill() != null)
        {
            return Fail("吟唱期间不应建立技能动作实例");
        }

        // 再推进一帧即到期：吟唱结束派发 kActionSkill，状态机切入技能动作态。
        Advance(role, 1);
        if (machine.GetCurrentState() != StateDefine.kStateSkill)
        {
            return Fail("吟唱到期后应进入技能动作态，实际 " + machine.GetCurrentState());
        }
        // active_skill 是从技能组件取出的当前动作实例，用于断言动作唯一且沿用请求的施法标识。
        ROSkill active_skill = skill_component.CurrentSkill();
        if (active_skill == null)
        {
            return Fail("进入技能动作态后应有唯一动作实例");
        }
        if (active_skill.GetSkillUUID() != 1UL)
        {
            return Fail("动作实例应沿用请求的施法标识 1，实际 " + active_skill.GetSkillUUID());
        }
        if (!singing.IsFinished())
        {
            return Fail("吟唱到期后状态组件应已结束");
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp - 20)
        {
            return Fail("吟唱到期后应只扣一次 20 点法力，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }

        // 动作存活期间再推进一帧仍是技能态、仍是同一动作实例：
        // 若同一帧被推进两次，动作会在半个时长内结束，这里就会失败。
        Advance(role, 1);
        if (machine.GetCurrentState() != StateDefine.kStateSkill)
        {
            return Fail("技能动作未到期前不应离开技能态，实际 " + machine.GetCurrentState());
        }
        if (!ReferenceEquals(skill_component.CurrentSkill(), active_skill))
        {
            return Fail("技能动作期间不应替换动作实例");
        }

        // 推进到动作自然结束：回到默认态并释放动作实例，MP 不再重复扣除。
        int waited = 0;
        while (machine.GetCurrentState() != StateDefine.kStateDefault && waited < MaxWaitFrames)
        {
            Advance(role, 1);
            waited++;
        }
        if (machine.GetCurrentState() != StateDefine.kStateDefault)
        {
            return Fail("技能动作到期后应回到默认态，实际 " + machine.GetCurrentState());
        }
        if (skill_component.CurrentSkill() != null)
        {
            return Fail("技能动作结束后应释放动作实例");
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp - 20)
        {
            return Fail("技能动作结束不应重复扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例二：未学习技能不进入读条，直接以既有拒绝码结束。</summary>
    private static int CheckUnlearnedSkillDoesNotSing()
    {
        // role 是本用例专用角色（ID 910102），只装载技能 10010，请求改发未学习的 10040。
        OnlineRole role = CreateRole(910102);
        // singing 接收吟唱组件，用于确认未学习分支下读条时长与结束标记都为零值。
        RoleSingingComponent singing;
        // machine 接收状态机组件，用于确认未学习分支不会停留在吟唱态。
        StateMachineComponent machine;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine))
        {
            return Fail("角色未注册吟唱组件或状态机");
        }
        // initial_mp 是拒绝前的 MP 基线，用于断言未学习技能一次都不扣蓝。
        int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;
        RequestCast(role, 10040, 2UL);
        // 吟唱在 OnStart 里立刻结束，状态机在下一帧 Update 里回默认态。
        Advance(role, 1);

        if (machine.GetCurrentState() != StateDefine.kStateDefault)
        {
            return Fail("未学习技能不应停留在吟唱态，实际 " + machine.GetCurrentState());
        }
        if (!singing.IsFinished())
        {
            return Fail("未学习技能应立刻结束吟唱状态");
        }
        if (ReadSingingInterval(singing) != 0)
        {
            return Fail("未学习技能不应产生读条时长，实际 " + ReadSingingInterval(singing));
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
        {
            return Fail("未学习技能不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例三：吟唱期间主动移动必须打断吟唱并回到默认态，且不扣蓝。</summary>
    private static int CheckMoveInterruptsSinging()
    {
        // role 是本用例专用角色（ID 910103），用来验证移动打断吟唱。
        OnlineRole role = CreateRole(910103);
        // singing 接收吟唱组件，用于确认移动后吟唱已结束。
        RoleSingingComponent singing;
        // machine 接收状态机组件，用于确认移动打断后回默认态。
        StateMachineComponent machine;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine))
        {
            return Fail("角色未注册吟唱组件或状态机");
        }
        // initial_mp 是移动打断前的 MP 基线，被打断的吟唱不允许扣蓝。
        int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;
        SingingStateFixture.Start(role, 10010, 3UL);
        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("移动用例前置失败：未进入吟唱");
        }

        // move_component 接收移动组件，后面直接调 OnMoveStart 制造主动移动。
        ROMoveComponent move_component;
        if (!role.TryGetComponent(out move_component))
        {
            return Fail("角色未注册移动组件");
        }
        // move_request 是构造的移动请求，只有 IsMoving 与三轴增量参与本次打断判断。
        MoveOperationReq move_request = new MoveOperationReq();
        move_request.IsMoving = true;
        move_request.Dx = 1f;
        move_request.Dy = 0f;
        move_request.Dz = 2f;
        move_component.OnMoveStart(move_request);

        if (!singing.IsFinished())
        {
            return Fail("主动移动未打断吟唱");
        }
        // 与 RO 一致：Move 只负责 Stop 当前状态，状态机在下一帧 Update 里回默认态。
        Advance(role, 1);
        if (machine.GetCurrentState() != StateDefine.kStateDefault)
        {
            return Fail("移动打断后应回到默认态，实际 " + machine.GetCurrentState());
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
        {
            return Fail("被打断的吟唱不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例四：血量归零后吟唱必须在下一次状态推进时被打断，且不扣蓝。</summary>
    private static int CheckDeathInterruptsSinging()
    {
        // role 是本用例专用角色（ID 910104），用来验证死亡打断吟唱。
        OnlineRole role = CreateRole(910104);
        // singing 接收吟唱组件，用于确认血量归零后吟唱已结束。
        RoleSingingComponent singing;
        // machine 接收状态机组件，用于确认死亡打断后回默认态。
        StateMachineComponent machine;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine))
        {
            return Fail("角色未注册吟唱组件或状态机");
        }
        // initial_mp 是死亡打断前的 MP 基线，被打断的吟唱不允许扣蓝。
        int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;
        SingingStateFixture.Start(role, 10010, 4UL);
        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("死亡用例前置失败：未进入吟唱");
        }

        role.mainRoleInfo.BaseInfo.CurrHp = 0;
        Advance(role, 1);
        if (!singing.IsFinished())
        {
            return Fail("血量归零后吟唱未被打断");
        }
        if (machine.GetCurrentState() != StateDefine.kStateDefault)
        {
            return Fail("死亡打断后应回到默认态，实际 " + machine.GetCurrentState());
        }
        if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
        {
            return Fail("死亡打断的吟唱不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>
    /// 用例五：吟唱期间的新请求进入队列，队列上限为 10，超过的请求被丢弃；
    /// 技能动作占用期间队首不出队，动作结束后由后置帧取队首继续施法。
    /// </summary>
    private static int CheckRequestQueueLimitAndLateUpdate()
    {
        // role 是本用例专用角色（ID 910105），用来验证请求队列上限与后置帧取队首。
        OnlineRole role = CreateRole(910105);
        // singing 接收吟唱组件，队列长度从这里读取。
        RoleSingingComponent singing;
        // machine 接收状态机组件，用于区分吟唱态与技能动作态。
        StateMachineComponent machine;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine))
        {
            return Fail("角色未注册吟唱组件或状态机");
        }
        SingingStateFixture.Start(role, 10010, 10UL);
        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("队列用例前置失败：未进入吟唱");
        }

        // 吟唱中连续来 11 个请求：前 10 个入队，第 11 个因队列已满被丢弃。
        for (int index = 0; index < 11; index++)
        {
            RequestCast(role, 10010, (ulong)(100 + index));
        }
        if (ReadRequestCount(singing) != 10)
        {
            return Fail("队列上限应为 10，实际 " + ReadRequestCount(singing));
        }

        // 推进到当前吟唱结束：技能动作态接手动作槽，此时队首还不能出队。
        int frames_to_finish = (SkillCastMilliseconds + FrameMilliseconds - 1) / FrameMilliseconds;
        Advance(role, frames_to_finish);
        if (machine.GetCurrentState() != StateDefine.kStateSkill)
        {
            return Fail("吟唱结束后应进入技能动作态，实际 " + machine.GetCurrentState());
        }
        if (ReadRequestCount(singing) != 10)
        {
            return Fail("技能动作占用期间队首不应出队，实际剩余 " + ReadRequestCount(singing));
        }

        // 继续推进到动作结束：后置帧取队首，正式普攻按零吟唱立即进入技能动作。
        int waited = 0;
        while (ReadRequestCount(singing) != 9 && waited < MaxWaitFrames)
        {
            Advance(role, 1);
            waited++;
        }
        if (ReadRequestCount(singing) != 9)
        {
            return Fail("技能动作结束后后置帧应取出一个请求，实际剩余 " + ReadRequestCount(singing));
        }
        if (machine.GetCurrentState() != StateDefine.kStateSkill)
        {
            return Fail("队首非吟唱请求应立即进入技能动作，实际 " + machine.GetCurrentState());
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例六：角色销毁时吟唱被停止，且销毁后派发不再产生任何状态变化。</summary>
    private static int CheckDisposeStopsSinging()
    {
        // role 是本用例专用角色（ID 910106），用来验证销毁时停止吟唱。
        OnlineRole role = CreateRole(910106);
        // singing 接收吟唱组件，销毁后要求已结束且队列被清空。
        RoleSingingComponent singing;
        // machine 接收状态机组件，用于确认销毁前确实处在吟唱态。
        StateMachineComponent machine;
        if (!role.TryGetComponent(out singing) || !role.TryGetComponent(out machine))
        {
            return Fail("角色未注册吟唱组件或状态机");
        }
        SingingStateFixture.Start(role, 10010, 200UL);
        if (machine.GetCurrentState() != StateDefine.kStateSinging)
        {
            return Fail("销毁用例前置失败：未进入吟唱");
        }

        role.Dispose();
        if (!singing.IsFinished())
        {
            return Fail("角色销毁后吟唱状态应已结束");
        }
        if (ReadRequestCount(singing) != 0)
        {
            return Fail("角色销毁后请求队列应被清空，实际剩余 " + ReadRequestCount(singing));
        }
        // 销毁后派发器已 UnInit，再次派发不得触发任何回调或异常。
        RequestCast(role, 10010, 201UL);
        return 0;
    }

    /// <summary>
    /// 正式数据回归：连续三次普攻按三段各 700 毫秒结束，再依次接续其余非吟唱技能。
    /// 每次请求当帧就建立动作并扣一次 MP/CD；动作结束前保持占用，到期即可施放下一技能。
    /// 时长取当前部署表的独立已核实值，避免沿用被测实现计算出的错误吟唱时间作为预期。
    /// </summary>
    private static int CheckNonSingingSkillSequence()
    {
        // role 是本用例独立角色，初始 MP 足够覆盖完整技能序列，不接入真实数据库或在线玩家。
        OnlineRole role = CreateRole(910107);
        try
        {
            // skills 持有学习列表、动作实例及权威 MP/CD 提交逻辑。
            RoleSkillComponent skills;
            role.TryGetComponent(out skills);
            // singing 用于检查真实入口没有创建读条或等待队列。
            RoleSingingComponent singing;
            role.TryGetComponent(out singing);
            // machine 用于核验动作结束前后的状态，而不是只检查施法返回值。
            StateMachineComponent machine;
            role.TryGetComponent(out machine);
            // ids 前三项验证普攻连段，其余项验证不同技能结束后的接续施放。
            int[] ids = { 10010, 10010, 10010, 10040, 10050, 10060, 10070, 10080 };
            // durations 为当前部署表现表的动作占用毫秒数，不含任何额外吟唱时间。
            int[] durations = { 700, 700, 700, 3100, 3433, 3167, 1133, 667 };
            // learned 是本测试角色的技能快照，重复的普攻只登记一次。
            RoleSkillInfoRet learned = new RoleSkillInfoRet { CmdCode = CmdCode.Succeed };
            learned.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10010, Level = 1 });
            // index 遍历普攻以外的技能，补齐本测试角色的学习等级。
            for (int index = 3; index < ids.Length; index++)
            {
                learned.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = ids[index], Level = 1 });
            }
            skills.LoadLearnedSkills(learned);
            role.mainRoleInfo.BaseInfo.CurrMp = 10000;
            role.mainRoleInfo.BaseInfo.MaxMp = 10000;
            // index 按玩家的连续按键顺序推进，下一项紧接上一动作的自然结束帧。
            for (int index = 0; index < ids.Length; index++)
            {
                // initialMp 记录本次请求前的权威 MP，以区分一次扣除与重复结算。
                int initialMp = role.mainRoleInfo.BaseInfo.CurrMp;
                // core 读取本次技能的 MP 消耗，测试不改变配置中的技能费用。
                ROSkillCore core = skills.ResolveSkillCore(ids[index]);
                RequestCast(role, ids[index], (ulong)(300 + index));
                if (ReadSingingInterval(singing) != 0 || singing.IsStartSinging() || !singing.IsFinished())
                {
                    return Fail("非吟唱技能不得等待 CastMs，技能 " + ids[index]);
                }
                if (ReadRequestCount(singing) != 0 || machine.GetCurrentState() != StateDefine.kStateSkill)
                {
                    return Fail("非吟唱技能应当帧进入动作，不能排队，技能 " + ids[index]);
                }
                // active 是本次当帧建立的动作，UUID 必须匹配请求，拒绝请求不能残留旧动作。
                ROSkill active = skills.CurrentSkill();
                if (active == null || active.GetSkillUUID() != (ulong)(300 + index))
                {
                    return Fail("接续施法没有建立对应动作，技能 " + ids[index]);
                }
                if (index < 3 && active.GetUsingTimes() != index)
                {
                    return Fail("连续普攻未依次使用第 0、1、2 段动作");
                }
                if (role.mainRoleInfo.BaseInfo.CurrMp != initialMp - core.GetEffect().MpCost)
                {
                    return Fail("非吟唱技能必须当帧且仅扣一次 MP，技能 " + ids[index]);
                }
                // frames 按服务端 50 毫秒一帧向上取整，覆盖非整帧动作的结束边界。
                int frames = (durations[index] + FrameMilliseconds - 1) / FrameMilliseconds;
                Advance(role, frames - 1);
                if (!ReferenceEquals(skills.CurrentSkill(), active))
                {
                    return Fail("动作不能早于配置的结束帧释放，技能 " + ids[index]);
                }
                Advance(role, 1);
                if (skills.CurrentSkill() != null || machine.GetCurrentState() != StateDefine.kStateDefault)
                {
                    return Fail("动作到期仍被额外占用，技能 " + ids[index]);
                }
                if (role.mainRoleInfo.BaseInfo.CurrMp != initialMp - core.GetEffect().MpCost)
                {
                    return Fail("动作结束重复扣除 MP，技能 " + ids[index]);
                }
            }
            return 0;
        }
        finally
        {
            role.Dispose();
        }
    }

    /// <summary>没有模型表现行时，零吟唱仍保留 CastMs 加 RecoveryMs 的原有动作占用。</summary>
    private static int CheckNoPresentationKeepsActionTime()
    {
        // presentations 是本测试进程内的配置索引，临时清空以覆盖现有的无表现回退分支。
        Dictionary<int, cfg.SkillPresentationInfo> presentations = LubanMgr.Instance.GetSkillPresentationInfos();
        // rows 保留原配置对象引用，finally 原样恢复，绝不写入正式表文件。
        List<cfg.SkillPresentationInfo> rows = new List<cfg.SkillPresentationInfo>(presentations.Values);
        // role 只用于本次无表现配置用例，不与其他角色共享技能动作。
        OnlineRole role = CreateRole(910108);
        try
        {
            presentations.Clear();
            // skills 提供正在执行的动作，判断是否过早结束或重复延长。
            RoleSkillComponent skills;
            role.TryGetComponent(out skills);
            // effect 提供既有无表现分支的动作与后摇时长，单位毫秒。
            cfg.SkillEffectInfo effect = skills.ResolveSkillCore(10010).GetEffect();
            // frames 是完整动作加后摇所需的逻辑帧数，向上取整到 50 毫秒。
            int frames = (effect.CastMs + effect.RecoveryMs + FrameMilliseconds - 1) / FrameMilliseconds;
            RequestCast(role, 10010, 400UL);
            if (skills.CurrentSkill() == null)
            {
                return Fail("无表现行的非吟唱技能也必须当帧建立动作");
            }
            Advance(role, frames - 1);
            if (skills.CurrentSkill() == null)
            {
                return Fail("无表现行不能漏算 CastMs 动作时长");
            }
            Advance(role, 1);
            if (skills.CurrentSkill() != null)
            {
                return Fail("无表现行动作到期后不能再次占用吟唱时间");
            }
            return 0;
        }
        finally
        {
            // index 依次恢复本进程的原配置引用，避免影响后面的吟唱状态测试。
            for (int index = 0; index < rows.Count; index++)
            {
                presentations.Add(rows[index].Id, rows[index]);
            }
            role.Dispose();
        }
    }

    /// <summary>构造一个已装载技能列表的在线角色，不加入在线索引，避免影响附近广播。</summary>
    /// <param name="role_id">角色 ID。</param>
    /// <returns>已完成组件初始化与技能装载的在线角色。</returns>
    private static OnlineRole CreateRole(int role_id)
    {
        // role 是本辅助方法新建的在线角色，只填最小基础信息，不进在线索引。
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo
        {
            BaseInfo = new RoleBaseInfo
            {
                RoleId = role_id,
                JobId = 1,
                CurrHp = 1000,
                MaxHp = 1000,
                CurrMp = 500,
                MaxMp = 500
            }
        };
        role.InitSerializes();
        role.InitializeSaveVersion(0L);
        role.CompleteRoleDataLoad();

        // skill_component 接收技能组件，装载技能列表必须走它的正式入口。
        RoleSkillComponent skill_component;
        if (!role.TryGetComponent(out skill_component))
        {
            return role;
        }
        // learned_skills 是伪造的技能列表回包，只含一条 10010 一级技能。
        RoleSkillInfoRet learned_skills = new RoleSkillInfoRet { CmdCode = CmdCode.Succeed };
        learned_skills.RoleSkillInfoList.Add(new RoleSkillInfo { SkillId = 10010, Level = 1 });
        skill_component.LoadLearnedSkills(learned_skills);
        return role;
    }

    /// <summary>按真实入口之后的事件格式派发一次客户端施法请求。</summary>
    /// <param name="role">目标在线角色。</param>
    /// <param name="skill_id">技能主表 ID。</param>
    /// <param name="skill_uuid">本次施法标识。</param>
    private static void RequestCast(OnlineRole role, int skill_id, ulong skill_uuid)
    {
        // args 是从对象池取用的施法请求事件，派发后在 finally 里归还调用方引用。
        ROClientFireSkillArgs args = ROEventPool<ROClientFireSkillArgs>.Instance.Get();
        try
        {
            args.skill_id_ = skill_id;
            args.skill_uuid_ = skill_uuid;
            args.target_ = 0UL;
            args.cast_point_ = new ROVector3(0f, 0f, 0f);
            role.GetEventDispatcher().DispatcherEvent(args);
        }
        finally
        {
            args.Release();
        }
    }

    /// <summary>
    /// 按固定帧推进角色逻辑，驱动状态机与后置阶段。
    /// 顺序与 GameGlobal.Update 一致：先推进逻辑 Tick，再驱动一帧角色逻辑。
    /// </summary>
    /// <param name="role">目标在线角色。</param>
    /// <param name="frames">要推进的逻辑帧数量。</param>
    private static void Advance(OnlineRole role, int frames)
    {
        // index 是要推进的逻辑帧计数器，从零到 frames 减一，每帧推进一次 LogicTick。
        for (int index = 0; index < frames; index++)
        {
            // next_tick 是下一帧的逻辑 Tick 值（毫秒制递增计数），模拟 GameGlobal.Update 的推进。
            long next_tick = GameGlobal.Instance.LogicTick + 1L;
            LogicTickField.SetValue(GameGlobal.Instance, next_tick);
            role.Update(FrameMilliseconds);
        }
    }

    /// <summary>输出失败原因并返回统一失败码。</summary>
    /// <param name="message">失败断言的中文说明。</param>
    /// <returns>固定非零测试失败码。</returns>
    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
