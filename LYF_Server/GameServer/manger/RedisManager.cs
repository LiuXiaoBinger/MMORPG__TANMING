/// <summary>Redis 相关业务管理器。</summary>
public sealed class RedisManager
{
    private static readonly RedisManager _instance = new RedisManager();
    public static RedisManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
