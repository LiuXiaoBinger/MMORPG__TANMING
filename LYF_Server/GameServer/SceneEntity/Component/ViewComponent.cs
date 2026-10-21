using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROObjectViewComponent 的视野组件。</summary>
    public sealed class ViewComponent : RuntimeEntityComponent
    {
        public ViewComponent(ROObject owner) : base(owner, EntityComponentType.View) { }
    }
}
