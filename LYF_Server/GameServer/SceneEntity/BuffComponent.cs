using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROBuffComponent 的状态效果组件。</summary>
    public sealed class BuffComponent : RuntimeEntityComponent
    {
        public BuffComponent(ROObject owner) : base(owner, EntityComponentType.Buff) { }
    }
}
