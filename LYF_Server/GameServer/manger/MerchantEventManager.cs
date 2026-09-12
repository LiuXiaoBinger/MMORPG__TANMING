/// <summary>商人事件管理器。</summary>
public sealed class MerchantEventManager
{
    private static readonly MerchantEventManager _instance = new MerchantEventManager();
    public static MerchantEventManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
