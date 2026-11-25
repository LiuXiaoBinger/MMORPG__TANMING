using SqlSugar;

/// <summary>每次玩家确认的持久交易回执，准备阶段保留可供 GS 重连恢复的完整指令。</summary>
[SugarTable("stall_receipt")]
public sealed class StallReceiptTable
{
    /// <summary>区号、角色号与玩家操作 UUID 组成幂等键。</summary>
    [SugarColumn(IsPrimaryKey = true, Length = 90)]
    public string RequestKey { get; set; }
    /// <summary>中心服分配的资产事务号。</summary>
    public long Ticket { get; set; }
    /// <summary>所属逻辑区。</summary>
    public int ServerId { get; set; }
    /// <summary>本次资产变动所属的角色。</summary>
    public int RoleId { get; set; }
    /// <summary>零为准备完成，一为已成功，二为已拒绝。</summary>
    public int State { get; set; }

    /// <summary>中心库已核实的操作和结果，按原单重发。</summary>
    [SugarColumn(ColumnDataType = "BLOB")]
    public byte[] Payload { get; set; }
}
