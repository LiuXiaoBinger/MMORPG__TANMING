using System.Collections.Generic;
using Google.Protobuf;

/// <summary>网络消息观察者回调。</summary>
public delegate void OnActionHandler(ByteString data);

/// <summary>
/// 在 Unity 主线程按协议号派发服务器消息。
/// </summary>
public class SocketDispatcher : Singleton<SocketDispatcher>
{
    /// <summary>按协议号索引的消息处理回调。</summary>
    private readonly Dictionary<int, OnActionHandler> _actionDis =
        new Dictionary<int, OnActionHandler>();

    /// <summary>当前同步派发中的连接代次，零表示不在派发。</summary>
    private long _currentDispatchGeneration;

    /// <summary>获取当前消息所属连接代次。</summary>
    public long CurrentDispatchGeneration
    {
        get { return _currentDispatchGeneration; }
    }

    /// <summary>注册指定协议的唯一处理回调。</summary>
    public void AddEventHandler(int protocode, OnActionHandler handler)
    {
        if (!_actionDis.ContainsKey(protocode) && handler != null)
        {
            _actionDis.Add(protocode, handler);
        }
    }

    /// <summary>删除指定协议的处理回调。</summary>
    public void RemoveEventHandler(int protocode)
    {
        _actionDis.Remove(protocode);
    }

    /// <summary>在当前调用栈内携带连接代次同步派发消息。</summary>
    public void DispatchEvent(int protocode, ByteString data, long connectionGeneration)
    {
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
