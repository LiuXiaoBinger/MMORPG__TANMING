using System.Collections.Generic;
using Google.Protobuf;

/// <summary>接收单条 Protobuf 网络消息的强类型观察者回调。</summary>
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

    /// <summary>当前同步派发中的连接代次，零表示不在派发。</summary>
    private long _currentDispatchGeneration;

    /// <summary>获取当前消息所属连接代次。</summary>
    public long CurrentDispatchGeneration
    {
        get { return _currentDispatchGeneration; }
    }

    /// <summary>指定协议尚无处理器时注册唯一的强类型回调。</summary>
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
    /// <param name="handler">注册时保存的强类型委托实例，用于验证注销所有权。</param>
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

    /// <summary>在当前调用栈内携带连接代次同步派发消息。</summary>
    /// <param name="protocode">收到的服务器协议号。</param>
    /// <param name="data">协议对应的只读 Protobuf 二进制消息体。</param>
    /// <param name="connectionGeneration">产生该消息的正数网络连接代次。</param>
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
