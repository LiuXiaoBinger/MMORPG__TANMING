using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROMercenaryComponent 的佣兵组件。</summary>
    public sealed class MercenaryComponent : RuntimeEntityComponent
    {
        public MercenaryComponent(ROObject owner) : base(owner, EntityComponentType.Mercenary) { }
    }
}
