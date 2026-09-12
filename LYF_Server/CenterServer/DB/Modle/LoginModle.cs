
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlSugar;

/// <summary>
/// 处理登录模块相关数据库业务
/// </summary>
public class LoginModle
{
    private SqlSugarClient _db = null;

    public LoginModle(SqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 登录请求处理
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    internal LoginRet Login(LoginReq req)
    {
        LoginRet ret = new LoginRet();

        AccoutTable table = _db.Queryable<AccoutTable>().Where(v => v.UserName == req.UserName).First();
        if (table == null)
        {
            ret.CmdCode = CmdCode.AcctNotExist; //账号不存在
        }
        else
        {
            if (table.Passwrod == req.Password)
            {
                if (table.State != 1)
                {
                    //账号禁用的
                    ret.CmdCode = CmdCode.AcctDisable;

                }
                else
                {
                    //todo 判断账号是否已经登录
                    //登录成功
                    GameServerTable gameServer = _db.Queryable<GameServerTable>()
                        .Where(v => v.Id == table.LastLoginServerId).First();
                    if (gameServer != null)
                    {
                        ret.GameServer = new GameServer()
                        {
                            ServerId = gameServer.Id,
                            ServerName = gameServer.ServerName,
                            RunState = gameServer.RunState,
                            IsNew = gameServer.IsNew,
                            IpHost = gameServer.IpHost,
                            Prot = gameServer.Port
                        };
                    }

                    ret.CmdCode = CmdCode.Succeed;
                    ret.AccountId = table.Id;

                }
            }
            else
            {
                ret.CmdCode = CmdCode.PasswordError;
            }

        }

        return ret;
    }

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="req"></param>
    internal RegistRet RegistAccont(RegistReq req)
    {
        RegistRet ret = new RegistRet();
        ret.CmdCode = CmdCode.Succeed;
        //1.判断是否已经注册
        List<AccoutTable> list = _db.Queryable<AccoutTable>().Where((v) => v.UserName == req.UserName).ToList();
        if (list.Count > 0)
        {
            ret.CmdCode = CmdCode.AcctExist;
        }
        else
        {
            string Varify = RedisMgr.Instance.GetValue("code_" + req.Email);
            //判断验证码
            if (string.IsNullOrEmpty(Varify) && req.Varify.Equals(Varify))
            {
                AccoutTable accout = new AccoutTable()
                {
                    UserName = req.UserName,
                    Email = req.Email,
                    Passwrod = req.Password,
                    LastLoginServerId = 1,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now

                };
                int id = _db.Insertable(accout).ExecuteCommand(); //返回的id小于等于0插入失败
                if (id <= 0)
                {
                    ret.CmdCode = CmdCode.ServerError;
                }
            }
            else
            {
                ret.CmdCode = CmdCode.VarifyError;
            }

        }

        return ret;
    }

    /// <summary>
    /// 获取服务器列表
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    internal GetServerListRet GetServerList(GetServerListReq req)
    {
        GetServerListRet ret = new GetServerListRet();
        if (req.ServerId == 0)
        {
            List<GameServerTable> listTable = _db.Queryable<GameServerTable>().ToList();
            if (listTable != null && listTable.Count > 0)
            {
                List<GameServerTable> tableSnapshot = new List<GameServerTable>(listTable);
                for (int index = 0; index < tableSnapshot.Count; index++)
                {
                    GameServerTable table = tableSnapshot[index];
                    GameServer gameServer = new GameServer()
                    {
                        ServerId = table.Id,
                        ServerName = table.ServerName,
                        RunState = table.RunState,
                        IsNew = table.IsNew,
                        IpHost = table.IpHost,
                        Prot = table.Port
                    };
                    ret.GameServers.Add(gameServer);
                }
            }
        }
        else
        {
            ret.CmdCode = CmdCode.ServerError;
        }

        return ret;
    }

    /// <summary>
    /// 请求登录游戏服务器处理
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    internal LoginGameServerRet LoginGameServer(LoginGameServerReq req)
    {
        LoginGameServerRet ret = new LoginGameServerRet();

        AccoutTable accountTable = _db.Queryable<AccoutTable>().
            Where(v => v.Id == req.AccountId).First();
        if (accountTable != null)
        {

            GameServerTable gameServerTable =
                _db.Queryable<GameServerTable>().Where(v => v.Id == req.GameServerId).First();
            if (gameServerTable != null)
            {

                accountTable.LastLoginServerId = req.GameServerId;
                if (_db.Updateable(accountTable).ExecuteCommand() > 0)
                {
                    //查询角色表， 当前用户是否已经创建了角色， 如果没有创建，那么返回默认数据
                    RoleTable roleTable = _db.Queryable<RoleTable>().Where(v => v.AccountID == req.AccountId).First();
                    if (roleTable != null)
                    {
                        ret.CreateRoleInfo = new CreateRoleRet();
                        ret.CreateRoleInfo.RoleId = roleTable.Id;
                        ret.CreateRoleInfo.Nickname = roleTable.Nickname;
                        ret.CreateRoleInfo.JobId = roleTable.JobID;
                        ret.CreateRoleInfo.Level = roleTable.Level;
                    }
                }
                else
                {
                    ret.CmdCode = CmdCode.ServerError; //服务端发送错误
                }
            }
            else
            {
                ret.CmdCode = CmdCode.ReqParamError; //请求参数错误
            }
        }
        else
        {
            ret.CmdCode = CmdCode.AcctNotExist; //账户不存在
        }

        return ret;
    }

    /// <summary>
    /// 创建角色请求处理
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public CreateRoleRet CreateRole(CreateRoleReq req)
    {
        CreateRoleRet ret = new CreateRoleRet();
        RoleTable roleTable = _db.Queryable<RoleTable>()
            .Where(v => v.Nickname == req.Nickname && v.ServerId == req.GameServerId).First();
        if (roleTable != null)
        {
            //已经存在该昵称
            ret.CmdCode = CmdCode.AcctNotExist;
        }
        else
        {
            var professionInfo = LubanMgr.Instance.GetProfessionInfoById(req.JobId);
            if (professionInfo == null)
            {
                ret.CmdCode = CmdCode.ServerError;
                return ret;
            }

            DateTime now = DateTime.Now;

            RoleTable role = new RoleTable()
            {
                AccountID = req.AccountId,
                Nickname = req.Nickname,
                JobID = req.JobId,
                Level = 1, //默认1
                Exp = 0,
                SkillUpPoint = 6, //用于测试
                Pos = "", //角色默认位置
                CameraOffset = "",
                MapId = 1,
                ServerId = req.GameServerId,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
            };
            int id = _db.Insertable(role).ExecuteReturnIdentity();
            if (id > 0)
            {
                RoleBaseArrtTable roleBaseArrtTable = new RoleBaseArrtTable()
                {
                    Id = id,
                    roleID = id,
                    XiuWei = professionInfo.Qi,
                    MaxHP = professionInfo.MaxHp,
                    CurrHP = professionInfo.MaxHp,
                    MaxMP = professionInfo.MaxMp,
                    CurrMP = professionInfo.MaxMp,
                    AtkExternalMin = professionInfo.AtkExternalMin,
                    AtkExternalMax = professionInfo.AtkExternalMax,
                    AtkInternalMin = professionInfo.AtkInternalMin,
                    AtkInternalMax = professionInfo.AtkInternalMax,
                    DefExternal = professionInfo.DefExternal,
                    DefExternalDelta = professionInfo.DefExternalDelta,
                    DefInternal = professionInfo.DefInternal,
                    DefInternalDelta = professionInfo.DefInternalDelta,
                    HitPoint = professionInfo.HitPoint,
                    BlockPoint = professionInfo.BlockPoint,
                    ZhenJi = professionInfo.ZhenJi,
                    WuGu = professionInfo.WuGu,
                    CritPoint = professionInfo.CritPoint,
                    CritResistPoint = professionInfo.CritResistPoint,
                    CritDamage = professionInfo.CritDamage,
                    CritDamageReduce = professionInfo.CritDamageReduce,
                    HealStrength = professionInfo.HealStrength,
                    HealBonus = professionInfo.HealBonus,
                    ZhuXin = professionInfo.ZhuXin,
                    JianRen = professionInfo.JianRen,
                };
                if (_db.Insertable(roleBaseArrtTable).ExecuteCommand() > 0)
                {
                    CreateRoleSkillInfo(id, req.JobId);
                    CreateRoleKnapsackInfo(id);
                    CreateRoleVirtualItemInfo(id);
                    ret.RoleId = id;
                    ret.Nickname = role.Nickname;
                    ret.JobId = role.JobID;
                    ret.Level = 1;
                }
            }
            else
            {
                ret.CmdCode = CmdCode.ServerError;
            }
        }

        return ret;
    }

    /// <summary>
    /// 为角色创建背包数据
    /// </summary>
    /// <param name="roleid"></param>
    private void CreateRoleKnapsackInfo(int roleid)
    {
        // 建角时为四类可持久化背包各创建一条 81 格记录；穿戴栏不参与扩容。
        List<RoleKnapsackTable> oldRows =
            _db.Queryable<RoleKnapsackTable>().Where(v => v.RoleId == roleid).ToList();
        if (oldRows != null && oldRows.Count > 0)
        {
            _db.Deleteable(oldRows).ExecuteCommand();
        }

        DateTime now = DateTime.Now;
        List<RoleKnapsackTable> rows = new List<RoleKnapsackTable>();
        KnapsackType[] persistedTypes = new[]
        {
            KnapsackType.RolePackPlain,
            KnapsackType.RolePackEquip,
            KnapsackType.RolePackConsume,
            KnapsackType.RolePackMaterial
        };
        for (int index = 0; index < persistedTypes.Length; index++)
        {
            KnapsackType type = persistedTypes[index];
            rows.Add(new RoleKnapsackTable
            {
                RoleId = roleid,
                roleKnapsack = (byte)type,
                roleKnapsackcount = 81,
                CreateDate = now,
                UpdateDate = now
            });
        }

        _db.Insertable(rows).ExecuteCommand();
    }

    /// <summary>创建角色初始铜币物品，货币从建角开始就使用虚拟物品背包持久化。</summary>
    /// <param name="roleId">新建角色 ID。</param>
    private void CreateRoleVirtualItemInfo(int roleId)
    {
        DateTime now = DateTime.Now;
        // 初始铜币的实例 UID 使用角色 ID 和物品配置 ID 组成，保证建角重试不会生成不同实例。
        long itemUid = ((long)roleId << 32) | (uint)ItemEnum.ZENY;
        ItemTable item = new ItemTable
        {
            ItemUID = itemUid,
            RoleID = roleId,
            ItemTypeID = (int)ItemEnum.ZENY,
            BagType = (int)KnapsackType.RoleVirtualItemPack,
            BagIndex = 0,
            count = 10000,
            ItemSign = (int)ItemSign.IsNoSign,
            MoneyType = 0,
            TotalPrice = 0L,
            CreateDate = now,
            UpdateDate = now
        };
        _db.Insertable(item).ExecuteCommand();
    }

    /// <summary>
    /// 为角色创建技能信息
    /// </summary>
    /// <param name="roleid"></param>
    private void CreateRoleSkillInfo(int roleid, int jobid)
        {
            //删除表数据
            List<RoleSkillTable> listRoleSkillTables =
                _db.Queryable<RoleSkillTable>().Where(v => v.RoleID == roleid).ToList();
            if (listRoleSkillTables != null && listRoleSkillTables.Count > 0)
            {
                _db.Deleteable(listRoleSkillTables).ExecuteCommand();
                listRoleSkillTables.Clear();
            }
            else
            {
                listRoleSkillTables = new List<RoleSkillTable>();
            }

            var JobSkillMap = LubanMgr.Instance.GetSkillInfosByJob(jobid);
            List<KeyValuePair<int, cfg.SkillInfo>> skillSnapshot =
                new List<KeyValuePair<int, cfg.SkillInfo>>(JobSkillMap);
            for (int index = 0; index < skillSnapshot.Count; index++)
            {
                KeyValuePair<int, cfg.SkillInfo> item = skillSnapshot[index];
                RoleSkillTable roleSkillTable = new RoleSkillTable()
                {
                    RoleID = roleid,
                    SkillID = item.Value.Id,
                    SkillLevel = 0,
                    Bindkey = "",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,

                };
                //普通攻击 滑步 轻工
                if (item.Value.Type == 1 || item.Value.Type == 3 || item.Value.Type == 4)
                {
                    roleSkillTable.SkillLevel = 1;
                    if (item.Value.Type == 1)
                    {
                        roleSkillTable.Bindkey = "Q";
                    }
                    else if (item.Value.Type == 3)
                    {
                        roleSkillTable.Bindkey = "F";
                    }
                    else
                    {
                        roleSkillTable.Bindkey = "V";
                    }
                }

                listRoleSkillTables.Add(roleSkillTable);
            }

            _db.Insertable(listRoleSkillTables).ExecuteCommand();
        }

    /// <summary>
    /// 开始请求游戏开始返回角色信息
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public StartGameRet StartGame(StartGameReq req)
    {
        StartGameRet ret = new StartGameRet();
        if (req == null || req.RoleId <= 0)
        {
            ret.CmdCode = CmdCode.ReqParamError;
            return ret;
        }

        RoleTable roleTable = _db.Queryable<RoleTable>()
            .Where(v => v.Id == req.RoleId)
            .First();
        if (roleTable == null)
        {
            ret.CmdCode = CmdCode.AcctNotExist;
            return ret;
        }

        RoleBaseArrtTable attrTable = _db.Queryable<RoleBaseArrtTable>()
            .Where(v => v.roleID == roleTable.Id)
            .First();
        if (attrTable == null)
        {
            ret.CmdCode = CmdCode.ServerError;
            return ret;
        }

        ret.CmdCode = CmdCode.Succeed;
        ret.MainRoleInfo = new MainRoleInfo
        {
            AccountId = roleTable.AccountID,

            Exp = roleTable.Exp,
            SkillUpPoint = roleTable.SkillUpPoint,
            CameraOffset = roleTable.CameraOffset ?? string.Empty,
            ServerId = roleTable.ServerId,
            BaseInfo = new RoleBaseInfo
            {
                RoleId = roleTable.Id,
                Nickname = roleTable.Nickname ?? string.Empty,
                Pos = roleTable.Pos ?? string.Empty,
                MapId = roleTable.MapId,
                XiuWei = attrTable.XiuWei,
                MaxHp = attrTable.MaxHP,
                CurrHp = attrTable.CurrHP,
                MaxMp = attrTable.MaxMP,
                CurrMp = attrTable.CurrMP,
                AtkExternalMin = attrTable.AtkExternalMin,
                AtkExternalMax = attrTable.AtkExternalMax,
                AtkInternalMin = attrTable.AtkInternalMin,
                AtkInternalMax = attrTable.AtkInternalMax,
                DefExternal = attrTable.DefExternal,
                DefExternalDelta = attrTable.DefExternalDelta,
                DefInternal = attrTable.DefInternal,
                DefInternalDelta = attrTable.DefInternalDelta,
                HitPoint = attrTable.HitPoint,
                BlockPoint = attrTable.BlockPoint,
                ZhenJi = attrTable.ZhenJi,
                WuGu = attrTable.WuGu,
                CritPoint = attrTable.CritPoint,
                CritResistPoint = attrTable.CritResistPoint,
                CritDamage = attrTable.CritDamage,
                CritDamageReduce = attrTable.CritDamageReduce,
                HealStrength = attrTable.HealStrength,
                HealBonus = attrTable.HealBonus,
                ZhuXin = attrTable.ZhuXin,
                JianRen = attrTable.JianRen,
                JobId = roleTable.JobID,
                Level = roleTable.Level,
            }
        };

        return ret;
    }

    private static KnapsackType[] GetPersistedKnapsackTypes()
    {
        return new[]
        {
            KnapsackType.RolePackPlain,
            KnapsackType.RolePackEquip,
            KnapsackType.RolePackConsume,
            KnapsackType.RolePackMaterial
        };
    }

    /// <summary>旧角色只有普通背包记录时，以其容量作为所有分类的迁移下限。</summary>
    private static int GetLegacyFallback(List<RoleKnapsackTable> rows)
    {
        RoleKnapsackTable oldAll = null;
        if (rows != null)
        {
            oldAll = rows.FirstOrDefault(v => v.roleKnapsack == (byte)KnapsackType.RolePackPlain);
        }
        if (oldAll == null)
        {
            return 81;
        }
        return Math.Max(81, (int)oldAll.roleKnapsackcount);
    }

    /// <summary>分类记录存在时保留更大容量；不存在时继承旧普通背包容量。</summary>
    private static int GetMigratedGridCount(RoleKnapsackTable row, int legacyFallback)
    {
        if (row == null)
        {
            return legacyFallback;
        }
        return Math.Max(legacyFallback, Math.Max(81, (int)row.roleKnapsackcount));
    }

    /// <summary>首次读取旧角色时补齐缺失分类记录，后续扩容可直接按类型更新。</summary>
    private void EnsureMissingKnapsackRows(int roleId, List<RoleKnapsackTable> rows, int legacyFallback)
    {
        List<RoleKnapsackTable> missingRows = new List<RoleKnapsackTable>();
        DateTime now = DateTime.Now;
        KnapsackType[] persistedTypes = GetPersistedKnapsackTypes();
        for (int typeIndex = 0; typeIndex < persistedTypes.Length; typeIndex++)
        {
            KnapsackType type = persistedTypes[typeIndex];
            if (rows != null && rows.Any(v => v.roleKnapsack == (byte)type))
            {
                continue;
            }

            RoleKnapsackTable row = new RoleKnapsackTable
            {
                RoleId = roleId,
                roleKnapsack = (byte)type,
                roleKnapsackcount = (byte)legacyFallback,
                CreateDate = now,
                UpdateDate = now
            };
            missingRows.Add(row);
            rows.Add(row);
        }

        if (missingRows.Count > 0)
        {
            _db.Insertable(missingRows).ExecuteCommand();
        }
    }
}

