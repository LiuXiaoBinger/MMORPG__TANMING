using System;

/// <summary>给一次数据变化分配编号并记录来源，便于把同次操作影响的多个模块关联起来。</summary>
public sealed class ClientChangeContext
{
    /// <summary>一次跨模块操作的唯一事务编号。</summary>
    public Guid TransactionId { get; private set; }
    /// <summary>产生变化的业务来源名称。</summary>
    public string Source { get; private set; }

    /// <summary>为这次变化生成新编号；source 是来源名称，为空时记为 Unknown。</summary>
    public ClientChangeContext(string source)
    {
        TransactionId = Guid.NewGuid();
        if (string.IsNullOrEmpty(source))
        {
            Source = "Unknown";
        }
        else
        {
            Source = source;
        }
    }
}
