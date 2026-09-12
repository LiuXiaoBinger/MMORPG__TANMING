using System;
using System.Collections.Generic;

/// <summary>按注册顺序管理客户端角色组件生命周期。</summary>
public sealed class RoleComponentContainer
{
    /// <summary>组件类型索引。</summary>
    private readonly Dictionary<Type, RoleComponentBase> _components =
        new Dictionary<Type, RoleComponentBase>();

    /// <summary>组件稳定生命周期顺序。</summary>
    private readonly List<RoleComponentBase> _orderedComponents =
        new List<RoleComponentBase>();

    /// <summary>创建角色组件容器。</summary>
    public RoleComponentContainer(ClientRole owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
    }

    /// <summary>注册组件，同一具体类型只能注册一次。</summary>
    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        if (component == null)
        {
            throw new ArgumentNullException("component");
        }

        Type type = typeof(T);
        if (_components.ContainsKey(type))
        {
            throw new InvalidOperationException("角色组件重复注册: " + type.Name);
        }
        _components.Add(type, component);
        _orderedComponents.Add(component);
    }

    /// <summary>获取已经注册的组件。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (!_components.TryGetValue(typeof(T), out value))
        {
            throw new KeyNotFoundException("角色组件未注册: " + typeof(T).Name);
        }
        return (T)value;
    }

    /// <summary>尝试获取已经注册的组件。</summary>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (_components.TryGetValue(typeof(T), out value))
        {
            component = (T)value;
            return true;
        }
        component = null;
        return false;
    }

    /// <summary>初始化全部组件。</summary>
    public void Initialize()
    {
        InvokeAll("Initialize", component => component.Initialize());
    }

    /// <summary>通知全部组件完成角色快照装载。</summary>
    public void AfterLoad()
    {
        InvokeAll("AfterLoad", component => component.AfterLoad());
    }

    /// <summary>通知全部组件角色已经加入客户端角色世界。</summary>
    public void AfterAddedToWorld()
    {
        InvokeAll("AfterAddedToWorld", component => component.AfterAddedToWorld());
    }

    /// <summary>更新全部组件。</summary>
    public void Update(int deltaMilliseconds)
    {
        InvokeAll("Update", component => component.Update(deltaMilliseconds));
    }

    /// <summary>按注册逆序释放全部组件。</summary>
    public void Dispose()
    {
        for (int index = _orderedComponents.Count - 1; index >= 0; index--)
        {
            RoleComponentBase component = _orderedComponents[index];
            Invoke(component, "Dispose", component.Dispose);
        }
        _orderedComponents.Clear();
        _components.Clear();
    }

    /// <summary>按注册顺序执行组件生命周期函数。</summary>
    private void InvokeAll(string name, Action<RoleComponentBase> action)
    {
        for (int index = 0; index < _orderedComponents.Count; index++)
        {
            RoleComponentBase component = _orderedComponents[index];
            Invoke(component, name, () => action(component));
        }
    }

    /// <summary>隔离单组件异常，避免中断其他组件生命周期。</summary>
    private static void Invoke(RoleComponentBase component, string name, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            LogMsg.Info("角色组件生命周期失败: " + component.GetType().Name + "." +
                name + "，" + exception, LogMsgType.Error);
        }
    }
}
