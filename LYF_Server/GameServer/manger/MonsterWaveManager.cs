/// <summary>怪物波次管理器。</summary>
public sealed class MonsterWaveManager
{
    private static readonly MonsterWaveManager _instance = new MonsterWaveManager();
    public static MonsterWaveManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
