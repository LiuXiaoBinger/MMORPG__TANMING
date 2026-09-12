using Google.Protobuf;

/// <summary>
/// 角色到 CenterServer 的数据发送器，对齐参考项目 RoleNetSender。
/// 同一角色只保留一个在途版本，失败或超时始终重发同一份快照。
/// </summary>
public sealed class RoleNetSender
{
    // 保存确认等待时间超过该值时，使用相同版本和快照重试。
    private const int RetryIntervalMilliseconds = 5000;
    // GameServer 到 CenterServer 的共享连接。
    private static NetClient _centerClient;
    // 所属角色，负责提供当前会话和待保存组件。
    private readonly OnlineRole _role;
    // 当前唯一在途的保存请求，确认前不可被新快照替换。
    private SaveRoleDataReq _inFlightRequest;
    // 当前在途请求自上次发送以来经过的毫秒数。
    private int _inFlightMilliseconds;

    public RoleNetSender(OnlineRole role)
    {
        _role = role;
    }

    public static void Configure(NetClient centerClient)
    {
        _centerClient = centerClient;
    }

    public void Update(int deltaMilliseconds)
    {
        if (_inFlightRequest == null || deltaMilliseconds <= 0)
        {
            return;
        }

        _inFlightMilliseconds += deltaMilliseconds;
        if (_inFlightMilliseconds >= RetryIntervalMilliseconds)
        {
            // ACK 丢失、连接失败或中心服处理失败时重发完全相同的版本和快照。
            SendInFlightRequest();
        }
    }

    public bool SendRoleDataToDB()
    {
        if (_role == null || _centerClient == null || _inFlightRequest != null)
        {
            return false;
        }

        SaveRoleDataReq request = _role.SaveAllMoudle();
        if (request == null ||
            (request.ItemDataLists.Count == 0 && request.RoleCountInfoList.Count == 0 &&
             !request.RoleCountSnapshot))
        {
            // RoleCountSnapshot 为 true 且列表为空表示清空次数表，不能被当成空请求丢弃。
            return false;
        }

        _inFlightRequest = request;
        SendInFlightRequest();
        return true;
    }

    public void OnSaveRoleDataResult(SaveRoleDataRet result)
    {
        if (result == null || _inFlightRequest == null || result.RoleId != _inFlightRequest.RoleId ||
            result.Version != _inFlightRequest.Version)
        {
            return;
        }

        if (result.CmdCode != CmdCode.Succeed)
        {
            // 过期或其他失败都不能清脏；保留相同快照，等待固定帧按重试间隔再次提交。
            // PersistedVersion 仅用于诊断，不能用旧 ACK 覆盖本地尚未确认的脏版本。
            _inFlightMilliseconds = 0;
            LogMsg.Info("角色物品保存失败，角色=" + result.RoleId + "，版本=" + result.Version +
                "，权威版本=" + result.PersistedVersion + "，原因=" + result.Message, LogMsgType.Error);
            return;
        }

        _role.OnRoleDataSaved(result.Version);
        _inFlightRequest = null;
        _inFlightMilliseconds = 0;

        // 在途期间产生的新版本不能再等待下一个 60 秒周期，旧版本确认后立即续发。
        SendRoleDataToDB();
    }

    public bool HasInFlightRequest()
    {
        return _inFlightRequest != null;
    }

    private void SendInFlightRequest()
    {
        if (_centerClient == null || _inFlightRequest == null)
        {
            return;
        }

        BasePackage package = new BasePackage
        {
            GateSessionId = _role.GateSessionId,
            UnitySessionId = _role.UnitySessionId
        };
        _centerClient.SendData(package, NetDefine.CMD_SaveRoleDataCode, _inFlightRequest.ToByteString());
        _inFlightMilliseconds = 0;
    }
}
