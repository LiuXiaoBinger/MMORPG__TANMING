/// <summary>解耦任务相关业务管理器。</summary>
public sealed class DecoupleManager
{
    private static readonly DecoupleManager _instance = new DecoupleManager();
    public static DecoupleManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
