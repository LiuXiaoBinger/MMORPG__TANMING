/// <summary>GVG 玩法管理器。</summary>
public sealed class GvgManager
{
    private static readonly GvgManager _instance = new GvgManager();
    public static GvgManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
