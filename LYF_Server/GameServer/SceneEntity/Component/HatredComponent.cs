using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 HatredComponent 的仇恨组件。</summary>
    public sealed class HatredComponent : RuntimeEntityComponent
    {
        public HatredComponent(ROObject owner) : base(owner, EntityComponentType.Hatred) { }
    }
}
