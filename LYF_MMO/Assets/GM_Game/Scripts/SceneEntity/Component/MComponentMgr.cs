using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

/// <summary>
/// 场景实体组件池。普通组件按实际类型进入池并挂接到 Components；
/// Move/Buff/Skill/Machine 由实体快捷属性独占，单独创建、更新和回收。
/// 同时维护动作状态专用池，供状态机获取/回收各动作状态组件。
/// </summary>
public sealed class MComponentMgr
{
    /// <summary>全局单例实例。</summary>
    private static readonly MComponentMgr _instance = new MComponentMgr();
    /// <summary>普通实体组件对象池，键为组件实际类型，值为可复用组件队列。</summary>
    private readonly Dictionary<Type, Queue<EntityComponentBase>> _componentPool =
        new Dictionary<Type, Queue<EntityComponentBase>>();
    /// <summary>动作状态专用对象池，键为动作状态类型，值为可复用动作状态队列。</summary>
    private readonly Dictionary<Type, Queue<MActionComponent>> _actionStatePool =
        new Dictionary<Type, Queue<MActionComponent>>();
    /// <summary>按实体类型登记的附加组件类型表，由 Builder/注册流程配置，创建实体时批量追加。</summary>
    private readonly Dictionary<EntityType, List<Type>> _extraComponentTypes =
        new Dictionary<EntityType, List<Type>>();

    /// <summary>私有构造，禁止外部 new，强制走 <see cref="Instance"/> 单例。</summary>
    private MComponentMgr()
    {
    }

    /// <summary>组件管理器单例访问入口。</summary>
    public static MComponentMgr Instance
    {
        get { return _instance; }
    }

    /// <summary>兼容旧代码的单例别名，与 <see cref="Instance"/> 等价。</summary>
    public static MComponentMgr singleton
    {
        get { return _instance; }
    }

    /// <summary>创建普通组件并挂接到宿主的普通组件容器。</summary>
    /// <typeparam name="T">组件类型，必须继承 <see cref="EntityComponentBase"/>。</typeparam>
    /// <param name="owner">宿主对象，通常为 <see cref="MEntity"/>。</param>
    /// <returns>创建并挂接成功的强类型组件，失败返回 null。</returns>
    public T CreateComponent<T>(ROObject owner) where T : EntityComponentBase
    {
        return CreateComponent(owner, typeof(T)) as T;
    }

    /// <summary>按运行时类型创建普通组件并挂接到宿主的普通组件容器。</summary>
    /// <param name="owner">宿主对象。</param>
    /// <param name="componentType">要创建的组件类型。</param>
    /// <returns>创建并挂接成功的组件实例，参数非法或实体生命周期不符合要求时返回 null。</returns>
    public EntityComponentBase CreateComponent(ROObject owner, Type componentType)
    {
        if (owner == null || componentType == null ||
            !typeof(EntityComponentBase).IsAssignableFrom(componentType))
        {
            return null;
        }
        // 仅允许在实体 Created 阶段挂接普通组件，避免已激活实体中途改组件结构
        if (owner.LifecycleState != EntityLifecycleState.Created)
        {
            return null;
        }

        EntityComponentBase component = TakeComponent(owner, componentType);
        if (component == null)
        {
            return null;
        }
        owner.AddComponent(component);
        return component;
    }

    /// <summary>创建实体专用组件，不进入普通 Components 容器。</summary>
    /// <param name="entity">宿主实体，为空时直接返回。</param>
    public void CreateSpecialComponents(MEntity entity)
    {
        if (entity == null)
        {
            return;
        }
        if (entity.MoveComp == null)
        {
            MMoveComponent move = TakeComponent(entity, typeof(MMoveComponent)) as MMoveComponent;
            if (move != null)
            {
                // 专用组件不经过 AddComponent，必须在这里完成唯一一次宿主绑定。
                move.RebindOwner(entity);
                entity.SetMoveComponent(move);
            }
        }
        if (entity.Buff == null)
        {
            MBuffComponent buff = TakeComponent(entity, typeof(MBuffComponent)) as MBuffComponent;
            if (buff != null)
            {
                buff.RebindOwner(entity);
                entity.SetBuffComponent(buff);
            }
        }
        if (entity.Skill == null)
        {
            // Skill 同时是动作状态，只进入动作状态池，由状态机拥有唯一回收权。
            MSkillComponent skill = AcquireActionState(typeof(MSkillComponent), entity) as MSkillComponent;
            if (skill != null)
            {
                entity.SetSkillComponent(skill);
            }
        }
        if (entity.Machine == null)
        {
            MStateMachine machine = TakeComponent(entity, typeof(MStateMachine)) as MStateMachine;
            if (machine != null)
            {
                machine.RebindOwner(entity);
                entity.SetMachineComponent(machine);
            }
        }
        if (entity.AttrComp == null)
        {
            // 属性组件
            EntityAttributesComponent AttCom = TakeComponent(entity, typeof(EntityAttributesComponent)) as EntityAttributesComponent;
            if (AttCom != null)
            {
                entity.SetAttComponent(AttCom);
            }
        }
    }

