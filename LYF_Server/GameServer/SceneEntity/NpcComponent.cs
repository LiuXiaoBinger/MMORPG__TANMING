using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 RONpcComponent 的 NPC 组件。</summary>
    public sealed class NpcComponent : RuntimeEntityComponent
    {
        public NpcComponent(ROObject owner) : base(owner, EntityComponentType.Npc) { }
    }
}
