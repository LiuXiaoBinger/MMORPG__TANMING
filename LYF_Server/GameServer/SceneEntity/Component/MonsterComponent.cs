using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROMonsterComponent 的怪物组件。</summary>
    public sealed class MonsterComponent : RuntimeEntityComponent
    {
        public MonsterComponent(ROObject owner) : base(owner, EntityComponentType.Monster) { }
    }
}
