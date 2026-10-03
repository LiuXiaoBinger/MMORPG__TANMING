using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 MoveComponent 的移动组件。</summary>
    public sealed class MoveComponent : RuntimeEntityComponent
    {
        public MoveComponent(ROObject owner) : base(owner, EntityComponentType.Move) { }
    }
}
