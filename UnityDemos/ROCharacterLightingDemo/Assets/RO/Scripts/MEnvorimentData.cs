using System;

/// <summary>
/// 场景环境数据组（对应 RO 源工程 MoonSerializable/Effect/MEnvorimentData.cs）。
/// 源类型由「光照图 + 天色 + 天气」三部分组成，是 MEnvorimentSetting.ApplyEnvorimentData 的入参；
/// 本 Demo 只迁入角色光照需要的天色部分，光照图与天气字段按文档说明裁剪（原因：对应加载/合批/天气特效链不迁入）。
/// </summary>
[Serializable]
public class MEnvorimentData
{
    /// <summary>天气类型位标志，源实现用于物件隔离与天气段选择；本 Demo 保留枚举以免资产/代码语义断裂，不做天气表现。</summary>
    public enum WeatherType
    {
        /// <summary>无天气（0）。</summary>
        Nothing = 0,

        /// <summary>晴（1）。</summary>
        Sunny = 1,

        /// <summary>多云（2）。</summary>
        Cloudy = 2,

        /// <summary>雾（4）。</summary>
        Fog = 4,

        /// <summary>小雨（8）。</summary>
        LightRain = 8,

        /// <summary>大雨（16）。</summary>
        HeavyRain = 16,

        /// <summary>雷雨（32）。</summary>
        ThunderRain = 32,

        /// <summary>小雪（64）。</summary>
        LightSnow = 64,

        /// <summary>大雪（128）。</summary>
        HeavySnow = 128,

        /// <summary>沙尘（256）。</summary>
        SandStorm = 256,

        /// <summary>雨后（512）。</summary>
        AfterRain = 512,

        /// <summary>雪后（1024）。</summary>
        AfterSnow = 1024
    }

    /// <summary>时段类型位标志：源实现用它选雾色档位、控制物件显隐；角色光照链用它记录当前天色。</summary>
    public enum DaytimeType
    {
        /// <summary>未指定（0）。</summary>
        Nothing = 0,

        /// <summary>白天（1）。</summary>
        Day = 1,

        /// <summary>夜晚（2）。</summary>
        Night = 2,

        /// <summary>黄昏（4）。</summary>
        Dusk = 4,

        /// <summary>黎明（8）。</summary>
        Dawn = 8
    }

    /// <summary>时间段数据（角色光四色 + 方向都在这里）。源类型里的 lightmapData / weatherData 未迁入：本 Demo 不接光照图切换与天气链。</summary>
    public MDayTimeData dayTimeData;
}
