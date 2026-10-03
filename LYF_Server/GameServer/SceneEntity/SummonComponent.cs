using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>参考项目 SummonComponent/SummonOwnerComponent 的召唤关系组件。</summary>
    public sealed class SummonComponent : RuntimeEntityComponent
    {
        public SummonComponent(ROObject owner) : base(owner, EntityComponentType.Summon) { }
    }
}
