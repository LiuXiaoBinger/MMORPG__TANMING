using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using SqlSugar;

/// <summary>
/// CenterServer 角色物品数据库写任务，对齐参考项目 CRoleWriteTask。
/// 任务在 CenterServer 主循环的单消费者队列中执行，负责将 GameServer 提交的角色物品快照
/// 以事务方式写入 MySQL（物品表、装备表、装备词条表），并更新角色保存版本。
/// 事务提交成功后才向 GameServer 返回 ACK；版本冲突或过期请求不会覆盖数据库权威数据。
/// </summary>
public sealed class CRoleWriteTask
{
    // GameServer 提交的完整角色物品快照和版本。
    private readonly SaveRoleDataReq _request;
    // 用于向发起请求的 GameServer 回传事务结果。
    private readonly ServerBase _serverBase;
    // 回包所需的网关和 Unity 会话信息副本，确保回包能沿原链路返回。
    private readonly BasePackage _basePackage;

    /// <summary>
    /// 构造角色物品写任务。
    /// </summary>
    /// <param name="request">GameServer 提交的保存请求，包含角色 ID、版本号和各背包物品快照。</param>
    /// <param name="serverBase">服务器基类，用于发送回包。</param>
    /// <param name="basePackage">原始请求的会话信息副本，为空时创建空包。</param>
    public CRoleWriteTask(SaveRoleDataReq request, ServerBase serverBase, BasePackage basePackage)
    {
        _request = request;
        _serverBase = serverBase;
        // 克隆会话信息，避免任务执行期间原始包被复用或修改。
        if (basePackage == null)
        {
            _basePackage = new BasePackage();
        }
        else
        {
            _basePackage = basePackage.Clone();
        }
    }

    /// <summary>
    /// 执行数据库写任务的主入口，在 CenterServer 逻辑线程调用。
    /// 流程：参数校验 → 开启事务 → 查询角色 → 版本三分支校验（同版本幂等/过期拒绝/新版本写入）
    /// → 删旧写新 → 更新角色版本 → 提交事务 → 回包。
    /// 任何异常都会回滚事务并返回错误码，不会部分写入。
    /// </summary>
    /// <param name="db">SqlSugar 数据库客户端，事务由此对象管理。</param>
    public void Execute(SqlSugarClient db)
    {
        // 构造默认失败回包，成功路径会覆盖 CmdCode。
        int roleId = 0;
        long version = 0L;
        if (_request != null)
        {
            roleId = _request.RoleId;
            version = _request.Version;
        }
        SaveRoleDataRet result = new SaveRoleDataRet
        {
            RoleId = roleId,
            Version = version,
            CmdCode = CmdCode.ServerError
        };

        try
        {
            // 第一步：校验请求参数合法性，非法直接抛异常进入回滚路径。
            ValidateRequest();
            // 第二步：开启数据库事务，后续所有删旧写新和版本更新要么全部成功要么全部回滚。
            db.Ado.BeginTran();

            // 查询角色记录，用于读取当前权威保存版本。
            RoleTable role = db.Queryable<RoleTable>().Where(v => v.Id == _request.RoleId).First();
            if (role == null)
            {
                throw new InvalidOperationException("角色不存在");
            }

            // 版本分支一：请求版本等于数据库版本，可能是幂等重试，也可能是版本碰撞的内容冲突。
            if (role.ItemSaveVersion == _request.Version)
            {
                // 同版本只有在快照内容完全一致时才是幂等重试；版本冲突必须拒绝覆盖。
                if (!SnapshotMatches(db, _request.ItemDataLists) ||
                    (_request.RoleCountSnapshot && !RoleCountSnapshotMatches(db, _request.RoleCountInfoList)))
                {
                    // 内容不一致：同版本但数据不同，拒绝并返回权威版本，GameServer 不得清脏。
                    result.Message = "角色物品保存版本内容冲突";
                    result.PersistedVersion = role.ItemSaveVersion;
                    db.Ado.RollbackTran();
                    Reply(result);
                    return;
                }

                // 内容一致：视为幂等重试，直接提交空事务返回成功。
                db.Ado.CommitTran();
                result.CmdCode = CmdCode.Succeed;
                result.PersistedVersion = role.ItemSaveVersion;
                Reply(result);
                return;
            }

            // 版本分支二：请求版本低于数据库版本，说明是旧快照，绝不能覆盖新版本。
            if (role.ItemSaveVersion > _request.Version)
            {
                // 旧快照不能覆盖数据库新版本；返回请求版本和权威版本，GameServer 不得按成功清脏。
                db.Ado.CommitTran();
                result.Message = "角色物品保存版本已过期";
                result.PersistedVersion = role.ItemSaveVersion;
                Reply(result);
                return;
            }

            // 版本分支三：请求版本高于数据库版本，执行正常的删旧写新保存。
            SaveItemDataLists(db, _request.ItemDataLists);
            if (_request.RoleCountSnapshot)
            {
                SaveRoleCountData(db, _request.RoleId, _request.Version, _request.RoleCountInfoList);
            }

            // 更新角色表的保存版本为请求版本，作为新的权威版本基线。
            role.ItemSaveVersion = _request.Version;
            role.UpdateDate = DateTime.Now;
            if (db.Updateable(role).ExecuteCommand() <= 0)
            {
                throw new InvalidOperationException("角色物品版本更新失败");
            }
            // 全部写入成功后提交事务。
            db.Ado.CommitTran();
            result.CmdCode = CmdCode.Succeed;
            result.PersistedVersion = role.ItemSaveVersion;
        }
        catch (Exception ex)
        {
            // 异常路径：尝试回滚事务，保证数据库不残留部分写入。
            try
            {
                db.Ado.RollbackTran();
            }
            catch
            {
                // 原异常是本次保存失败的主要原因，回滚异常不覆盖它。
            }

            result.Message = ex.Message;
            LogMsg.Info("角色物品数据库任务失败，角色=" + result.RoleId + "，版本=" +
                result.Version + ": " + ex, LogMsgType.Error);
        }

        // 无论成功或失败，最终都向 GameServer 回包。
        Reply(result);
    }

