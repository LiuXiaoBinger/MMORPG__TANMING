using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROCollectionComponent 的采集组件。</summary>
    public sealed class CollectionComponent : RuntimeEntityComponent
    {
        public CollectionComponent(ROObject owner) : base(owner, EntityComponentType.Collection) { }
    }
}
