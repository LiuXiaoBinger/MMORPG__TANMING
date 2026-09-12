/// <summary>后台活动管理器。</summary>
public sealed class BackstageActivityManager
{
    private static readonly BackstageActivityManager _instance = new BackstageActivityManager();
    public static BackstageActivityManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
