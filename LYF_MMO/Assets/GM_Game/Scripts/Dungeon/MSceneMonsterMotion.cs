/// <summary>一个出生位最近收到的服务端动作及其到达时刻，供晚加载的模型还原动作进度。</summary>
public sealed class MSceneMonsterMotion
{
    /// <summary>服务端给出的起终点、朝向、动作阶段和剩余时长。</summary>
    public readonly MonsterActionData Action;
    /// <summary>客户端接收此动作时的单调时钟秒数，与资源加载和游戏缩放时间无关。</summary>
    public readonly double ReceivedAt;

    /// <summary>缓存这次权威动作，不改变后续收到的同出生位动作。</summary>
    /// <param name="action">服务端可靠下行中的单个出生位动作。</param>
    /// <param name="receivedAt">收包时实时单调时钟的秒数。</param>
    public MSceneMonsterMotion(MonsterActionData action, double receivedAt)
    {
        Action = action;
        ReceivedAt = receivedAt;
    }
}
