

using System.Collections.Generic;
using System.Threading;

public class SessionMgr :Singleton<SessionMgr>
{
    private  Dictionary<int, Session> _sessionDic =new Dictionary<int, Session>();

    private int _instanceInter;

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

    public int getInstanceInter()
    {
        return Interlocked.Increment(ref _instanceInter);
    }

    public void RemoveSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            _sessionDic.Remove(sessionId);
        }
    }

    public Session GetSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            return _sessionDic[sessionId];
        }
        return null;
    }

    public int GetSessionCount()
    {
        return _sessionDic.Count;
    }
}