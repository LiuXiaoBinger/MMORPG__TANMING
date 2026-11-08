/// <summary>原SceneAI黑板的具名类型值，拒绝object业务载荷。</summary>
internal sealed class MoshentaSceneVariable
{
    /// <summary>原kTypeInt/kTypeFloat/kTypeString枚举名称。</summary>
    internal string Type;
    /// <summary>类型为整数时的实际值。</summary>
    internal int IntValue;
    /// <summary>类型为浮点时的实际值。</summary>
    internal float FloatValue;
    /// <summary>类型为字符串时的实际值。</summary>
    internal string StringValue;
}
