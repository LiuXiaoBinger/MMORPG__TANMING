
using System;
using System.Collections.Generic;
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
    public GetServerListRet GetServerList(GetServerListReq req)
    {
        GetServerListRet ret = new GetServerListRet();
        if (req.ServerId == 0)
        {
            List<GameServerTable> listTable = _db.Queryable<GameServerTable>().ToList();
            if (listTable != null && listTable.Count > 0)
            {
                foreach (GameServerTable table in listTable)
                {
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

        AccoutTable accountTable = _db.Queryable<AccoutTable>().Where(v => v.Id == req.AccountId).First();
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
            DateTime now = DateTime.Now;

            RoleTable role = new RoleTable()
            {
                AccountID = req.AccountId,
                Money = 10000, //默认是0
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
                    roleID = id,
                };
                CreateRoleSkillInfo(id, req.JobId);
                CreateRoleKnapsackInfo(id);
                if (_db.Insertable(roleBaseArrtTable).ExecuteCommand() > 0)
                {
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
        RoleKnapsackTable roleKnapsackTable =
            _db.Queryable<RoleKnapsackTable>().Where(v => v.RoleId == roleid).First();
        if (roleKnapsackTable != null)
        {
            _db.Deleteable(roleKnapsackTable).ExecuteCommand();
            
        }
        roleKnapsackTable = new RoleKnapsackTable()
        {
            RoleId = roleid,
        };

//创建每一个背包格子数据
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            if (i == 99)
            {
                sb.Append($" {0},{0}");

            }
            else
            {
                sb.Append($" {0},{0}|");
            }
        }

        roleKnapsackTable.Knapsack = sb.ToString();
        _db.Insertable(roleKnapsackTable).ExecuteCommand();
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
            foreach (var item in JobSkillMap)
            {
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
                    roleSkillTable.Bindkey = item.Value.Type == 1 ? "Q" :
                        item.Value.Type == 3 ? "F" :
                        item.Value.Type == 4 ? "V" : "";
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
                Money = roleTable.Money,

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
    }

