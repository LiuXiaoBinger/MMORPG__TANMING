using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROAIComponent 的 AI 组件。</summary>
    public sealed class AiComponent : RuntimeEntityComponent
    {
        public AiComponent(ROObject owner) : base(owner, EntityComponentType.AI) { }
    }
}
