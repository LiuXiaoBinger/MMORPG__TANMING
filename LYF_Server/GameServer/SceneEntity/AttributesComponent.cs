using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 AttributeComponent 的战斗属性组件。</summary>
    public sealed class AttributesComponent : RuntimeEntityComponent
    {
        public AttributesComponent(ROObject owner) : base(owner, EntityComponentType.Attributes) { }
    }
}
