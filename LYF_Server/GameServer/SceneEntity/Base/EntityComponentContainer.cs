using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>按组件标识管理实体组件，初始化和释放顺序与参考项目装配顺序一致。</summary>
    public sealed class EntityComponentContainer
    {
        private readonly Dictionary<EntityComponentType, EntityComponentBase> _components =
            new Dictionary<EntityComponentType, EntityComponentBase>();
        private readonly List<EntityComponentBase> _ordered = new List<EntityComponentBase>();

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

        public bool Contains(EntityComponentType type)
        {
            return _components.ContainsKey(type);
        }

        public T Get<T>(EntityComponentType type) where T : EntityComponentBase
        {
            EntityComponentBase component;
            if (!_components.TryGetValue(type, out component))
            {
                throw new KeyNotFoundException("实体组件未注册: " + type);
            }
            return (T)component;
        }

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

        public void Initialize()
        {
            for (int i = 0; i < _ordered.Count; i++)
            {
                EntityComponentBase component = _ordered[i];
                Invoke(component, "Initialize", delegate { component.Initialize(); });
            }
        }

        public void Update(int deltaMilliseconds)
        {
            for (int i = 0; i < _ordered.Count; i++)
            {
                EntityComponentBase component = _ordered[i];
                Invoke(component, "Update", delegate { component.Update(deltaMilliseconds); });
            }
        }

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
