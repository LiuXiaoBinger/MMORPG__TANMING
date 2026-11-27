using System;

/// <summary>终绝周挑战共用的周五分界和已发布角色次数键，周键为北京时间周期首日的日序。</summary>
public static class DungeonFinalWeek
{
    /// <summary>规范难度表中的终绝编号。</summary>
    public const int Difficulty = 6;
    /// <summary>按周键持久化的最高完整通关层。</summary>
    public const int Highest = 14;
    /// <summary>按周键持久化的不同副本种数。</summary>
    public const int Distinct = 15;
    /// <summary>按基础副本编号保存最后一次计入种数的周键。</summary>
    public const int DungeonSeen = 16;
    /// <summary>按周键持久化的宝箱领取标志，1 为已领取。</summary>
    public const int Claimed = 17;

    /// <summary>计算当前 UTC 时刻所属的北京周五零时周期。</summary>
    /// <param name="utc">游戏服或展示端的 UTC 时间。</param>
    public static int Period(DateTime utc)
    {
        // 北京采用固定东八区，周五到下周四属于同一期。
        DateTime localDay = utc.AddHours(8).Date;
        int daysAfterFriday = ((int)localDay.DayOfWeek + 2) % 7;
        return (int)(localDay.Ticks / TimeSpan.TicksPerDay) - daysAfterFriday;
    }

    /// <summary>把角色存档中的周键转换为北京周五日期，供进度标签和倒计时显示。</summary>
    /// <param name="period">北京周五的日序号。</param>
    public static DateTime Friday(int period) { return new DateTime((long)period * TimeSpan.TicksPerDay); }
}
