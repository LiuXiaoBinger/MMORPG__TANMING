using System;

/// <summary>
/// 服务端实体事件类型，对应参考工程 rogamelibs/core/event/event.h 的 ROEventDefine。
/// 只登记本项目真实存在监听者的事件，不预先搬运 RO 的其余事件枚举。
/// </summary>
public enum ROEventDefine : uint
{
    /// <summary>客户端请求释放技能，对应 RO kActionClienFireSkill（保留 RO 原始拼写）。</summary>
    kActionClienFireSkill = 0,

    /// <summary>进入吟唱结算阶段，对应 RO kActionSinging；由施法请求组件派发给吟唱状态。</summary>
    kActionSinging = 1,

    /// <summary>吟唱结束后的攻击动作，对应 RO kActionSkill；由吟唱状态派发给技能组件。</summary>
    kActionSkill = 2,

    /// <summary>
    /// 事件类型上界，对应 RO 的 kActionMax。
    /// 只作为 OnGetFromPool 的初始哨兵值：若派生类的 Reset 没有回填真实类型，会停留在此值并打错误日志。
    /// </summary>
    kActionMax = 3
}

/// <summary>
/// 事件载荷使用的三维坐标，替代参考工程 rocommongamelibs 的 Vector3。
/// 三个分量均为世界坐标，单位米；本项目协议当前不携带施法点，保留字段与 RO 对齐。
/// </summary>
public struct ROVector3
{
    /// <summary>X 轴世界坐标，单位米。</summary>
    public float x_;

    /// <summary>Y 轴世界坐标（高度），单位米。</summary>
    public float y_;

    /// <summary>Z 轴世界坐标，单位米。</summary>
    public float z_;

    /// <summary>按三个分量构造坐标。</summary>
    /// <param name="x">X 轴世界坐标，单位米。</param>
    /// <param name="y">Y 轴世界坐标（高度），单位米。</param>
    /// <param name="z">Z 轴世界坐标，单位米。</param>
    public ROVector3(float x, float y, float z)
    {
        x_ = x;
        y_ = y;
        z_ = z;
    }
}

/// <summary>
/// 实体事件基类，对应 event.h 的 ROEvent。
/// 通过 AddRef/Release 记录多少处仍在使用此事件；全部用完后清空字段并归还对象池。
/// 这套计数控制的是对象何时可以被复用，不替代 C# 的垃圾回收。
/// </summary>
public class ROEvent
{

    #region 字段
    /// <summary>事件类型，派发时用于查监听表；由 Reset 在每次取用时回填。</summary>
    private ROEventDefine event_type_;

    /// <summary>本次事件实例的令牌，对应 RO 的 token_；每次从池中取出都会换新值。</summary>
    private ulong token_;

    /// <summary>全局单调自增令牌种子，对应 RO 的 static_token_。</summary>
    private static ulong static_token_;

    /// <summary>
    /// 当前引用计数：一及以上是活跃引用数，零是"引用已归零但尚未入池"，
    /// 负一是"已进入对象池"的一次性标记。归零到入池之间只允许发生一次转移，
    /// 因此重复归还拿不到第二次标记，池里不会出现同一实例两份。
    /// </summary>
    private int ref_count_;

    /// <summary>池内哨兵值：引用计数取此值表示实例已在空闲栈中，既不能再被引用，也不能再次归还。</summary>
    internal const int InPoolRefCount = -1;
    #endregion

    #region 构造与初始化
    /// <summary>以事件类型构造事件；新建实例自带一个调用方引用。</summary>
    /// <param name="event_type">本次事件在 ROEventDefine 中的类型。</param>
    protected ROEvent(ROEventDefine event_type)
    {
        event_type_ = event_type;
        ref_count_ = 1;
    }

    /// <summary>
    /// 清空业务字段并把事件类型回填为派生类的真实类型。
    /// 对应 event.h 中 ROSingingArgs::Reset（:716-739）；RO 的 ROClientFireSkillArgs 没有 Reset，
    /// 本项目因为走对象池，必须补上，否则复用实例会带着上一次的字段。
    /// </summary>
    protected virtual void Reset()
    {
    }
    #endregion

