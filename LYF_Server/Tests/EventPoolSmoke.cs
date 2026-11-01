using System;
using System.Reflection;

/// <summary>
/// 实体事件对象池测试：覆盖实例复用、归还即清业务引用、令牌递增、引用计数生命周期、
/// 同实例嵌套派发、异步持有期间不提前复用与不提前重置、池归还入口对活跃实例与重复归还的拒绝，
/// 以及用当前线程累计分配字节实测"池化取用/派发/归还"相对同等 new 的分配差异。
/// 独立进程运行，不进入正式服务工程。
/// </summary>
public static class EventPoolSmoke
{
    /// <summary>分配测量循环次数，暖机后每段采样两万次。</summary>
    private const int AllocationLoopCount = 20000;

    /// <summary>暖机循环次数，把 JIT、池容量与派发缓冲的一次性开销排除在采样之外。</summary>
    private const int WarmUpLoopCount = 2000;

    /// <summary>
    /// ROEvent.ref_count_ 私有字段。引用计数是池化所有权的核心状态，
    /// 正式代码不为此暴露测试入口，测试侧只在必要时反射读取，边界仅限本测试进程。
    /// </summary>
    private static readonly FieldInfo RefCountField =
        typeof(ROEvent).GetField("ref_count_", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// GC.GetAllocatedBytesForCurrentThread 的缓存委托；解析一次后反复调用，
    /// 避免每次反射 Invoke 自己在采样区间里产生分配而污染读数。
    /// </summary>
    private static Func<long> allocated_bytes_reader_;

    /// <summary>
    /// 探针事件使用的具名载荷。用强类型代替 object，
    /// 保证测试里的"业务引用"也是具体类型，而不是可以被任意替换的弱类型字段。
    /// </summary>
    private sealed class ProbePayload
    {
        /// <summary>载荷名，用于失败信息定位与"归还后引用已清空"的说明。</summary>
        public string name_;

        /// <summary>载荷序号，用于区分不同次数创建的载荷实例。</summary>
        public int sequence_;
    }

    /// <summary>
    /// 测试专用池化事件：带一个具名强类型载荷与构造计数。
    /// 用它证明"归还瞬间就把业务引用清掉"，同时统计暖机后池路径是否还在新建实例。
    /// </summary>
    private sealed class ProbeEvent : ROEvent
    {
        /// <summary>本类型累计构造次数，只在测试进程内统计，正式代码没有任何计数器。</summary>
        public static int created_count_;

        /// <summary>具名强类型载荷，用于验证归还后池中实例不再持有业务对象。</summary>
        public ProbePayload payload_;

        /// <summary>构造探针事件并累计构造次数。</summary>
        public ProbeEvent()
            : base(ROEventDefine.kActionSkill)
        {
            created_count_++;
        }

        /// <summary>清空载荷并回填事件类型。</summary>
        protected override void Reset()
        {
            payload_ = null;
            SetEventType(ROEventDefine.kActionSkill);
        }

        /// <summary>把实例归还到探针事件对象池。</summary>
        protected override void Recycle()
        {
            ROEventPool<ProbeEvent>.Instance.Release(this);
        }
    }

    /// <summary>记录回调次数并在需要时再次派发的探针监听者。</summary>
    private sealed class NestedListener : IROEventListener
    {
        /// <summary>本探针收到的回调次数。</summary>
        public int count_;

        /// <summary>剩余可再嵌套派发的次数。</summary>
        public int remaining_nesting_;

        /// <summary>派发器引用，嵌套时复用同一个派发器。</summary>
        public ROEventDispatcher dispatcher_;

        /// <summary>统计一次回调，并在配额内对同一事件实例再派发一次。</summary>
        /// <param name="evt">派发器传入的事件对象。</param>
        public void OnEvent(ROEvent evt)
        {
            count_++;
            if (remaining_nesting_ <= 0 || dispatcher_ == null)
            {
                return;
            }
            remaining_nesting_--;
            // 同一个事件实例在派发途中再次派发，验证引用计数能挡住提前回收。
            dispatcher_.DispatcherEvent(evt);
        }
    }

    /// <summary>只统计回调次数的监听者，供分配测量使用，回调体本身不分配。</summary>
    private sealed class CountingListener : IROEventListener
    {
        /// <summary>累计回调次数。</summary>
        public int count_;

        /// <summary>只累加计数，不做任何分配。</summary>
        /// <param name="evt">派发器传入的事件对象。</param>
        public void OnEvent(ROEvent evt)
        {
            count_++;
        }
    }

    /// <summary>测试入口：全部用例通过返回 0，任一失败返回 1 并打印原因。</summary>
    /// <param name="args">命令行参数；本测试不接收参数。</param>
    public static int Main(string[] args)
    {
        try
        {
            if (RefCountField == null)
            {
                return Fail("未找到 ROEvent.ref_count_ 字段，无法验证引用计数");
            }
            // result 承接当前用例的返回码，非零即代表该用例断言失败，后面不再继续跑。
            int result = CheckReuseAndFieldReset();
            if (result != 0)
            {
                return result;
            }
            result = CheckTokenIncreases();
            if (result != 0)
            {
                return result;
            }
            result = CheckRefCountLifecycle();
            if (result != 0)
            {
                return result;
            }
            result = CheckNestedDispatchSameInstance();
            if (result != 0)
            {
                return result;
            }
            result = CheckAsyncHolderPreventsEarlyReuse();
            if (result != 0)
            {
                return result;
            }
            result = CheckDuplicateReturnPoolsOnce();
            if (result != 0)
            {
                return result;
            }
            result = CheckActiveInstanceRejectedByPool();
            if (result != 0)
            {
                return result;
            }
            result = CheckPayloadClearedOnReturn();
            if (result != 0)
            {
                return result;
            }
            result = CheckAllocationOnHotPath();
            if (result != 0)
            {
                return result;
            }
            Console.WriteLine("EventPoolSmoke: all cases passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("EventPoolSmoke 异常: " + ex);
            return 1;
        }
    }

    /// <summary>读取事件的当前引用计数，用于断言所有权平衡。</summary>
    /// <param name="evt">待读取的事件实例。</param>
    /// <returns>当前引用计数；正数是活跃引用数，零是已归零未入池，负数是已入池。</returns>
    private static int ReadRefCount(ROEvent evt)
    {
        return (int)RefCountField.GetValue(evt);
    }

    /// <summary>用例一：归还后再次取用必须复用同一实例，且业务字段被清空。</summary>
    private static int CheckReuseAndFieldReset()
    {
        // pool 是吟唱事件类型的全局单例池，本用例的取用与归还全都走它，不做任何直接 new。
        ROEventPool<ROSingingArgs> pool = ROEventPool<ROSingingArgs>.Instance;
        // 先预热一轮，保证池里已有一个可复用实例，避免把首次新建算进复用断言。
        ROSingingArgs warm = pool.Get();
        warm.Release();

        // first 是首次取用的实例，写入字段后再归还，用于验证复用与字段清空。
        ROSingingArgs first = pool.Get();
        first.skill_id_ = 4321;
        first.skill_uuid_ = 99UL;
        first.target_ = 77UL;
        first.cast_point_ = new ROVector3(1f, 2f, 3f);
        if (!first.Release())
        {
            return Fail("首次释放未把引用降为零");
        }

        // second 是归还后再次取用的实例，按断言应与 first 引用同一对象。
        ROSingingArgs second = pool.Get();
        if (!ReferenceEquals(first, second))
        {
            return Fail("归还后再次取用未复用同一实例");
        }
        // 布尔判断按字段分组拆开，每组最多三个条件，失败信息能直接指出是哪组字段没清。
        if (second.skill_id_ != 0 || second.skill_uuid_ != 0UL)
        {
            return Fail("复用实例未清空技能标识");
        }
        if (second.target_ != 0UL)
        {
            return Fail("复用实例未清空目标 UID");
        }
        if (second.cast_point_.x_ != 0f || second.cast_point_.y_ != 0f || second.cast_point_.z_ != 0f)
        {
            return Fail("复用实例未清空施法点坐标");
        }
        if (second.GetEventType() != ROEventDefine.kActionSinging)
        {
            return Fail("复用实例的事件类型未回填，实际 " + second.GetEventType());
        }
        second.Release();
        return 0;
    }

    /// <summary>用例二：每次取用都换发新的令牌，令牌单调递增。</summary>
    private static int CheckTokenIncreases()
    {
        // pool 是攻击动作事件类型的单例池，令牌断言只看这个池发出的实例。
        ROEventPool<ROAttackActionArgs> pool = ROEventPool<ROAttackActionArgs>.Instance;
        // first 是第一次取用的攻击动作事件，取完令牌立即归还。
        ROAttackActionArgs first = pool.Get();
        // first_token 是第一次取用时分发的令牌值，作为递增断言的基数。
        ulong first_token = first.GetToken();
        first.Release();
        // second 是归还后重新取用的实例，令牌必须比 first_token 大。
        ROAttackActionArgs second = pool.Get();
        // second_token 是复用后换发的新令牌值，用于确认令牌单调递增。
        ulong second_token = second.GetToken();
        second.Release();
        if (first_token == 0UL)
        {
            return Fail("首次取用的令牌不应为零");
        }
        if (second_token <= first_token)
        {
            return Fail("复用后令牌未递增：前 " + first_token + "，后 " + second_token);
        }
        return 0;
    }

    /// <summary>
    /// 用例三：引用计数生命周期。取用为一，AddRef 后为二，最后一个引用释放时入池，
    /// 入池后计数落到池内哨兵（负值），此后既不允许 AddRef 也不允许再次 Release。
    /// </summary>
    private static int CheckRefCountLifecycle()
    {
        // pool 是攻击动作事件类型的单例池，本用例逐档检查引用计数的每个状态。
        ROEventPool<ROAttackActionArgs> pool = ROEventPool<ROAttackActionArgs>.Instance;
        // evt 是被全程跟踪引用计数的事件实例，从取用一路跟到入池哨兵。
        ROAttackActionArgs evt = pool.Get();
        if (ReadRefCount(evt) != 1)
        {
            return Fail("取用后引用计数应为 1，实际 " + ReadRefCount(evt));
        }
        if (!evt.AddRef() || ReadRefCount(evt) != 2)
        {
            return Fail("AddRef 后引用计数应为 2");
        }
        if (evt.Release())
        {
            return Fail("仍有引用时不应回池");
        }
        if (!evt.Release())
        {
            return Fail("最后一个引用释放时应回池");
        }
        if (ReadRefCount(evt) >= 0)
        {
            return Fail("入池后引用计数应为池内哨兵（负值），实际 " + ReadRefCount(evt));
        }
        if (evt.AddRef())
        {
            return Fail("已回池实例不应允许 AddRef");
        }
        if (evt.Release())
        {
            return Fail("已回池实例不应允许重复 Release");
        }
        return 0;
    }

    /// <summary>用例四：同一事件实例在派发途中被再次派发时，引用计数保持平衡且不崩溃。</summary>
    private static int CheckNestedDispatchSameInstance()
    {
        // dispatcher 是本用例私有的派发器实例，避免与其它用例的监听登记互相污染。
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        // listener 是会在回调里再次派发同一实例的监听者，用来制造嵌套派发。
        NestedListener listener = new NestedListener();
        listener.dispatcher_ = dispatcher;
        listener.remaining_nesting_ = 2;
        dispatcher.AddEventListener(ROEventDefine.kActionSkill, listener, listener.OnEvent);

        // pool 是攻击动作事件类型的单例池，嵌套派发用的实例从这里取。
        ROEventPool<ROAttackActionArgs> pool = ROEventPool<ROAttackActionArgs>.Instance;
        // evt 是同一次嵌套派发里被反复传入的同一个实例。
        ROAttackActionArgs evt = pool.Get();
        dispatcher.DispatcherEvent(evt);
        if (listener.count_ != 3)
        {
            return Fail("同实例嵌套派发应回调 3 次，实际 " + listener.count_);
        }
        if (ReadRefCount(evt) != 1)
        {
            return Fail("嵌套派发后引用计数应回到 1，实际 " + ReadRefCount(evt));
        }
        evt.Release();
        return 0;
    }

    /// <summary>
    /// 用例五：异步持有者加引用期间，实例不会被提前放回复用，也不会被提前重置。
    /// 归还后的 Reset 只在引用真正归零时发生，这是池不残留业务引用的前提。
    /// </summary>
    private static int CheckAsyncHolderPreventsEarlyReuse()
    {
        // pool 是客户端施法请求事件类型的单例池，用来模拟异步请求持有。
        ROEventPool<ROClientFireSkillArgs> pool = ROEventPool<ROClientFireSkillArgs>.Instance;
        // held 是被异步逻辑长期持有的实例，引用归零前不允许被复用。
        ROClientFireSkillArgs held = pool.Get();
        held.skill_id_ = 5005;
        held.skill_uuid_ = 66UL;
        // 模拟异步请求持有：加一个引用后，调用方再释放自己的那一份。
        held.AddRef();
        held.Release();
        if (ReadRefCount(held) != 1)
        {
            return Fail("异步持有期间引用计数应为 1，实际 " + ReadRefCount(held));
        }
        if (held.skill_id_ != 5005 || held.skill_uuid_ != 66UL)
        {
            return Fail("仍有引用时不应重置业务字段");
        }
        // another 是异步持有期间额外取用的实例，按断言不能与 held 是同一个对象。
        ROClientFireSkillArgs another = pool.Get();
        if (ReferenceEquals(held, another))
        {
            return Fail("仍有异步引用时实例被提前复用");
        }
        another.Release();
        held.Release();
        return 0;
    }

    /// <summary>
    /// 用例六：重复归还与"绕过引用计数直接调池归还口"都只能入池一次。
    /// 顺序完全复现评审给出的复现路径：先借两个，正常释放，再对同一实例重复释放一次，
    /// 最后绕过引用计数直接调池的归还入口；之后连续借三个，必须两两不同。
    /// </summary>
    private static int CheckDuplicateReturnPoolsOnce()
    {
        // pool 是吟唱事件类型的单例池，本用例专测重复入池。
        ROEventPool<ROSingingArgs> pool = ROEventPool<ROSingingArgs>.Instance;
        // first 是第一个借出实例，后续会对它重复释放并绕过引用计数直接归还。
        ROSingingArgs first = pool.Get();
        // second 是第二个借出实例，用于确认两次取用拿到的不是同一个对象。
        ROSingingArgs second = pool.Get();
        if (ReferenceEquals(first, second))
        {
            return Fail("两次取用返回了同一实例，池已处于脏状态");
        }
        if (!first.Release())
        {
            return Fail("首次正常释放未把引用降为零");
        }
        // 同一实例再释放一次：引用计数已是池内哨兵，必须被拒绝。
        if (first.Release())
        {
            return Fail("重复释放同一实例被接受，会重复入池");
        }
        second.Release();
        // 绕过引用计数直接调池的归还入口：活跃引用已归零但已在池中的实例必须被拒绝。
        pool.Release(first);

        // 连借三个：若上面任一次归还造成了重复入栈，这里就会出现两个相同实例。
        ROSingingArgs rented_a = pool.Get();
        // rented_b 是连借的第二个实例，若池里存在重复入栈就会与 A 撞成同一个对象。
        ROSingingArgs rented_b = pool.Get();
        // rented_c 是连借的第三个实例，与 A、B 一起组成两两不同的校验。
        ROSingingArgs rented_c = pool.Get();
        if (ReferenceEquals(rented_a, rented_b))
        {
            return Fail("池中同一实例出现了两份（A 与 B 相同）");
        }
        if (ReferenceEquals(rented_a, rented_c))
        {
            return Fail("池中同一实例出现了两份（A 与 C 相同）");
        }
        if (ReferenceEquals(rented_b, rented_c))
        {
            return Fail("池中同一实例出现了两份（B 与 C 相同）");
        }
        rented_a.Release();
        rented_b.Release();
        rented_c.Release();
        return 0;
    }

    /// <summary>
    /// 用例七：活跃实例（仍有引用）被直接交给池的归还入口时必须被拒绝。
    /// 这是"关闭绕过引用计数的直接归还口"的核心回归：拒绝后该实例不会被任何一次取用借出。
    /// </summary>
    private static int CheckActiveInstanceRejectedByPool()
    {
        // pool 是吟唱事件类型的单例池，本用例检查活跃实例被直接归还时的拒绝行为。
        ROEventPool<ROSingingArgs> pool = ROEventPool<ROSingingArgs>.Instance;
        // active 是仍持有一个引用的活跃实例，直接交给池的归还口必须被拒绝。
        ROSingingArgs active = pool.Get();
        active.skill_id_ = 8080;
        if (ReadRefCount(active) != 1)
        {
            return Fail("活跃实例引用计数应为 1，实际 " + ReadRefCount(active));
        }
        // 活跃实例直接归还：必须被所有权标记拒绝，引用计数保持为一。
        pool.Release(active);
        if (ReadRefCount(active) != 1)
        {
            return Fail("活跃实例被池接收，引用计数被改成 " + ReadRefCount(active));
        }
        if (active.skill_id_ != 8080)
        {
            return Fail("活跃实例被提前重置，业务字段已丢失");
        }

        // 连借三个：活跃实例不能出现在其中任何一个位置。
        ROSingingArgs rented_a = pool.Get();
        // rented_b 是连借的第二个实例，不能等于仍在活跃的 active。
        ROSingingArgs rented_b = pool.Get();
        // rented_c 是连借的第三个实例，与 A、B 一起确认活跃实例没有被重复借出。
        ROSingingArgs rented_c = pool.Get();
        if (ReferenceEquals(rented_a, active))
        {
            return Fail("活跃实例被重复借出（A）");
        }
        if (ReferenceEquals(rented_b, active))
        {
            return Fail("活跃实例被重复借出（B）");
        }
        if (ReferenceEquals(rented_c, active))
        {
            return Fail("活跃实例被重复借出（C）");
        }
        if (ReferenceEquals(rented_a, rented_b) || ReferenceEquals(rented_b, rented_c))
        {
            return Fail("三个借出实例不是两两不同");
        }
        rented_a.Release();
        rented_b.Release();
        rented_c.Release();
        active.Release();
        return 0;
    }

    /// <summary>
    /// 用例八：带具名强类型载荷的探针事件，引用归零归还时业务引用必须立即被清掉，
    /// 而不是等到下一次取用才清。
    /// </summary>
    private static int CheckPayloadClearedOnReturn()
    {
        // pool 是测试专用探针事件类型的单例池，探针事件的载荷是具名强类型字段。
        ROEventPool<ProbeEvent> pool = ROEventPool<ProbeEvent>.Instance;
        // first 是首个探针实例，会挂上一个载荷后归还，验证归还瞬间的清空时机。
        ProbeEvent first = pool.Get();
        // payload 是本用例临时构造的载荷对象，只挂在事件上用来观察引用是否被清掉。
        ProbePayload payload = new ProbePayload();
        payload.name_ = "first-payload";
        payload.sequence_ = 1;
        first.payload_ = payload;
        // first_token 是首个探针实例的令牌，供复用后比对是否换发。
        ulong first_token = first.GetToken();
        if (first.payload_ == null)
        {
            return Fail("探针事件取用后应能承载具名强类型载荷");
        }

        if (!first.Release())
        {
            return Fail("探针事件释放未把引用降为零");
        }
        // 关键断言：此刻实例已经躺在池里，载荷必须已经是空，不能等下一次 Get 才清。
        if (first.payload_ != null)
        {
            return Fail("归还瞬间未清空业务引用，载荷仍为 " + payload.name_);
        }

        // second 是归还后重新取用的探针实例，按断言应与 first 是同一对象且载荷为空。
        ProbeEvent second = pool.Get();
        if (!ReferenceEquals(first, second))
        {
            return Fail("探针事件归还后未复用同一实例");
        }
        if (second.payload_ != null)
        {
            return Fail("复用实例带着上一次的载荷");
        }
        if (second.GetToken() <= first_token)
        {
            return Fail("复用实例未换发新令牌");
        }
        second.Release();
        return 0;
    }

    /// <summary>
    /// 用例九：用 GC.GetAllocatedBytesForCurrentThread 实测同一线程上
    /// "池化取用/派发/归还"与同等次直接 new 的分配字节差异。
    /// 采样区间内不输出日志、不构造回调，读数为当前线程精确累计分配字节；
    /// 覆盖范围只限单事件类型的取用、派发与归还，不代表整条施法链零 GC。
    /// </summary>
    private static int CheckAllocationOnHotPath()
    {
        // read_allocated 是当前线程累计分配字节的读取委托，为空表示运行时不提供该计数。
        Func<long> read_allocated = ResolveAllocatedBytesReader();
        if (read_allocated == null)
        {
            // 运行时没有当前线程分配计数时如实报告不可测，不拿其它读数冒称精确字节。
            Console.WriteLine("EventPoolSmoke 分配实测：当前运行时不提供 GC.GetAllocatedBytesForCurrentThread，" +
                "该项不可测；复用证据仅来自构造次数与实例同一性。");
            return 0;
        }

        // dispatcher 是本用例私有的派发器，采样区间内只做派发、不产生日志。
        ROEventDispatcher dispatcher = new ROEventDispatcher();
        // listener 只累加回调次数，回调体不分配，保证采样读数只反映取用/派发/归还本身。
        CountingListener listener = new CountingListener();
        dispatcher.AddEventListener(ROEventDefine.kActionSkill, listener, listener.OnEvent);
        // pool 是探针事件类型的单例池，池化路径的采样全部走它。
        ROEventPool<ProbeEvent> pool = ROEventPool<ProbeEvent>.Instance;

        // 暖机：把 JIT、静态初始化、池容量与派发快路径都跑热，全部放在采样区间之外。
        for (int index = 0; index < WarmUpLoopCount; index++)
        {
            // warm 是暖机阶段从池中取用的事件实例，派发后立刻归还，只为把路径跑热。
            ProbeEvent warm = pool.Get();
            warm.payload_ = null;
            dispatcher.DispatcherEvent(warm);
            warm.Release();
        }
        // index 是暖机计数器，从零到 WarmUpLoopCount 减一，这一轮只做直接 new 的 JIT 预热。
        for (int index = 0; index < WarmUpLoopCount; index++)
        {
            // created 是暖机阶段的直接新建实例，把构造函数的 JIT 开销提前跑掉。
            ProbeEvent created = new ProbeEvent();
            created.payload_ = null;
        }

        // 基线：同等次数的直接 new，代表"没有对象池"时的当前线程分配量。
        int created_before_baseline = ProbeEvent.created_count_;
        // baseline_start 是基线采样开始前的累计分配字节，单位字节，供采样结束后做差。
        long baseline_start = read_allocated();
        // index 是采样计数器，只用于控制采样次数，不参与业务语义。
        for (int index = 0; index < AllocationLoopCount; index++)
        {
            // created 是基线路径每次直接新建的实例，只承担构造开销，不派发也不归还。
            ProbeEvent created = new ProbeEvent();
            created.payload_ = null;
        }
        // baseline_bytes 是基线路径（全部直接 new）在采样区间内的分配字节数，单位字节。
        long baseline_bytes = read_allocated() - baseline_start;
        // baseline_created 是基线路径新建的实例个数，应与采样次数相等。
        int baseline_created = ProbeEvent.created_count_ - created_before_baseline;

        // 池化：与正式事件链一致的 Get → 派发 → 归还，采样区间内不做任何输出与委托构造。
        int created_before_pool = ProbeEvent.created_count_;
        // first_rented 记录池化采样中第一个取用的实例，只用于首末同一性比对。
        ProbeEvent first_rented = null;
        // last_rented 记录池化采样中最后一个取用的实例，与 first_rented 相同即证明全程复用同一对象。
        ProbeEvent last_rented = null;
        // pool_start 是池化采样开始前的累计分配字节，单位字节，与 baseline_start 口径一致。
        long pool_start = read_allocated();
        // index 是采样计数器，只用于控制采样次数，不参与业务语义。
        for (int index = 0; index < AllocationLoopCount; index++)
        {
            // evt 是本轮从池中取用的事件实例，派发后立刻归还，模拟正式链路上的单次传递。
            ProbeEvent evt = pool.Get();
            if (first_rented == null)
            {
                first_rented = evt;
            }
            last_rented = evt;
            evt.payload_ = null;
            dispatcher.DispatcherEvent(evt);
            evt.Release();
        }
        // pool_bytes 是池化路径在采样区间内的分配字节数，单位字节，正常应为零。
        long pool_bytes = read_allocated() - pool_start;
        // pool_created 是池化路径新建的实例个数，正常应为零。
        int pool_created = ProbeEvent.created_count_ - created_before_pool;

        // 实例同一性：整段采样只应复用同一个实例，首末相同即为证据。
        bool same_instance = ReferenceEquals(first_rented, last_rented);

        Console.WriteLine("EventPoolSmoke 分配实测（GC.GetAllocatedBytesForCurrentThread，单线程，循环 " +
            AllocationLoopCount + " 次）：基线 new=" + baseline_bytes +
            " 字节/新建 " + baseline_created + " 个；池化 Get/派发/归还=" + pool_bytes +
            " 字节/新建 " + pool_created + " 个；派发回调 " + listener.count_ +
            " 次；首末实例同一=" + same_instance + "。");
        Console.WriteLine("EventPoolSmoke 口径说明：只覆盖单事件类型的取用、派发与归还，不外推整条施法链零 GC。");

        if (pool_created != 0)
        {
            return Fail("池化热路径不应新建事件实例，实际新建 " + pool_created + " 个");
        }
        if (!same_instance)
        {
            return Fail("池化热路径未复用同一实例，首末实例不同");
        }
        if (baseline_bytes <= 0L)
        {
            return Fail("基线路径分配读数应大于零，实际 " + baseline_bytes + " 字节");
        }
        if (pool_bytes >= baseline_bytes)
        {
            return Fail("池化路径分配未低于直接新建，基线 " + baseline_bytes +
                " 字节，池化 " + pool_bytes + " 字节");
        }
        return 0;
    }

    /// <summary>
    /// 解析当前线程累计分配字节的读取器。
    /// 该 API 自 .NET Framework 4.7.1 起提供，只统计当前线程，正好适合单线程采样；
    /// 解析不到时返回空，表示本运行时不支持精确读数。
    /// </summary>
    /// <returns>可重复调用的分配字节读取委托；运行时不支持时为空。</returns>
    private static Func<long> ResolveAllocatedBytesReader()
    {
        if (allocated_bytes_reader_ != null)
        {
            return allocated_bytes_reader_;
        }
        // method 是按名称反射到的静态无参方法信息，为空说明该运行时没有这个计数 API。
        MethodInfo method = typeof(GC).GetMethod(
            "GetAllocatedBytesForCurrentThread",
            BindingFlags.Public | BindingFlags.Static,
            null,
            Type.EmptyTypes,
            null);
        if (method == null)
        {
            return null;
        }
        allocated_bytes_reader_ = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
        return allocated_bytes_reader_;
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
