using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>标记场景对象具有基础实体组件；当前没有额外的逐帧业务。</summary>
    public sealed class EntityComponent : RuntimeEntityComponent
    {
        /// <summary>绑定基础实体组件的所属对象。</summary>
        /// <param name="owner">由实体工厂创建并持有此组件的对象。</param>
        public EntityComponent(ROObject owner) : base(owner, EntityComponentType.Entity) { }
    }
}