    /// <summary>
    /// 校验保存请求的参数合法性，包括角色 ID、版本号、背包类型去重、物品字段合法性、
    /// 物品 UID 全局去重，以及装备扩展数据与物品实例的一致性。
    /// 校验不通过时抛出 InvalidOperationException，由 Execute 捕获并回滚。
    /// </summary>
    private void ValidateRequest()
    {
        // 基础字段校验：请求非空、角色 ID 合法、版本号大于 0，且至少包含物品、次数或次数快照标记。
        if (_request == null || _request.RoleId <= 0 || _request.Version <= 0 ||
            (_request.ItemDataLists.Count == 0 && _request.RoleCountInfoList.Count == 0 &&
             !_request.RoleCountSnapshot))
        {
            throw new InvalidOperationException("角色保存请求参数无效，缺少物品或次数快照");
        }

        // bagTypes 用于检测同一背包类型是否在请求中重复出现。
        HashSet<int> bagTypes = new HashSet<int>();
        // itemUids 用于检测同一物品实例 UID 是否在多个背包或多条记录中重复。
        HashSet<long> itemUids = new HashSet<long>();
        foreach (ItemDataList itemDataList in _request.ItemDataLists)
        {
            // 背包类型必须在合法枚举范围内，且同一请求中不能重复出现同一背包类型。
            if (!IsValidBagType(itemDataList.BagType) || !bagTypes.Add(itemDataList.BagType))
            {
                throw new InvalidOperationException("背包类型无效或重复");
            }

            foreach (RoleItemInfo item in itemDataList.Items)
            {
                // 物品字段校验：UID/类型 ID/数量合法、角色 ID 匹配、背包类型与所属列表一致、
                // 格子索引非负、UID 全局唯一。
                if (item == null || item.ItemUid <= 0 || item.ItemTypeId <= 0 || item.Count <= 0 ||
                    item.RoleId != _request.RoleId || item.BagType != itemDataList.BagType ||
                    item.BagIndex < 0 || !itemUids.Add(item.ItemUid))
                {
                    throw new InvalidOperationException("物品快照包含非法或重复数据");
                }
                // 装备扩展数据校验：装备信息和词条信息的 UID、角色 ID 必须与主物品记录一致。
                if ((item.EquipInfo != null &&
                    (item.EquipInfo.ItemUid != item.ItemUid || item.EquipInfo.RoleId != item.RoleId)) ||
                    (item.EquipGeneInfo != null &&
                    (item.EquipGeneInfo.ItemUid != item.ItemUid || item.EquipGeneInfo.RoleId != item.RoleId)))
                {
                    throw new InvalidOperationException("装备扩展数据与物品实例不一致");
                }
            }
        }

    }

