/// <summary>RO 同名位移停止参数，按原对象、UUID 和代次只取消所属施法，不拉到理论终点。</summary>
public sealed class MDisplacementStopEventArgs
{
    /// <summary>请求停止的原技能对象，旧 Q 段不得解除新段。</summary>
    public MSkill Source;
    /// <summary>停止请求所属的非零施法 UUID。</summary>
    public ulong skillUuid;
    /// <summary>开始运动时保存的原施法代次，Reset 自增后仍使用原值取消。</summary>
    public ulong fireGeneration;
}
