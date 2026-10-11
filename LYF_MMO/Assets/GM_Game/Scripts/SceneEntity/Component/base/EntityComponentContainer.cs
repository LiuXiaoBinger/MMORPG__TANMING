using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 只管理普通实体组件的容器，Move/Buff/Skill/Machine 不进入此集合。
/// 内部同时维护按类型索引的字典和按注册顺序排列的列表，初始化、更新和销毁按顺序驱动。
/// </summary>
public sealed class EntityComponentContainer
{
    /// <summary>按组件类型索引的组件字典，用于 Get/TryGet/Contains 快速查找。</summary>
    private readonly Dictionary<EntityComponentType, EntityComponentBase> _components =
        new Dictionary<EntityComponentType, EntityComponentBase>();
    /// <summary>按注册顺序排列的组件列表，用于 Initialize/Update/LateUpdate 顺序驱动。</summary>
    private readonly List<EntityComponentBase> _ordered = new List<EntityComponentBase>();

    /// <summary>向容器注册一个组件，同类型组件不允许重复注册。</summary>
    /// <param name="component">要注册的组件实例，为空时抛出异常。</param>
    /// <exception cref="ArgumentNullException">component 为空时抛出。</exception>
    /// <exception cref="InvalidOperationException">该类型组件已注册时抛出。</exception>
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

    /// <summary>判断指定类型的组件是否已注册。</summary>
    /// <param name="type">组件类型标识。</param>
    /// <returns>已注册返回 true，否则返回 false。</returns>
    public bool Contains(EntityComponentType type)
    {
        return _components.ContainsKey(type);
    }

    /// <summary>按类型获取组件，未注册或类型不匹配时抛出异常。</summary>
    /// <typeparam name="T">期望的组件类型，必须继承 <see cref="EntityComponentBase"/>。</typeparam>
    /// <param name="type">组件类型标识。</param>
    /// <returns>强类型组件实例。</returns>
    /// <exception cref="KeyNotFoundException">该类型组件未注册时抛出。</exception>
    /// <exception cref="InvalidCastException">组件实际类型与泛型 T 不匹配时抛出。</exception>
    public T GetMComponentByT<T>(EntityComponentType type) where T : EntityComponentBase
    {
        EntityComponentBase component;
        if (!_components.TryGetValue(type, out component))
        {
            throw new KeyNotFoundException("实体组件未注册: " + type);
        }
        T typedComponent = component as T;
        if (typedComponent == null)
        {
            throw new InvalidCastException("实体组件类型不匹配: " + type);
        }
        return typedComponent;
    }

    /// <summary>
    /// 按组件实际类型获取当前实体已注册的组件。
    /// 支持使用父类类型获取派生组件；未找到时抛出异常。
    /// </summary>
    /// <typeparam name="T">目标组件类型。</typeparam>
    /// <returns>匹配的实体组件实例。</returns>
    public T GetMComponentByT<T>() where T : EntityComponentBase
    {
        Dictionary<EntityComponentType, EntityComponentBase>.
            Enumerator enumerator =
            _components.GetEnumerator();

        while (enumerator.MoveNext())
        {
            EntityComponentBase component = enumerator.Current.Value;
            T typedComponent = component as T;
            if (typedComponent != null)
            {
                return typedComponent;
            }
        }

        throw new KeyNotFoundException(
            "实体组件未注册，组件类型: " + typeof(T).Name);
    }

    /// <summary>按类型尝试获取组件，未注册或类型不匹配时返回 false 而不抛异常。</summary>
    /// <typeparam name="T">期望的组件类型，必须继承 <see cref="EntityComponentBase"/>。</typeparam>
    /// <param name="type">组件类型标识。</param>
    /// <param name="component">输出参数，成功时为强类型组件实例，失败时为 null。</param>
    /// <returns>获取成功返回 true，否则返回 false。</returns>
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

    /// <summary>按注册顺序依次调用每个组件的 Initialize。</summary>
    public void Initialize()
    {
        for (int index = 0; index < _ordered.Count; index++)
        {
            EntityComponentBase component = _ordered[index];
            Invoke(component, "Initialize", delegate { component.Initialize(); });
        }
    }

    /// <summary>移动提交前推进容器内的程序位移组件，其初始化和回收仍属于普通组件生命周期。</summary>
    /// <param name="deltaMilliseconds">本帧间隔，单位毫秒；由实体更新入口调用一次。</param>
    public void UpdateBeforeMovement(int deltaMilliseconds)
    {
        // 容器按独立类型键持有的原位移实例；不创建每帧闭包或临时集合。
        EntityComponentBase displacement;
        if (!_components.TryGetValue(EntityComponentType.Displacement, out displacement))
        {
            return;
        }
        try
        {
            displacement.Update(deltaMilliseconds);
        }
        // 当前位移组件的推进异常，隔离后允许原输入移动与其它组件继续更新。
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("实体组件生命周期失败: " + displacement.GetType().Name + ".Update，" + exception);
        }
    }

    /// <summary>按注册顺序推进其余普通组件，位移已由移动前阶段推进，不能再次更新。</summary>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public void Update(int deltaMilliseconds)
    {
        // 当前普通组件在原注册列表中的位置，位移保持单帧唯一更新。
        for (int index = 0; index < _ordered.Count; index++)
        {
            // 本次更新的原组件引用，位移类型在前一阶段已经完成求值。
            EntityComponentBase component = _ordered[index];
            if (component.ComponentType == EntityComponentType.Displacement)
            {
                continue;
            }
            Invoke(component, "Update",
                delegate { component.Update(deltaMilliseconds); });
        }
    }

    /// <summary>逆序销毁所有组件，统一交由组件池回收，最后清空容器。</summary>
    public void Shutdown()
    {
        // 逆序释放，与注册顺序相反，保证后挂载的组件先销毁
        for (int index = _ordered.Count - 1; index >= 0; index--)
        {
            EntityComponentBase component = _ordered[index];
            // 统一交给组件池执行 Shutdown、状态重置和宿主解绑，避免生命周期重复调用。
            MComponentMgr.Instance.ReleaseComponent(component);
        }
        _ordered.Clear();
        _components.Clear();
    }

    /// <summary>按注册顺序依次调用每个组件的 LateUpdate。</summary>
    /// <param name="deltaSeconds">距上一帧的秒数。</param>
    public void LateUpdate(float deltaSeconds)
    {
        for (int index = 0; index < _ordered.Count; index++)
        {
            EntityComponentBase component = _ordered[index];
            Invoke(component, "LateUpdate", delegate { component.LateUpdate(deltaSeconds); });
        }
    }

    /// <summary>包装单个组件生命周期调用，捕获异常并打印日志，避免单个组件崩溃阻断其他组件。</summary>
    /// <param name="component">正在执行生命周期的组件。</param>
    /// <param name="method">生命周期方法名，用于错误日志定位。</param>
    /// <param name="action">实际要执行的生命周期委托。</param>
    private static void Invoke(EntityComponentBase component, string method, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("实体组件生命周期失败: " + component.GetType().Name + "." +
                method + "，" + exception);
        }
    }
}
