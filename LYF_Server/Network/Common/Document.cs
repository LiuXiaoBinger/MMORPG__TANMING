using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 网络协议说明文档占位类：仅存放 TCP 粘包与 ProtoBuf 概念笔记，无任何运行时代码。
/// </summary>
internal class Document
{


    #region TCP粘包
    /**
     * TCP 传输的是连续字节，不保留应用消息的边界。一次读取可能只拿到半条消息，
     * 也可能拿到多条消息。这里说的“粘包”是应用还没有正确划分消息，并非 TCP 把内容传错。
     * 项目由 NetUtils 在消息前放长度字段，BaseLink 累计读取足够字节后再交给协议处理器。
     * 具体字段宽度和长度是否含包头，以 NetDefine、NetUtils 和 BaseLink 的实现为准。
     */
    #endregion

    #region ProtoBuf
    /**
     * 本项目用 .proto 文件声明消息字段，再生成 C# 类型。发送时转换为二进制，
     * 接收时由对应 Parser.ParseFrom 还原；外层消息包的协议号决定使用哪一种业务消息。
     * Protobuf 使用字段编号识别数据。修改协议时不能随意复用已有编号，
     * 也不能只因增加了字段就断言所有新旧客户端一定兼容，仍需核对字段类型和业务含义。
     * JSON 是便于直接阅读的文本格式，Protobuf 是依赖协议定义的二进制格式；
     * 两者的具体大小、速度和内存差异取决于数据和实现，本项目没有在此记录性能实测。
     */
    #endregion

}