    /// <summary>
    /// 将请求中的物品快照以"删旧写新"方式写入数据库。
    /// 删除顺序：先删装备词条、再删装备、最后按背包类型删物品主表；
    /// 插入顺序：先插物品主表、再插装备、最后插装备词条，保证外键依赖顺序。
    /// 所有操作在 Execute 开启的事务内执行。
    /// </summary>
    /// <param name="db">SqlSugar 数据库客户端。</param>
    /// <param name="itemDataLists">请求携带的各背包物品快照列表。</param>
    private void SaveItemDataLists(SqlSugarClient db, IEnumerable<ItemDataList> itemDataLists)
    {
        List<ItemDataList> dataLists = itemDataLists.ToList();
        List<int> bagTypes = dataLists.Select(v => v.BagType).ToList();

        // 查询本次涉及背包类型中已有的旧物品 UID，用于级联删除装备和词条。
        List<long> oldItemUids = db.Queryable<ItemTable>()
            .Where(v => v.RoleID == _request.RoleId && bagTypes.Contains(v.BagType))
            .Select(v => v.ItemUID)
            .ToList();

        // 删旧阶段一：先删除装备词条表（依赖装备 UID）。
        if (oldItemUids.Count > 0)
        {
            db.Deleteable<EquipXLGeneTable>()
                .Where(v => v.RoleID == _request.RoleId && 
                            oldItemUids.Contains(v.ItemUID))
                .ExecuteCommand();
            // 删旧阶段二：删除装备表。
            db.Deleteable<EquipTable>()
                .Where(v => v.RoleID == _request.RoleId && oldItemUids.Contains(v.ItemUID))
                .ExecuteCommand();
        }

        // 删旧阶段三：按背包类型删除物品主表的全部旧记录。
        foreach (int bagType in bagTypes)
        {
            db.Deleteable<ItemTable>()
                .Where(v => v.RoleID == _request.RoleId && v.BagType == bagType)
                .ExecuteCommand();
        }

        // 写新阶段：将协议快照转换为三张表的实体列表，统一批量插入。
        DateTime now = DateTime.Now;
        List<ItemTable> items = new List<ItemTable>();
        List<EquipTable> equips = new List<EquipTable>();
        List<EquipXLGeneTable> genes = new List<EquipXLGeneTable>();
        foreach (RoleItemInfo item in dataLists.SelectMany(v => v.Items))
        {
            DateTime? expireDate = null;
            if (item.ExpireTimeUtcTicks > 0)
            {
                expireDate = FromUtcTicks(item.ExpireTimeUtcTicks, now);
            }
            // 物品主表实体：包含基础属性、位置、绑定、价格和时间字段。
            items.Add(new ItemTable
            {
                ItemUID = item.ItemUid,
                RoleID = item.RoleId,
                ItemTypeID = item.ItemTypeId,
                BagType = item.BagType,
                BagIndex = item.BagIndex,
                count = item.Count,
                ItemSign = item.ItemSign,
                MoneyType = item.MoneyType,
                TotalPrice = item.TotalPrice,
                // 创建时间按 UTC Ticks 转本地时间，非法值回退到当前时间。
                CreateDate = FromUtcTicks(item.CreateTimeUtcTicks, now),
                // 过期时间为 0 时表示永不过期，存 null。
                ExpireDate = expireDate,
                UpdateDate = now
            });

            // 装备扩展表：仅当物品携带装备信息时插入。
            if (item.EquipInfo != null)
            {
                equips.Add(new EquipTable
                {
                    ItemUID = item.ItemUid,
                    RoleID = item.RoleId,
                    EquipType = item.EquipInfo.EquipType,
                    StrengthenLevel = item.EquipInfo.StrengthenLevel
                });
            }

            // 装备词条表：仅当物品携带词条信息时插入，三个词条槽位分别存储 ID 和数值。
            if (item.EquipGeneInfo != null)
            {
                genes.Add(new EquipXLGeneTable
                {
                    ItemUID = item.ItemUid,
                    RoleID = item.RoleId,
                    GeneID0 = item.EquipGeneInfo.GeneId0,
                    GeneID1 = item.EquipGeneInfo.GeneId1,
                    GeneID2 = item.EquipGeneInfo.GeneId2,
                    GeneValue0 = item.EquipGeneInfo.GeneValue0,
                    GeneValue1 = item.EquipGeneInfo.GeneValue1,
                    GeneValue2 = item.EquipGeneInfo.GeneValue2
                });
            }
        }

        // 批量插入三张表，空列表跳过避免无意义 SQL。
        if (items.Count > 0)
        {
            db.Insertable(items).ExecuteCommand();
        }
        if (equips.Count > 0)
        {
            db.Insertable(equips).ExecuteCommand();
        }
        if (genes.Count > 0)
        {
            db.Insertable(genes).ExecuteCommand();
        }
    }

