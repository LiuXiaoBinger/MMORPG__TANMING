/// <summary>
/// 物品变动原因的附加参数类型。
/// </summary>
public enum RExtraParamType
{
    kExtraParamTypeNone = 0,       // 无附加参数类型。
    kExtraParamTypeKillRole = 1,   // 击杀玩家获得总积分。
    kExtraParamTypeAssist = 2,     // 助攻获得总积分。
    kExtraParamTypeKillMonster = 3,// 击杀怪物。
    kExtraParamTypeCollect = 4     // 采集物。
}
