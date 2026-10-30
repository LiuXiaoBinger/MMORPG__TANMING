using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// 网络层全局常量：各服务监听地址/端口与协议号定义。
/// </summary>
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
    public const ushort CMD_RoleKnapsackInfoCode = 11110;//同步其他角色给主角
    public const ushort CMD_RoleShopPurchaseInfoCode = 11111;//同步角色商城购买次数
    public const ushort CMD_OpenKnapsackGridCode = 11120;//请求开启背包格子并返回当前已开启数量
    public const ushort CMD_SyncKnapsackGridCountCode = 11121;//GameServer 同步已开背包格子数量到中心服
    public const ushort CMD_BuyShopItemCode = 11130;//客户端购买商城商品
    public const ushort CMD_SaveRoleDataCode = 11140;//GameServer 定时保存角色物品数据
    public const ushort CMD_CountUpdateSyncInfoCode = 11150;//更新商店购买 副本次数到客户端 与中心服务器
    public const ushort CMD_SC_UpdateItemInfoCode = 11160;//更新item给客户端
    /// <summary>客户端施法请求与服务端权威结果共用的协议号。</summary>
    public const ushort CMD_SkillCastCode = 11170;
    /// <summary>服务器确认施法后的表现开始通知协议号。</summary>
    public const ushort CMD_SkillCastStartCode = 11171;
    /// <summary>服务器正常结束或中断施法的表现结束通知协议号。</summary>
    public const ushort CMD_SkillCastEndCode = 11173;
    /// <summary>Unity 按 RO UpdateMove 上报位置和朝向的请求协议号。</summary>
    public const ushort CMD_MoveOperationCode = 11180;
    /// <summary>服务器向现有附近角色转发移动状态的通知协议号。</summary>
    public const ushort CMD_SyncMoveCode = 11181;
    /// <summary>客户端请求把实时协议绑定到 KCP 通道，网关回包携带会话号与令牌。</summary>
    public const ushort CMD_KcpBindCode = 11190;
    /// <summary>创建队伍请求，对应 RO TeamMgr.lua RequestCreateTeam。</summary>
    public const ushort CMD_CreateTeamCode = 11200;
    /// <summary>邀请他人入队请求，对应 RO TeamMgr.lua InviteJoinTeam。</summary>
    public const ushort CMD_InviteJoinTeamCode = 11201;
    /// <summary>同意入队邀请请求，对应 RO TeamMgr.lua AcceptTeamInvatation。</summary>
    public const ushort CMD_AcceptTeamInvatationCode = 11202;
    /// <summary>离队请求，对应 RO TeamMgr.lua LeaveTeam。</summary>
    public const ushort CMD_LeaveTeamCode = 11203;
    /// <summary>按队伍ID申请加入请求，对应 RO TeamMgr.lua BegInTeamByTeamId。</summary>
    public const ushort CMD_BegInTeamByTeamIdCode = 11204;
    /// <summary>队长请求进入副本，对应 RO DungeonMgr CaptainRequestEnterFBPtc。</summary>
    public const ushort CMD_CaptainRequestEnterFBCode = 11205;
    /// <summary>服务端下发队伍完整状态，客户端据此刷新队伍窗口。</summary>
    public const ushort CMD_TeamInfoCode = 11210;
    /// <summary>服务端下发邀请入队通知，对应 RO TeamInvatationData。</summary>
    public const ushort CMD_TeamInvatationCode = 11211;
    /// <summary>服务端下发副本入场通知，携带实例ID、场景与出生点。</summary>
    public const ushort CMD_TeamEnterDungeonCode = 11212;
    /// <summary>服务端下发角色离开视野通知，客户端据此删除远端角色。</summary>
    public const ushort CMD_RoleLeaveCode = 11213;
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
