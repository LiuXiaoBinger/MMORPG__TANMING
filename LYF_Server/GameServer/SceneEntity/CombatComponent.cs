using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROCombatComponent 的战斗入口组件。</summary>
    public sealed class CombatComponent : RuntimeEntityComponent
    {
        public CombatComponent(ROObject owner) : base(owner, EntityComponentType.Combat) { }
    }
}
