/// <summary>
/// 场景时间段类型。取值与 RO 源工程 MEnvorimentData.DaytimeType 逐项一致，
/// 用于选择时间段相关参数；当前环境组件仅为 Dusk 提供单独的雾参数分支。
/// </summary>
public enum MDaytimeType
{
    /// <summary>未指定时间段，环境应用时走默认（非分时段）参数</summary>
    Nothing = 0,

    /// <summary>白天</summary>
    Day = 1,

    /// <summary>夜晚</summary>
    Night = 2,

    /// <summary>黄昏，启用分时雾时读取天气配置中的 Dusk 参数</summary>
    Dusk = 4,

    /// <summary>黎明</summary>
    Dawn = 8
}