    /// <summary>覆盖保存角色商城、副本次数快照。</summary>
    private void SaveRoleCountData(SqlSugarClient db, int roleId, long version, IEnumerable<RoleCountInfo> counts)
    {
        // 空次数列表是合法的完整快照，删除旧记录后不插入新行即可完成清库。
        List<RoleCountInfo> list = new List<RoleCountInfo>();
        if (counts != null)
        {
            list = counts.ToList();
        }
        db.Deleteable<RoleCountTable>().Where(v => v.RoleId == roleId).ExecuteCommand();
        DateTime now = DateTime.Now;
        List<RoleCountTable> rows = new List<RoleCountTable>();
        foreach (RoleCountInfo value in list)
        {
            if (value == null || value.Action <= 0 || value.CountKey <= 0 || value.Count < 0) continue;
            DateTime refresh = DateTime.MinValue;
            if (value.LastRefreshTime > 0) refresh = new DateTime(value.LastRefreshTime, DateTimeKind.Utc).ToLocalTime();
            rows.Add(new RoleCountTable { RoleId = roleId, Action = value.Action, CountKey = value.CountKey,
                Count = value.Count, LastRefreshTime = refresh, Version = version, CreateDate = now, UpdateDate = now });
        }
        if (rows.Count > 0) db.Insertable(rows).ExecuteCommand();
    }

    /// <summary>比较同版本角色次数快照，空列表代表清空。</summary>
    private bool RoleCountSnapshotMatches(SqlSugarClient db, IEnumerable<RoleCountInfo> counts)
    {
        List<RoleCountTable> stored = db.Queryable<RoleCountTable>().Where(v => v.RoleId == _request.RoleId).ToList();
        List<RoleCountInfo> requested = new List<RoleCountInfo>();
        if (counts != null)
        {
            requested = counts.ToList();
        }
        if (stored.Count != requested.Count) return false;
        foreach (RoleCountInfo value in requested)
        {
            RoleCountTable row = stored.Find(v => v.Action == value.Action && v.CountKey == value.CountKey);
            if (row == null || row.Count != value.Count) return false;
        }
        return true;
    }

