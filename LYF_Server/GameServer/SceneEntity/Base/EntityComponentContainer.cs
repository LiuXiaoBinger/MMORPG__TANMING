using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>按组件标识管理实体组件，初始化和释放顺序与参考项目装配顺序一致。</summary>
    public sealed class EntityComponentContainer
    {
        /// <summary>按类别定位组件，同一实体中每类最多登记一个。</summary>
        private readonly Dictionary<EntityComponentType, EntityComponentBase> _components =
            new Dictionary<EntityComponentType, EntityComponentBase>();

        /// <summary>保留添加顺序：按顺序初始化和更新，按反序释放。</summary>
        private readonly List<EntityComponentBase> _ordered = new List<EntityComponentBase>();

        #region 生命周期
        /// <summary>依次初始化已登记组件；单个组件失败会记录日志，后续组件仍继续。</summary>
        public void Initialize()
        {
            for (int i = 0; i < _ordered.Count; i++)
            {
                EntityComponentBase component = _ordered[i];
                Invoke(component, "Initialize", delegate { component.Initialize(); });
            }
        }

        /// <summary>按装配顺序推进全部组件。</summary>
        /// <param name="deltaMilliseconds">本帧经过的毫秒数。</param>
        public void Update(int deltaMilliseconds)
        {
            for (int i = 0; i < _ordered.Count; i++)
            {
                EntityComponentBase component = _ordered[i];
                Invoke(component, "Update", delegate { component.Update(deltaMilliseconds); });
            }
        }

        /// <summary>按装配的相反顺序释放组件，再清空索引和顺序列表。</summary>
        public void Shutdown()
        {
            for (int i = _ordered.Count - 1; i >= 0; i--)
            {
                EntityComponentBase component = _ordered[i];
                Invoke(component, "Shutdown", delegate { component.Shutdown(); });
            }
            _ordered.Clear();
            _components.Clear();
        }
        #endregion

        /// <summary>登记组件和装配顺序；重复类别被视为装配错误。</summary>
        /// <param name="component">要登记的组件实例。</param>
        public void Add(EntityComponentBase component)
        {
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }
            if (_components.ContainsKey(component.ComponentType))
            {
                throw new InvalidOperationException("实体组件重复注册: " + component.ComponentType);
            }
            _components.Add(component.ComponentType, component);
            _ordered.Add(component);
        }

        #region 查询与判定
        /// <summary>判断实体是否已登记指定类别的组件。</summary>
        /// <param name="type">要查找的组件类别。</param>
        public bool Contains(EntityComponentType type)
        {
            return _components.ContainsKey(type);
        }

        /// <summary>取得指定类别的组件；缺少组件时抛出异常。</summary>
        /// <typeparam name="T">调用者期望的具体组件类型。</typeparam>
        /// <param name="type">组件容器的类别键。</param>
        public T Get<T>(EntityComponentType type) where T : EntityComponentBase
        {
            EntityComponentBase component;
            if (!_components.TryGetValue(type, out component))
            {
                throw new KeyNotFoundException("实体组件未注册: " + type);
            }
            return (T)component;
        }

        /// <summary>尝试同时按类别和具体类型取组件，缺失或类型不符时返回 false。</summary>
        /// <typeparam name="T">调用者期望的具体组件类型。</typeparam>
        /// <param name="type">要查找的组件类别。</param>
        /// <param name="component">成功时返回原组件实例，失败时为 null。</param>
        public bool TryGet<T>(EntityComponentType type, out T component) where T : EntityComponentBase
        {
            EntityComponentBase value;
            if (_components.TryGetValue(type, out value))
            {
                component = value as T;
                return component != null;
            }
            component = null;
            return false;
        }
        #endregion

        /// <summary>执行一个生命周期回调，并把失败记录为组件名和方法名。</summary>
        /// <param name="component">执行回调的组件。</param>
        /// <param name="method">用于定位失败的生命周期方法名。</param>
        /// <param name="action">本次要执行的初始化、更新或释放操作。</param>
        private static void Invoke(EntityComponentBase component, string method, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                LogMsg.Info("实体组件生命周期失败: " + component.GetType().Name + "." + method + "，" + exception,
                    LogMsgType.Error);
            }
        }
    }
}
