/// <summary>把 GameGlobal 固定逻辑帧交给当前已开启的队伍副本怪物。</summary>
public sealed class MonsterWaveManager
{
    /// <summary>进程启动后共用的管理器实例。</summary>
    private static readonly MonsterWaveManager _instance = new MonsterWaveManager();
    /// <summary>返回逻辑帧调用的共享实例。</summary>
    public static MonsterWaveManager Instance { get { return _instance; } }
    /// <summary>推进本帧的副本怪物决策、移动和攻击结果。</summary>
    /// <param name="deltaMilliseconds">自上一逻辑帧经过的毫秒。</param>
    public void Update(int deltaMilliseconds) { TeamManager.Instance.UpdateMonsters(deltaMilliseconds); }
}
