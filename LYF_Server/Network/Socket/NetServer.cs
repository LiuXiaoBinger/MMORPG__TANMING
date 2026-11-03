using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// TCP 监听服务端：绑定端口并异步 Accept 客户端连接，为每个连接创建 Session。
/// </summary>
public class NetServer
{
    // 连接断开时收到本地 SessionID；回调本身不切线程，GameServer 宿主把它投递到逻辑队列后处理下线。
    public Action<int> OnSessionDisconnected { get; set; }
    /// <summary>监听 Socket。</summary>
    private Socket _socket;

    /// <summary>协议号到命令处理器的映射。</summary>
    private Dictionary<int, IContainer> _cmdDic = new Dictionary<int, IContainer>();
    /// <summary>本服务作为客户端连接上游时使用的连接器，可为 null（如中心服）。</summary>
    private  NetClient _client;
    /// <summary>构造监听服务端。</summary>
    /// <param name="client">本服务连接上游的连接器；无上游时传 null。</param>
    public NetServer(NetClient client )
    {
        _client = client;
    }

    /// <summary>绑定地址端口并在后台线程开始监听。</summary>
    public void StartServer(string ip, int port)
    {

        //创建Socket                  寻址方式：IPV4         数据传输方式：Stream  协议类型：TCP
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //设置Ip和端口
        EndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        //绑定IP和端口
        _socket.Bind(endPoint);

        Console.WriteLine("服务器开启成功：" + endPoint.ToString());

        // 待应用接收的连接请求队列长度设为 100；这不是在线会话总数上限。
        _socket.Listen(100);


        Thread listenThread = new Thread(ListenConnectSocket);
        listenThread.IsBackground = true;
        listenThread.Start();
    }

    /// <summary>
    /// 开始监听客户端连接
    /// </summary>
    private void ListenConnectSocket()
    {
        _socket.BeginAccept(ClientConnectCB, null);
    }


    /// <summary>
    /// 有客户端连接
    /// </summary>
    /// <param name="ar"></param>
    private void ClientConnectCB(IAsyncResult ar)
    {

        try
        {
            Socket clientSocket = _socket.EndAccept(ar);
            
            
            LogMsg.Info($"客户    5sdf55端::{clientSocket.RemoteEndPoint} 连接成功....");
            //开始接收客户端数据
            Session session = new Session(_cmdDic, _client, OnSessionDisconnected);
            session.ReceiveData(clientSocket);

            //当处理完成当前客户端连接后，继续处理下一个客户端的连接请求
            ListenConnectSocket();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    /// <summary>为监听端收到的协议登记处理器；新 Session 共用这张协议表。</summary>
    public void RegistCommand(int cmd, IContainer container)
    {
        _cmdDic.Add(cmd, container);
    }

}
