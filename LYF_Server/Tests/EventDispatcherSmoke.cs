using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Google.Protobuf;

/// <summary>
/// 实体事件派发器行为测试：覆盖注册、重复注册替换、解除、回调内解除、同类型嵌套派发、
/// 不同角色派发器隔离、宿主销毁清理，以及施法请求组件的注册与解除。
/// 独立进程运行，不进入正式服务工程。
/// </summary>
public static class EventDispatcherSmoke
{
    /// <summary>
    /// GameGlobal.LogicTick 的自动属性后备字段。技能动作期限按逻辑 Tick 判定，
    /// 独立测试进程不跑 GameGlobal.Update，只能用反射推进这一计时基准；
    /// 边界仅限本测试进程，正式代码不含任何测试入口。
    /// </summary>
    private static readonly FieldInfo LogicTickField =
        typeof(GameGlobal).GetField("<LogicTick>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>记录回调次数的探针监听者，实现监听身份接口以获得独立登记。</summary>
    private sealed class ProbeListener : IROEventListener
    {
        /// <summary>本探针收到的回调次数。</summary>
        public int count_;

        /// <summary>本探针最近一次收到的事件对象，用于确认派发的是同一实例。</summary>
        public ROEvent last_event_;

        /// <summary>统计一次回调并记录事件引用。</summary>
        /// <param name="evt">派发器传入的事件对象。</param>
        public void OnEvent(ROEvent evt)
        {
            count_++;
            last_event_ = evt;
        }
    }

    /// <summary>在回调中执行附加动作的探针，用于模拟回调内解除与嵌套派发。</summary>
    private sealed class HookListener : IROEventListener
    {
        /// <summary>回调执行时调用的附加动作，由测试用例注入。</summary>
        public Action action_;

        /// <summary>执行注入的附加动作。</summary>
        /// <param name="evt">派发器传入的事件对象。</param>
        public void OnEvent(ROEvent evt)
        {
            if (action_ != null)
            {
                action_();
            }
        }
    }

    /// <summary>测试入口：全部用例通过返回 0，任一失败返回 1 并打印原因。</summary>
    /// <param name="args">命令行参数；本测试不接收参数。</param>
    public static int Main(string[] args)
    {
        try
        {
            // result 是本轮用例的返回码：非零立即向上返回，失败用例不会被后续用例掩盖。
            int result = CheckRegisterAndDispatch();
            if (result != 0)
            {
                return result;
            }
            result = CheckDuplicateRegisterReplaces();
            if (result != 0)
            {
                return result;
            }
            result = CheckRemoveEventListener();
            if (result != 0)
            {
                return result;
            }
            result = CheckRemoveInsideCallback();
            if (result != 0)
            {
                return result;
            }
            result = CheckNestedDispatch();
            if (result != 0)
            {
                return result;
            }
            result = CheckRoleIsolation();
            if (result != 0)
            {
                return result;
            }
            result = CheckUnInit();
            if (result != 0)
            {
                return result;
            }
            result = CheckRoleDisposeAndComponentUnregister();
            if (result != 0)
            {
                return result;
            }
            result = CheckEventDrivesRealSkillCast();
            if (result != 0)
            {
                return result;
            }
            result = CheckMoveInterruptRepliesTerminal();
            if (result != 0)
            {
                return result;
            }
            result = CheckQueueFullRepliesTerminal();
            if (result != 0)
            {
                return result;
            }
            result = CheckQueueTimeoutRepliesTerminal();
            if (result != 0)
            {
                return result;
            }
            Console.WriteLine("EventDispatcherSmoke: all cases passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("EventDispatcherSmoke 异常: " + ex);
            return 1;
        }
    }

    /// <summary>用例一：注册后派发，回调收到同一事件实例。</summary>
    private static int CheckRegisterAndDispatch()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        ProbeListener probe = new ProbeListener();
        dispatcher.AddEventListener(ROEventDefine.kActionClienFireSkill, probe, probe.OnEvent);

        ROClientFireSkillArgs args = new ROClientFireSkillArgs();
        args.skill_id_ = 1001;
        dispatcher.DispatcherEvent(args);

        if (probe.count_ != 1)
        {
            return Fail("注册后派发次数应为 1，实际 " + probe.count_);
        }
        if (!ReferenceEquals(probe.last_event_, args))
        {
            return Fail("回调收到的不是同一个事件实例");
        }
        return 0;
    }

    /// <summary>用例二：同一监听者重复注册时替换为新回调，旧回调不再执行。</summary>
    private static int CheckDuplicateRegisterReplaces()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        // 监听身份实例，两次注册共用同一个监听者，只有回调不同。
        ProbeListener listener = new ProbeListener();
        // 旧回调 A 的执行次数，替换后必须保持 0。
        int old_callback_count = 0;
        // 新回调 B 的执行次数，替换后必须恰好为 1。
        int new_callback_count = 0;
        Action<ROEvent> old_callback = delegate { old_callback_count++; };
        Action<ROEvent> new_callback = delegate { new_callback_count++; };

        dispatcher.AddEventListener(ROEventDefine.kActionSinging, listener, old_callback);
        dispatcher.AddEventListener(ROEventDefine.kActionSinging, listener, new_callback);

        dispatcher.DispatcherEvent(new ROSingingArgs());
        if (old_callback_count != 0)
        {
            return Fail("重复注册后旧回调仍被执行，次数 " + old_callback_count);
        }
        if (new_callback_count != 1)
        {
            return Fail("重复注册后新回调应执行 1 次，实际 " + new_callback_count);
        }
        return 0;
    }

