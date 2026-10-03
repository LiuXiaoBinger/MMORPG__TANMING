/// <summary>
/// RO 常用动作状态名常量定义。
/// 状态组件和 MAnimator 只通过这些常量访问模型动画，避免硬编码字符串散落各处。
/// </summary>
public static class MAnimationStateNames
{
    /// <summary>待机动画状态名。</summary>
    public const string Idle = "Idle";
    /// <summary>行走/移动动画状态名。</summary>
    public const string Move = "Move";
    /// <summary>女性待机动画状态名。</summary>
    public const string FIdle = "FIdle";
    /// <summary>女性移动动画状态名。</summary>
    public const string FMove = "FMove";
    /// <summary>出生/登场动画状态名。</summary>
    public const string Born = "Born";
    /// <summary>死亡动画状态名。</summary>
    public const string Dead = "Dead";
    /// <summary>受击动画状态名。</summary>
    public const string BeHit = "BeHit";
    /// <summary>眩晕动画状态名。</summary>
    public const string Stun = "Stun";
    /// <summary>吟唱动画状态名。</summary>
    public const string Singing = "Singing";
    /// <summary>技能动画状态名。</summary>
    public const string Skill = "Skill";
    /// <summary>特殊动作动画状态名。</summary>
    public const string Special = "Special";
    /// <summary>攀爬动画状态名。</summary>
    public const string Climb = "Climb";
    /// <summary>采集动画状态名。</summary>
    public const string Collect = "Collect";
    /// <summary>钓鱼动画状态名。</summary>
    public const string Fishing = "Fishing";
    /// <summary>跳舞动画状态名。</summary>
    public const string Dance = "Dance";
    /// <summary>跳跃起跳动画状态名；三段跳共用同一个起跳剪辑。</summary>
    public const string JumpStart = "Jump_Start";
    /// <summary>跳跃空中循环动画状态名；三段跳共用同一个循环剪辑。</summary>
    public const string JumpLoop = "Jump_Loop";
    /// <summary>跳跃落地动画状态名；三段跳共用同一个落地剪辑。</summary>
    public const string JumpEnd = "Jump_End";
}
