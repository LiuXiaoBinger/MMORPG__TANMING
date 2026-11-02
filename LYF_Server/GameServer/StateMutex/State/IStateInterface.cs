/// <summary>
/// 状态互斥中单个状态的抽象基类，对应参考工程 rogamelibs/state_mutex/state/i_state_interface.h。
/// 每个派生状态只回答三件事：当前是否处于该状态、如何离开该状态、拒绝时回哪个错误码。
///
/// 类名沿用参考工程的 I 前缀，但它是 abstract class 而不是 C# interface：
/// RO 侧本类就是 C++ 抽象基类，自身持有数据成员 type_、提供 Check/Leave 的默认实现、
/// 并把 GetErrorCode 声明为纯虚。C# 的 interface 不能保存字段，也无法给出
/// “Check/Leave 默认返回 false、派生类按需重写”的默认实现，因此只能用抽象类忠实对应。
/// </summary>
public abstract class IStateInterface
{
    #region 字段与构造
    /// <summary>
    /// 本状态对应的互斥状态标识，构造时写入、之后只读。
    /// 取值同时是互斥表的行下标，数值语义见 StateTransitionType。
    /// </summary>
    private readonly StateTransitionType type_;

    /// <summary>
    /// 由派生状态在构造时传入自己对应的互斥状态标识，对应 RO 的 IStateInterface(StateTransitionType)。
    /// RO 原型的构造写在 public 段，但 C++ 抽象类本就无法直接实例化；C# 用 protected
    /// 表达同一约束，即只有派生类能调用它。
    /// </summary>
    /// <param name="type">派生状态对应的互斥状态标识，需落在合法互斥状态范围内。</param>
    protected IStateInterface(StateTransitionType type)
    {
        type_ = type;
    }
    #endregion

    #region 查询：状态标识
    /// <summary>
    /// 返回本状态对应的互斥状态标识，对应 RO 的非虚 GetType()：值在构造时固定，派生类不得重写。
    ///
    /// 这里必须用 new 隐藏 System.Object.GetType()：RO 的 GetType 返回 StateTransitionType，
    /// C# 的 object.GetType 返回 System.Type，两者同名不同义。隐藏后，通过本类型或其派生类型的
    /// 引用调用得到的是状态标识，管理器按标识查表的代码因此可以逐处保持原样；确实需要反射类型时，
    /// 仍可用 ((object)state).GetType() 取得，两种语义互不干扰。
    /// </summary>
    /// <returns>本状态对应的互斥状态标识。</returns>
    public new StateTransitionType GetType()
    {
        return type_;
    }
    #endregion

    #region 业务逻辑：状态判定与离开
    /// <summary>
    /// 检查该角色当前是否处于本状态，对应 RO 的 virtual bool Check(ROObject*)。
    /// 基类默认返回 false，表示本状态不做额外判定；真正的判定由派生状态按自己依赖的组件实现。
    /// </summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <returns>处于本状态返回 true；基类默认 false。</returns>
    public virtual bool Check(OnlineRole role)
    {
        return false;
    }

    /// <summary>
    /// 让该角色离开本状态，对应 RO 的 virtual bool Leave(ROObject*)。
    /// 基类默认返回 false，表示本状态不提供离开动作；需要主动打断的派生状态自行重写。
    /// </summary>
    /// <param name="role">待离开状态的在线角色。</param>
    /// <returns>成功离开返回 true；基类默认 false。</returns>
    public virtual bool Leave(OnlineRole role)
    {
        return false;
    }

    /// <summary>
    /// 本状态拒绝切换时回给调用方的错误码，对应 RO 的纯虚 virtual KKSG::ErrorCode GetErrorCode() = 0。
    /// 错误码必须由具体状态给出，基类无法提供合理默认值，因此声明为 abstract。
    /// </summary>
    /// <returns>拒绝切换时使用的错误码。</returns>
    public abstract CmdCode GetErrorCode();
    #endregion
}