    /// <summary>用例三：解除监听后不再回调。</summary>
    private static int CheckRemoveEventListener()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        ProbeListener probe = new ProbeListener();
        dispatcher.AddEventListener(ROEventDefine.kActionSinging, probe, probe.OnEvent);
        dispatcher.RemoveEventListener(ROEventDefine.kActionSinging, probe);
        dispatcher.DispatcherEvent(new ROSingingArgs());

        if (probe.count_ != 0)
        {
            return Fail("解除监听后仍被回调，次数 " + probe.count_);
        }
        return 0;
    }

    /// <summary>用例四：回调内解除其它监听者，本轮剩余回调按 RO 语义跳过。</summary>
    private static int CheckRemoveInsideCallback()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        ProbeListener second = new ProbeListener();
        HookListener remover = new HookListener();
        remover.action_ = delegate { dispatcher.RemoveEventListener(ROEventDefine.kActionSinging, second); };

        dispatcher.AddEventListener(ROEventDefine.kActionSinging, remover, remover.OnEvent);
        dispatcher.AddEventListener(ROEventDefine.kActionSinging, second, second.OnEvent);
        dispatcher.DispatcherEvent(new ROSingingArgs());

        if (second.count_ != 0)
        {
            return Fail("回调内解除的监听者不应再被回调，次数 " + second.count_);
        }
        dispatcher.DispatcherEvent(new ROSingingArgs());
        if (second.count_ != 0)
        {
            return Fail("解除后下一轮派发仍回调，次数 " + second.count_);
        }
        return 0;
    }

    /// <summary>用例五：同类型事件在回调内再次派发，内外两层都执行且不破坏监听表。</summary>
    private static int CheckNestedDispatch()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        ProbeListener probe = new ProbeListener();
        HookListener nested = new HookListener();
        int nested_times = 0;
        nested.action_ = delegate
        {
            if (nested_times == 0)
            {
                nested_times++;
                dispatcher.DispatcherEvent(new ROSingingArgs());
            }
        };

        dispatcher.AddEventListener(ROEventDefine.kActionSinging, probe, probe.OnEvent);
        dispatcher.AddEventListener(ROEventDefine.kActionSinging, nested, nested.OnEvent);
        dispatcher.DispatcherEvent(new ROSingingArgs());

        // 外层一次 + 内层一次，探针共两次；嵌套层数受 nested_times 限制，不会无限递归。
        if (probe.count_ != 2)
        {
            return Fail("嵌套派发回调次数应为 2，实际 " + probe.count_);
        }
        dispatcher.DispatcherEvent(new ROSingingArgs());
        if (probe.count_ != 3)
        {
            return Fail("嵌套派发后监听表被破坏，累计次数 " + probe.count_);
        }
        return 0;
    }

    /// <summary>用例六：两个角色的派发器互不影响。</summary>
    private static int CheckRoleIsolation()
    {
        OnlineRole role_a = new OnlineRole();
        OnlineRole role_b = new OnlineRole();
        if (ReferenceEquals(role_a.GetEventDispatcher(), role_b.GetEventDispatcher()))
        {
            return Fail("不同角色不应共用同一个事件派发器");
        }

        ProbeListener probe_a = new ProbeListener();
        role_a.GetEventDispatcher().AddEventListener(ROEventDefine.kActionSinging, probe_a, probe_a.OnEvent);
        role_b.GetEventDispatcher().DispatcherEvent(new ROSingingArgs());
        if (probe_a.count_ != 0)
        {
            return Fail("角色 B 的派发影响了角色 A 的监听");
        }
        role_a.GetEventDispatcher().DispatcherEvent(new ROSingingArgs());
        if (probe_a.count_ != 1)
        {
            return Fail("角色 A 自身派发未回调，次数 " + probe_a.count_);
        }
        return 0;
    }

    /// <summary>用例七：UnInit 后派发不回调。</summary>
    private static int CheckUnInit()
    {
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        ProbeListener probe = new ProbeListener();
        dispatcher.AddEventListener(ROEventDefine.kActionClienFireSkill, probe, probe.OnEvent);
        dispatcher.UnInit();
        dispatcher.DispatcherEvent(new ROClientFireSkillArgs());
        if (probe.count_ != 0)
        {
            return Fail("UnInit 后仍被回调，次数 " + probe.count_);
        }
        return 0;
    }

    /// <summary>
    /// 用例八：角色 Dispose 清理派发器，施法请求组件释放后不再转发事件。
    /// 角色按正式登录后的组件装配顺序构建（状态机 + 技能组件 + 施法请求组件），
    /// 用真实的客户端施法事件驱动，断言的是"请求事件被转发为吟唱事件"这条链路，
    /// 而不是脱离状态机后的空派发——否则无法证明组件解除监听的时机正确。
    /// </summary>
    private static int CheckRoleDisposeAndComponentUnregister()
    {
        // 吟唱开始要读技能效果表，与正式进程一样先加载配置。
        LubanMgr.Instance.Init();

        // role 是在线角色本体，组件装配、事件派发与销毁都作用在它身上。
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo();
        role.mainRoleInfo.BaseInfo = new RoleBaseInfo();
        role.mainRoleInfo.BaseInfo.RoleId = 910002;
        role.mainRoleInfo.BaseInfo.JobId = 1;
        role.mainRoleInfo.BaseInfo.CurrHp = 1000;
        role.mainRoleInfo.BaseInfo.MaxHp = 1000;
        role.mainRoleInfo.BaseInfo.CurrMp = 500;
        role.mainRoleInfo.BaseInfo.MaxMp = 500;
        role.InitSerializes();
        role.InitializeSaveVersion(0L);
        role.CompleteRoleDataLoad();

        // skill_component 是角色的技能组件，用来装载中心服学习快照；取不到说明装配顺序有问题。
        RoleSkillComponent skill_component;
        if (!role.TryGetComponent(out skill_component))
        {
            return Fail("角色未注册技能组件");
        }
        // learned_skills 是中心服技能列表回包，这里只放一条 10010 一级技能作为施法前置。
        RoleSkillInfoRet learned_skills = new RoleSkillInfoRet();
        learned_skills.CmdCode = CmdCode.Succeed;
        // learned_row 是列表中的一条学习记录，技能 ID 与等级必须与技能主表一致。
        RoleSkillInfo learned_row = new RoleSkillInfo();
        learned_row.SkillId = 10010;
        learned_row.Level = 1;
        learned_skills.RoleSkillInfoList.Add(learned_row);
        if (!skill_component.LoadLearnedSkills(learned_skills))
        {
            return Fail("技能装载失败，无法驱动吟唱");
        }

        // singing_probe 挂在 kActionSinging 上，统计请求事件有没有被转发成吟唱事件。
        ProbeListener singing_probe = new ProbeListener();
        role.GetEventDispatcher().AddEventListener(ROEventDefine.kActionSinging, singing_probe, singing_probe.OnEvent);

        // 未释放时：请求事件必须被转发为一次吟唱事件。
        DispatchClientFireSkill(role, 10010, 1UL);
        if (singing_probe.count_ != 1)
        {
            return Fail("施法请求组件未把事件转发为吟唱事件，次数 " + singing_probe.count_);
        }

        // singing_component 是施法请求组件，本用例要手动释放它来验证解除监听。
        RoleSingingComponent singing_component;
        if (!role.TryGetComponent(out singing_component))
        {
            return Fail("角色未注册施法请求组件");
        }
        singing_component.Dispose();
        DispatchClientFireSkill(role, 10010, 2UL);
        if (singing_probe.count_ != 1)
        {
            return Fail("施法请求组件释放后仍转发事件，次数 " + singing_probe.count_);
        }

        // 角色释放后派发器被 UnInit，残余监听不得再被回调。
        role.Dispose();
        role.GetEventDispatcher().DispatcherEvent(new ROClientFireSkillArgs());
        if (singing_probe.count_ != 1)
        {
            return Fail("角色释放后仍触发回调，次数 " + singing_probe.count_);
        }
        return 0;
    }

    /// <summary>
    /// 用例九：客户端施法请求经真实消息入口与两段事件到达技能业务，
    /// 并用本机环回网关通道断言客户端实际收到的 SkillCastRet 与 SkillCastStartNtf。
    /// 环回通道是真实 TCP 连接与真实封包，但不是完整登录链路，属于协议级送达验证。
    /// </summary>
    private static int CheckEventDrivesRealSkillCast()
    {
        // 技能主表与效果表来自正式 Luban 导出，测试进程从固定目录加载。
        LubanMgr.Instance.Init();

        // 环回网关通道：一端交给角色回包，另一端由本用例按真实帧格式读取。
        using (LoopbackGate gate = new LoopbackGate())
        {
            // 待验证的在线角色，装配顺序与正式登录后的角色一致。
            int role_id = 910001;
            // role 是按本机环回会话装配出来的施法角色，回包会写到本用例可读的一端。
            OnlineRole role = CreateCastingRole(gate, role_id);
            if (role == null)
            {
                return Fail("在线角色装配失败");
            }

            // 首次施法前的权威 MP，主表配置为 500，成功一次扣 20。
            int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;

            // cast_result 是入口调用返回码，非零表示命令构造或会话解析失败。
            int cast_result = InvokeSkillCastHandler(gate, role, 10010, 7UL);
            if (cast_result != 0)
            {
                return cast_result;
            }
            // 普攻走 RO 零吟唱分支：不推进逻辑帧也必须立即收到成功结果与开始通知。

            // 第一帧必须是权威结果：成功、回带技能标识、MP 已扣、冷却已下发。
            BasePackage result_package;
            if (!gate.TryReadFrame(out result_package, 2000))
            {
                return Fail("未收到施法结果回包");
            }
            if (result_package.ProtoCode != NetDefine.CMD_SkillCastCode)
            {
                return Fail("第一帧应为施法结果，实际协议号 " + result_package.ProtoCode);
            }
            // ret 是第一帧解析出的施法结果，用于断言成功码、回带标识与权威 MP。
            SkillCastRet ret = SkillCastRet.Parser.ParseFrom(result_package.Data);
            if (ret.CmdCode != CmdCode.Succeed)
            {
                return Fail("施法应成功，实际 " + ret.CmdCode);
            }
            if (ret.SkillId != 10010)
            {
                return Fail("回包技能 ID 应为 10010，实际 " + ret.SkillId);
            }
            if (ret.SkillUuid != 7UL)
            {
                return Fail("回包施法标识应为 7，实际 " + ret.SkillUuid);
            }
            if (ret.CurrentMp != initial_mp - 20)
            {
                return Fail("回包权威 MP 应为 " + (initial_mp - 20) + "，实际 " + ret.CurrentMp);
            }
            if (ret.CooldownEndUnixMs <= 0L)
            {
                return Fail("成功回包应携带权威冷却结束时间");
            }

            // 第二帧必须是开始表现：证明结果确实先于开始通知送达。
            BasePackage start_package;
            if (!gate.TryReadFrame(out start_package, 2000))
            {
                return Fail("未收到施法开始通知");
            }
            if (start_package.ProtoCode != NetDefine.CMD_SkillCastStartCode)
            {
                return Fail("第二帧应为开始通知，实际协议号 " + start_package.ProtoCode);
            }
            // start_notice 是第二帧解析出的开始表现通知，用于断言回包顺序与施法者身份。
            SkillCastStartNtf start_notice = SkillCastStartNtf.Parser.ParseFrom(start_package.Data);
            if (start_notice.SkillId != 10010)
            {
                return Fail("开始通知技能 ID 应为 10010，实际 " + start_notice.SkillId);
            }
            if (start_notice.SkillUuid != 7UL)
            {
                return Fail("开始通知施法标识应为 7，实际 " + start_notice.SkillUuid);
            }
            if (start_notice.CasterRoleId != (int)role.GetID())
            {
                return Fail("开始通知施法者应为 " + role.GetID() + "，实际 " + start_notice.CasterRoleId);
            }

            // 拒绝分支：同一技能立即二次释放，由既有冷却校验在入口直接拒绝。
            int reject_result = InvokeSkillCastHandler(gate, role, 10010, 8UL);
            if (reject_result != 0)
            {
                return reject_result;
            }
            // 入口非提交校验必须尽早拒绝：不派发事件、不读条，因此这里不再推进任何逻辑帧。
            BasePackage reject_package;
            if (!gate.TryReadFrame(out reject_package, 2000))
            {
                return Fail("拒绝分支未收到结果回包");
            }
            if (reject_package.ProtoCode != NetDefine.CMD_SkillCastCode)
            {
                return Fail("拒绝分支第一帧应为施法结果，实际协议号 " + reject_package.ProtoCode);
            }
            // reject_ret 是拒绝分支的施法结果，用于断言不扣蓝且回带本次施法标识。
            SkillCastRet reject_ret = SkillCastRet.Parser.ParseFrom(reject_package.Data);
            if (reject_ret.CmdCode == CmdCode.Succeed)
            {
                return Fail("冷却中二次释放不应成功");
            }
            if (reject_ret.SkillUuid != 8UL)
            {
                return Fail("拒绝回包应回带本次施法标识 8，实际 " + reject_ret.SkillUuid);
            }
            if (reject_ret.CurrentMp != initial_mp - 20)
            {
                return Fail("拒绝分支不应再扣蓝，实际 MP " + reject_ret.CurrentMp);
            }
            if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp - 20)
            {
                return Fail("拒绝分支角色 MP 被改动，实际 " + role.mainRoleInfo.BaseInfo.CurrMp);
            }

            // 拒绝分支不应产生开始表现：短时间内读不到第二帧即为正确。
            BasePackage extra_package;
            if (gate.TryReadFrame(out extra_package, 300))
            {
                return Fail("拒绝分支不应发出开始通知，实际收到协议号 " + extra_package.ProtoCode);
            }

            // index 表示紧接第一段之后的第二、三段普攻；每段在部署表的 700 毫秒边界立即续发。
            for (int index = 1; index <= 2; index++)
            {
                AdvanceLogicFrames(role, 14);
                // end_package 必须是上一动作自然结束的通知，不能仍在等待错误的吟唱计时。
                BasePackage end_package;
                if (!gate.TryReadFrame(out end_package, 2000) || end_package.ProtoCode != NetDefine.CMD_SkillCastEndCode)
                {
                    return Fail("连续普攻未在 700 毫秒后收到动作结束通知");
                }
                // next_uuid 是本次接续请求的唯一施法标识，避开上面的被拒绝请求 8。
                ulong next_uuid = (ulong)(8 + index);
                if (InvokeSkillCastHandler(gate, role, 10010, next_uuid) != 0)
                {
                    return Fail("连续普攻的真实 OnCall 入口调用失败");
                }
                // next_package 不推进帧就应收到本次成功结果，证明不再进入 300 毫秒等待队列。
                BasePackage next_package;
                if (!gate.TryReadFrame(out next_package, 2000) || next_package.ProtoCode != NetDefine.CMD_SkillCastCode)
                {
                    return Fail("连续普攻没有立即收到权威结果");
                }
                // next_ret 核对成功、原 UUID、权威连段和只扣一次蓝的结果。
                SkillCastRet next_ret = SkillCastRet.Parser.ParseFrom(next_package.Data);
                if (next_ret.CmdCode != CmdCode.Succeed || next_ret.SkillUuid != next_uuid || next_ret.UsingTimes != index)
                {
                    return Fail("连续普攻被错误拒绝或连段不正确，实际 " + next_ret.CmdCode);
                }
                if (next_ret.CastDurationMs != 700 || next_ret.CurrentMp != initial_mp - 20 * (index + 1))
                {
                    return Fail("连续普攻的动作时长或 MP 扣除不正确");
                }
                // next_start_package 必须紧随本次结果，保持结果先于开始表现的协议顺序。
                BasePackage next_start_package;
                if (!gate.TryReadFrame(out next_start_package, 2000) || next_start_package.ProtoCode != NetDefine.CMD_SkillCastStartCode)
                {
                    return Fail("连续普攻没有紧随权威结果发送开始通知");
                }
                // next_start 用请求 UUID 排除延迟到达的旧开始通知。
                SkillCastStartNtf next_start = SkillCastStartNtf.Parser.ParseFrom(next_start_package.Data);
                if (next_start.SkillUuid != next_uuid)
                {
                    return Fail("连续普攻开始通知的 UUID 不匹配");
                }
            }

            // 角色释放后派发器清空，避免残留回调引用。
            role.Dispose();
            RoleManager.Instance.RemoveOnlineRole(role_id);
            return 0;
        }
    }

    /// <summary>
    /// 用例十：吟唱中主动移动打断，客户端必须收到一条明确的终态结果，
    /// 回带原技能标识与施法标识，不扣蓝、不发开始表现，也不重复终结同一请求。
    /// </summary>
    private static int CheckMoveInterruptRepliesTerminal()
    {
        LubanMgr.Instance.Init();
        // gate 是本用例的环回网关通道，提供真实会话并承载服务端真实写出的协议帧。
        using (LoopbackGate gate = new LoopbackGate())
        {
            // role_id 是本用例独占的角色 ID，避免与其它用例的在线索引互相冲突。
            int role_id = 910003;
            // role 是装配完成并已加入在线索引的施法角色。
            OnlineRole role = CreateCastingRole(gate, role_id);
            if (role == null)
            {
                return Fail("在线角色装配失败");
            }
            // initial_mp 是进入吟唱前的权威 MP，被打断后必须保持不变。
            int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;

            // 正式普攻没有读条；本用例用测试夹具准备合成吟唱，只验证打断或队列的终态回包。
            SingingStateFixture.Start(role, 10010, 21UL);
            // pending_package 用于确认吟唱期间不应提前回包，读到帧即判失败。
            BasePackage pending_package;
            if (gate.TryReadFrame(out pending_package, 300))
            {
                return Fail("吟唱期间不应提前回包，实际收到协议号 " + pending_package.ProtoCode);
            }

            // 移动打断吟唱：状态机 Stop 吟唱态，必须立刻给出终态。
            ROMoveComponent move_component;
            if (!role.TryGetComponent(out move_component))
            {
                return Fail("角色未注册移动组件");
            }
            // move_request 是构造的移动上报，IsMoving 为真代表玩家主动移动。
            MoveOperationReq move_request = new MoveOperationReq();
            move_request.IsMoving = true;
            move_request.Dx = 1f;
            move_request.Dy = 0f;
            move_request.Dz = 2f;
            move_component.OnMoveStart(move_request);

            // terminal_package 是吟唱被打断后应立刻收到的终态回包。
            BasePackage terminal_package;
            if (!gate.TryReadFrame(out terminal_package, 2000))
            {
                return Fail("吟唱被打断后未收到终态回包");
            }
            if (terminal_package.ProtoCode != NetDefine.CMD_SkillCastCode)
            {
                return Fail("吟唱被打断的第一帧应为施法结果，实际协议号 " + terminal_package.ProtoCode);
            }
            // terminal 是终态结果，用于断言非成功码且回带原技能标识与施法标识。
            SkillCastRet terminal = SkillCastRet.Parser.ParseFrom(terminal_package.Data);
            if (terminal.CmdCode == CmdCode.Succeed)
            {
                return Fail("被打断的吟唱不应回成功结果");
            }
            if (terminal.SkillUuid != 21UL || terminal.SkillId != 10010)
            {
                return Fail("终态结果应回带原技能标识与施法标识，实际 " +
                    terminal.SkillId + "/" + terminal.SkillUuid);
            }
            if (terminal.CurrentMp != initial_mp)
            {
                return Fail("被打断的吟唱不应扣蓝，回包 MP " + terminal.CurrentMp);
            }
            if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
            {
                return Fail("被打断的吟唱改动了角色 MP，实际 " + role.mainRoleInfo.BaseInfo.CurrMp);
            }

            // 同一请求只终结一次，且不得产生开始或结束表现帧。
            BasePackage extra_package;
            if (gate.TryReadFrame(out extra_package, 300))
            {
                return Fail("被打断的吟唱不应再发任何表现帧，实际协议号 " + extra_package.ProtoCode);
            }

            role.Dispose();
            RoleManager.Instance.RemoveOnlineRole(role_id);
            return 0;
        }
    }

    /// <summary>
    /// 用例十一：吟唱期间请求队列达到上限后，被丢弃的请求必须收到明确终态，
    /// 而不是静默消失；同时不得扣蓝、不得发开始表现。
    /// </summary>
    private static int CheckQueueFullRepliesTerminal()
    {
        LubanMgr.Instance.Init();
        // gate 是本用例的环回网关通道，队列满的终态要写到这条真实连接上。
        using (LoopbackGate gate = new LoopbackGate())
        {
            // role_id 是本用例独占的角色 ID，队列满用例单独占一个。
            int role_id = 910004;
            // role 是装配完成并已加入在线索引的施法角色。
            OnlineRole role = CreateCastingRole(gate, role_id);
            if (role == null)
            {
                return Fail("在线角色装配失败");
            }
            // initial_mp 是入队前的权威 MP，队列满丢弃请求不应扣蓝。
            int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;

            // 正式普攻没有读条；本用例用测试夹具准备合成吟唱，只验证打断或队列的终态回包。
            SingingStateFixture.Start(role, 10010, 31UL);

            // 队列上限为 10：先塞满 10 个请求，第 11 个会被丢弃。
            for (int index = 0; index < 10; index++)
            {
                // queued_result 是本次入队请求的入口返回码，队列未满时应为 0。
                int queued_result = InvokeSkillCastHandler(gate, role, 10010, (ulong)(40 + index));
                if (queued_result != 0)
                {
                    return queued_result;
                }
            }
            // overflow_result 是第 11 个请求的入口返回码，入口本身仍然放行。
            int overflow_result = InvokeSkillCastHandler(gate, role, 10010, 60UL);
            if (overflow_result != 0)
            {
                return overflow_result;
            }

            // full_package 是被丢弃请求应立刻收到的终态回包。
            BasePackage full_package;
            if (!gate.TryReadFrame(out full_package, 2000))
            {
                return Fail("队列满被丢弃的请求未收到终态回包");
            }
            if (full_package.ProtoCode != NetDefine.CMD_SkillCastCode)
            {
                return Fail("队列满的终态应为施法结果，实际协议号 " + full_package.ProtoCode);
            }
            // full_ret 是队列满的终态结果，用于断言非成功码且回带被丢弃请求的施法标识。
            SkillCastRet full_ret = SkillCastRet.Parser.ParseFrom(full_package.Data);
            if (full_ret.CmdCode == CmdCode.Succeed)
            {
                return Fail("队列满被丢弃的请求不应回成功结果");
            }
            if (full_ret.SkillUuid != 60UL)
            {
                return Fail("队列满的终态应回带被丢弃请求的施法标识 60，实际 " + full_ret.SkillUuid);
            }
            if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
            {
                return Fail("队列满不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
            }

            role.Dispose();
            RoleManager.Instance.RemoveOnlineRole(role_id);
            return 0;
        }
    }

    /// <summary>
    /// 用例十二：队列中的请求超过等待上限后被清理，必须收到明确终态，
    /// 而不是一直悬挂到客户端超时。
    /// </summary>
    private static int CheckQueueTimeoutRepliesTerminal()
    {
        LubanMgr.Instance.Init();
        // gate 是本用例的环回网关通道，超时清理的终态要写到这条真实连接上。
        using (LoopbackGate gate = new LoopbackGate())
        {
            // role_id 是本用例独占的角色 ID，超时清理用例单独占一个。
            int role_id = 910005;
            // role 是装配完成并已加入在线索引的施法角色。
            OnlineRole role = CreateCastingRole(gate, role_id);
            if (role == null)
            {
                return Fail("在线角色装配失败");
            }
            // initial_mp 是超时前的权威 MP，超时清理同样不应扣蓝。
            int initial_mp = role.mainRoleInfo.BaseInfo.CurrMp;

            // 正式普攻没有读条；本用例用测试夹具准备合成吟唱，只验证打断或队列的终态回包。
            SingingStateFixture.Start(role, 10010, 71UL);
            // 第二个请求在吟唱期间入队，随后超过队列等待上限被清理。
            int queued_result = InvokeSkillCastHandler(gate, role, 10010, 72UL);
            if (queued_result != 0)
            {
                return queued_result;
            }
            // 队列超时使用真实 UTC 毫秒判定，这里必须真实等待超过上限再推进一帧。
            System.Threading.Thread.Sleep(QueueTimeoutWaitMilliseconds);
            AdvanceLogicFrames(role, 1);

            // timeout_package 是超时请求被清理后应收到的终态回包。
            BasePackage timeout_package;
            if (!gate.TryReadFrame(out timeout_package, 2000))
            {
                return Fail("超时被清理的请求未收到终态回包");
            }
            if (timeout_package.ProtoCode != NetDefine.CMD_SkillCastCode)
            {
                return Fail("超时终态应为施法结果，实际协议号 " + timeout_package.ProtoCode);
            }
            // timeout_ret 是超时终态结果，用于断言非成功码且回带超时请求的施法标识。
            SkillCastRet timeout_ret = SkillCastRet.Parser.ParseFrom(timeout_package.Data);
            if (timeout_ret.CmdCode == CmdCode.Succeed)
            {
                return Fail("超时被清理的请求不应回成功结果");
            }
            if (timeout_ret.SkillUuid != 72UL)
            {
                return Fail("超时终态应回带超时请求的施法标识 72，实际 " + timeout_ret.SkillUuid);
            }
            if (role.mainRoleInfo.BaseInfo.CurrMp != initial_mp)
            {
                return Fail("超时清理不应扣蓝，实际 MP " + role.mainRoleInfo.BaseInfo.CurrMp);
            }

            role.Dispose();
            RoleManager.Instance.RemoveOnlineRole(role_id);
            return 0;
        }
    }

    /// <summary>队列等待上限 300 毫秒，测试需要真实等待超过它才能触发清理。</summary>
    private const int QueueTimeoutWaitMilliseconds = 360;

    /// <summary>
    /// 按固定帧推进角色逻辑，驱动吟唱状态、技能动作态与后置阶段。
    /// 顺序与 GameGlobal.Update 一致：先推进逻辑 Tick，再驱动一帧角色逻辑。
    /// </summary>
    /// <param name="role">目标在线角色。</param>
    /// <param name="frames">要推进的逻辑帧数量。</param>
    private static void AdvanceLogicFrames(OnlineRole role, int frames)
    {
        // index 是本次推进的帧序号，从 0 数到 frames-1，每轮只推进一帧。
        for (int index = 0; index < frames; index++)
        {
            // next_tick 是下一逻辑帧的 Tick，先写回 GameGlobal 再驱动角色，顺序与正式帧一致。
            long next_tick = GameGlobal.Instance.LogicTick + 1L;
            LogicTickField.SetValue(GameGlobal.Instance, next_tick);
            role.Update(50);
        }
    }

    /// <summary>
    /// 按互斥校验通过后的真实事件格式派发一次客户端施法请求。
    /// 事件来自对象池并在派发后立即释放，与 SkillCastHandler 的持有方式一致。
    /// </summary>
    /// <param name="role">目标在线角色。</param>
    /// <param name="skill_id">技能主表 ID。</param>
    /// <param name="skill_uuid">本次施法标识。</param>
    private static void DispatchClientFireSkill(OnlineRole role, int skill_id, ulong skill_uuid)
    {
        // args 是从对象池取出的施法事件，finally 里立即归还，与 SkillCastHandler 的持有方式一致。
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
    /// 装配一个可施法的在线角色：会话标识、组件、已学技能快照与在线索引全部就位。
    /// </summary>
    /// <param name="gate">提供真实网关会话标识的环回通道。</param>
    /// <param name="role_id">角色 ID，用于在线索引与通知校验。</param>
    /// <returns>装配完成并已加入在线索引的角色；任一步失败返回 null。</returns>
    private static OnlineRole CreateCastingRole(LoopbackGate gate, int role_id)
    {
        OnlineRole role = new OnlineRole();
        role.mainRoleInfo = new MainRoleInfo();
        role.mainRoleInfo.BaseInfo = new RoleBaseInfo();
        role.mainRoleInfo.BaseInfo.RoleId = role_id;
        role.mainRoleInfo.BaseInfo.JobId = 1;
        role.mainRoleInfo.BaseInfo.CurrHp = 1000;
        role.mainRoleInfo.BaseInfo.MaxHp = 1000;
        role.mainRoleInfo.BaseInfo.CurrMp = 500;
        role.mainRoleInfo.BaseInfo.MaxMp = 500;
        // 会话标识必须与环回通道一致，回包才能写到本用例可读的一端。
        role.GateSessionId = gate.GateSessionId;
        role.UnitySessionId = gate.UnitySessionId;
        role.InitSerializes();
        role.InitializeSaveVersion(0L);
        role.CompleteRoleDataLoad();

        RoleSkillComponent skill_component;
        if (!role.TryGetComponent(out skill_component))
        {
            return null;
        }

        // 中心服技能列表只装载已学等级，与正式链路一致。
        RoleSkillInfoRet learned_skills = new RoleSkillInfoRet();
        learned_skills.CmdCode = CmdCode.Succeed;
        RoleSkillInfo learned_row = new RoleSkillInfo();
        learned_row.SkillId = 10010;
        learned_row.Level = 1;
        learned_skills.RoleSkillInfoList.Add(learned_row);
        if (!skill_component.LoadLearnedSkills(learned_skills))
        {
            return null;
        }

        // 在线索引是 OnCall 反查角色的唯一入口，必须先登记再发请求。
        RoleManager.Instance.AddOnlineRole(role_id, role);
        if (RoleManager.Instance.GetOnlineRoleBySession(role.GateSessionId, role.UnitySessionId) == null)
        {
            return null;
        }
        return role;
    }

    /// <summary>
    /// 按真实入站命令格式调用客户端施法入口，走 OnCall 而非直接派发事件。
    /// </summary>
    /// <param name="gate">提供真实网关会话与传输对象的环回通道。</param>
    /// <param name="role">已加入在线索引的施法角色。</param>
    /// <param name="skill_id">请求释放的技能主表 ID。</param>
    /// <param name="skill_uuid">本次施法的客户端标识。</param>
    /// <returns>入口调用成功返回 0，构造命令或会话解析失败返回 1。</returns>
    private static int InvokeSkillCastHandler(LoopbackGate gate, OnlineRole role, int skill_id, ulong skill_uuid)
    {
        SkillCastReq request = new SkillCastReq();
        request.SkillId = skill_id;
        request.SkillUuid = skill_uuid;

        BasePackage package = new BasePackage();
        package.ProtoCode = NetDefine.CMD_SkillCastCode;
        package.UnitySessionId = role.UnitySessionId;
        package.GateSessionId = role.GateSessionId;
        package.Data = request.ToByteString();

        // 命令信封与网络线程入队时一致，处理器据此还原协议包与会话。
        GameCommand command = GameCommand.From(GameCommandSource.GateServer, gate.Session, package);
        if (command == null)
        {
            return Fail("构造入站命令失败");
        }
        GameMessageHandlerContext context = new GameMessageHandlerContext(command);
        if (context.GateSession == null)
        {
            return Fail("入站命令未解析出网关会话");
        }
        new SkillCastHandler().OnCall(context);
        return 0;
    }

    /// <summary>
    /// 环回网关通道：在本机环回端口建立一对真实 TCP 连接，
    /// 一端作为会话的回包出口，另一端由测试按真实帧格式读取。
    /// </summary>
    private sealed class LoopbackGate : IDisposable
    {
        /// <summary>临时监听器，端口由系统分配，避免与正式服务端口冲突。</summary>
        private readonly TcpListener listener_;
        /// <summary>测试侧连接，用于读取服务端真实写出的协议帧。</summary>
        private readonly TcpClient peer_;
        /// <summary>交给业务的真实会话对象，Send 会真实写到本机对端。</summary>
        private readonly LoopbackSession session_;
        /// <summary>测试侧网络流，按两字节长度头读取帧体。</summary>
        private readonly NetworkStream stream_;

        /// <summary>建立环回连接并绑定会话。</summary>
        public LoopbackGate()
        {
            UnitySessionId = 990002;
            listener_ = new TcpListener(IPAddress.Loopback, 0);
            listener_.Start();
            peer_ = new TcpClient();
            peer_.Connect((IPEndPoint)listener_.LocalEndpoint);
            peer_.NoDelay = true;
            Socket accepted = listener_.AcceptSocket();
            session_ = new LoopbackSession();
            session_.AttachSocket(accepted);
            stream_ = peer_.GetStream();
        }

        /// <summary>会话在 SessionMgr 中的编号，作为协议包的网关会话标识。</summary>
        public int GateSessionId
        {
            get { return session_.SessionID; }
        }

        /// <summary>测试侧 Unity 会话标识，随回包原样写入协议包。</summary>
        public int UnitySessionId { get; private set; }

        /// <summary>提供给入站命令与角色回包共用的真实会话对象。</summary>
        public Session Session
        {
            get { return session_; }
        }

        /// <summary>读取一帧真实回包；超时或帧不完整返回 false。</summary>
        /// <param name="package">解析出的协议包。</param>
        /// <param name="timeout_milliseconds">本次读取的超时毫秒数。</param>
        /// <returns>读到并解析成功返回 true。</returns>
        public bool TryReadFrame(out BasePackage package, int timeout_milliseconds)
        {
            package = null;
            stream_.ReadTimeout = timeout_milliseconds;

            byte[] head = new byte[2];
            if (!TryReadFully(head, 2))
            {
                return false;
            }
            ushort body_length = BitConverter.ToUInt16(head, 0);
            if (body_length == 0)
            {
                return false;
            }
            byte[] body = new byte[body_length];
            if (!TryReadFully(body, body_length))
            {
                return false;
            }

            // ParseData 以长度头为基准偏移解析，必须把长度头与帧体拼接后再传入。
            byte[] frame_buffer = new byte[2 + body_length];
            Buffer.BlockCopy(head, 0, frame_buffer, 0, 2);
            Buffer.BlockCopy(body, 0, frame_buffer, 2, body_length);
            byte[] payload = NetUtils.Instance.ParseData(frame_buffer, body_length, 0);
            if (payload == null)
            {
                return false;
            }
            package = BasePackage.Parser.ParseFrom(payload);
            return true;
        }

        /// <summary>从网络流读满指定字节数，处理 TCP 拆包与超时。</summary>
        /// <param name="buffer">接收缓冲区。</param>
        /// <param name="count">需要读取的字节数。</param>
        /// <returns>读满返回 true；超时或对端关闭返回 false。</returns>
        private bool TryReadFully(byte[] buffer, int count)
        {
            int total = 0;
            try
            {
                while (total < count)
                {
                    int read = stream_.Read(buffer, total, count - total);
                    if (read <= 0)
                    {
                        return false;
                    }
                    total += read;
                }
                return true;
            }
            catch (IOException)
            {
                // 读超时属于预期路径：拒绝分支本就不应产生第二帧。
                return false;
            }
        }

        /// <summary>关闭测试侧连接、会话与监听器，避免残留句柄。</summary>
        public void Dispose()
        {
            SessionMgr.Instance.RemoveSession(session_.SessionID);
            if (stream_ != null)
            {
                stream_.Dispose();
            }
            if (peer_ != null)
            {
                peer_.Close();
            }
            if (listener_ != null)
            {
                listener_.Stop();
            }
        }
    }

    /// <summary>只用于测试的会话子类：允许直接绑定已建立的环回套接字。</summary>
    private sealed class LoopbackSession : Session
    {
        /// <summary>
        /// 按游戏服到网关的服务间链路构造环回会话：
        /// 上游连接器类型为 GameServer，使实时协议保持原 TCP 帧，与正式拓扑一致。
        /// </summary>
        public LoopbackSession()
            : base(new Dictionary<int, IContainer>(), new NetClient("127.0.0.1", 0, ClientType.GameServer))
        {
        }

        /// <summary>绑定已连接的套接字，使 Send 真实写到本机对端。</summary>
        /// <param name="socket">已建立的环回套接字。</param>
        public void AttachSocket(Socket socket)
        {
            _socket = socket;
        }
    }

    /// <summary>打印失败原因并返回失败码。</summary>
    /// <param name="message">失败说明。</param>
    private static int Fail(string message)
    {
        Console.WriteLine("EventDispatcherSmoke 失败: " + message);
        return 1;
    }
}
