using System;
using System.Collections.Generic;

/// <summary>按注册顺序管理角色组件生命周期和持久化。</summary>
public sealed class RoleComponentContainer
{
    private readonly Dictionary<Type, RoleComponentBase> _components = new Dictionary<Type, RoleComponentBase>();
    private readonly List<RoleComponentBase> _orderedComponents = new List<RoleComponentBase>();

    /// <summary>创建角色组件容器并绑定角色。</summary>
    public RoleComponentContainer(OnlineRole owner)
    {
        if (owner == null) throw new ArgumentNullException("owner");
    }

    /// <summary>注册组件，具体类型只能注册一次。</summary>
    public void SetComponent<T>(T component) where T : RoleComponentBase
    {
        if (component == null) throw new ArgumentNullException("component");
        Type type = typeof(T);
        if (_components.ContainsKey(type)) throw new InvalidOperationException("角色组件重复注册: " + type.Name);
        _components.Add(type, component);
        _orderedComponents.Add(component);
    }
    /// <summary>获取已注册组件。</summary>
    public T GetComponent<T>() where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (!_components.TryGetValue(typeof(T), out value)) throw new KeyNotFoundException("角色组件未注册: " + typeof(T).Name);
        return (T)value;
    }
    /// <summary>尝试获取已注册组件。</summary>
    public bool TryGetComponent<T>(out T component) where T : RoleComponentBase
    {
        RoleComponentBase value;
        if (_components.TryGetValue(typeof(T), out value)) { component = (T)value; return true; }
        component = null;
        return false;
    }
    /// <summary>初始化全部组件。</summary>
    public void Initialize() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "Initialize", delegate { value.Initialize(); }); } }
    /// <summary>通知全部组件完成登录加载。</summary>
    public void AfterLoad() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "AfterLoad", delegate { value.AfterLoad(); }); } }
    /// <summary>通知全部组件加入在线列表。</summary>
    public void AfterAddedToRole() { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "AfterAddedToRole", delegate { value.AfterAddedToRole(); }); } }
    /// <summary>更新全部组件。</summary>
    public void Update(int deltaMilliseconds) { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "Update", delegate { value.Update(deltaMilliseconds); }); } }
    /// <summary>统一收集全部组件脏数据。</summary>
    public bool Save(RoleDataFieldManager fields, long version)
    {
        bool saved = false;
        for (int i = 0; i < _orderedComponents.Count; i++)
        {
            RoleComponentBase value = _orderedComponents[i];
            if (value.Save(fields, version))
                saved = true;
        }
        return saved;
    }
    /// <summary>统一判断是否有脏组件。</summary>
    public bool NeedSave() { for (int i = 0; i < _orderedComponents.Count; i++) if (_orderedComponents[i].NeedSave()) return true; return false; }
   
    /// <summary>ACK 后统一通知组件清理已确认版本。</summary>
    public void OnSaveAcknowledged(long version) { for (int i = 0; i < _orderedComponents.Count; i++) { RoleComponentBase value = _orderedComponents[i]; Invoke(value, "OnSaveAcknowledged", delegate { value.OnSaveAcknowledged(version); }); } }
    /// <summary>逆序释放组件。</summary>
    public void Dispose()
    {
        for (int index = _orderedComponents.Count - 1; index >= 0; index--) { RoleComponentBase value = _orderedComponents[index]; Invoke(value, "Dispose", delegate { value.Dispose(); }); }
    }
    /// <summary>隔离单组件异常，保证其他组件继续运行。</summary>
    private static void Invoke(RoleComponentBase component, string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { LogMsg.Info("角色组件生命周期失败: " + component.GetType().Name + "." + name + "，" + ex, LogMsgType.Error); }
    }
}
