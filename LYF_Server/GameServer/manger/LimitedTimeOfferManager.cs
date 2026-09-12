/// <summary>限时特惠管理器。</summary>
public sealed class LimitedTimeOfferManager
{
    private static readonly LimitedTimeOfferManager _instance = new LimitedTimeOfferManager();
    public static LimitedTimeOfferManager Instance { get { return _instance; } }
    public void Update(int deltaMilliseconds) { }
}
