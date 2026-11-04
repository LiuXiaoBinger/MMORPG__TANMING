using System.Collections.Generic;
using Google.Protobuf;
using System.Numerics;

/// <summary>GS 的组队场景执行器。队伍关系由 MS 持有，这里仅存活跃副本和本机已显示队伍的角色。</summary>
public sealed class TeamManager
{
    /// <summary>当前游戏进程的组队场景入口。</summary>
    public static readonly TeamManager Instance = new TeamManager();

    /// <summary>仅保存已实际创建的副本，键为队伍服在本分区分配的实例号。</summary>
    private readonly Dictionary<ulong, Dungeon> _instances = new Dictionary<ulong, Dungeon>();
    /// <summary>已向本机玩家投递非空队伍 UI 的角色，用于下线或队伍服断线时清除该次展示。</summary>
    private readonly HashSet<ulong> _visibleTeamRoles = new HashSet<ulong>();
    /// <summary>参加逻辑帧更新的在场副本，最后一人离场时移除。</summary>
    private readonly List<Dungeon> dungeonTeams = new List<Dungeon>();

    /// <summary>队伍人数上限，对应 RO TeamData.lua:63 的 maxTeamNumber = 5。</summary>
    public const int MaxMember = 5;

    /// <summary>魔神塔副本配置ID，对应 theme=7 映射的 DungeonsTable 4060。</summary>
    public const int MoshentaDungeonId = 4060;

    /// <summary>魔神塔场景ID，服务端下发入场通知时使用。</summary>
    public const int MoshentaSceneId = 4060;

    /// <summary>魔神塔地图ID，写入成员 BaseInfo.MapId，供附近过滤使用。</summary>
    public const int MoshentaMapId = 4060;

    /// <summary>
    /// 魔神塔出生坐标，沿用 RoleBaseInfo.pos 的 X_Y_Z 下划线格式。
    /// 取值来源：已验场景 Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity
    /// 中 RoleSpawnPoint 的世界坐标（其父节点位于原点），与客户端 MainCityMgr._localRoleSpawnPoint 一致，
    /// 不能再用 0_0_0，否则服务端通知位置与客户端实际出生点不同，进入后互见与移动同步会错位。
    /// </summary>
    public const string MoshentaSpawnPos = "132.97_58.89_60.95";

    /// <summary>主城地图ID，与进入副本前的公共场景一致，0 表示公共主城。</summary>
    public const int MainCityMapId = 0;

    /// <summary>主城场景通知ID，对应客户端 SceneType.Scene_MainCity 的枚举序号 2。</summary>
    public const int MainCitySceneId = 2;

    /// <summary>
    /// 主城出生坐标，沿用 RoleBaseInfo.pos 的 X_Y_Z 下划线格式。
    /// 取值来源：客户端 MainCityMgr.ResolveMainRoleSpawnPosition 未配置出生点时的原有硬编码坐标，
    /// 主城离队回城必须回到该点，不能用 0_0_0 顶替。
    /// </summary>
    public const string MainCitySpawnPos = "62.1493416_19.4139996_80.6689758";

    /// <summary>单个副本的场景对象和实际入场角色，独立于 MS 的组队名单。</summary>
    private sealed class Dungeon
    {
        /// <summary>与角色 BaseInfo 对应的本次副本编号。</summary>
        public ulong InstanceId;
        /// <summary>当前在场的角色，用于场景消息投递与最后一人离场回收。</summary>
        public readonly List<ulong> Members = new List<ulong>();
        /// <summary>本实例独立的刷怪和生命值状态。</summary>
        public readonly SceneWaveSpawner WaveSpawner = new SceneWaveSpawner();
        /// <summary>复用本实例的逐帧怪物动作通知。</summary>
        public readonly MonsterAttrUpdateNtf MonsterFrame = new MonsterAttrUpdateNtf();
    }

    /// <summary>由游戏进程统一创建副本运行入口。</summary>
    private TeamManager() { }

    /// <summary>由已提交的普攻在逻辑线程调用，只向角色所在实例的成员广播本窗口实际命中的怪物。</summary>
    /// <param name="caster">从网关会话确认且正在施法的在线角色。</param>
    /// <param name="forward">释放时确认的水平单位朝向。</param>
    /// <param name="damage">当前普攻配置的每次伤害。</param>
    /// <param name="hit_targets">当前碰撞窗口内已命中的服务端实体编号。</param>
    public void OnMonsterHit(OnlineRole caster, Vector2 forward, int damage, HashSet<ulong> hit_targets)
    {
        // 以施法者当前所在的副本查找本机运行状态。
        Dungeon team;
        if (!_instances.TryGetValue((ulong)caster.mainRoleInfo.BaseInfo.InstanceId, out team)) return;
        // 同一逻辑线程先扣血、再构造一份全队共享的可靠下行载荷。
        MonsterAttrUpdateNtf notice = new MonsterAttrUpdateNtf { InstanceId = team.InstanceId };
        team.WaveSpawner.ChangeHP(notice, caster.mainRoleInfo.BaseInfo.Pos.ToVector3(), forward, damage, hit_targets, caster);
        if (notice.Monsters.Count == 0) return;
        SendMonsterNotice(team, notice);
    }