    /// <summary>
    /// 从对象池取出时调用，对应 event.h 的 ROEvent::OnGetFromPool（:517-529）。
    /// 先把事件类型置为 kActionMax 作为哨兵，换发新令牌并把引用计数重置为一，
    /// 再调用派生类 Reset 回填真实事件类型与业务字段。
    /// </summary>
    public void OnGetFromPool()
    {
        event_type_ = ROEventDefine.kActionMax;
        token_ = ++static_token_;
        ref_count_ = 1;
        Reset();
        if (event_type_ == ROEventDefine.kActionMax)
        {
            // 派生类忘记在 Reset 里回填事件类型时，派发器无法路由，必须显式报错而不是静默丢弃。
            LogMsg.Info("事件未在 Reset 中回填事件类型: " + GetType().Name, LogMsgType.Error);
        }
    }

    /// <summary>增加一个使用引用；计数为零或负一时拒绝，防止再次使用正在归还或已在池中的实例。</summary>
    /// <returns>成功增加引用返回 true；实例已归还对象池时返回 false。</returns>
    public bool AddRef()
    {
        if (ref_count_ <= 0)
        {
            LogMsg.Info("已归还对象池的事件被 AddRef: " + GetType().Name, LogMsgType.Error);
            return false;
        }
        ref_count_++;
        return true;
    }

    #region 移除与回收
    /// <summary>
    /// 释放一个引用；引用归零时先清空业务字段，再归还对象池。
    /// 归零即 Reset 是刻意安排：事件在池中等待期间不能保留任何业务对象引用，
    /// 否则池会替上一个请求把载荷一直挂在内存里，直到下一次取用才清。
    /// </summary>
    /// <returns>本次释放把引用降为零并触发回池时返回 true；仍有其它引用时返回 false。</returns>
    public bool Release()
    {
        if (ref_count_ <= 0)
        {
            // 计数为零或已是池内哨兵时再释放，说明所有权算错了；这里只拒绝，不重复回池。
            LogMsg.Info("引用计数为零或已入池的事件被重复 Release: " + GetType().Name, LogMsgType.Error);
            return false;
        }
        ref_count_--;
        if (ref_count_ > 0)
        {
            return false;
        }
        Reset();
        Recycle();
        return true;
    }

    /// <summary>把实例归还到自己的对象池；未池化的事件保持空实现，交给垃圾回收。</summary>
    protected virtual void Recycle()
    {
    }
    #endregion

    #region 查询与判定
    /// <summary>返回事件类型，供派发器定位监听表。</summary>
    public ROEventDefine GetEventType()
    {
        return event_type_;
    }

    /// <summary>返回本次事件实例的令牌，用于判断回调是否属于同一次动作。</summary>
    public ulong GetToken()
    {
        return token_;
    }

    /// <summary>
    /// 归还前的所有权确认，只允许对象池在入栈前调用。
    /// 只有引用计数恰好为零（活跃引用已全部释放）才标记为池内哨兵并放行；
    /// 活跃实例（大于零）与已经入池的实例（负一）一律拒绝。
    /// 有了这道确认，即使程序集内部误调池的归还入口，也不会出现同一实例在池中两份。
    /// </summary>
    /// <returns>本次成功标记入池返回 true；实例仍活跃或已在池中返回 false。</returns>
    internal bool TryEnterPool()
    {
        if (ref_count_ != 0)
        {
            return false;
        }
        ref_count_ = InPoolRefCount;
        return true;
    }
    #endregion

    /// <summary>回填事件类型，仅供派生类 Reset 使用；对应 RO Reset 里对 event_type_ 的直接赋值。</summary>
    /// <param name="event_type">派生类自己的事件类型。</param>
    protected void SetEventType(ROEventDefine event_type)
    {
        event_type_ = event_type;
    }
}

/// <summary>
/// 客户端请求施法事件，对应 event.h 的 ROClientFireSkillArgs。
/// 由消息入口构造并交给状态互斥，回调成功后再由 RoleSingingComponent 转换为吟唱事件。
/// </summary>
public sealed class ROClientFireSkillArgs : ROEvent
{

    #region 构造与初始化
    /// <summary>构造客户端施法事件，事件类型固定为 kActionClienFireSkill。</summary>
    public ROClientFireSkillArgs()
        : base(ROEventDefine.kActionClienFireSkill)
    {
    }

