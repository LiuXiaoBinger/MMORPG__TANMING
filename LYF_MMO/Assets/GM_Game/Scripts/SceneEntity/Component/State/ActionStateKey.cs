using System;
using MMoRpgCommon.SceneEntity;

/// <summary>动作状态索引键，保留状态定义和实体类别用于扩展差异化状态表。</summary>
public struct ActionStateKey : IEquatable<ActionStateKey>
{
    /// <summary>状态枚举。</summary>
    public readonly StateDefine State;
    /// <summary>实体类型，用于区分不同实体类别下的同名状态。</summary>
    public readonly EntityType EntityType;

    /// <summary>构造复合键。</summary>
    /// <param name="state">状态枚举。</param>
    /// <param name="entityType">实体类型。</param>
    public ActionStateKey(StateDefine state, EntityType entityType)
    {
        State = state;
        EntityType = entityType;
    }

    /// <summary>强类型相等比较，两个字段都相等才视为相等。</summary>
    /// <param name="other">另一个复合键。</param>
    /// <returns>相等返回 true。</returns>
    public bool Equals(ActionStateKey other)
    {
        return State == other.State && EntityType == other.EntityType;
    }

    /// <summary>object 相等比较，类型不匹配时返回 false。</summary>
    /// <param name="obj">比较对象。</param>
    /// <returns>相等返回 true。</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is ActionStateKey))
        {
            return false;
        }
        return Equals((ActionStateKey)obj);
    }

    /// <summary>计算哈希码，使用 397 质数乘法组合两个字段。</summary>
    /// <returns>哈希码。</returns>
    public override int GetHashCode()
    {
        return ((int)State * 397) ^ (int)EntityType;
    }
}
