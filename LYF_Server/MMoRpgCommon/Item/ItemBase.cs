using System;

/// <summary>
/// 游戏运行时物品实例的基础数据。
/// ItemUID 标识一件具体物品，ItemID 标识策划配置，两者禁止互相替代。
/// </summary>
public class ItemBase
{
    // 物品实例唯一 ID，用于容器、装备和持久化关联。
    private long _itemUid;
    // 物品配置 ID，用于读取 Luban 配置和同类物品统计。
    private int _itemId;
    // 当前所属角色 ID。
    private int _roleId;
    // 当前所在背包或容器类型。
    private int _bagType;
    // 当前所在容器格位。
    private int _bagIndex;
    // 当前堆叠数量。
    private int _count;
    // 物品状态位，绑定和损坏等状态由各业务位定义。
    private int _itemSign;
    // 购买或出售使用的货币配置 ID。
    private int _moneyType;
    // 当前堆叠物品的总买入价格。 //道具买入平均价格 用于出售 防止玩家屯道具刷钱;
    private long _totalPrice;
    // 物品实例创建时间，统一使用 UTC。
    private DateTime _createTime;
    // 物品实例过期时间，DateTime.MinValue 表示永久有效。
    private DateTime _expireTime;
    // 装备、消耗品等子类使用的扩展状态数据。
    private readonly ItemExtraData _extraData;

    /// <summary>创建新物品实例，并生成进程内唯一 UID。</summary>
    public ItemBase()
        : this(true)
    {
    }

    /// <summary>由子类选择是否生成 UID；持久化恢复必须传 false。</summary>
    protected ItemBase(bool generateUid)
    {
        if (generateUid)
        {
            _itemUid = UIDHelper.CreateUID(UIDType.UID_Item);
        }
        _createTime = DateTime.UtcNow;
        _extraData = new ItemExtraData();
    }


    /// <summary>按配置创建新物品实例。</summary>
    public ItemBase(int itemId, int count, int itemSign = 0)
        : this()
    {
        SetItemID(itemId);
        SetItemCount(count);
        SetItemSign(itemSign);
    }

    /// <summary>创建不生成新 UID 的普通物品对象，仅用于恢复权威持久化数据。</summary>
    public static ItemBase CreateForRestore()
    {
        return new ItemBase(false);
    }

    /// <summary>获取物品实例唯一 ID。</summary>
    public long GetItemUID()
    {
        return _itemUid;
    }

