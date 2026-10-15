

using System.Collections.Generic;
using System.Threading;

/// <summary>
/// 会话管理器：按会话 ID 管理所有活动 TCP 会话，全局单例。
/// </summary>
public class SessionMgr :Singleton<SessionMgr>
{
    /// <summary>会话 ID 到会话对象的映射。</summary>
    private  Dictionary<int, Session> _sessionDic =new Dictionary<int, Session>();

    /// <summary>自增会话 ID 生成器。</summary>
    private int _instanceInter;

    /// <summary>注册会话；sessionId 小于等于 0 时自动分配自增 ID。</summary>
    public void AddSession( Session session , int sessionId=-1)
    {
        if (sessionId <= 0)
        {
            sessionId = getInstanceInter();
        }

        if (!_sessionDic.ContainsKey(sessionId))
        {
            session.SessionID = sessionId;
            _sessionDic.Add(sessionId, session);
        }
    }

    /// <summary>原子自增并返回下一个会话 ID。</summary>
    public int getInstanceInter()
    {
        return Interlocked.Increment(ref _instanceInter);
    }

    /// <summary>按会话 ID 移除会话。</summary>
    public void RemoveSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            _sessionDic.Remove(sessionId);
        }
    }

    /// <summary>按会话 ID 查询会话；不存在返回 null。</summary>
    public Session GetSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            return _sessionDic[sessionId];
        }
        return null;
    }

    /// <summary>返回当前活动会话数量。</summary>
    public int GetSessionCount()
    {
        return _sessionDic.Count;
    }
}