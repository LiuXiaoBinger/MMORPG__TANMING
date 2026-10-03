using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROSyncComponent/ROStateSyncComponent 的同步组件。</summary>
    public sealed class SyncComponent : RuntimeEntityComponent
    {
        public SyncComponent(ROObject owner) : base(owner, EntityComponentType.Sync) { }
    }
}
