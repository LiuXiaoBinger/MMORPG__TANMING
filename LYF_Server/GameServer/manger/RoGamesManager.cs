/// <summary>小游戏相关业务管理器。</summary>
public sealed class RoGamesManager
{
    private static readonly RoGamesManager _instance = new RoGamesManager();
    public static RoGamesManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
