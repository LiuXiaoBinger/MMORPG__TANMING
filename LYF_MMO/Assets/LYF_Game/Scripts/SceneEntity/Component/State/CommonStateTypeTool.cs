/// <summary>
/// 用固定表判断一种动作状态能否切到另一种状态。
/// 行是当前状态，列是目标状态；索引顺序必须与 StateDefine 一致。
/// </summary>
public static class CommonStateTypeTool
{
    /// <summary>
    /// 状态切换许可矩阵，行=当前状态，列=目标状态，true 表示允许切换。
    /// 列顺序：默认、吟唱、技能、特殊、死亡、恐惧、受击、眩晕、攀爬、采集、出生、钓鱼、双动作、跳舞、场景触发。
    /// </summary>
    private static readonly bool[,] StateMap =
    {
        // 默认、吟唱、技能、特殊、死亡、恐惧、受击、眩晕、攀爬、采集、出生、钓鱼、双动作、跳舞、场景触发
        { false, true,  true,  true,  true,  true,  true,  true,  true,  true,  false, true,  true,  true,  true  },
        { true,  false, true,  false, true,  true,  false, true,  false, false, false, false, false, false, true  },
        { true,  true,  false, false, true,  true,  false, true,  false, false, false, false, false, false, true  },
        { true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, true,  true,  true,  true  },
        { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
        { true,  false, false, false, true,  false, true,  true,  false, false, false, false, false, false, false },
        { true,  true,  true,  false, true,  true,  true,  true,  false, true,  false, false, false, false, false },
        { true,  false, false, false, true,  false, false, false, false, false, false, false, false, false, false },
        { true,  false, false, false, true,  false, true,  false, true,  false, false, false, false, false, false },
        { true,  false, false, false, true,  true,  false, true,  false, false, false, false, false, false, false },
        { true,  false, false, false, true,  false, false, false, false, false, false, false, false, false, false },
        { true,  true,  true,  false, true,  true,  true,  true,  false, false, false, false, false, false, false },
        { true,  false, false, false, true,  true,  true,  true,  false, false, false, false, false, false, false },
        { true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true  },
        { true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, true,  true,  true,  true  }
    };

    /// <summary>判断当前状态是否允许切换到目标状态，非法枚举值一律拒绝。</summary>
    /// <param name="source">当前状态枚举。</param>
    /// <param name="destination">目标状态枚举。</param>
    /// <returns>允许切换返回 true，任一枚举越界或矩阵中为 false 时返回 false。</returns>
    public static bool StatePermitted(StateDefine source, StateDefine destination)
    {
        // 当前状态对应的表行号。
        int sourceIndex = (int)source;
        // 目标状态对应的表列号。
        int destinationIndex = (int)destination;
        // 有效状态个数，不包括末尾的 kStateCount 标记。
        int stateCount = (int)StateDefine.kStateCount;
        // 越界枚举一律拒绝，避免数组访问异常
        if (sourceIndex < 0 || sourceIndex >= stateCount)
        {
            return false;
        }
        if (destinationIndex < 0 || destinationIndex >= stateCount)
        {
            return false;
        }
        return StateMap[sourceIndex, destinationIndex];
    }
}
