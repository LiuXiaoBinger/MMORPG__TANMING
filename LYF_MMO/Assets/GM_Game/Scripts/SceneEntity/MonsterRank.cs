/// <summary>
/// 参考项目怪物实体等级枚举。
/// 等级用于区分怪物强弱档位，但不改变 Monster 的实体根类型。
/// </summary>
public enum MonsterRank
{
    /// <summary>普通怪物</summary>
    Normal = 0,
    /// <summary>精英怪物</summary>
    Elite = 1,
    /// <summary>BOSS怪物</summary>
    Boss = 2,
    /// <summary>召唤兽/小怪群</summary>
    Mob = 3,
    /// <summary>迷你怪物</summary>
    Mini = 4,
    /// <summary>MVP怪物（强力精英）</summary>
    Mvp = 5
}
