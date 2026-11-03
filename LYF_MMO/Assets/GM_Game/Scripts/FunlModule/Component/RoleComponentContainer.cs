using System;
using System.Collections.Generic;

/// <summary>保存角色的业务组件，按加入顺序初始化和更新，释放时按相反顺序处理。</summary>
public sealed class RoleComponentContainer
{
    /// <summary>组件类型索引。</summary>
    private readonly Dictionary<Type, RoleComponentBase> _components =
        new Dictionary<Type, RoleComponentBase>();

    /// <summary>按加入先后保存组件，后续生命周期回调沿用这个顺序。</summary>
    private readonly List<RoleComponentBase> _orderedComponents =
        new List<RoleComponentBase>();

    #region 构造与初始化
    /// <summary>检查所属角色 owner 不为空；角色绑定保存在各组件自身。</summary>
    public RoleComponentContainer(PlayerEntity owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }
    }

    /// <summary>更新全部组件。</summary>
    public void Update(int deltaMilliseconds)
    {
        InvokeAll("Update", component => component.Update(deltaMilliseconds));
    }

    /// <summary>初始化全部组件。</summary>
    public void Initialize()
    {
        InvokeAll("Initialize", component => component.Initialize());
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
    #endregion

    #region 查询与判定
    /// <summary>按类型 T 取得组件；没有加入该类型时抛出异常。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (!_components.TryGetValue(typeof(T), out value))
        {
            throw new KeyNotFoundException("角色组件未注册: " + typeof(T).Name);
        }
        return (T)value;
    }

    /// <summary>按类型 T 查找组件并写入 component；未找到时输出 null 并返回 false。</summary>
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
    #endregion

    #region 核心逻辑
    /// <summary>注册组件，同一具体类型只能注册一次。</summary>
    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        if (component == null)
        {
            throw new ArgumentNullException("component");
        }

        // 用调用方指定的类型 T 作为字典键。
        Type type = typeof(T);
        if (_components.ContainsKey(type))
        {
            throw new InvalidOperationException("角色组件重复注册: " + type.Name);
        }
        _components.Add(type, component);
        _orderedComponents.Add(component);
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

    /// <summary>按注册顺序执行组件生命周期函数。</summary>
    private void InvokeAll(string name, Action<RoleComponentBase> action)
    {
        for (int index = 0; index < _orderedComponents.Count; index++)
        {
            RoleComponentBase component = _orderedComponents[index];
            Invoke(component, name, () => action(component));
        }
    }

    /// <summary>执行 component 的 action；发生异常时记录组件类型和阶段 name，外层继续处理其他组件。</summary>
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
    #endregion
}
