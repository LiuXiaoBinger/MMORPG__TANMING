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
    /// <summary>构造默认成功返回值。</summary>
    /// <param name="Code">错误码。</param>
    /// <param name="sneed">仍需空格数。</param>
    public ItemZhuDongGetResult(CmdCode Code = CmdCode.Succeed,int sneed=0)
    {
        space_still_need = 0;
        ErrorCode = CmdCode.Succeed;
    }
};