using System;
using System.Collections.Generic;

/// <summary>按注册顺序管理角色组件生命周期和持久化。</summary>
public sealed class RoleComponentContainer
{
    /// <summary>用注册时的具体 C# 类型定位组件，不会自动按基类匹配。</summary>
    private readonly Dictionary<Type, RoleComponentBase> _components = new Dictionary<Type, RoleComponentBase>();

    /// <summary>保留注册顺序，初始化和更新正序执行，释放反序执行。</summary>
    private readonly List<RoleComponentBase> _orderedComponents = new List<RoleComponentBase>();

    #region 构造与初始化
    /// <summary>建立组件容器并确认角色参数非空；角色引用由各组件自己持有，容器不保存 owner。</summary>
    public RoleComponentContainer(OnlineRole owner)
    {
        if (owner == null) throw new ArgumentNullException("owner");
    }

    /// <summary>初始化全部组件。</summary>
    public void Initialize() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "Initialize", delegate { value.Initialize(); }); } }

    /// <summary>更新全部组件。</summary>
    public void Update(int deltaMilliseconds) { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "Update", delegate { value.Update(deltaMilliseconds); }); } }

    /// <summary>后置更新全部组件：在同一帧所有 Update 之后按注册顺序执行，对应 RO 的 LateUpdate 阶段。</summary>
    /// <param name="deltaMilliseconds">本逻辑帧推进的毫秒数，原样传给每个组件。</param>
    public void LateUpdate(int deltaMilliseconds)
    {
        // i 是注册顺序下标，从零到组件数减一，保证后置更新与 Update 阶段顺序一致。
        for (int i = 0; i < _orderedComponents.Count; i++)
        {
            // value 是本轮要后置更新的组件；i 是注册顺序下标，保证与 Update 阶段保持同一顺序。
            RoleComponentBase value = _orderedComponents[i];
            Invoke(value, "LateUpdate", delegate { value.LateUpdate(deltaMilliseconds); });
        }
    }

    /// <summary>逆序释放组件。</summary>
    public void Dispose()
    {
        for (int index = _orderedComponents.Count - 1; index >= 0; index--) { RoleComponentBase value = _orderedComponents[index]; Invoke(value, "Dispose", delegate { value.Dispose(); }); }
    }
    #endregion

    /// <summary>收到中心服保存成功确认后，依注册顺序通知组件处理该版本。</summary>
    public void OnSaveAcknowledged(long version) { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "OnSaveAcknowledged", delegate { value.OnSaveAcknowledged(version); }); } }

    #region 查询与判定
    /// <summary>按注册时的具体类型取得组件；不存在时抛异常。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (!_components.TryGetValue(typeof(T), out value)) throw new KeyNotFoundException("角色组件未注册: " + typeof(T).Name);
        return (T)value;
    }

    /// <summary>尝试按注册时的具体类型取得组件；不存在时抛异常。</summary>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (_components.TryGetValue(typeof(T), out value)) { component = (T)value; return true; }
        component = null;
        return false;
    }
    #endregion

    /// <summary>统一收集全部组件脏数据。</summary>
    public bool Save(RoleDataFieldManager fields, long version)
    {
        // 只要任意组件把数据加入请求，就把本次收集结果记为 true。
        bool saved = false;
        for (int i = 0; i < _orderedComponents.Count; i++)
        {
            RoleComponentBase value = _orderedComponents[i];
            if (value.Save(fields, version))
                saved = true;
        }
        return saved;
    }

    #region 核心逻辑
    /// <summary>注册组件，具体类型只能注册一次。</summary>
    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        if (component == null) throw new ArgumentNullException("component");
        // 以泛型参数声明的类型作为键，与 GetComponent 的查找方式一致。
        Type type = typeof(T);
        if (_components.ContainsKey(type)) throw new InvalidOperationException("角色组件重复注册: " + type.Name);
        _components.Add(type, component);
        _orderedComponents.Add(component);
    }

    /// <summary>通知全部组件完成登录加载。</summary>
    public void AfterLoad() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "AfterLoad", delegate { value.AfterLoad(); }); } }

    /// <summary>通知全部组件加入在线列表。</summary>
    public void AfterAddedToRole() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "AfterAddedToRole", delegate { value.AfterAddedToRole(); }); } }

    /// <summary>统一判断是否有脏组件。</summary>
    public bool NeedSave() { for (int i = 0; i < _orderedComponents.Count; i++) if (_orderedComponents[i].NeedSave()) return true; return false; }

    /// <summary>隔离单组件异常，保证其他组件继续运行。</summary>
    private static void Invoke(RoleComponentBase component, string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { LogMsg.Info("角色组件生命周期失败: " + component.GetType().Name + "." + name + "，" + ex, LogMsgType.Error); }
    }
    #endregion
}
