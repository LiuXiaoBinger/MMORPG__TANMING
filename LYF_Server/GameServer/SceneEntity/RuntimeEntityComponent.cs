using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>实体运行时的轻量组件标记，实现公共生命周期并保留组件类别。</summary>
    public class RuntimeEntityComponent : EntityComponentBase
    {
        private readonly EntityComponentType _componentType;

        public RuntimeEntityComponent(ROObject owner, EntityComponentType componentType)
            : base(owner)
        {
            _componentType = componentType;
        }

        public override EntityComponentType ComponentType
        {
            get { return _componentType; }
        }
    }
}
