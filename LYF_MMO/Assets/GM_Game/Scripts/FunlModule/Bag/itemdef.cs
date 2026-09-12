//主动领奖返回值 对外接口返回值
public struct ItemZhuDongGetResult
{
    public CmdCode ErrorCode;
    public int space_still_need;       //还需要多少个格子才能放下所领取的奖励
    public ItemZhuDongGetResult(CmdCode Code = CmdCode.Succeed,int sneed=0)
    {
        space_still_need = 0;
        ErrorCode = CmdCode.Succeed;
    }
};