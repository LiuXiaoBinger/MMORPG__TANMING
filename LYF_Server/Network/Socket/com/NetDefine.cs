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
    public const int GameServerPort = 10130;//游戏逻辑服务器端口号
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
    public const ushort CMD_RoleKnapsackInfoCode = 11110;//查询并装载角色的分类背包与次数数据
    public const ushort CMD_RoleShopPurchaseInfoCode = 11111;//同步角色商城购买次数
    public const ushort CMD_OpenKnapsackGridCode = 11120;//请求开启背包格子并返回当前已开启数量
    public const ushort CMD_SyncKnapsackGridCountCode = 11121;//GameServer 同步已开背包格子数量到中心服
    public const ushort CMD_BuyShopItemCode = 11130;//客户端购买商城商品
    public const ushort CMD_SaveRoleDataCode = 11140;//GameServer 提交角色物品/次数快照，CenterServer 返回保存结果
    public const ushort CMD_CountUpdateSyncInfoCode = 11150;//将商城、副本等次数变化通知客户端
    public const ushort CMD_SC_UpdateItemInfoCode = 11160;//更新item给客户端
    /// <summary>客户端装备穿脱、换装和特技切换请求与权威结果共用编号。</summary>
    public const ushort CMD_EquipOperationCode = 11161;
    /// <summary>装备与基因效果计算后的最终角色属性通知。</summary>
    public const ushort CMD_RoleAttributeUpdateCode = 11162;
    /// <summary>客户端施法请求与服务端权威结果共用的协议号。</summary>
    public const ushort CMD_SkillCastCode = 11170;
    /// <summary>服务器确认施法后的表现开始通知协议号。</summary>
    public const ushort CMD_SkillCastStartCode = 11171;
    /// <summary>服务器正常结束或中断施法的表现结束通知协议号。</summary>
    public const ushort CMD_SkillCastEndCode = 11173;
    /// <summary>副本实例内怪物的权威生命值变化，走可靠下行。</summary>
    public const ushort CMD_MonsterAttrUpdateCode = 11174;
    /// <summary>Unity 按 RO UpdateMove 上报位置和朝向的请求协议号。</summary>
    public const ushort CMD_MoveOperationCode = 11180;
    /// <summary>服务器向现有附近角色转发移动状态的通知协议号。</summary>
    public const ushort CMD_SyncMoveCode = 11181;
    /// <summary>客户端请求把实时协议绑定到 KCP 通道，网关回包携带会话号与令牌。</summary>
    public const ushort CMD_KcpBindCode = 11190;
    /// <summary>16 kHz 单声道 16 位 PCM 每秒字节数。</summary>
    public const int ChatAudioBytesPerSecond = 32000;
    /// <summary>录音上限 30 秒。</summary>
    public const int ChatAudioMaxBytes = ChatAudioBytesPerSecond * 30;
    /// <summary>为 16 位 TCP 帧头、protobuf 及压缩预留空间的语音片长。</summary>
    public const int ChatAudioChunkBytes = 24 * 1024;
    /// <summary>客户端向自托管媒体服务上传一个录音片。</summary>
    public const ushort RpcC2M_UploadChatAudio = 11234;
    /// <summary>客户端点击气泡按 ID 读取一个录音片。</summary>
    public const ushort RpcC2M_DownloadChatAudio = 11235;
    /// <summary>游戏服携带已认证角色身份转发媒体请求。</summary>
    public const ushort PtcG2M_ChatAudioTransfer = 11236;
    /// <summary>RO 发言 RPC，客户端请求和单播结果共享编号。</summary>
    public const ushort RpcC2M_SendChatMsg = 11230;
    /// <summary>RO 网关发给玩家的聊天正文通知。</summary>
    public const ushort PtcT2C_ChattingMsgNtf = 11231;
    /// <summary>MS 下发世界广播或权威队伍收件名单。</summary>
    public const ushort PtcM2T_ChatBroadCastM2T = 11232;
    /// <summary>MS 交给持有场景角色的 GS 计算附近收件人。</summary>
    public const ushort PtcM2G_ChatBroadCastM2Gs = 11233;
    /// <summary>独立 MasterServer 在本机服务网段监听的 TCP 端口。</summary>
    public const int MasterServerPort = 10150;
    /// <summary>GS 转发已认证玩家的组队请求。</summary>
    public const ushort CMD_MasterTeamRequest = 11220;
    /// <summary>MS 交付给单个玩家的邀请或业务错误。</summary>
    public const ushort CMD_MasterTeamDelivery = 11221;
    /// <summary>MS 发布成员名单，GS 补充在线角色属性后发送。</summary>
    public const ushort CMD_MasterTeamRoster = 11222;
    /// <summary>MS 授权 GS 按成员清单创建或重用副本。</summary>
    public const ushort CMD_MasterTeamEnter = 11223;
    /// <summary>MS 通知 GS 执行主动退队后的场景清理。</summary>
    public const ushort CMD_MasterTeamExit = 11224;
    /// <summary>GS 告知 MS 角色已离线。</summary>
    public const ushort CMD_MasterTeamOffline = 11225;
    /// <summary>GS 队列内部的队伍服断线事件，不注册为网络命令。</summary>
    public const ushort CMD_MasterTeamLost = 11226;

    /// <summary>客户端点击副本物件或采集波次，身份来自网关会话。</summary>
    public const ushort CMD_DungeonInteractCode = 11240;
    /// <summary>副本交互结果以及权威位置校正。</summary>
    public const ushort CMD_DungeonInteractResultCode = 11241;
    /// <summary>副本原NPC对话命令向当前实例SceneAI发送具名事件。</summary>
    public const ushort CMD_DungeonCommandEventCode = 11242;
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
    /// <summary>队员回复是否进本，名称沿用 RO PtcC2M_MemberReplyEnterFBPtc。</summary>
    public const ushort PtcC2M_MemberReplyEnterFBPtc = 11206;
    /// <summary>MS 通知全队开始入场确认。</summary>
    public const ushort PtcM2C_AskMemberEnterFBNtf = 11214;
    /// <summary>MS 向全队同步某成员的同意或拒绝。</summary>
    public const ushort PtcM2C_SyncMemberReplyEnterFBNtf = 11215;
    /// <summary>MS 通知确认超时或名单变化，本次不进入副本。</summary>
    public const ushort PtcM2C_FailEnterFBNtf = 11216;
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
