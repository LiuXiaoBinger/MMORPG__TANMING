/// <summary>
/// 怪物档位，作为普通、精英、Boss 等分类数据保存。
/// 它与 EntityType 是两个独立字段，设置档位不会自动修改实体类别。
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
