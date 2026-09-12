/// <summary>世界事件管理器。</summary>
public sealed class WorldEventManager
{
    private static readonly WorldEventManager _instance = new WorldEventManager();
    public static WorldEventManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
