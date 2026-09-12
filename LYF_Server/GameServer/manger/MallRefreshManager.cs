/// <summary>商城刷新管理器。</summary>
public sealed class MallRefreshManager
{
    private static readonly MallRefreshManager _instance = new MallRefreshManager();
    public static MallRefreshManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
