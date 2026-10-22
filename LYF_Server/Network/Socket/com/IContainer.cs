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

    /// <summary>处理来自上游服务端的协议包。</summary>
    /// <param name="serverBase">接收该包的连接对象。</param>
    /// <param name="basePackage">已拆包并反序列化的协议包。</param>
    void OnServerCommand(BaseLink serverBase, BasePackage basePackage);

    /// <summary>处理来自下游客户端的协议包。</summary>
    /// <param name="serverBase">接收该包的连接对象。</param>
    /// <param name="basePackage">已拆包并反序列化的协议包。</param>
    void OnClientCommand(BaseLink serverBase, BasePackage basePackage);

}
