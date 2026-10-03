using System;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>服务端实体组件基类；与 OnlineRole 的持久化组件完全隔离。</summary>
    public abstract class EntityComponentBase : IEntityComponent
    {
        protected EntityComponentBase(ROObject owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
        }

        protected ROObject Owner { get; private set; }

        public abstract EntityComponentType ComponentType { get; }

        public virtual void Initialize()
        {
        }

        public virtual void Update(int deltaMilliseconds)
        {
        }

        public virtual void Shutdown()
        {
            Owner = null;
        }
    }
}