    /// <summary>向本副本当前在线队员可靠广播同一份怪物结果。</summary>
    /// <param name="team">拥有本次事件的队伍。</param>
    /// <param name="notice">在当前逻辑线程已结算的动作和生命变化。</param>
    private static void SendMonsterNotice(Dungeon team, MonsterAttrUpdateNtf notice)
    {
        // 同实例所有观察者复用相同的一份序列化结果。
        ByteString payload = notice.ToByteString();
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 队员的位置仍属于本实例才接收，不使用附近距离裁剪血量。
            OnlineRole member = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (member != null && (ulong)member.mainRoleInfo.BaseInfo.InstanceId == team.InstanceId)
                member.SendToClient(NetDefine.CMD_MonsterAttrUpdateCode, payload);
        }
    }

    /// <summary>只驱动仍持有实例的队伍，发出本帧产生的移动、施法与玩家扣血。</summary>
    /// <param name="deltaMilliseconds">本固定逻辑帧的毫秒数。</param>
    public void UpdateMonsters(int deltaMilliseconds)
    {
        // 副本列表的下标；队员进出与帧更新都由 GameGlobal 串行执行。
        for (int index = 0; index < dungeonTeams.Count; index++)
        {
            // 同一副本的兽人及该实例本次复用的事件集合。
            Dungeon team = dungeonTeams[index];
            // 当前帧本实例专属的广播载荷，跳过没有动作和伤害的帧。
            MonsterAttrUpdateNtf frame = team.MonsterFrame;
            frame.InstanceId = team.InstanceId;
            frame.MonsterActions.Clear();
            frame.PlayerHits.Clear();
            team.WaveSpawner.Update(deltaMilliseconds, frame);
            if (frame.MonsterActions.Count > 0 || frame.PlayerHits.Count > 0) SendMonsterNotice(team, frame);
        }
    }

    /// <summary>把 MS 的权威名单补上本机的实时角色展示数据，再沿原网关协议广播。</summary>
    /// <param name="notice">MS 已确定的队伍号、队长、成员和实例号。</param>
    public void PublishRoster(TeamInfoNtf notice)
    {
        // 先填所有成员的展示数据，再编码一次供整队共用。
        for (int index = 0; index < notice.Members.Count; index++)
        {
            // MS 已确定身份的展示行。
            TeamMemberInfo member = notice.Members[index];
            // 只有 GS 持有当前血量、等级及场景。
            OnlineRole role = RoleManager.Instance.GetOnlineRole((int)member.RoleId);
            if (role == null || role.State != OnlineRoleState.Online) continue;
            // 当前角色的服务端属性。
            RoleBaseInfo info = role.mainRoleInfo.BaseInfo;
            member.Nickname = info.Nickname;
            member.Level = info.Level;
            member.JobId = info.JobId;
            member.SceneId = info.MapId;
            member.CurrHp = info.CurrHp;
            member.MaxHp = info.MaxHp;
        }
        // 所有成员接收相同的队伍通知。
        ByteString payload = notice.ToByteString();
        // 网关会话由本 GS 的在线角色索引定位。
        for (int index = 0; index < notice.Members.Count; index++)
        {
            // 接收最新名单的本机角色。
            OnlineRole role = RoleManager.Instance.GetOnlineRole((int)notice.Members[index].RoleId);
            if (role == null || role.State != OnlineRoleState.Online) continue;
            _visibleTeamRoles.Add(role.GetID());
            role.SendToClient(NetDefine.CMD_TeamInfoCode, payload);
        }
    }

    /// <summary>处理 MS 批准的主动离队，保持城内位置，或在副本内回主城。</summary>
    /// <param name="roleId">获准离开的玩家。</param>
    public void LeaveDungeon(ulong roleId)
    {
        _visibleTeamRoles.Remove(roleId);
        ResetRoleInstance(roleId, true);
    }

    /// <summary>在线角色离线时立即清理本机场景，同时通知 MS 移除组队关系。</summary>
    /// <param name="roleId">从游戏会话下线的角色号。</param>
    public void OnRoleOffline(ulong roleId)
    {
        if (_visibleTeamRoles.Remove(roleId))
        {
            ResetRoleInstance(roleId, false);
        }
        MasterTeamLink.Instance.Offline(roleId);
    }

    /// <summary>原 MS 连接消失时清除该连接对应的队伍 UI 和入场实例。</summary>
    public void OnMasterLost()
    {
        // 离队会修改集合，快照仅记录要通知的角色号。
        List<ulong> roles = new List<ulong>(_visibleTeamRoles);
        // 对实际显示过队伍的角色执行与主动离队相同的场景清理。
        for (int index = 0; index < roles.Count; index++) LeaveDungeon(roles[index]);
    }

    /// <summary>
    /// 向本人下发空队伍状态，客户端据此清空队伍界面并判定已离队。
    /// 主城退队、副本退队共用同一份下发内容，保证两条分支的队伍界面表现一致。
    /// </summary>
    /// <param name="role">接收空队伍状态的在线角色。</param>
    private static void SendEmptyTeamInfo(OnlineRole role)
    {
        if (role == null)
        {
            return;
        }
        // 空队伍状态：TeamId 与 CaptainId 都为 0，客户端据此清空队伍界面并判定已离队。
        TeamInfoNtf emptyNotice = new TeamInfoNtf();
        emptyNotice.TeamId = 0UL;
        emptyNotice.CaptainId = 0UL;
        emptyNotice.MaxMember = MaxMember;
        role.SendToClient(NetDefine.CMD_TeamInfoCode, emptyNotice.ToByteString());
    }

    /// <summary>按 MS 批准的名单创建或复用场景，再按原顺序广播离场、入场及副本内容。</summary>
    /// <param name="order">已获队长权限检查的队伍、实例、目标副本和成员。</param>
    public void EnterDungeon(MasterTeamDungeonOrder order)
    {
        // 保存本机当前在线的获准成员，已下线的角色不占用实例。
        List<ulong> entering = new List<ulong>();
        // 保留 MS 的入场顺序。
        for (int index = 0; index < order.MemberIds.Count; index++)
        {
            // GS 持有的真实在线角色。
            OnlineRole role = RoleManager.Instance.GetOnlineRole((int)order.MemberIds[index]);
            if (role != null && role.State == OnlineRoleState.Online) entering.Add(order.MemberIds[index]);
        }
        if (entering.Count == 0) return;
        // 重复进本时复用怪物、掉血与实例 ID。
        Dungeon team;
        if (!_instances.TryGetValue(order.InstanceId, out team))
        {
            team = new Dungeon { InstanceId = order.InstanceId };
            _instances.Add(order.InstanceId, team);
            dungeonTeams.Add(team);
        }
        team.Members.Clear();
        team.Members.AddRange(entering);
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员在"改写位置之前"的在线角色对象，为空说明该成员已掉线，跳过。
            OnlineRole leavingRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (leavingRole == null)
            {
                continue;
            }
            RoleManager.Instance.BroadcastRoleLeave(leavingRole);
        }

        // 先写服务器权威位置，再通知客户端加载，保证进入后过滤条件已生效。
        // index 是队伍成员表的零基下标，范围 [0, team.Members.Count)，本循环负责写权威位置。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的权威角色ID，先取出再查在线对象，避免下标表达式重复求值。
            ulong memberId = team.Members[index];
            // 当前成员的在线角色对象，用于写入 MapId/InstanceId/出生点；为空说明已掉线，跳过。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)memberId);
            if (memberRole == null || memberRole.mainRoleInfo == null ||
                memberRole.mainRoleInfo.BaseInfo == null)
            {
                continue;
            }
            memberRole.mainRoleInfo.BaseInfo.MapId = MoshentaMapId;
            memberRole.mainRoleInfo.BaseInfo.InstanceId = (int)team.InstanceId;
            memberRole.mainRoleInfo.BaseInfo.Pos = MoshentaSpawnPos;
        }

        // 新实例内互相可见：先把本人补发给同实例其他成员，再把同实例其他成员补发给本人。
        // 第一轮：index 为成员表零基下标，逐个把"本人"广播给同实例其他成员。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象；为空说明已掉线，无需广播。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            RoleManager.Instance.BroadcastRoleEnterWorldByRole(memberRole);
        }
        // 第二轮：index 同上，逐个把"同实例其他成员"补发给本人，使双向可见。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象，作为接收方；为空说明已掉线，跳过补发。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            RoleManager.Instance.ResendNearbyRoles(memberRole);
        }

        // 入场确认通知：全队共用同一份内容，先构造一次再序列化，避免每个成员重复构造。
        TeamEnterDungeonNtf notice = new TeamEnterDungeonNtf();
        notice.TeamId = order.TeamId;
        notice.DungeonId = order.DungeonId;
        notice.SceneId = MoshentaSceneId;
        notice.InstanceId = team.InstanceId;
        notice.MapId = MoshentaMapId;
        notice.Pos = MoshentaSpawnPos;
        team.WaveSpawner.FillEnterScene(notice);
        // 已序列化的入场通知字节，全队复用同一份，保证各成员收到的场景与实例完全一致。
        ByteString payload = notice.ToByteString();
        // index 为成员表零基下标，逐个下发同一份 payload。
        for (int index = 0; index < team.Members.Count; index++)
        {
            // 当前成员的在线角色对象，作为接收方；为空说明已掉线，跳过下发。
            OnlineRole memberRole = RoleManager.Instance.GetOnlineRole((int)team.Members[index]);
            if (memberRole == null)
            {
                continue;
            }
            memberRole.SendToClient(NetDefine.CMD_TeamEnterDungeonCode, payload);
        }
        // 队伍状态变化后同步成员实例信息。

    }

    /// <summary>
    /// 离队后的场景与实例处理，按"角色当前是否真的身处副本实例"以及"本次是否掉线清理"分流。
    /// 普通主城主动离队只清队伍关系；副本内退队才广播离开并回城；掉线一律广播离开并复位。
    /// </summary>
    /// <param name="roleId">需要处理的角色ID。</param>
    /// <param name="notifySelf">是否向本人下发回城与空队伍状态；掉线时为 false，避免向失效会话发包。</param>
    private void ResetRoleInstance(ulong roleId, bool notifySelf)
    {
        // 待处理的在线角色对象，为空说明角色已下线或数据不完整，直接放弃处理。
        OnlineRole role = RoleManager.Instance.GetOnlineRole((int)roleId);
        if (role == null || role.mainRoleInfo == null || role.mainRoleInfo.BaseInfo == null)
        {
            return;
        }
        // 掉线清理走 notifySelf=false，必须无条件广播离开，否则旧场景观察者会留下幽灵角色。
        bool offlineCleanup = !notifySelf;
        // 角色当前是否真的身处副本实例：0 表示本来就在主城公共场景，离队不该改变他的位置。
        bool inDungeonInstance = role.mainRoleInfo.BaseInfo.InstanceId != 0;
        if (!offlineCleanup && !inDungeonInstance)
        {
            // 普通场景主动离队：只清队伍关系并下发空队伍状态。
            // 不广播离开、不改写 MapId/InstanceId/Pos、不回城，
            // 否则同场景的其他玩家会收到离开通知删掉该角色，而该角色并未重载场景、不会再广播出现，
            // 表现为"退队后队友再也看不到我"。
            SendEmptyTeamInfo(role);
            return;
        }
        // 副本内退队或掉线：改写字段前广播，此时附近过滤读到的仍是角色离开前的旧地图与旧实例。
        // 从本副本的实际在场名单移除，最后一人离开时释放所有怪物。
        Dungeon instance;
        if (_instances.TryGetValue((ulong)role.mainRoleInfo.BaseInfo.InstanceId, out instance))
        {
            instance.Members.Remove(roleId);
            if (instance.Members.Count == 0)
            {
                instance.WaveSpawner.Clear();
                _instances.Remove(instance.InstanceId);
                dungeonTeams.Remove(instance);
            }
        }
        RoleManager.Instance.BroadcastRoleLeave(role);

        // 离队后回到公共主城：实例归零、地图回到主城、出生点回到主城真实坐标，避免残留在副本视野。
        role.mainRoleInfo.BaseInfo.InstanceId = 0;
        role.mainRoleInfo.BaseInfo.MapId = MainCityMapId;
        role.mainRoleInfo.BaseInfo.Pos = MainCitySpawnPos;

        // 主动离队且会话仍在线时，下发回城通知让客户端切回主城；掉线不发。
        if (!notifySelf)
        {
            return;
        }
        // 回城通知：复用入场通知结构，SceneId 填主城、InstanceId 归零，客户端据此切回主城场景。
        TeamEnterDungeonNtf backNotice = new TeamEnterDungeonNtf();
        backNotice.TeamId = 0UL;
        backNotice.DungeonId = 0;
        backNotice.SceneId = MainCitySceneId;
        backNotice.InstanceId = 0UL;
        backNotice.MapId = MainCityMapId;
        backNotice.Pos = MainCitySpawnPos;
        role.SendToClient(NetDefine.CMD_TeamEnterDungeonCode, backNotice.ToByteString());
        // 回城后按新的主城位置向主城观察者补发出现，否则只有本人收到附近列表、其他人仍看不到该角色。
        RoleManager.Instance.BroadcastRoleEnterWorldByRole(role);
        // 回到主城后重新补发主城附近角色，客户端据此重建远端列表并保留本人。
        RoleManager.Instance.ResendNearbyRoles(role);

        SendEmptyTeamInfo(role);
    }
}
