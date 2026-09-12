/// <summary>
/// 物品或货币变动的完整原因，保持与参考服务器的 ChangeReason 语义一致。
/// </summary>
public sealed class ChangeReason
{
    // 物品变动的主原因。
    private ItemChangeReason _mainReason;
    // 物品变动的次原因。
    private ItemChangeReason _subReason;
    // 与击杀怪物相关的怪物类型，-1 表示不适用。
    private int _monsterType;
    // 原因携带的附加数值参数。
    private int _extraParam;
    // 订单或交易流水号。
    private string _billNo;
    // 经验和职业经验加成倍率。
    private double _expRate;
    // 附加参数的业务类型。
    private RExtraParamType _extraParamType;

    /// <summary>使用参考服务器默认值创建原因。</summary>
    public ChangeReason()
    {
        _mainReason = ItemChangeReason.ITEM_REASON_NONE;
        _subReason = ItemChangeReason.ITEM_REASON_NONE;
        _monsterType = -1;
        _extraParam = 0;
        _billNo = string.Empty;
        _expRate = 1.0;
        _extraParamType = RExtraParamType.kExtraParamTypeNone;
    }

    /// <summary>使用主原因创建原因，次原因为 NONE。</summary>
    public ChangeReason(ItemChangeReason reason) : this()
    {
        _mainReason = reason;
    }

    /// <summary>使用主原因和次原因创建原因。</summary>
    public ChangeReason(ItemChangeReason mainReason, ItemChangeReason subReason) : this()
    {
        _mainReason = mainReason;
        _subReason = subReason;
    }

    /// <summary>主原因。</summary>
    public ItemChangeReason MainReason
    {
        get { return _mainReason; }
        set { _mainReason = value; }
    }

    /// <summary>次原因。</summary>
    public ItemChangeReason SubReason
    {
        get { return _subReason; }
        set { _subReason = value; }
    }

    /// <summary>怪物类型，-1 表示不适用。</summary>
    public int MonsterType
    {
        get { return _monsterType; }
        set { _monsterType = value; }
    }

    /// <summary>附加数值参数。</summary>
    public int ExtraParam
    {
        get { return _extraParam; }
        set { _extraParam = value; }
    }

    /// <summary>订单或交易流水号。</summary>
    public string BillNo
    {
        get { return _billNo; }
        set { _billNo = value ?? string.Empty; }
    }

    /// <summary>经验和职业经验加成倍率。</summary>
    public double ExpRate
    {
        get { return _expRate; }
        set { _expRate = value; }
    }

    /// <summary>附加参数类型。</summary>
    public RExtraParamType ExtraParamType
    {
        get { return _extraParamType; }
        set { _extraParamType = value; }
    }

    public ItemChangeReason GetMainReason() { return _mainReason; }
    public void SetMainReason(ItemChangeReason value) { _mainReason = value; }
    public ItemChangeReason GetSubReason() { return _subReason; }
    public void SetSubReason(ItemChangeReason value) { _subReason = value; }
    public int GetMonsterType() { return _monsterType; }
    public void SetMonsterType(int value) { _monsterType = value; }
    public int GetExtraParam() { return _extraParam; }
    public void SetExtraParam(int value) { _extraParam = value; }
    public string GetBillNo() { return _billNo; }
    public void SetBillNo(string value) { _billNo = value ?? string.Empty; }
    public double GetExpRate() { return _expRate; }
    public void SetExpRate(double value) { _expRate = value; }
    public RExtraParamType GetExtraParamType() { return _extraParamType; }
    public void SetExtraParamType(RExtraParamType value) { _extraParamType = value; }

    /// <summary>转换为主原因整数；空引用按 NONE 处理。</summary>
    public static implicit operator int(ChangeReason reason)
    {
        return reason == null ? (int)ItemChangeReason.ITEM_REASON_NONE : (int)reason._mainReason;
    }

    /// <summary>转换为主原因无符号整数；空引用按 NONE 处理。</summary>
    public static implicit operator uint(ChangeReason reason)
    {
        return reason == null ? (uint)ItemChangeReason.ITEM_REASON_NONE : (uint)reason._mainReason;
    }

    /// <summary>转换为主原因枚举；空引用按 NONE 处理。</summary>
    public static implicit operator ItemChangeReason(ChangeReason reason)
    {
        return reason == null ? ItemChangeReason.ITEM_REASON_NONE : reason._mainReason;
    }
}
