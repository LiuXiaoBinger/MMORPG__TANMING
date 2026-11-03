using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>标记实体装配了战斗入口组件；当前只保存组件类别和所属实体，尚无独立的战斗入口处理。</summary>
    public sealed class CombatComponent : RuntimeEntityComponent
    {
        /// <summary>把此组件绑定到所属场景实体，并登记固定的组件类别。</summary>
        /// <param name="owner">拥有此组件的场景实体，释放组件时解除引用。</param>
        public CombatComponent(ROObject owner) : base(owner, EntityComponentType.Combat) { }
    }
}
