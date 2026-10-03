using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROFightGroupComponent 的阵营组件。</summary>
    public sealed class FightGroupComponent : RuntimeEntityComponent
    {
        public FightGroupComponent(ROObject owner) : base(owner, EntityComponentType.FightGroup) { }
    }
}
