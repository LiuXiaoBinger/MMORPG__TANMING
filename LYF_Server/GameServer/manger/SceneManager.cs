/// <summary>场景管理的逻辑帧接入位置；当前只有单例和空 Update，尚未执行该业务。</summary>
public sealed class SceneManager
{
    /// <summary>进程启动后共用的管理器实例。</summary>
    private static readonly SceneManager _instance = new SceneManager();
    /// <summary>返回逻辑帧调用的共享实例。</summary>
    public static SceneManager Instance { get { return _instance; } }
    /// <summary>保留逐帧调用入口，当前方法没有业务处理。</summary>
    /// <param name="deltaMilliseconds">本帧经过的毫秒数，当前尚未使用。</param>
    public void Update(int deltaMilliseconds) { }
}
