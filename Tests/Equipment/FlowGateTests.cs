extern alias Gate;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Google.Protobuf;
using GateClientResponseForwarder = Gate::GateClientResponseForwarder;

/// <summary>用本机临时 TCP 连接验证真实网关转发结果，不连接正式网关或玩家会话。</summary>
public static class FlowGateTests
{
    /// <summary>建立临时回环连接，确认装备成功、装备失败和属性通知都保持原协议与载荷。</summary>
    public static int Main()
    {
        // 端口 0 由操作系统分配当前测试独占的可用端口。
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            // 接收真实 Session.Send 写出的协议帧，不替换发送实现。
            using (Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                receiver.ReceiveTimeout = 3000;
                receiver.Connect(listener.LocalEndpoint);
                // 网关会话实际使用的发送端连接，生命周期仅限本次测试。
                using (Socket sender = listener.AcceptSocket())
                {
                    // 不启动接收线程，只测试真实网关向客户端发送的路径。
                    Session session = new Session(new Dictionary<int, IContainer>(), null);
                    // 反射仅为测试安装已连接的 Socket；ForwardToUnity 与 Session.Send 均使用正式实现。
                    typeof(BaseLink).GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, sender);
                    // 成功穿戴结果保留 64 位实例编号和操作类型。
                    EquipOperationRet success = new EquipOperationRet
                    {
                        CmdCode = CmdCode.Succeed, ItemUid = 9876543210123L, Operation = EquipOperation.Wear
                    };
                    CheckForward(session, receiver, NetDefine.CMD_EquipOperationCode, success.ToByteString());
                    // 满背包卸装失败仍返回专用装备结果，不能被网关改写为通用错误包。
                    EquipOperationRet failure = new EquipOperationRet
                    {
                        CmdCode = CmdCode.BagFull, ItemUid = success.ItemUid, Operation = EquipOperation.TakeOff
                    };
                    CheckForward(session, receiver, NetDefine.CMD_EquipOperationCode, failure.ToByteString());
                    // 属性通知不含 CmdCode，网关仅验证属性载荷并原样发送。
                    RoleAttributeUpdateNtf attributes = new RoleAttributeUpdateNtf
                    {
                        BaseInfo = new RoleBaseInfo { RoleId = 91001, MaxHp = 1234, CurrHp = 980, AtkExternalMin = 456 }
                    };
                    CheckForward(session, receiver, NetDefine.CMD_RoleAttributeUpdateCode, attributes.ToByteString());
                }
            }
        }
        finally
        {
            listener.Stop();
        }
        Console.WriteLine("PASS Equipment Gate：3 组真实 TCP 转发、12 项协议/会话/载荷断言。");
        return 0;
    }

    /// <summary>将正式回包交给真实网关转发器，再从临时客户端 Socket 读取并比较完整帧。</summary>
    /// <param name="session">当前测试使用的真实网关会话。</param>
    /// <param name="receiver">接收会话发出的 TCP 帧的回环客户端。</param>
    /// <param name="code">本次应保持不变的业务协议编号。</param>
    /// <param name="body">本次应原样转发的 protobuf 载荷。</param>
    private static void CheckForward(Session session, Socket receiver, int code, ByteString body)
    {
        GateClientResponseForwarder.ForwardToUnity(session, new BasePackage
        {
            ProtoCode = code, UnitySessionId = 71, GateSessionId = 19, Data = body
        });
        // 先收满两个字节的长度头，TCP 接收不假设一次调用得到整帧。
        byte[] header = new byte[2];
        ReceiveExact(receiver, header, 0, header.Length);
        // 帧体长度不包含两字节长度头，沿用正式网络协议的小端格式。
        int length = BitConverter.ToUInt16(header, 0);
        // 完整帧交给真实 CRC 与解压入口，不复制协议拆包算法。
        byte[] frame = new byte[length + 2];
        Buffer.BlockCopy(header, 0, frame, 0, 2);
        ReceiveExact(receiver, frame, 2, length);
        // 按实际网络拆包后的 protobuf 字节还原外层协议包。
        BasePackage actual = BasePackage.Parser.ParseFrom(NetUtils.Instance.ParseData(frame, length));
        if (actual.ProtoCode != code) throw new Exception("网关改写了装备协议号");
        if (actual.UnitySessionId != 71) throw new Exception("网关丢失 Unity 会话号");
        if (actual.GateSessionId != 19) throw new Exception("网关丢失 Gate 会话号");
        if (!actual.Data.Equals(body)) throw new Exception("网关改写了装备结果或最终属性载荷");
    }

    /// <summary>读取指定字节数，覆盖真实 TCP 可能出现的分片接收。</summary>
    /// <param name="socket">当前回环客户端连接。</param>
    /// <param name="buffer">接收当前帧头或帧体的数组。</param>
    /// <param name="offset">数组中本段开始写入的位置。</param>
    /// <param name="count">当前协议段要求收满的字节数。</param>
    private static void ReceiveExact(Socket socket, byte[] buffer, int offset, int count)
    {
        // 当前协议段已实际收到的字节数。
        int received = 0;
        while (received < count)
        {
            // 本次 Socket 返回的字节数，0 表示发送端在整帧完成前断开。
            int read = socket.Receive(buffer, offset + received, count - received, SocketFlags.None);
            if (read == 0) throw new Exception("网关测试连接未收到完整协议帧");
            received += read;
        }
    }
}
