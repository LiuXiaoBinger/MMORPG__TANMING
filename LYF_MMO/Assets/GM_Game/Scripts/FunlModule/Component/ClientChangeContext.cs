using System;

/// <summary>跨模块变化的通用上下文，供背包与商城、拍卖行等事务关联。</summary>
public sealed class ClientChangeContext
{
    /// <summary>一次跨模块操作的唯一事务编号。</summary>
    public Guid TransactionId { get; private set; }
    /// <summary>产生变化的业务来源名称。</summary>
    public string Source { get; private set; }

    /// <summary>创建变化上下文。</summary>
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
