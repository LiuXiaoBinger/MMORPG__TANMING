/// <summary>场景管理器。</summary>
public sealed class SceneManager
{
    private static readonly SceneManager _instance = new SceneManager();
    public static SceneManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
