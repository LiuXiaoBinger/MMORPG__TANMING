using System.Collections.Generic;
using Google.Protobuf;

/// <summary>处理某个协议的服务器消息；具体控制器在这里解析消息体。</summary>
/// <param name="data">当前协议对应的只读 Protobuf 二进制消息体。</param>
public delegate void OnActionHandler(ByteString data);

/// <summary>
/// 在 Unity 主线程按协议号派发服务器消息。
/// </summary>
public class SocketDispatcher : Singleton<SocketDispatcher>
{
    /// <summary>按协议号索引的消息处理回调。</summary>
    private readonly Dictionary<int, OnActionHandler> _actionDis =
        new Dictionary<int, OnActionHandler>();

    /// <summary>正在处理的消息来自第几次网络连接；没有正在处理的消息时为 0。</summary>
    private long _currentDispatchGeneration;

    /// <summary>取得当前消息的连接编号，供控制器排除旧连接的回包。</summary>
    public long CurrentDispatchGeneration
    {
        get { return _currentDispatchGeneration; }
    }

    /// <summary>为协议号登记一个处理方法；已有方法时保留原登记。</summary>
    /// <param name="protocode">服务器协议号，必须与发送端协议定义一致。</param>
    /// <param name="handler">负责反序列化并处理该协议消息的非空委托。</param>
    public void AddEventHandler(int protocode, OnActionHandler handler)
    {
        if (!_actionDis.ContainsKey(protocode) && handler != null)
        {
            _actionDis.Add(protocode, handler);
        }
    }

    /// <summary>仅当指定协议仍由同一个委托实例持有时删除处理回调。</summary>
    /// <param name="protocode">需要解除注册的协议号。</param>
    /// <param name="handler">当初登记的方法；只有仍与当前登记相同才会移除。</param>
    /// <returns>当前协议处理器与传入委托相同时返回 true，否则不修改并返回 false。</returns>
    public bool RemoveEventHandler(int protocode, OnActionHandler handler)
    {
        if (handler == null)
        {
            return false;
        }
        // currentHandler 是协议当前实际持有的委托；旧所有者不得删除后来注册的新实例。
        OnActionHandler currentHandler;
        if (!_actionDis.TryGetValue(protocode, out currentHandler) ||
            currentHandler != handler)
        {
            return false;
        }
        return _actionDis.Remove(protocode);
    }

    /// <summary>查找协议处理方法并立即调用；调用期间可读取消息的连接编号。</summary>
    /// <param name="protocode">收到的服务器协议号。</param>
    /// <param name="data">协议对应的只读 Protobuf 二进制消息体。</param>
    /// <param name="connectionGeneration">接收到这条消息时使用的网络连接编号。</param>
    public void DispatchEvent(int protocode, ByteString data, long connectionGeneration)
    {
        // 该协议已登记的处理方法；未登记的消息直接忽略。
        OnActionHandler handler;
        if (!_actionDis.TryGetValue(protocode, out handler) || handler == null)
        {
            return;
        }

        _currentDispatchGeneration = connectionGeneration;
        try
        {
            handler.Invoke(data);
        }
        finally
        {
            _currentDispatchGeneration = 0;
        }
    }
}