    /// <summary>清空上一次复用留下的业务字段并回填事件类型。</summary>
    protected override void Reset()
    {
        skill_id_ = 0;
        target_ = 0UL;
        cast_point_ = new ROVector3(0f, 0f, 0f);
        skill_uuid_ = 0UL;
        // 事件类型必须在这里回填，否则 OnGetFromPool 的哨兵检查会报错且派发器无法路由。
        SetEventType(ROEventDefine.kActionClienFireSkill);
    }
    #endregion

    /// <summary>客户端请求释放的技能主表 ID；服务端按自己的学习等级解析效果，不采信客户端等级。</summary>
    public int skill_id_;

    /// <summary>技能目标实体 UID，无目标技能为 0。</summary>
    public ulong target_;

    /// <summary>客户端上报的施法点世界坐标，单位米；当前协议未携带时保持零值。</summary>
    public ROVector3 cast_point_;

    /// <summary>客户端生成的单次施法标识，用于结束通知与去重，服务端原样回传。</summary>
    public ulong skill_uuid_;

    /// <summary>把实例归还到客户端施法事件对象池。</summary>
    protected override void Recycle()
    {
        ROEventPool<ROClientFireSkillArgs>.Instance.Release(this);
    }
}

/// <summary>
/// 吟唱事件，对应 event.h 的 ROSingingArgs。
/// 由 RoleSingingComponent 派发，由吟唱状态消费并启动读条。
/// </summary>
public sealed class ROSingingArgs : ROEvent
{

    #region 构造与初始化
    /// <summary>构造吟唱事件，事件类型固定为 kActionSinging。</summary>
    public ROSingingArgs()
        : base(ROEventDefine.kActionSinging)
    {
    }

    /// <summary>清空上一次复用留下的业务字段并回填事件类型。</summary>
    protected override void Reset()
    {
        skill_id_ = 0;
        target_ = 0UL;
        cast_point_ = new ROVector3(0f, 0f, 0f);
        skill_uuid_ = 0UL;
        SetEventType(ROEventDefine.kActionSinging);
    }
    #endregion

    /// <summary>本次吟唱要释放的技能主表 ID。</summary>
    public int skill_id_;

    /// <summary>技能目标实体 UID，无目标技能为 0。</summary>
    public ulong target_;

    /// <summary>施法点世界坐标，单位米。</summary>
    public ROVector3 cast_point_;

    /// <summary>客户端生成的单次施法标识，贯穿开始与结束通知。</summary>
    public ulong skill_uuid_;

    /// <summary>把实例归还到吟唱事件对象池。</summary>
    protected override void Recycle()
    {
        ROEventPool<ROSingingArgs>.Instance.Release(this);
    }
}

/// <summary>
/// 吟唱结束后的攻击动作事件，对应 event.h 的 ROAttackActionArgs（:1053-）。
/// 由吟唱状态在 OnFinished 中派发，由技能组件消费并执行权威释放结算。
/// </summary>
public sealed class ROAttackActionArgs : ROEvent
{

    #region 构造与初始化
    /// <summary>构造攻击动作事件，事件类型固定为 kActionSkill。</summary>
    public ROAttackActionArgs()
        : base(ROEventDefine.kActionSkill)
    {
    }

    /// <summary>清空上一次复用留下的业务字段并回填事件类型。</summary>
    protected override void Reset()
    {
        skill_id_ = 0;
        target_ = 0UL;
        cast_point_ = new ROVector3(0f, 0f, 0f);
        skill_uuid_ = 0UL;
        SetEventType(ROEventDefine.kActionSkill);
    }
    #endregion

    /// <summary>本次要释放的技能主表 ID。</summary>
    public int skill_id_;

    /// <summary>技能目标实体 UID，无目标技能为 0。</summary>
    public ulong target_;

    /// <summary>施法点世界坐标，单位米。</summary>
    public ROVector3 cast_point_;

    /// <summary>客户端生成的单次施法标识，服务端原样回传到开始与结束通知。</summary>
    public ulong skill_uuid_;

    /// <summary>把实例归还到攻击动作事件对象池。</summary>
    protected override void Recycle()
    {
        ROEventPool<ROAttackActionArgs>.Instance.Release(this);
    }
}
