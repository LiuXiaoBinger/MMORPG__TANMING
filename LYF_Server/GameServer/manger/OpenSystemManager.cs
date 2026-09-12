/// <summary>系统开放管理器。</summary>
public sealed class OpenSystemManager
{
    private static readonly OpenSystemManager _instance = new OpenSystemManager();
    public static OpenSystemManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
