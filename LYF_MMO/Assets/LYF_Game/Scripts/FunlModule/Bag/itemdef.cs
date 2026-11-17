/// <summary>
/// 主动批量领奖的对外返回值：携带错误码与背包仍需多少空格才能放下奖励。
/// </summary>
//主动领奖返回值 对外接口返回值
public struct ItemZhuDongGetResult
{
    /// <summary>本次领奖结果错误码，成功为 Succeed。</summary>
    public CmdCode ErrorCode;
    /// <summary>还需要多少个格子才能放下所领取的奖励。</summary>
    public int space_still_need;       //还需要多少个格子才能放下所领取的奖励
    /// <summary>始终创建成功且缺格数为 0 的结果；当前实现未使用传入参数。</summary>
    /// <param name="Code">保留参数，当前实现不读取。</param>
    /// <param name="sneed">保留参数，当前实现不读取。</param>
    public ItemZhuDongGetResult(CmdCode Code = CmdCode.Succeed,int sneed=0)
    {
        space_still_need = 0;
        ErrorCode = CmdCode.Succeed;
    }
};