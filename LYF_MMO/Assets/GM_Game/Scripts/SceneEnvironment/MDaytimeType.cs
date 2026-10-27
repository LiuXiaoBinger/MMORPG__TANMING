/// <summary>
/// 场景时间段类型。取值与 RO 源工程 MEnvorimentData.DaytimeType 逐项一致，
/// 用于选择对应时间段的雾色等环境参数；本场景固定使用 Dusk（黄昏）。
/// </summary>
public enum MDaytimeType
{
    /// <summary>未指定时间段，环境应用时走默认（非分时段）参数</summary>
    Nothing = 0,

    /// <summary>白天</summary>
    Day = 1,

    /// <summary>夜晚</summary>
    Night = 2,

    /// <summary>黄昏，魔神塔场景使用的时段</summary>
    Dusk = 4,

    /// <summary>黎明</summary>
    Dawn = 8
}
