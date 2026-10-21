using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目技能组件的共享运行时占位。</summary>
    public sealed class SkillComponent : RuntimeEntityComponent
    {
        public SkillComponent(ROObject owner) : base(owner, EntityComponentType.Skill) { }
    }
}
