using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 RONavigationComponent 的导航组件。</summary>
    public sealed class NavigationComponent : RuntimeEntityComponent
    {
        public NavigationComponent(ROObject owner) : 
            base(owner, EntityComponentType.Navigation) { }
    }
}
