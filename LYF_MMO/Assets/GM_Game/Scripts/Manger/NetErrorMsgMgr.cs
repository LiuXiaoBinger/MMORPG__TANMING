using Google.Protobuf;
using System;
using UnityEngine;

/**
 * Title:
 * Description:
 */


public class NetErrorMsgMgr : Singleton<NetErrorMsgMgr>
{

    public void Init()
    {

        //注册错误相关消息
        SocketDispatcher.Instance.AddEventHandler(NetDefine.CMD_ErrCode, OnErrorMsgHandle);
    }

    /// <summary>
    /// 处理错误消息
    /// </summary>
    /// <param name="data"></param>
    private void OnErrorMsgHandle(ByteString data)
    {
        if (data == null || data.Length == 0)
        {
            Debug.LogWarning("收到空的通用错误码消息");
            TipsMgr.Instance.ShowSystemTips("请求失败，请稍后重试");
            return;
        }

        ErrMsg errMsg;
        try
        {
            errMsg = ErrMsg.Parser.ParseFrom(data);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"解析通用错误码消息失败：{exception.Message}");
            TipsMgr.Instance.ShowSystemTips("请求失败，请稍后重试");
            return;
        }

        switch (errMsg.CmdCode)
        {
            case CmdCode.AcctExist:
                TipsMgr.Instance.ShowSystemTips("账号已存在..");
                break;
            case CmdCode.ServerError:
                TipsMgr.Instance.ShowSystemTips("服务端错误..");
                break;
            case CmdCode.AcctNotExist:
                TipsMgr.Instance.ShowSystemTips("账号不存在..");
                break;
            case CmdCode.PasswordError:
                TipsMgr.Instance.ShowSystemTips("密码错误..");
                break;
            case CmdCode.AcctDisable:
                TipsMgr.Instance.ShowSystemTips("账号已禁用..");
                break;
            case CmdCode.ReqParamError:
                TipsMgr.Instance.ShowSystemTips("请求参数错误..");
                break;
            case CmdCode.NicknameExist:
                TipsMgr.Instance.ShowSystemTips("昵称已经存在..");
                break;
            case CmdCode.UserNameIllegal:
                TipsMgr.Instance.ShowSystemTips("用户名不合法..");
                break;
            case CmdCode.PhoneNumIllegal:
                TipsMgr.Instance.ShowSystemTips("手机号不合法..");
                break;
            case CmdCode.PasswordIllegal:
                TipsMgr.Instance.ShowSystemTips("密码不合法..");
                break;
            case CmdCode.UserOftenLogin:
                TipsMgr.Instance.ShowSystemTips("用户频繁登录,请稍等..");
                break;
            case CmdCode.VarifyError:
                TipsMgr.Instance.ShowSystemTips("验证码错误");
                break;
            case CmdCode.RoleNotExist:
                TipsMgr.Instance.ShowSystemTips("角色不存在");
                break;
            default:
                // GateServer 只发送通用错误码时，未知或后续新增错误码也必须反馈给玩家。
                TipsMgr.Instance.ShowSystemTips($"请求失败，错误码：{errMsg.CmdCode}");
                break;
        }

    }
}