    /// <summary>依次初始化实体四个专用组件：Move、Buff、Skill、Machine。</summary>
    /// <param name="entity">宿主实体，为空时直接返回。</param>
    public void InitializeSpecialComponents(MEntity entity)
    {
        if (entity == null)
        {
            return;
        }
        InitializeComponent(entity.MoveComp);
        InitializeComponent(entity.Buff);
        InitializeComponent(entity.Skill);
        InitializeComponent(entity.Machine);
    }

    /// <summary>依次更新实体四个专用组件。</summary>
    /// <param name="entity">宿主实体。</param>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    public void UpdateSpecialComponents(MEntity entity, int deltaMilliseconds)
    {
        if (entity == null)
        {
            return;
        }
        UpdateComponent(entity.MoveComp, deltaMilliseconds);
        UpdateComponent(entity.Buff, deltaMilliseconds);
        UpdateComponent(entity.Skill, deltaMilliseconds);
        UpdateComponent(entity.Machine, deltaMilliseconds);
    }

    /// <summary>回收实体四个专用组件，并清空实体快捷引用。</summary>
    /// <param name="entity">宿主实体，为空时直接返回。</param>
    public void ReleaseSpecialComponents(MEntity entity)
    {
        if (entity == null)
        {
            return;
        }
        MMoveComponent move = entity.MoveComp;
        MBuffComponent buff = entity.Buff;
        MStateMachine machine = entity.Machine;
        // 状态机拥有 Skill 动作状态的唯一回收权，Skill 不得再进入普通组件池。
        ReleaseComponent(machine, true);
        entity.SetMoveComponent(null);
        entity.SetBuffComponent(null);
        entity.SetSkillComponent(null);
        entity.SetMachineComponent(null);
        ReleaseComponent(move, true);
        ReleaseComponent(buff, true);
    }

    /// <summary>组件从宿主卸载后进入类型队列；不可池化的 Marker 等组件直接丢弃。</summary>
    /// <param name="component">要回收的组件实例，为空时直接返回。</param>
    public void ReleaseComponent(EntityComponentBase component)
    {
        ReleaseComponent(component, true);
    }

    /// <summary>从动作状态池获取状态，状态始终由 MStateMachine 独立持有。</summary>
    /// <param name="stateType">动作状态类型。</param>
    /// <param name="owner">宿主对象。</param>
    /// <returns>可复用或新建并已重新绑定宿主的动作状态实例，参数非法时返回 null。</returns>
    public MActionComponent AcquireActionState(Type stateType, ROObject owner)
    {
        if (stateType == null || owner == null || !typeof(MActionComponent).IsAssignableFrom(stateType))
        {
            return null;
        }
        Queue<MActionComponent> queue;
        MActionComponent state = null;
        // 优先从对象池取出，没有可用实例再反射新建
        if (_actionStatePool.TryGetValue(stateType, out queue) && queue.Count > 0)
        {
            state = queue.Dequeue();
        }
        else
        {
            try
            {
                state = Activator.CreateInstance(stateType, owner) as MActionComponent;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("动作状态创建失败: " + stateType.Name + "，" + exception);
            }
        }
        if (state != null)
        {
            state.RebindOwner(owner);
        }
        return state;
    }

