using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROSceneComponent/ROSceneObjectComponent 的场景组件。</summary>
    public sealed class SceneComponent : RuntimeEntityComponent
    {
        public SceneComponent(ROObject owner) : base(owner, EntityComponentType.Scene) { }
    }
}
