/// <summary>
/// 次数/活动数据的周期刷新标记；kRefreshMarkCount 仅作数组上界哨兵，不代表真实周期。
/// </summary>
public enum RefreshMark
{
    kRefreshMarkNone = 0,
    kRefreshMarkDayZero,      //>> 每天凌晨0点
    kRefreshMarkDayFive,      //>> 每天凌晨5点
    kRefreshMarkWeekZero,     //>> 每周凌晨0点
    kRefreshMarkWeekFive,     //>> 每周凌晨5点
    kRefreshMarkMonthZero,    //>> 每月一号凌晨0点
    kRefreshMarkMonthFive,    //>> 每月一号凌晨5点
    kRefreshMarkDayTwelve,    //>> 每天中午12点
    kRefreshMarkCount
};