using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 协议命令处理器接口：一个协议号对应一个处理器，由各业务服务实现。
/// </summary>
public interface IContainer
{
    /// <summary>处理器初始化。</summary>
    void OnInit();

    /// <summary>处理监听端 Session 收到的协议包；发送方可能是玩家客户端，也可能是下游服务。</summary>
    /// <param name="serverBase">接收该包的连接对象。</param>
    /// <param name="basePackage">已拆包并反序列化的协议包。</param>
    void OnServerCommand(BaseLink serverBase, BasePackage basePackage);

    /// <summary>处理主动连接器 NetClient 收到的协议包，通常是上游服务的响应。</summary>
    /// <param name="serverBase">接收该包的连接对象。</param>
    /// <param name="basePackage">已拆包并反序列化的协议包。</param>
    void OnClientCommand(BaseLink serverBase, BasePackage basePackage);

}