    /// <summary>设置物品实例 UID。仅用于从权威持久化数据恢复实例。</summary>
    public void SetItemUID(long itemUid)
    {
        if (itemUid <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemUid), "物品实例 UID 必须为正数。");
        }

        _itemUid = itemUid;
    }

    /// <summary>获取物品配置 ID。</summary>
    public int GetItemID()
    {
        return _itemId;
    }

    /// <summary>设置物品配置 ID。</summary>
    public void SetItemID(int itemId)
    {
        if (itemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemId), "物品配置 ID 必须为正数。");
        }

        _itemId = itemId;
    }

    /// <summary>获取所属角色 ID。</summary>
    public int GetRoleID()
    {
        return _roleId;
    }

    /// <summary>获取当前容器类型。</summary>
    public int GetBagType()
    {
        return _bagType;
    }

    /// <summary>获取当前容器格位。</summary>
    public int GetBagIndex()
    {
        return _bagIndex;
    }

    /// <summary>设置实例归属和容器位置，仅由容器在放入物品时调用。</summary>
    public void SetLocation(int roleId, int bagType, int bagIndex)
    {
        _roleId = roleId;
        _bagType = bagType;
        _bagIndex = bagIndex;
    }

    /// <summary>仅更新容器格位，供容器整理时保持归属与容器类型不变。</summary>
    public void SetBagIndex(int bagIndex)
    {
        _bagIndex = bagIndex;
    }

    /// <summary>获取当前堆叠数量。</summary>
    public int GetItemCount()
    {
        return _count;
    }

    /// <summary>设置堆叠数量；数量必须不小于零。</summary>
    public void SetItemCount(long count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "物品数量不能小于零。");
        }

        _count = (int)count;
    }

    /// <summary>调整堆叠数量；数量降到零时由容器负责移除实例。</summary>
    public bool TryChangeCount(long delta)
    {
        long changedCount = _count + delta;
        if (changedCount < 0 || changedCount > int.MaxValue)
        {
            return false;
        }

        _count = (int)changedCount;
        return true;
    }

    /// <summary>获取物品状态位。</summary>
    public int GetItemSign()
    {
        return _itemSign;
    }

    /// <summary>设置物品状态位。</summary>
    public void SetItemSign(int itemSign)
    {
        _itemSign = itemSign;
    }

    /// <summary>设置指定物品状态位，不影响其他状态位。</summary>
    public void SetSign(SignBit bit, bool sign)
    {
        // 先生成目标位掩码，再按设置或清除语义更新状态位。
        int signMask = 1 << (int)bit;
        SetItemSign(sign ? GetItemSign() | signMask : GetItemSign() & ~signMask);
    }

    /// <summary>获取指定物品状态位是否已设置。</summary>
    public bool GetSign(SignBit bit)
    {
        return ((GetItemSign() >> (int)bit) & 1) == 1;
    }

    /// <summary>是否已绑定。绑定状态使用状态位最低位保存。</summary>
    public bool IsBound()
    {
        return (_itemSign & 1) != 0;
    }

    /// <summary>设置绑定状态，不影响其他状态位。</summary>
    public void SetBound(bool isBound)
    {
        _itemSign = isBound ? _itemSign | 1 : _itemSign & ~1;
    }

    /// <summary>获取购买或出售所用货币配置 ID。</summary>
    public int GetMoneyType()
    {
        return _moneyType;
    }

    public void SetTotalPrice(long price) { _totalPrice = price > 0 ? price : 0; ; }
    /// <summary>获取该物品堆叠的总买入价格。</summary>
    public long GetTotalPrice() { return _totalPrice; }
    public long GetAvgPrice() { return  _count > 0 ? _totalPrice / _count : 0; }

    /// <summary>设置价格和货币类型，负价格会规范化为零。</summary>
    public void SetPrice(int moneyType, long totalPrice)
    {
        _moneyType = moneyType;
        _totalPrice = Math.Max(0L, totalPrice);
    }

    /// <summary>获取物品创建时间。</summary>
    public DateTime GetCreateTime()
    {
        return _createTime;
    }

    /// <summary>设置物品创建时间。</summary>
    public void SetCreateTime(DateTime createTime)
    {
        _createTime = createTime;
    }

    /// <summary>获取物品到期时间；未设置时为 DateTime.MinValue。</summary>
    public DateTime GetExpireTime()
    {
        return _expireTime;
    }

    /// <summary>设置过期时间；未过期物品传入 DateTime.MinValue。</summary>
    public void SetExpireTime(DateTime expireTime)
    {
        _expireTime = expireTime;
    }

    /// <summary>获取不同物品子类使用的扩展数据。</summary>
    public ItemExtraData GetExtraData()
    {
        return _extraData;
    }

    /// <summary>复制可变实例数据，不复制 UID、角色归属和容器格位。</summary>
    public virtual void CopyMutableStateFrom(ItemBase source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        _itemId = source.GetItemID();
        _count = source.GetItemCount();
        _itemSign = source.GetItemSign();
        _moneyType = source.GetMoneyType();
        _totalPrice = source.GetTotalPrice();
        _createTime = source.GetCreateTime();
        _expireTime = source.GetExpireTime();
    }
    void SetBind(bool bind)
    {
        SetSign(SignBit.BindBit, bind);
    }

    public ItemBind IsBind()
    {
        if (GetSign(SignBit.BindBit))
        {
            return ItemBind.kItemBindIs;
        }
        return ItemBind.kItemBindNo;
    }

    
    public static bool IsMoney(int item_id)
    {
        return item_id == (int)ItemEnum.ZENY 
               || item_id == (int)ItemEnum.ROMONEY 
               || item_id == (int)ItemEnum.DIAMOND 
               || item_id == (int)ItemEnum.Coin104;
    }
    public static bool IsDiamond(int item_id)
    {
        return item_id == (int)ItemEnum.DIAMOND || item_id == (int)ItemEnum.Coin104;
    }
    public static bool IsPrimaryMoney(int item_id)
    {
        return item_id == (int)ItemEnum.DIAMOND 
               || item_id == (int)ItemEnum.Coin104 
               || item_id == (int)ItemEnum.QQ_POINT;
    }
}
