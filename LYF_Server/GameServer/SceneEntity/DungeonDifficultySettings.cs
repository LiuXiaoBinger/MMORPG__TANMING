using System;

/// <summary>副本实例创建时固定的难度选择；重置、晚加入和召唤沿用此对象。</summary>
internal sealed class DungeonDifficultySettings
{
    /// <summary>原副本表编号，各难度复用同一场景与波次。</summary>
    internal int DungeonId { get; private set; }
    /// <summary>规范难度编号，普通1、精英2、宗师3、宗绝4。</summary>
    internal int DifficultyId { get; private set; }
    /// <summary>未单独覆盖时的普攻起手最小间隔，单位毫秒。</summary>
    internal int BasicAttackIntervalMs { get; private set; }

    /// <summary>将MS确认的副本与难度绑定到本次独立实例。</summary>
    /// <param name="dungeonId">已登记的基础副本号。</param>
    /// <param name="degree">难度1至4，旧指令0兼容普通。</param>
    internal DungeonDifficultySettings(int dungeonId, int degree)
    {
        if (degree == 0) degree = 1;
        // 配置查找不改变原表，也不写静态当前难度。
        DungeonDifficultyTable row = LubanMgr.Instance.GetDungeonDifficultyById(degree);
        if (row == null) throw new ArgumentOutOfRangeException(nameof(degree));
        DungeonId = dungeonId;
        DifficultyId = degree;
        BasicAttackIntervalMs = row.BasicAttackIntervalMs;
    }

    /// <summary>查询当前实例中此模板的覆盖；空值代表继承原怪物。</summary>
    /// <param name="entityId">原EntityTable模板号。</param>
    internal DungeonMonsterDifficultyTable Monster(int entityId)
    {
        return LubanMgr.Instance.GetDungeonMonsterDifficulty(DungeonId, DifficultyId, entityId);
    }
}
