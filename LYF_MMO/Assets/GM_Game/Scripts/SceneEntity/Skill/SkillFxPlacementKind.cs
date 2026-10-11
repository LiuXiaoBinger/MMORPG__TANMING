/// <summary>技能特效生成位置的确定性语义，编辑器与运行时使用同一枚举。</summary>
public enum SkillFxPlacementKind
{
    /// <summary>角色根节点局部坐标，跟随角色变换。</summary>
    ActorRoot = 0,

    /// <summary>角色模型内具名节点局部坐标，节点路径必须存在。</summary>
    NamedSocket = 1,

    /// <summary>目标模型头顶世界坐标，无目标时采用既有施法者前方落点。</summary>
    TargetTop = 2,

    /// <summary>施法者当前世界位置沿朝向偏移指定米数。</summary>
    CasterForwardWorld = 3,

    /// <summary>旧配置未指定父节点时的绝对世界坐标。</summary>
    WorldAbsolute = 4
}
