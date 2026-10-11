/// <summary>RO 同名单技能或公共组冷却记录，由组件列表和字典共享持有，到期或清场后归还工厂池。</summary>
public sealed class SkillCDGroupInfo
{
    /// <summary>单技能列表中为技能主表 ID，公共组列表中为 CooldownGroupId；两类索引相互独立。</summary>
    public int SkillGroupType;
    /// <summary>本次设置的总秒数；与 CurrentCD 在设置时同值，之后保留以供进度查询。</summary>
    public float CD;
    /// <summary>最后一次更新时的非负剩余秒数，归零时从所属列表和字典一起删除。</summary>
    public float CurrentCD;
    /// <summary>CurrentCD 对应的本机不缩放秒数；帧间查询及更新从此时刻扣减，避免缩放或毫秒取整导致双时钟。</summary>
    internal float UpdatedRealtime;
}
