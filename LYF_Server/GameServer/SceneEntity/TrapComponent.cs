using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>陷阱专用触发组件。</summary>
    public sealed class TrapComponent : RuntimeEntityComponent
    {
        public TrapComponent(ROObject owner) : base(owner, EntityComponentType.Trap) { }
    }
}
