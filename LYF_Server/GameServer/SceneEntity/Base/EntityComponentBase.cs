using System;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>服务端实体组件基类；与 OnlineRole 的持久化组件完全隔离。</summary>
    public abstract class EntityComponentBase : IEntityComponent
    {
        /// <summary>保存所属实体；场景组件必须依附于一个有效对象。</summary>
        /// <param name="owner">当前组件所属的场景实体。</param>
        protected EntityComponentBase(ROObject owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            Owner = owner;
        }

        /// <summary>组件访问的所属实体；Shutdown 后清空引用。</summary>
        protected ROObject Owner { get; private set; }

        /// <summary>在同一实体的组件容器中标识组件类别。</summary>
        public abstract EntityComponentType ComponentType { get; }

        /// <summary>实体加入世界后调用的初始化入口，派生组件按需扩展。</summary>
        public virtual void Initialize()
        {
        }

        /// <summary>活动实体每个逻辑帧调用的入口，基类不执行业务。</summary>
        /// <param name="deltaMilliseconds">本次逻辑帧经过的毫秒数。</param>
        public virtual void Update(int deltaMilliseconds)
        {
        }

        /// <summary>实体销毁时解除组件对所属对象的引用。</summary>
        public virtual void Shutdown()
        {
            Owner = null;
        }
    }
}
