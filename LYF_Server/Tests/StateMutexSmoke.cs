using System;
using System.Reflection;

/// <summary>
/// 状态互斥测试：覆盖真实互斥表取值、生产注册表为空时的即时放行、禁止切换、
/// 直接切换、先退旧状态、退旧失败、请求取消与组件销毁释放事件引用。
/// 所有测试态只在本进程内注册，不进入生产注册表。
/// 独立进程运行，不进入正式服务工程。
/// </summary>
public static class StateMutexSmoke
{
    /// <summary>
    /// StateTransferComponent.state_request_transfer_map_ 私有字段。
    /// 挂起请求数量是组件内部状态，正式代码不为此暴露测试入口，测试侧只在必要时反射读取，
    /// 边界仅限本测试进程。
    /// </summary>
    private static readonly FieldInfo PendingMapField = typeof(StateTransferComponent).GetField(
        "state_request_transfer_map_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>ROEvent.ref_count_ 私有字段，用于断言池化事件的所有权平衡。</summary>
    private static readonly FieldInfo RefCountField = typeof(ROEvent).GetField(
        "ref_count_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>读取状态切换组件当前挂起的请求数量。</summary>
    /// <param name="transfer">待读取的状态切换组件。</param>
    /// <returns>当前挂起请求数量。</returns>
    private static int PendingCount(StateTransferComponent transfer)
    {
        // map 是反射读到的挂起请求字典本身（未复制），只读取它的 Count 作为挂起数量。
        System.Collections.IDictionary map =
            (System.Collections.IDictionary)PendingMapField.GetValue(transfer);
        return map.Count;
    }

    /// <summary>读取事件的当前引用计数。</summary>
    /// <param name="evt">待读取的事件实例。</param>
    /// <returns>当前引用计数；正数是活跃引用数，零是已归零未入池，负数是已入池。</returns>
    private static int RefCount(ROEvent evt)
    {
        return (int)RefCountField.GetValue(evt);
    }

    /// <summary>
    /// StateMutexManager.states_ 私有数组：下标即状态类型数值，未注册的槽位保持空。
    /// 生产代码不为此保留计数属性，测试侧只在本进程内反射统计。
    /// </summary>
    private static readonly FieldInfo StatesField = typeof(StateMutexManager).GetField(
        "states_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>统计已注册的互斥状态数量。</summary>
    /// <param name="manager">待统计的互斥管理器。</param>
    /// <returns>注册表槽位中非空的数量。</returns>
    private static int RegisteredCount(StateMutexManager manager)
    {
        // states 是注册表槽位数组，下标对应 StateTransitionType 数值。
        IStateInterface[] states = (IStateInterface[])StatesField.GetValue(manager);
        // count 是已注册状态的累计槽位数，遍历结束后作为注册表规模返回。
        int count = 0;
        // index 是注册表槽位下标，逐个统计非空槽位。
        for (int index = 0; index < states.Length; index++)
        {
            if (states[index] != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 可配置的测试互斥状态，只在本测试进程内使用。
    /// 继承正式抽象基类 IStateInterface：状态标识由构造交给基类保存，
    /// 本类只重写 Check/Leave/GetErrorCode，与生产状态子类的写法一致。
    /// </summary>
    private sealed class ProbeState : IStateInterface
    {
        /// <summary>Check 返回值，模拟角色是否处于本状态。</summary>
        public bool in_state_;

        /// <summary>Leave 的返回值。</summary>
        public bool leave_result_ = true;

        /// <summary>Leave 成功后是否把 in_state_ 清掉，false 用于模拟"离开成功但仍处于该状态"。</summary>
        public bool leave_clears_state_ = true;

        /// <summary>拒绝时返回的错误码。</summary>
        public CmdCode error_ = CmdCode.SkillStateBlocked;

        /// <summary>Leave 被调用的次数。</summary>
        public int leave_count_;

        /// <summary>
        /// 构造探针状态，把本状态要模拟的互斥状态标识交给基类保存。
        /// 改为构造传参后不再保留可随意改写的 type_ 字段，避免实例注册后标识还能被改掉、
        /// 导致注册表下标与状态自述不一致。
        /// </summary>
        /// <param name="type">本探针状态模拟的互斥状态标识。</param>
        public ProbeState(StateTransitionType type) : base(type)
        {
        }

        /// <summary>返回当前是否处于本状态，取值来自 in_state_。</summary>
        /// <param name="role">待检查的在线角色，本探针不使用。</param>
        /// <returns>in_state_ 为 true 时返回 true。</returns>
        public override bool Check(OnlineRole role)
        {
            return in_state_;
        }

        /// <summary>离开本状态，累加调用次数并按配置决定是否清掉 in_state_。</summary>
        /// <param name="role">待离开状态的在线角色，本探针不使用。</param>
        /// <returns>leave_result_ 的取值。</returns>
        public override bool Leave(OnlineRole role)
        {
            leave_count_++;
            if (leave_result_ && leave_clears_state_)
            {
                in_state_ = false;
            }
            return leave_result_;
        }

        /// <summary>返回拒绝时使用的错误码，取值来自 error_。</summary>
        /// <returns>error_ 的取值。</returns>
        public override CmdCode GetErrorCode()
        {
            return error_;
        }
    }

    /// <summary>
    /// 只实现 GetErrorCode、不重写 Check/Leave 的派生状态，用于验证基类默认实现。
    /// 基类的 Check/Leave 默认返回 false，表示本状态不做判定、也不提供离开动作。
    /// </summary>
    private sealed class DefaultBehaviourState : IStateInterface
    {
        /// <summary>本状态拒绝切换时回的错误码。</summary>
        private readonly CmdCode error_;

        /// <summary>构造默认行为状态，把状态标识与错误码固定下来。</summary>
        /// <param name="type">本状态对应的互斥状态标识。</param>
        /// <param name="error">拒绝切换时回的错误码。</param>
        public DefaultBehaviourState(StateTransitionType type, CmdCode error) : base(type)
        {
            error_ = error;
        }

        /// <summary>返回构造时给定的错误码。</summary>
        /// <returns>拒绝切换时使用的错误码。</returns>
        public override CmdCode GetErrorCode()
        {
            return error_;
        }
    }

    /// <summary>测试入口：全部用例通过返回 0，任一失败返回 1 并打印原因。</summary>
    /// <param name="args">命令行参数；本测试不接收参数。</param>
    public static int Main(string[] args)
    {
        try
        {
            // manager 是全局互斥管理器单例，测试入口先让它装载真实互斥表。
            StateMutexManager manager = StateMutexManager.Instance;
            if (!manager.Init())
            {
                return Fail("状态互斥表装载失败");
            }
            // result 承接当前用例返回码，非零即该用例失败，后续用例不再执行。
            int result = CheckBaseContract();
            if (result != 0)
            {
                return result;
            }
            result = CheckRealTableValues();
            if (result != 0)
            {
                return result;
            }
            result = CheckProductionRegistryIsEmpty();
            if (result != 0)
            {
                return result;
            }
            result = CheckForbidTransition();
            if (result != 0)
            {
                return result;
            }
            result = CheckDirectTransition();
            if (result != 0)
            {
                return result;
            }
            result = CheckNeedLeaveOldState();
            if (result != 0)
            {
                return result;
            }
            result = CheckLeaveFailure();
            if (result != 0)
            {
                return result;
            }
            result = CheckCancelTransferState();
            if (result != 0)
            {
                return result;
            }
            result = CheckDisposeReleasesHeldEvent();
            if (result != 0)
            {
                return result;
            }
            result = CheckCallbackExceptionReleasesHeldEvent();
            if (result != 0)
            {
                return result;
            }
            Console.WriteLine("StateMutexSmoke: all cases passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("StateMutexSmoke 异常: " + ex);
            return 1;
        }
    }

    /// <summary>
    /// 用例零：抽象基类的继承契约。
    /// 覆盖三件事：状态标识由构造交给基类保存、基类 Check/Leave 默认返回 false、
    /// 经基类引用调用时 override 会派发到派生实现。不涉及互斥表与管理器。
    /// </summary>
    private static int CheckBaseContract()
    {
        // probe 是构造时就带上状态标识的探针态，用来确认标识由基类保存，而不是派生类上可改写的字段。
        ProbeState probe = new ProbeState(StateTransitionType.kSingleAction);
        if (probe.GetType() != StateTransitionType.kSingleAction)
        {
            return Fail("基类构造未保存状态标识，实际 " + probe.GetType());
        }
        // base_ref 是以基类类型持有的同一实例，用来验证 GetType 经基类引用读回一致。
        IStateInterface base_ref = probe;
        if (base_ref.GetType() != StateTransitionType.kSingleAction)
        {
            return Fail("基类引用读回的状态标识不一致，实际 " + base_ref.GetType());
        }

        // default_state 只实现 GetErrorCode，用来验证基类 Check/Leave 的默认实现。
        DefaultBehaviourState default_state = new DefaultBehaviourState(
            StateTransitionType.kCollectionAction, CmdCode.SkillStateBlocked);
        if (default_state.Check(null))
        {
            return Fail("基类 Check 默认应返回 false");
        }
        if (default_state.Leave(null))
        {
            return Fail("基类 Leave 默认应返回 false");
        }
        if (default_state.GetErrorCode() != CmdCode.SkillStateBlocked)
        {
            return Fail("派生状态未返回自己的错误码，实际 " + default_state.GetErrorCode());
        }

        // 经基类引用调用 Check/Leave，必须派发到 ProbeState 的 override。
        probe.in_state_ = true;
        if (!base_ref.Check(null))
        {
            return Fail("基类引用调用 Check 未派发到派生实现");
        }
        if (!base_ref.Leave(null))
        {
            return Fail("基类引用调用 Leave 未派发到派生实现");
        }
        if (probe.leave_count_ != 1)
        {
            return Fail("派生 Leave 应被调用一次，实际 " + probe.leave_count_);
        }
        return 0;
    }

    /// <summary>用例一：互斥表必须是 RO 原始数据的直接结果，吟唱列取值逐条对上。</summary>
    private static int CheckRealTableValues()
    {
        // table 是已装载的互斥表单例，本用例只读它的行数与取值，不做任何写入。
        StateExclusionTable table = StateExclusionTable.Instance;
        if (!table.IsLoaded)
        {
            return Fail("互斥表未装载");
        }
        if (table.StateCount != 38)
        {
            return Fail("互斥表状态行数应为 38，实际 " + table.StateCount);
        }
        if (table.GetStateName(StateTransitionType.kStateTransitionRequestSinging) != "State_AJ_StateSinging")
        {
            return Fail("第 35 行应为 State_AJ_StateSinging，实际 " +
                table.GetStateName(StateTransitionType.kStateTransitionRequestSinging));
        }

        // 跳舞到吟唱为 0 禁止；单人动作到吟唱为 1 直接允许；自动采集到吟唱为 2 先离开旧状态。
        if (!ExpectValue(StateTransitionType.kStateTransitionDance, StateValueType.kForbidTransition))
        {
            return Fail("跳舞 -> 吟唱 取值不符，期望 0 禁止");
        }
        if (!ExpectValue(StateTransitionType.kSingleAction, StateValueType.kCanTransition))
        {
            return Fail("单人动作 -> 吟唱 取值不符，期望 1 直接切换");
        }
        if (!ExpectValue(StateTransitionType.kStateTransitionAutoCollect, StateValueType.kNeedCodeTransition))
        {
            return Fail("自动采集 -> 吟唱 取值不符，期望 2 先退旧状态");
        }
        // 吟唱自身对吟唱未配置，与 RO 表一致。
        if (!ExpectValue(StateTransitionType.kStateTransitionRequestSinging, StateValueType.kMutexStateNone))
        {
            return Fail("吟唱 -> 吟唱 应为 -1 未配置");
        }
        return 0;
    }

    /// <summary>断言某个源状态到吟唱的真实取值。</summary>
    /// <param name="from">源状态。</param>
    /// <param name="expected">期望取值。</param>
    /// <returns>取值一致返回 true。</returns>
    private static bool ExpectValue(StateTransitionType from, StateValueType expected)
    {
        // actual 是互斥表里「源状态 → 吟唱」的真实取值，与期望值比对后决定断言结果。
        StateValueType actual = StateExclusionTable.Instance.GetStateValue(
            from, StateTransitionType.kStateTransitionRequestSinging);
        if (actual != expected)
        {
            LogMsg.Info("互斥表取值不符：" + from + " -> 吟唱 期望 " + expected + "，实际 " + actual,
                LogMsgType.Error);
            return false;
        }
        return true;
    }

    /// <summary>用例二：生产注册表为空，切换即时放行且不产生异步令牌。</summary>
    private static int CheckProductionRegistryIsEmpty()
    {
        // manager 是全局互斥管理器单例，本用例在不注册任何状态的前提下检查空表行为。
        StateMutexManager manager = StateMutexManager.Instance;
        // registered 是当前注册表里非空槽位数量，生产代码不注册任何 RO 特殊状态，应为零。
        int registered = RegisteredCount(manager);
        if (registered != 0)
        {
            return Fail("生产注册表应为空，实际 " + registered);
        }
        // role 是本用例角色（ID 920001），只用来承载状态切换请求。
        OnlineRole role = CreateRole(920001);
        // callback_count 是最终回调被触发的次数，空表路径应恰好一次。
        int callback_count = 0;
        // last_code 预置成一个不可能成功的码，用来确认回调确实被写入过结果。
        CmdCode last_code = CmdCode.ReqParamError;
        // token 接收异步请求令牌，零表示同步完成、未挂起。
        ulong token;
        // ret 是切换入口的即时返回码，成功时再与回调结果互相印证。
        CmdCode ret = manager.TransferStateByFunc(role,
            StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是互斥框架给出的本次切换结果码，这里只做记录供后面断言。
            delegate(CmdCode code) { callback_count++; last_code = code; }, out token);

        if (ret != CmdCode.Succeed || callback_count != 1 || last_code != CmdCode.Succeed)
        {
            return Fail("空注册表下切换应即时成功，返回 " + ret + "，回调次数 " + callback_count);
        }
        if (token != 0UL)
        {
            return Fail("即时放行不应产生异步令牌，实际 " + token);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例三：真实表中"0 禁止"的源状态会拒绝切换，回调恰好一次。</summary>
    private static int CheckForbidTransition()
    {
        // manager 是全局互斥管理器单例，本用例先清空再重装，保证注册表只有本用例的探针态。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // dance 是本用例伪造的跳舞态，真实表里「跳舞 → 吟唱」为 0 禁止。
        ProbeState dance = new ProbeState(StateTransitionType.kStateTransitionDance);
        dance.in_state_ = true;
        if (!manager.RegisterState(dance))
        {
            return Fail("测试态注册失败");
        }

        // role 是本用例角色（ID 920002），用来发起被禁止的吟唱切换。
        OnlineRole role = CreateRole(920002);
        // callback_count 是最终回调次数，拒绝分支也必须恰好一次。
        int callback_count = 0;
        // last_code 预置成成功码，回调写入拒绝码后才说明拒绝链路真的走到了回调。
        CmdCode last_code = CmdCode.Succeed;
        // token 接收异步请求令牌，禁止切换分支不应产生令牌。
        ulong token;
        // ret 是切换入口的即时返回码，本用例期望直接是 SkillStateBlocked。
        CmdCode ret = manager.TransferStateByFunc(role,
            StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是互斥框架给出的本次切换结果码，这里只做记录供后面断言。
            delegate(CmdCode code) { callback_count++; last_code = code; }, out token);

        if (ret != CmdCode.SkillStateBlocked || last_code != CmdCode.SkillStateBlocked)
        {
            return Fail("处于跳舞状态时应禁止吟唱，返回 " + ret + "，回调 " + last_code);
        }
        if (callback_count != 1)
        {
            return Fail("拒绝分支回调应恰好一次，实际 " + callback_count);
        }
        if (token != 0UL)
        {
            return Fail("拒绝分支不应产生异步令牌，实际 " + token);
        }
        if (dance.leave_count_ != 0)
        {
            return Fail("禁止切换不应调用 Leave");
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例四：真实表中"1 直接切换"的源状态放行，且不需要退旧状态。</summary>
    private static int CheckDirectTransition()
    {
        // manager 是全局互斥管理器单例，本用例重装后再注册一个「直接切换」的探针态。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // collection 变量实际构造的是单人动作状态，用于检查到吟唱的直接允许分支。
        ProbeState collection = new ProbeState(StateTransitionType.kSingleAction);
        collection.in_state_ = true;
        manager.RegisterState(collection);

        // role 是本用例角色（ID 920003），用来发起直接放行的吟唱切换。
        OnlineRole role = CreateRole(920003);
        // callback_count 是最终回调次数，直接切换同样必须恰好一次。
        int callback_count = 0;
        // last_code 预置成失败码，回调写入成功码才说明放行链路完整。
        CmdCode last_code = CmdCode.ReqParamError;
        // token 接收异步请求令牌，直接切换不经过挂起，应为零。
        ulong token;
        // ret 是切换入口的即时返回码，本用例期望 Succeed。
        CmdCode ret = manager.TransferStateByFunc(role,
            StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是互斥框架给出的本次切换结果码，这里只做记录供后面断言。
            delegate(CmdCode code) { callback_count++; last_code = code; }, out token);

        if (ret != CmdCode.Succeed || last_code != CmdCode.Succeed || callback_count != 1)
        {
            return Fail("直接切换应放行，返回 " + ret + "，回调 " + last_code + "，次数 " + callback_count);
        }
        if (collection.leave_count_ != 0)
        {
            return Fail("直接切换不应调用 Leave，实际 " + collection.leave_count_);
        }
        if (token != 0UL)
        {
            return Fail("直接切换不应产生异步令牌，实际 " + token);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例五：真实表中"2 先退旧状态"会先离开旧状态，再回调成功。</summary>
    private static int CheckNeedLeaveOldState()
    {
        // manager 是全局互斥管理器单例，本用例重装后注册一个「先退旧状态」的探针态。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // single_action 是本用例伪造的自动采集态，真实表里它对吟唱是 2 先退旧状态。
        ProbeState single_action = new ProbeState(StateTransitionType.kStateTransitionAutoCollect);
        single_action.in_state_ = true;
        manager.RegisterState(single_action);

        // role 是本用例角色（ID 920004），用来发起需要先退旧状态的吟唱切换。
        OnlineRole role = CreateRole(920004);
        // callback_count 是退旧成功后的回调次数，应恰好一次。
        int callback_count = 0;
        // last_code 预置成失败码，回调写入成功码才说明退旧链路走通。
        CmdCode last_code = CmdCode.ReqParamError;
        // token 接收异步请求令牌，退旧走异步挂起，应为非零值。
        ulong token;
        // ret 是切换入口的即时返回码，只要受理就应返回 Succeed。
        CmdCode ret = manager.TransferStateByFunc(role,
            StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是互斥框架给出的本次切换结果码，这里只做记录供后面断言。
            delegate(CmdCode code) { callback_count++; last_code = code; }, out token);

        if (ret != CmdCode.Succeed)
        {
            return Fail("先退旧状态应受理请求，实际返回 " + ret);
        }
        if (single_action.leave_count_ != 1)
        {
            return Fail("应先离开单人动作状态一次，实际 " + single_action.leave_count_);
        }
        if (callback_count != 1 || last_code != CmdCode.Succeed)
        {
            return Fail("退旧状态后应回调成功，实际回调 " + callback_count + " 次，结果 " + last_code);
        }
        if (token == 0UL)
        {
            return Fail("先退旧状态应产生异步请求令牌");
        }
        // transfer 接收角色身上的状态切换组件，用于确认请求完成后不残留挂起记录。
        StateTransferComponent transfer;
        if (!role.TryGetComponent(out transfer) || PendingCount(transfer) != 0)
        {
            return Fail("完成的请求不应残留在切换组件里");
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例六：旧状态离开失败时按失败进入，回调拿到拒绝码而不是永久挂起。</summary>
    private static int CheckLeaveFailure()
    {
        // manager 是全局互斥管理器单例，本用例构造一个「离开失败」的旧状态。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // single_action 是 leave_result_ 为假的探针态，用来模拟旧状态离开失败。
        ProbeState single_action = new ProbeState(StateTransitionType.kStateTransitionAutoCollect);
        single_action.in_state_ = true;
        single_action.leave_result_ = false;
        manager.RegisterState(single_action);

        // role 是本用例角色（ID 920005），用来验证离开失败时回调拿到拒绝码。
        OnlineRole role = CreateRole(920005);
        // callback_count 是失败路径的回调次数，应恰好一次。
        int callback_count = 0;
        // last_code 预置成成功码，回调写入拒绝码后才说明失败链路正确。
        CmdCode last_code = CmdCode.Succeed;
        // token 接收异步请求令牌，本用例只关心它被写出，不校验取值。
        ulong token;
        // ret 是切换入口的即时返回码，请求被受理即返回 Succeed，失败在回调里体现。
        CmdCode ret = manager.TransferStateByFunc(role,
            StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是互斥框架给出的本次切换结果码，这里只做记录供后面断言。
            delegate(CmdCode code) { callback_count++; last_code = code; }, out token);

        if (ret != CmdCode.Succeed)
        {
            return Fail("请求应先被受理，实际返回 " + ret);
        }
        if (callback_count != 1 || last_code != CmdCode.SkillStateBlocked)
        {
            return Fail("退旧失败应回调拒绝码，实际回调 " + callback_count + " 次，结果 " + last_code);
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例七：请求挂起后可被取消，取消后回调不再触发。</summary>
    private static int CheckCancelTransferState()
    {
        // manager 是全局互斥管理器单例，本用例构造一个永远等不到离开的旧状态。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // single_action 是 leave_clears_state_ 为假的探针态，使请求长期挂起以验证取消。
        ProbeState single_action = new ProbeState(StateTransitionType.kStateTransitionAutoCollect);
        single_action.in_state_ = true;
        // 离开成功但仍处于该状态：请求会一直等待 OnLeaveState，用于验证取消语义。
        single_action.leave_clears_state_ = false;
        manager.RegisterState(single_action);

        // role 是本用例角色（ID 920006），用来承载被取消的挂起请求。
        OnlineRole role = CreateRole(920006);
        // transfer 接收状态切换组件，用反射读它的挂起请求数量。
        StateTransferComponent transfer;
        if (!role.TryGetComponent(out transfer))
        {
            return Fail("角色未注册状态切换组件");
        }
        // callback_count 是被取消请求的回调次数，取消后必须保持为零。
        int callback_count = 0;
        // token 接收挂起请求令牌，取消时要用同一个令牌定位请求。
        ulong token;
        manager.TransferStateByFunc(role, StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是若请求完成时传入的结果码；本用例取消请求，期望回调始终不执行。
            delegate(CmdCode code) { callback_count++; }, out token);

        if (token == 0UL || PendingCount(transfer) != 1)
        {
            return Fail("请求应处于挂起状态，令牌 " + token + "，挂起数 " + PendingCount(transfer));
        }
        manager.CancelTransferState(role, token);
        if (PendingCount(transfer) != 0)
        {
            return Fail("取消后不应残留挂起请求，实际 " + PendingCount(transfer));
        }
        if (callback_count != 0)
        {
            return Fail("取消后回调不应触发，实际触发 " + callback_count + " 次");
        }
        role.Dispose();
        return 0;
    }

    /// <summary>用例八：组件销毁必须释放挂起请求持有的池化事件引用。</summary>
    private static int CheckDisposeReleasesHeldEvent()
    {
        // manager 是全局互斥管理器单例，本用例验证销毁时是否释放挂起请求持有的事件引用。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // single_action 是 leave_clears_state_ 为假的探针态，让请求挂起并持有事件引用。
        ProbeState single_action = new ProbeState(StateTransitionType.kStateTransitionAutoCollect);
        single_action.in_state_ = true;
        single_action.leave_clears_state_ = false;
        manager.RegisterState(single_action);

        // role 是本用例角色（ID 920007），销毁它来触发挂起请求的清理。
        OnlineRole role = CreateRole(920007);
        // held 是从池中取用的施法请求事件，调用方先持有一份引用，再交给挂起请求额外持有一份。
        ROClientFireSkillArgs held = ROEventPool<ROClientFireSkillArgs>.Instance.Get();
        held.skill_id_ = 10010;
        held.skill_uuid_ = 1UL;

        // callback_count 是销毁前的回调次数，销毁不应触发任何回调，应保持为零。
        int callback_count = 0;
        // token 接收挂起请求令牌，为零说明请求没有真正挂起、用例前置失败。
        ulong token;
        manager.TransferStateByFunc(role, StateTransitionType.kStateTransitionRequestSinging, true,
            // code 是组件释放时给出的结果码，本用例只关心回调次数。
            held, delegate(CmdCode code) { callback_count++; }, out token);
        if (token == 0UL)
        {
            return Fail("请求应处于挂起状态");
        }
        if (RefCount(held) != 2)
        {
            return Fail("挂起请求应额外持有一个事件引用，实际计数 " + RefCount(held));
        }
        // 调用方交还自己的引用，剩下的一份由挂起请求持有。
        held.Release();
        if (RefCount(held) != 1)
        {
            return Fail("交还调用方引用后应只剩请求持有的一份，实际 " + RefCount(held));
        }

        role.Dispose();
        if (RefCount(held) >= 0)
        {
            return Fail("组件销毁后事件引用应归零并入池，实际 " + RefCount(held));
        }
        if (callback_count != 0)
        {
            return Fail("销毁时不应触发回调，实际 " + callback_count + " 次");
        }
        return 0;
    }

    /// <summary>
    /// 用例九：最终回调抛出业务异常时，异常必须继续向上抛，同时请求持有的事件引用必须归还。
    /// 回调之后释放引用被 try/finally 包住，因此异常路径不会把池化事件永久占住。
    /// </summary>
    private static int CheckCallbackExceptionReleasesHeldEvent()
    {
        // manager 是全局互斥管理器单例，本用例验证回调抛异常时事件引用仍被归还。
        StateMutexManager manager = StateMutexManager.Instance;
        manager.UnInit();
        manager.Init();

        // single_action 是本用例的自动采集探针态，让挂起请求先持有一份事件引用。
        ProbeState single_action = new ProbeState(StateTransitionType.kStateTransitionAutoCollect);
        single_action.in_state_ = true;
        manager.RegisterState(single_action);

        // role 是本用例角色（ID 920008），用来承载最终回调抛异常的那次切换。
        OnlineRole role = CreateRole(920008);
        // transfer 接收状态切换组件，用于确认异常路径上不残留挂起记录。
        StateTransferComponent transfer;
        if (!role.TryGetComponent(out transfer))
        {
            return Fail("角色未注册状态切换组件");
        }
        // held 是被异常路径检验的池化事件，调用方持有一份引用，挂起请求再持有一份。
        ROClientFireSkillArgs held = ROEventPool<ROClientFireSkillArgs>.Instance.Get();
        held.skill_id_ = 10010;
        held.skill_uuid_ = 8UL;
        if (RefCount(held) != 1)
        {
            return Fail("取用后调用方应持有唯一引用，实际 " + RefCount(held));
        }

        // thrown 记录回调异常是否真的抛到调用方；只有回调自己抛的异常类型才算通过。
        bool thrown = false;
        // token 接收挂起请求令牌，异常路径上同样要能被正确摘除。
        ulong token;
        try
        {
            manager.TransferStateByFunc(role, StateTransitionType.kStateTransitionRequestSinging, true,
                // code 是结果码；本用例故意在回调里抛异常，验证池化事件仍然被释放。
                held, delegate(CmdCode code) { throw new InvalidOperationException("测试回调异常"); },
                out token);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }
        if (!thrown)
        {
            return Fail("回调抛出的异常应原样上抛，不能被吞掉或包装");
        }

        // 请求在回调前已摘除，异常路径上也不应残留挂起记录。
        if (PendingCount(transfer) != 0)
        {
            return Fail("回调抛异常后不应残留挂起请求，实际 " + PendingCount(transfer));
        }
        // 关键断言：请求持有的那一份引用已由 finally 归还，只剩调用方自己的一份。
        if (RefCount(held) != 1)
        {
            return Fail("回调抛异常后事件引用未归还，实际计数 " + RefCount(held));
        }
        held.Release();
        if (RefCount(held) >= 0)
        {
            return Fail("调用方归还后事件应入池，实际计数 " + RefCount(held));
        }
        role.Dispose();
        return 0;
    }

    /// <summary>构造一个已初始化组件的在线角色，不加入在线索引。</summary>
    /// <param name="role_id">角色 ID。</param>
    /// <returns>已完成组件初始化的在线角色。</returns>
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
        return role;
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
