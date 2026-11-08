/// <summary>同一次原Lua计算得到的权威伤害及命中类型，不在网络层重新抽取暴击。</summary>
internal sealed class MoshentaDamageResult
{
    /// <summary>原Lua写回的整点伤害，尚未裁切目标剩余生命。</summary>
    internal int Damage;
    /// <summary>原DAMAGE_TYPE数值：普通1、暴击2、闪避3等。</summary>
    internal int Type;
    /// <summary>本结果的展示段数；Loop不自动重复结算或增加伤害。</summary>
    internal int Count;
    /// <summary>是否原Lua实际判定为暴击。</summary>
    internal bool Critical { get { return Type == 2; } }
}
