/// <summary>
/// 记录一次客户端先行播放的施法，以及等待服务器确认的截止时间。
/// 技能动画结束不等于确认完成，因此记录可以继续保留到确认或超时。
/// </summary>
public sealed class MSkillPrediction
{
    /// <summary>本次施法请求与服务器通知共用的非零 UUID。</summary>
    public ulong SkillUuid { get; private set; }
    /// <summary>本次预测前的单技能冷却截止时间，单位为本机不缩放秒数。</summary>
    public float PreviousCooldownEnd { get; private set; }
    /// <summary>等待服务器确认的截止时刻，使用 Time.realtimeSinceStartup 的秒数；由创建者计算。</summary>
    public float DeadlineRealtime { get; private set; }

    /// <summary>创建空预测记录，正式 UUID 在借出后由 Initialize 写入。</summary>
    public MSkillPrediction()
    {
    }

    /// <summary>写入一次预表现的 UUID 与两个不缩放时钟截止值。</summary>
    /// <param name="skillUuid">请求与通知共用的非零施法 UUID。</param>
    /// <param name="previousCooldownEnd">预测前单技能 CD 截止秒数。</param>
    /// <param name="deadlineRealtime">服务器确认最晚到达的不缩放秒数。</param>
    public void Initialize(ulong skillUuid, float previousCooldownEnd, float deadlineRealtime)
    {
        SkillUuid = skillUuid;
        PreviousCooldownEnd = previousCooldownEnd;
        DeadlineRealtime = deadlineRealtime;
    }

    /// <summary>归还前清除旧 UUID 与时钟，防止下一次借用匹配旧施法。</summary>
    public void Reset()
    {
        SkillUuid = 0;
        PreviousCooldownEnd = 0f;
        DeadlineRealtime = 0f;
    }
}
