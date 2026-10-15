/// <summary>主动领奖/发放物品的对外结果，携带错误码与背包剩余格子缺口。</summary>
//主动领奖返回值 对外接口返回值
public struct ItemZhuDongGetResult
{
    /// <summary>发放结果错误码。</summary>
    public CmdCode ErrorCode;
    /// <summary>背包已满时仍需要的空格子数量（正数表示放不下）。</summary>
    public int space_still_need;
    //构造参数 Code/sneed 历史未生效：结构体先固定初始化为成功且无需补格子，待发放逻辑接入。
    public ItemZhuDongGetResult(CmdCode Code = CmdCode.Succeed,int sneed=0)
    {
        space_still_need = 0;
        ErrorCode = CmdCode.Succeed;
    }
};