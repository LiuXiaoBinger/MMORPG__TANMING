using System;
using System.Collections.Generic;

/// <summary>
/// 事件监听者身份接口。
/// 监听表以该接口的实例引用作为唯一键，避免用 object 充当监听身份导致类型失配。
/// </summary>
public interface IROEventListener
{
}

/// <summary>
/// 一条监听登记，对应 event_dispatcher.h 的 SPEventDispatchEntry。
/// 回调为空表示该监听已被解除，派发时跳过。
/// </summary>
public sealed class ROEventListenerEntry
{
    /// <summary>监听者实例，作为重复注册替换与解除监听的唯一键。</summary>
    public IROEventListener listener;

    /// <summary>强类型事件回调；解除监听后置空，用于标记本条登记失效。</summary>
    public Action<ROEvent> callback;

    /// <summary>以监听者与回调构造一条登记。</summary>
    /// <param name="listener">监听者实例，不能为空。</param>
    /// <param name="callback">事件回调，不能为空。</param>
    public ROEventListenerEntry(IROEventListener listener, Action<ROEvent> callback)
    {
        this.listener = listener;
        this.callback = callback;
    }
}

/// <summary>
/// 实体事件派发器，对应 event_dispatcher.h 的 ROEventDispatcher。
/// 语义与 RO 保持一致：同步就地派发、按事件类型分表、同一监听者重复注册为替换、
/// 解除监听立即失效、宿主销毁时清理全部回调引用。
/// </summary>
public sealed class ROEventDispatcher
{
    /// <summary>
    /// 事件类型到监听登记列表的映射；同一类型内按注册顺序派发。
    /// 用列表而非字典保存顺序，监听者数量很少，线性查找即可满足替换与解除。
    /// </summary>
    private readonly Dictionary<ROEventDefine, List<ROEventListenerEntry>> hash_sp_event_handler_ =
        new Dictionary<ROEventDefine, List<ROEventListenerEntry>>();

    /// <summary>
    /// 注册事件监听；同一监听者对同一事件重复注册时替换原回调，不叠加调用。
    /// </summary>
    /// <param name="event_type">监听的事件类型。</param>
    /// <param name="listener">监听者实例，同时作为唯一键。</param>
    /// <param name="callback">事件到达时执行的回调。</param>
    public void AddEventListener(ROEventDefine event_type, IROEventListener listener, Action<ROEvent> callback)
    {
        if (listener == null)
        {
            return;
        }
        if (callback == null)
        {
            return;
        }

        List<ROEventListenerEntry> it_listener;
        if (!hash_sp_event_handler_.TryGetValue(event_type, out it_listener))
        {
            it_listener = new List<ROEventListenerEntry>();
            hash_sp_event_handler_.Add(event_type, it_listener);
        }

        // 同一监听者只保留一条登记，后注册的回调覆盖先前回调，与 RO 的替换语义一致。
        // 当前比对的登记下标，范围为零到该事件类型监听数量减一。
        for (int index = 0; index < it_listener.Count; index++)
        {
            // 已存在的登记，按监听者引用判断是否同一监听者。
            ROEventListenerEntry exist_entry = it_listener[index];
            if (ReferenceEquals(exist_entry.listener, listener))
            {
                exist_entry.callback = callback;
                return;
            }
        }

        it_listener.Add(new ROEventListenerEntry(listener, callback));
    }

    /// <summary>
    /// 解除事件监听；解除后该监听者不再收到此事件。
    /// 派发过程中解除只把回调置空并从列表移除，当前这一轮已取出的登记按 RO 语义跳过。
    /// </summary>
    /// <param name="event_type">要解除的事件类型。</param>
    /// <param name="listener">要解除的监听者实例。</param>
    public void RemoveEventListener(ROEventDefine event_type, IROEventListener listener)
    {
        if (listener == null)
        {
            return;
        }

        List<ROEventListenerEntry> it_listener;
        if (!hash_sp_event_handler_.TryGetValue(event_type, out it_listener))
        {
            return;
        }

        // 当前比对的登记下标，范围为零到该事件类型监听数量减一。
        for (int index = 0; index < it_listener.Count; index++)
        {
            // 待解除的登记，只有监听者引用一致才处理。
            ROEventListenerEntry entry_ptr = it_listener[index];
            if (!ReferenceEquals(entry_ptr.listener, listener))
            {
                continue;
            }
            // 先置空回调再移除，保证已取出的登记引用在派发中同样失效。
            entry_ptr.callback = null;
            it_listener.RemoveAt(index);
            return;
        }
    }

    /// <summary>
    /// 同步派发事件：按事件类型取出监听登记并就地回调，不入队、不跨线程。
    /// 派发前复制一份登记快照，因此本轮不纳入派发开始后新注册的条目；
    /// 已被快照引用的条目若在本轮内被解除或替换，回调置空或换新后立即可见。
    /// 同类型事件在回调中再次派发时，嵌套那次同样按当前表重新取快照。
    /// </summary>
    /// <param name="evt">待派发的事件对象，为空时直接返回。</param>
    public void DispatcherEvent(ROEvent evt)
    {
        if (evt == null)
        {
            return;
        }

        ROEventDefine event_type = evt.GetEventType();
        List<ROEventListenerEntry> it_listener;
        if (!hash_sp_event_handler_.TryGetValue(event_type, out it_listener))
        {
            return;
        }

        // 快照只固定本轮开始时的登记顺序与条目引用，不复制回调内容。
        ROEventListenerEntry[] snapshot = it_listener.ToArray();
        // 当前遍历到的快照下标，范围为零到快照长度减一。
        for (int index = 0; index < snapshot.Length; index++)
        {
            // 本轮取出的登记引用；回调被置空说明它在本轮内被解除，直接跳过。
            ROEventListenerEntry entry_ptr = snapshot[index];
            if (entry_ptr.callback == null)
            {
                continue;
            }
            entry_ptr.callback(evt);
        }
    }

    /// <summary>
    /// 宿主销毁时清空全部监听登记，释放对回调目标的引用。
    /// 对应 RO 的 UnInit；销毁后再次派发不会触发任何回调。
    /// </summary>
    public void UnInit()
    {
        // 先取事件类型快照，再按索引遍历，避免遍历字典本身。
        List<ROEventDefine> event_types = new List<ROEventDefine>(hash_sp_event_handler_.Keys);
        // 当前处理的事件类型下标，范围为零到事件类型数量减一。
        for (int type_index = 0; type_index < event_types.Count; type_index++)
        {
            // 该事件类型下的监听登记列表。
            List<ROEventListenerEntry> it_listener = hash_sp_event_handler_[event_types[type_index]];
            // 当前清理的登记下标，先把回调置空再整表清空，彻底断开对回调目标的引用。
            for (int index = 0; index < it_listener.Count; index++)
            {
                it_listener[index].callback = null;
            }
            it_listener.Clear();
        }
        hash_sp_event_handler_.Clear();
    }
}
