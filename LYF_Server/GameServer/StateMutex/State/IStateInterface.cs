/// <summary>
/// 状态互斥中的单个状态接口，对应参考工程 rogamelibs/state_mutex/state/i_state_interface.h。
/// 每个实现只回答三件事：当前是否处于该状态、如何离开该状态、拒绝时回哪个错误码。
/// </summary>
public interface IStateInterface
{
    /// <summary>本实现对应的互斥状态标识。</summary>
    StateTransitionType GetType();

    /// <summary>检查该角色当前是否处于本状态。</summary>
    /// <param name="role">待检查的在线角色。</param>
    /// <returns>处于本状态返回 true。</returns>
    bool Check(OnlineRole role);

    /// <summary>让该角色离开本状态。</summary>
    /// <param name="role">待离开状态的在线角色。</param>
    /// <returns>成功离开返回 true。</returns>
    bool Leave(OnlineRole role);

    /// <summary>本状态拒绝切换时回给调用方的错误码，对应 RO 的 GetErrorCode。</summary>
    CmdCode GetErrorCode();
}
