using System;

/// <summary>副本实例创建时固定的难度选择；重置、晚加入和召唤沿用此对象。</summary>
internal sealed class DungeonDifficultySettings
{
    /// <summary>原副本表编号，各难度复用同一场景与波次。</summary>
    internal int DungeonId { get; private set; }
    /// <summary>规范难度表主键，生命周期内不因切换其他队伍而改变。</summary>
    internal int DifficultyId { get; private set; }
    /// <summary>本实例已确认的层数；非分层为0，分层从1开始。</summary>
    internal int Layer { get; private set; }
    /// <summary>未单独覆盖时的普攻起手最小间隔，单位毫秒。</summary>
    internal int BasicAttackIntervalMs { get; private set; }

    /// <summary>将MS确认的副本与难度绑定到本次独立实例。</summary>
    /// <param name="dungeonId">已登记的基础副本号。</param>
    /// <param name="degree">难度表主键，旧指令0兼容1。</param>
    /// <param name="layer">所选层数；旧入口默认0，按难度表决定是否规范为首层。</param>
    internal DungeonDifficultySettings(int dungeonId, int degree, int layer = 0)
    {
        // 配置查找不改变原表，也不写静态当前难度或层数。
        DungeonDifficultyTable row;
        // 入口、实例构造共用同一规范，避免旧请求分层默认值不同。
        int normalizedLayer;
        if (!LubanMgr.Instance.TryGetDungeonDifficulty(degree, layer, out row, out normalizedLayer))
            throw new ArgumentOutOfRangeException(nameof(layer), "副本难度或层数未登记");
        DungeonId = dungeonId;
        DifficultyId = row.Id;
        Layer = normalizedLayer;
        BasicAttackIntervalMs = row.BasicAttackIntervalMs;
    }

    /// <summary>查询当前实例中此模板的覆盖；空值代表继承原怪物。</summary>
    /// <param name="entityId">原EntityTable模板号。</param>
    internal DungeonMonsterDifficultyTable Monster(int entityId)
    {
        return LubanMgr.Instance.GetDungeonMonsterDifficulty(DungeonId, DifficultyId, Layer, entityId);
    }
}