    /// <summary>动作状态停止后清理并回收到专用状态池。</summary>
    /// <param name="state">要回收的动作状态实例，为空时直接返回。</param>
    public void ReleaseActionState(MActionComponent state)
    {
        if (state == null)
        {
            return;
        }
        // 先调用 Shutdown 释放业务资源，再重置对象池状态
        state.Shutdown();
        state.ResetForPool();
        Type stateType = state.GetType();
        Queue<MActionComponent> queue;
        if (!_actionStatePool.TryGetValue(stateType, out queue))
        {
            queue = new Queue<MActionComponent>();
            _actionStatePool.Add(stateType, queue);
        }
        queue.Enqueue(state);
    }

    /// <summary>内部回收入口，可控制是否允许回池；不允许回池或为 Marker 组件时直接丢弃。</summary>
    /// <param name="component">要回收的组件实例。</param>
    /// <param name="allowPool">是否允许回收到普通组件池，false 时只清理不缓存。</param>
    internal void ReleaseComponent(EntityComponentBase component, bool allowPool)
    {
        if (component == null)
        {
            return;
        }
        try
        {
            component.Shutdown();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("实体组件回收清理失败: " + component.GetType().Name + "，" + exception);
        }
        // 重置宿主引用与事件登记，保证下次复用时干净
        component.ResetForPool();
        // Marker 等无状态组件不复用，直接丢弃；禁止回池时也直接丢弃
        if (!allowPool || component is EntityComponentMarker)
        {
            return;
        }

        Type componentType = component.GetType();
        Queue<EntityComponentBase> queue;
        if (!_componentPool.TryGetValue(componentType, out queue))
        {
            queue = new Queue<EntityComponentBase>();
            _componentPool.Add(componentType, queue);
        }
        queue.Enqueue(component);
    }

    /// <summary>登记某类实体需要追加的附加组件类型，Builder 或启动流程在创建实体前调用。</summary>
    /// <param name="entityType">实体类型标识。</param>
    /// <param name="componentTypes">要追加的组件类型数组，自动去重并校验是否继承 <see cref="EntityComponentBase"/>。</param>
    public void RegisterExtraComponents(EntityType entityType, params Type[] componentTypes)
    {
        List<Type> types;
        if (!_extraComponentTypes.TryGetValue(entityType, out types))
        {
            types = new List<Type>();
            _extraComponentTypes.Add(entityType, types);
        }
        if (componentTypes == null)
        {
            return;
        }
        for (int index = 0; index < componentTypes.Length; index++)
        {
            Type type = componentTypes[index];
            if (type != null && typeof(EntityComponentBase).IsAssignableFrom(type) &&
                !types.Contains(type))
            {
                types.Add(type);
            }
        }
    }

    

    /// <summary>从普通组件池取出一个可复用组件，池中无可用实例时反射新建。</summary>
    /// <param name="owner">宿主对象，用于构造新组件。</param>
    /// <param name="componentType">组件实际类型。</param>
    /// <returns>可用组件实例，创建失败返回 null。</returns>
    private EntityComponentBase TakeComponent(ROObject owner, Type componentType)
    {
        Queue<EntityComponentBase> queue;
        EntityComponentBase component = null;
        // 优先复用池内组件
        if (_componentPool.TryGetValue(componentType, out queue) && queue.Count > 0)
        {
            component = queue.Dequeue();
        }
        else
        {
            try
            {
                component = Activator.CreateInstance(componentType, owner) as EntityComponentBase;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("实体组件创建失败: " + componentType.Name + "，" + exception);
            }
        }
        // 普通组件由 MObject.AddComponent 绑定；专用组件由创建入口绑定，避免重复清空事件登记。
        return component;
    }

    /// <summary>安全调用单个专用组件的 Initialize，捕获异常避免阻断其他组件。</summary>
    /// <param name="component">待初始化组件，为空直接跳过。</param>
    private static void InitializeComponent(EntityComponentBase component)
    {
        if (component == null)
        {
            return;
        }
        try
        {
            component.Initialize();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("实体专用组件初始化失败: " + component.GetType().Name + "，" + exception);
        }
    }

    /// <summary>安全调用单个专用组件的 Update，捕获异常避免阻断其他组件。</summary>
    /// <param name="component">待更新组件，为空直接跳过。</param>
    /// <param name="deltaMilliseconds">距上一帧的毫秒数。</param>
    private static void UpdateComponent(EntityComponentBase component, int deltaMilliseconds)
    {
        if (component == null)
        {
            return;
        }
        try
        {
            component.Update(deltaMilliseconds);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("实体专用组件更新失败: " + component.GetType().Name + "，" + exception);
        }
    }

}
