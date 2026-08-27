using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class NetDefine
{
    public const string IPHost = "127.0.0.1";//本机IP
    public const int CenterServerPort = 10110;//中心服务器端口号
    
    public const int LoginServerPort = 10120;//登录服务器的端口号
    public const int GameServerPort = 10130;//登录服务器的端口号
    public const int GateServerPort = 10140;//网关服务器端口号
    public const ushort CMD_ErrCode = 10001;//错误码，
    public const ushort CMD_RegistVarifyCode = 11000;//获取验证码
    public const ushort CMD_RegistCode = 11010;//请求码
    public const ushort CMD_LoginCode = 11020;//登录请求码
    public const ushort CMD_GetServerListCode = 11030;//获取服务器列表
    public const ushort CMD_LoginGameServerCode = 11040;//登录游戏服务器
    public const ushort CMD_CreateRoleCode = 11050;//创建角色
    public const ushort CMD_StartGameCode = 11060;//开始游戏请求码
    public const ushort CMD_EnterWroldCode = 11070;//角色进入世界
    public const ushort CMD_RoleSkillInfoCode = 11080;//技能
    public const ushort CMD_SyncRoleEnterWorldCode = 11090;//同步角色进入游戏世界数据给其他玩家
    public const ushort CMD_SyncotherOnlineCode = 11100;//同步其他角色给主角
}


/// <summary>
/// 连接状态
/// </summary>
public enum ConnState
{

    Connected,
    Disconnected,

}


/// <summary>
/// 客户端类型
/// </summary>
public enum ClientType
{
    Unity,
    LoginServer,
    GameServer,
    GateServer,
}