    /// <summary>
    /// 比较数据库当前快照与同版本请求，确保重复保存不会因版本碰撞误报成功。
    /// 比对范围：物品主表的全部业务字段、装备表的装备类型和强化等级、
    /// 装备词条表的三个词条 ID 和数值；任一字段不一致即返回 false。
    /// </summary>
    /// <param name="db">SqlSugar 数据库客户端。</param>
    /// <param name="itemDataLists">请求携带的物品快照。</param>
    /// <returns>数据库内容与请求完全一致返回 true；数量或任一字段不一致返回 false。</returns>
    private bool SnapshotMatches(SqlSugarClient db, IEnumerable<ItemDataList> itemDataLists)
    {
        List<ItemDataList> lists = itemDataLists.ToList();
        if (lists.Count == 0)
        {
            return true;
        }
        HashSet<int> bagTypes = new HashSet<int>(lists.Select(v => v.BagType));
        // 查询数据库中本次涉及背包类型的全部物品记录。
        List<ItemTable> storedItems = db.Queryable<ItemTable>()
            .Where(v => v.RoleID == _request.RoleId && bagTypes.Contains(v.BagType))
            .ToList();
        List<RoleItemInfo> requestedItems = lists.SelectMany(v => v.Items).ToList();
        // 数量不一致直接判定不匹配。
        if (storedItems.Count != requestedItems.Count)
        {
            return false;
        }

        // 按 UID 建立请求物品字典，逐条比对物品主表业务字段。
        Dictionary<long, RoleItemInfo> requestedByUid = requestedItems.ToDictionary(v => v.ItemUid);
        foreach (ItemTable stored in storedItems)
        {
            RoleItemInfo requested;
            // UID 不存在或任一字段（类型、背包、格子、数量、绑定、货币、总价）不一致即不匹配。
            if (!requestedByUid.TryGetValue(stored.ItemUID, out requested) ||
                stored.ItemTypeID != requested.ItemTypeId || stored.BagType != requested.BagType ||
                stored.BagIndex != requested.BagIndex || stored.count != requested.Count ||
                stored.ItemSign != requested.ItemSign || stored.MoneyType != requested.MoneyType ||
                stored.TotalPrice != requested.TotalPrice)
            {
                return false;
            }
        }

        // 查询这些物品对应的装备和词条记录，用于扩展字段比对。
        HashSet<long> itemUids = new HashSet<long>(storedItems.Select(v => v.ItemUID));
        List<EquipTable> storedEquips = new List<EquipTable>();
        List<EquipXLGeneTable> storedGenes = new List<EquipXLGeneTable>();
        if (itemUids.Count > 0)
        {
            storedEquips = db.Queryable<EquipTable>()
                .Where(v => v.RoleID == _request.RoleId && itemUids.Contains(v.ItemUID)).ToList();
            storedGenes = db.Queryable<EquipXLGeneTable>()
                .Where(v => v.RoleID == _request.RoleId && itemUids.Contains(v.ItemUID)).ToList();
        }
        Dictionary<long, EquipTable> equipByUid = storedEquips.ToDictionary(v => v.ItemUID);
        Dictionary<long, EquipXLGeneTable> geneByUid = storedGenes.ToDictionary(v => v.ItemUID);
        foreach (RoleItemInfo requested in requestedItems)
        {
            EquipTable storedEquip;
            // 请求有装备信息时，数据库必须存在且装备类型、强化等级一致。
            if (requested.EquipInfo != null)
            {
                if (!equipByUid.TryGetValue(requested.ItemUid, out storedEquip) ||
                    storedEquip.EquipType != requested.EquipInfo.EquipType ||
                    storedEquip.StrengthenLevel != requested.EquipInfo.StrengthenLevel)
                {
                    return false;
                }
            }
            // 请求无装备信息时，数据库中也不应存在该 UID 的装备记录。
            else if (equipByUid.ContainsKey(requested.ItemUid))
            {
                return false;
            }

            EquipXLGeneTable storedGene;
            // 请求有词条信息时，数据库必须存在且三个词条的 ID 和数值全部一致。
            if (requested.EquipGeneInfo != null)
            {
                if (!geneByUid.TryGetValue(requested.ItemUid, out storedGene) ||
                    storedGene.GeneID0 != requested.EquipGeneInfo.GeneId0 ||
                    storedGene.GeneID1 != requested.EquipGeneInfo.GeneId1 ||
                    storedGene.GeneID2 != requested.EquipGeneInfo.GeneId2 ||
                    storedGene.GeneValue0 != requested.EquipGeneInfo.GeneValue0 ||
                    storedGene.GeneValue1 != requested.EquipGeneInfo.GeneValue1 ||
                    storedGene.GeneValue2 != requested.EquipGeneInfo.GeneValue2)
                {
                    return false;
                }
            }
            // 请求无词条信息时，数据库中也不应存在该 UID 的词条记录。
            else if (geneByUid.ContainsKey(requested.ItemUid))
            {
                return false;
            }
        }

        // 最终校验：数据库中装备和词条的记录数必须与请求中携带扩展信息的物品数一致，
        // 防止数据库存在请求未覆盖的孤立扩展记录。
        return equipByUid.Count == requestedItems.Count(v => v.EquipInfo != null) &&
            geneByUid.Count == requestedItems.Count(v => v.EquipGeneInfo != null);
    }

    /// <summary>
    /// 向发起保存请求的 GameServer 回传结果。
    /// </summary>
    /// <param name="result">保存结果回包，包含命令码、角色 ID、版本号和权威持久化版本。</param>
    private void Reply(SaveRoleDataRet result)
    {
        if (_serverBase != null)
        {
            // 通过原始会话链路发送回包，GameServer 收到后按版本号清理对应脏状态。
            _serverBase.SendData(_basePackage, NetDefine.CMD_SaveRoleDataCode, result.ToByteString());
        }
    }

    /// <summary>
    /// 判断背包类型值是否在合法枚举范围内。
    /// </summary>
    /// <param name="value">背包类型整数值。</param>
    /// <returns>在角色可持久化背包类型范围内返回 true，包括虚拟物品背包。</returns>
    private static bool IsValidBagType(int value)
    {
        return value >= (int)KnapsackType.RolePackPlain && value <= (int)KnapsackType.RoleVirtualItemPack;
    }

    /// <summary>
    /// 将 UTC Ticks 转换为本地时间，非法值或 0 时回退到指定默认时间。
    /// </summary>
    /// <param name="ticks">UTC 时间刻度数。</param>
    /// <param name="fallback"> ticks 非法或为 0 时的回退时间。</param>
    /// <returns>转换后的本地时间。</returns>
    private static DateTime FromUtcTicks(long ticks, DateTime fallback)
    {
        // 超出 DateTime 范围的 ticks 视为非法，回退到默认时间。
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            return fallback;
        }

        // ticks 为 0 表示未设置时间，回退到默认时间；否则按 UTC 构造并转本地时间。
        if (ticks == 0)
        {
            return fallback;
        }
        return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
    }
}
