using System;

/// <summary>
/// 服务端实体事件类型，对应参考工程 rogamelibs/core/event/event.h 的 ROEventDefine。
/// 只登记本项目真实存在监听者的事件，不预先搬运 RO 的其余事件枚举。
/// </summary>
public enum ROEventDefine : uint
{
    /// <summary>客户端请求释放技能，对应 RO kActionClienFireSkill（保留 RO 原始拼写）。</summary>
    kActionClienFireSkill = 0,

    /// <summary>进入吟唱结算阶段，对应 RO kActionSinging；由施法请求组件派发给技能组件。</summary>
    kActionSinging = 1
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
    /// <param name="y">Y 轴世界坐标，单位米。</param>
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
/// 派发器只依赖事件类型做分发，具体业务字段由派生的事件参数承载。
/// </summary>
public class ROEvent
{
    /// <summary>事件类型，派发时用于查监听表；构造后不再变更。</summary>
    private readonly ROEventDefine event_type_;

    /// <summary>以事件类型构造事件。</summary>
    /// <param name="event_type">本次事件在 ROEventDefine 中的类型。</param>
    public ROEvent(ROEventDefine event_type)
    {
        event_type_ = event_type;
    }

    /// <summary>返回事件类型，供派发器定位监听表。</summary>
    public ROEventDefine GetEventType()
    {
        return event_type_;
    }
}

/// <summary>
/// 客户端请求施法事件，对应 event.h 的 ROClientFireSkillArgs。
/// 由消息入口构造，派发后由 RoleSingingComponent 转换为吟唱事件。
/// </summary>
public sealed class ROClientFireSkillArgs : ROEvent
{
    /// <summary>客户端请求释放的技能主表 ID；服务端按自己的学习等级解析效果，不采信客户端等级。</summary>
    public int skill_id_;

    /// <summary>技能目标实体 UID，无目标技能为 0。</summary>
    public ulong target_;

    /// <summary>客户端上报的施法点世界坐标，单位米；当前协议未携带时保持零值。</summary>
    public ROVector3 cast_point_;

    /// <summary>客户端生成的单次施法标识，用于结束通知与去重，服务端原样回传。</summary>
    public ulong skill_uuid_;

    /// <summary>构造客户端施法事件，事件类型固定为 kActionClienFireSkill。</summary>
    public ROClientFireSkillArgs()
        : base(ROEventDefine.kActionClienFireSkill)
    {
    }
}

/// <summary>
/// 吟唱事件，对应 event.h 的 ROSingingArgs。
/// 由 RoleSingingComponent 派发，由技能组件消费并执行实际释放流程。
/// </summary>
public sealed class ROSingingArgs : ROEvent
{
    /// <summary>本次吟唱要释放的技能主表 ID。</summary>
    public int skill_id_;

    /// <summary>技能目标实体 UID，无目标技能为 0。</summary>
    public ulong target_;

    /// <summary>施法点世界坐标，单位米。</summary>
    public ROVector3 cast_point_;

    /// <summary>客户端生成的单次施法标识，贯穿开始与结束通知。</summary>
    public ulong skill_uuid_;

    /// <summary>构造吟唱事件，事件类型固定为 kActionSinging。</summary>
    public ROSingingArgs()
        : base(ROEventDefine.kActionSinging)
    {
    }
}
