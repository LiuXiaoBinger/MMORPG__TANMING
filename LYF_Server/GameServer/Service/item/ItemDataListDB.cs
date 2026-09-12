using System;
using System.Collections.Generic;

/// <summary>
/// 单个物品容器的数据库快照状态，对齐参考项目 ItemDataListDB。
/// 物品变化阶段只置脏，Save2Db 才把容器当前状态序列化到保存请求。
/// </summary>
public sealed class ItemDataListDB
{
    // 绑定的实际背包容器，用于保存时读取权威内存快照。
    private readonly RoItemContainer _container;
    // 本列表是否存在尚未得到 CenterServer 确认的修改。
    private bool _needSave;
    // 本列表最后一次修改对应的角色数据版本。
    private long _dirtyVersion;
    // 每次进入脏状态批次递增，用于识别保存请求发出后的新修改。
    private long _changeGeneration;
    // 最近一次组包时包含的修改批次。
    private long _savedGeneration;
    // 最近一次组包对应的角色保存版本，ACK 必须同时匹配该版本。
    private long _savedVersion;
    // 当前容器中的实例数量缓存。
    private int _size;

    /// <summary>创建容器持久化状态并读取当前实例数量。</summary>
    /// <param name="container">绑定的物品容器。</param>
    public ItemDataListDB(RoItemContainer container)
    {
        _container = container;
        if (container == null)
        {
            _size = 0;
        }
        else
        {
            _size = container.GetItemMap().Count;
        }
    }

    /// <summary>标记容器存在待持久化修改，并记录修改时的角色版本。</summary>
    /// <param name="version">修改发生时的角色保存版本。</param>
    public void SetNeedSave(long version)
    {
        _needSave = true;
        if (_changeGeneration < long.MaxValue)
        {
            _changeGeneration++;
        }
        _dirtyVersion = version;
    }

    /// <summary>按 ACK 版本和保存批次清理已确认脏状态。</summary>
    /// <param name="acknowledgedVersion">中心服确认的角色保存版本。</param>
    public void ResetNeedSave(long acknowledgedVersion)
    {
        // 在途期间又发生变化时 dirtyVersion 会更大，旧 ACK 不能清除新脏数据。
        if (_needSave && _changeGeneration == _savedGeneration &&
            _savedVersion == acknowledgedVersion && _dirtyVersion <= acknowledgedVersion)
        {
            _needSave = false;
        }
    }

    /// <summary>判断容器是否存在尚未确认的修改。</summary>
    public bool NeedSave()
    {
        return _needSave;
    }

    /// <summary>获取容器当前物品实例数量。</summary>
    public int GetSize()
    {
        return _size;
    }

    /// <summary>记录新增物品实例并标记脏状态。</summary>
    public void OnAddItem(long version)
    {
        _size++;
        SetNeedSave(version);
    }

    /// <summary>记录删除物品实例并标记脏状态。</summary>
    public void OnDelItem(long version)
    {
        _size = Math.Max(0, _size - 1);
        SetNeedSave(version);
    }

    /// <summary>记录物品属性或数量修改并标记脏状态。</summary>
    public void OnModifyItem(long version)
    {
        SetNeedSave(version);
    }

    /// <summary>记录物品交换并标记脏状态。</summary>
    public void OnSwapItem(long version)
    {
        SetNeedSave(version);
    }

    /// <summary>清理登录装载产生的临时状态，建立干净持久化基线。</summary>
    public void ResetLoadedState()
    {
        if (_container == null)
        {
            _size = 0;
        }
        else
        {
            _size = _container.GetItemMap().Count;
        }
        _needSave = false;
        _dirtyVersion = 0;
        _changeGeneration = 0;
        _savedGeneration = 0;
        _savedVersion = 0;
    }

    /// <summary>把当前容器完整快照写入字段管理器，删除项通过快照缺失表达。</summary>
    public bool Save2Db(RoleDataFieldManager fieldManager, long saveVersion)
    {
        if (!_needSave || fieldManager == null || _container == null)
        {
            return false;
        }
        // 保存请求只记录本次组包批次，ACK 必须回传相同版本才能清脏。
        _savedGeneration = _changeGeneration;
        _savedVersion = saveVersion;

        ItemDataList itemDataList = new ItemDataList
        {
            BagType = (int)_container.ContainerType
        };
        List<ItemBase> items = new List<ItemBase>(_container.GetItemMap().Values);
        for (int i = 0; i < items.Count; i++)
        {
            ItemBase item = items[i];
            RoleItemInfo itemInfo = new RoleItemInfo
            {
                ItemUid = item.GetItemUID(),
                Count = item.GetItemCount(),
                RoleId = item.GetRoleID(),
                ItemTypeId = item.GetItemID(),
                BagType = item.GetBagType(),
                BagIndex = item.GetBagIndex(),
                ItemSign = item.GetItemSign(),
                MoneyType = item.GetMoneyType(),
                TotalPrice = item.GetTotalPrice(),
                CreateTimeUtcTicks = ToUtcTicks(item.GetCreateTime()),
                ExpireTimeUtcTicks = ToUtcTicks(item.GetExpireTime())
            };

            EquipBase equip = item as EquipBase;
            if (equip != null)
            {
                itemInfo.EquipInfo = new RoleEquipItemInfo
                {
                    ItemUid = item.GetItemUID(),
                    RoleId = item.GetRoleID(),
                    StrengthenLevel = equip.StrengthenLevel,
                    EquipType = equip.EquipType
                };
                itemInfo.EquipGeneInfo = new RoleEquipGeneInfo
                {
                    ItemUid = item.GetItemUID(),
                    RoleId = item.GetRoleID(),
                    GeneId0 = equip.GeneID0,
                    GeneId1 = equip.GeneID1,
                    GeneId2 = equip.GeneID2,
                    GeneValue0 = equip.GeneValue0,
                    GeneValue1 = equip.GeneValue1,
                    GeneValue2 = equip.GeneValue2
                };
            }

            itemDataList.Items.Add(itemInfo);
        }

        fieldManager.SaveField(itemDataList);
        return true;
    }

    private static long ToUtcTicks(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return 0L;
        }
        return value.ToUniversalTime().Ticks;
    }
}
