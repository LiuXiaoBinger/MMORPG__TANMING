using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>实体运行时的轻量组件标记，实现公共生命周期并保留组件类别。</summary>
    public class RuntimeEntityComponent : EntityComponentBase
    {
        /// <summary>构造时确定的类别，用作实体组件容器的查找键。</summary>
        private readonly EntityComponentType _componentType;

        /// <summary>保存组件归属与类别，供没有独立业务的组件标记复用。</summary>
        /// <param name="owner">持有组件的场景实体。</param>
        /// <param name="componentType">该组件在容器中的唯一类别。</param>
        public RuntimeEntityComponent(ROObject owner, EntityComponentType componentType)
            : base(owner)
        {
            _componentType = componentType;
        }

        /// <summary>返回构造时登记的组件类别。</summary>
        public override EntityComponentType ComponentType
        {
            get { return _componentType; }
        }
    }
}
