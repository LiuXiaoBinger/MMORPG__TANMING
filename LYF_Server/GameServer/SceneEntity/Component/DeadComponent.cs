using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 DeadComponent 的死亡状态组件。</summary>
    public sealed class DeadComponent : RuntimeEntityComponent
    {
        public DeadComponent(ROObject owner) : base(owner, EntityComponentType.Dead) { }
    }
}
