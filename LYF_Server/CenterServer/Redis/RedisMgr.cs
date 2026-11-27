using StackExchange.Redis;
using Google.Protobuf;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

/// <summary>
/// Redis 管理器（进程级单例）。ConnectionMultiplexer 线程安全并全进程复用，
/// 封装字符串、列表、哈希等常用读写；当前仅 Init 与 GetValue 被注册流程实际使用，
/// 其余方法为预留通用封装。
/// </summary>
public class RedisMgr : Singleton<RedisMgr>
{
    /// <summary>Redis 连接多路复用器，Init 成功后持有，Close 时置空。</summary>
    private ConnectionMultiplexer _connection;

    /// <summary>默认数据库操作对象，由连接复用得到，所有读写经此对象发出。</summary>
    private IDatabase _database;

    /// <summary>Redis 是否已初始化且连接可用。</summary>
    public bool IsInitialized
    {
        get { return _connection != null && _connection.IsConnected; }
    }

    /// <summary>
    /// 初始化 Redis 连接。重复调用直接返回；host 为空或端口非法时抛参数异常。
    /// </summary>
    /// <param name="host">Redis 主机地址，默认 127.0.0.1。</param>
    /// <param name="port">Redis 端口，默认 6380。</param>
    /// <param name="password">连接密码，默认 123456。</param>
    public void Init(
        string host = "127.0.0.1",
        int port = 6380,
        string password = "123456")
    {
        if (_connection != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Redis host cannot be empty.", "host");
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException("port", "Redis port must be between 1 and 65535.");
        }

        // 连接和同步操作最多等待 5000 毫秒，连接暂不可达时不在首次连接阶段直接终止。
        ConfigurationOptions options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            // 区服启动器从现有验证码配置提供共享认证，直接运行服务时沿用原参数。
            Password = Environment.GetEnvironmentVariable("LYF_REDIS_PASSWORD") ?? password
        };
        options.EndPoints.Add(host.Trim(), port);

        _connection = ConnectionMultiplexer.Connect(options);
        _database = _connection.GetDatabase();
    }

    /// <summary>关闭并释放 Redis 连接，置空连接与数据库对象。</summary>
    public void Close()
    {
        if (_connection == null)
        {
            return;
        }

        _connection.Close();
        _connection.Dispose();
        _connection = null;
        _database = null;
    }

    /// <summary>按中心服全局角色主键和已确认存档版本区分交接数据。</summary>
    /// <param name="roleId">中心库生成的持久角色主键。</param>
    /// <param name="version">物品、次数和技能共用的一次持久化版本。</param>
    private static string SceneKey(int roleId, long version)
    {
        return "lyf:scene:player:" + roleId + ":" + version;
    }

    /// <summary>将一次成功查档的两份回包一起写入 Redis，旧版本十分钟后自动过期。</summary>
    /// <param name="skills">中心服返回的完整技能和绑定键位。</param>
    /// <param name="inventory">同次查询返回的物品、货币、装备、次数与版本。</param>
    internal void StoreSceneRole(RoleSkillInfoRet skills, RoleKanpsackInfoRet inventory)
    {
        if (!IsInitialized || skills.CmdCode != CmdCode.Succeed || inventory.CmdCode != CmdCode.Succeed) return;
        if (inventory.RoleKanpsackInfo == null) return;
        // 两类存档一次 SET，任何读者都能用这一条完整数据恢复同一版本。
        byte[] entry = PackSceneData(new SceneRoleSnapshot { Skills = skills, Inventory = inventory });
        try
        {
            _database.StringSet(SceneKey(skills.RoleId, inventory.RoleKanpsackInfo.SaveVersion),
                entry, TimeSpan.FromMinutes(10), flags: CommandFlags.FireAndForget);
        }
        catch (RedisException ex)
        {
            LogMsg.Info("角色交接缓存写入失败，角色=" + skills.RoleId + "，" + ex.Message, LogMsgType.Warn);
        }
    }

    /// <summary>只改使用次数的事务提交后，复用上一版背包和技能，并原子写入本版完整次数。</summary>
    /// <param name="previousVersion">MySQL 事务写入前实际读到的持久版本。</param>
    /// <param name="request">已成功提交的角色保存请求，次数列表可为空，表示全部清除。</param>
    internal void StoreConfirmedSceneCounts(long previousVersion, SaveRoleDataReq request)
    {
        if (!IsInitialized || !request.RoleCountSnapshot) return;
        if (request.ItemDataLists.Count != 0 || request.SkillLevels.Count != 0) return;
        // 入场扣次数走独立增量；旧缓存缺失则继续由目标 GS 按正常路径回源。
        const string promote = "local base = redis.call('GET', KEYS[1]); " +
            "if not base or redis.call('EXISTS', KEYS[2]) == 1 then return 0 end; " +
            "redis.call('SET', KEYS[2], base, 'EX', 600); " +
            "redis.call('SET', KEYS[3], ARGV[1], 'EX', 600); return 1";
        // 本版主键与次数键必须同时发布，避免读取一半更新的进本次数。
        string key = SceneKey(request.RoleId, request.Version);
        try
        {
            _database.ScriptEvaluate(promote,
                new RedisKey[] { SceneKey(request.RoleId, previousVersion), key, key + ":counts" },
                new RedisValue[] { PackSceneData(request) }, CommandFlags.FireAndForget);
        }
        catch (RedisException ex)
        {
            LogMsg.Info("角色次数缓存写入失败，角色=" + request.RoleId + "，" + ex.Message, LogMsgType.Warn);
        }
    }

    /// <summary>异步读取指定存档版本，缓存不可用或不完整时由 GS 向中心服取档。</summary>
    /// <param name="roleId">网关已确认的持久角色编号。</param>
    /// <param name="version">原 GS 等待落库后发出的准确版本，包含初始版本零。</param>
    internal async Task<SceneRoleSnapshot> ReadSceneRoleAsync(int roleId, long version)
    {
        if (!IsInitialized) return null;
        // 一次 MGET 取得完整快照和该版本可能单独更新的全量次数。
        string key = SceneKey(roleId, version);
        RedisValue[] values;
        try
        {
            values = await _database.StringGetAsync(new RedisKey[] { key, key + ":counts" }).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            LogMsg.Info("角色交接缓存读取失败，角色=" + roleId + "，" + ex.Message, LogMsgType.Warn);
            return null;
        }
        // 完整快照必须属于当前角色，两类模块都曾由中心服成功查得。
        ByteString data = UnpackSceneData(values[0]);
        if (data == null) return null;
        SceneRoleSnapshot snapshot = SceneRoleSnapshot.Parser.ParseFrom(data);
        if (snapshot.Skills?.RoleId != roleId || snapshot.Inventory?.RoleKanpsackInfo == null) return null;
        if (snapshot.Skills.CmdCode != CmdCode.Succeed || snapshot.Inventory.CmdCode != CmdCode.Succeed) return null;
        // 完整快照已经是所需版本时，可直接交给角色组件。
        RoleKanpsackInfo inventory = snapshot.Inventory.RoleKanpsackInfo;
        if (inventory.SaveVersion == version) return snapshot;
        // 只有已提交的同角色同版本完整次数，才允许复用旧版中保持不变的技能和物品。
        ByteString countData = UnpackSceneData(values[1]);
        if (countData == null || inventory.SaveVersion > version) return null;
        SaveRoleDataReq counts = SaveRoleDataReq.Parser.ParseFrom(countData);
        if (counts.RoleId != roleId || counts.Version != version || !counts.RoleCountSnapshot) return null;
        inventory.RoleCountInfoList.Clear();
        inventory.RoleCountInfoList.Add(counts.RoleCountInfoList);
        inventory.SaveVersion = version;
        return snapshot;
    }

    /// <summary>将角色存档或完整次数封装为 SHA-256 摘要和原始协议字节。</summary>
    /// <param name="message">中心服已经确认的 Protobuf 消息，接口仅用于序列化边界。</param>
    private static byte[] PackSceneData(IMessage message)
    {
        // 摘要与后跟的原文来自同一次序列化。
        byte[] payload = message.ToByteArray();
        using (SHA256 hash = SHA256.Create())
        {
            // 摘要占固定 32 字节，供读取时识别截断或意外改写。
            byte[] digest = hash.ComputeHash(payload);
            // Redis 中保存一个完整的二进制值。
            byte[] entry = new byte[digest.Length + payload.Length];
            Buffer.BlockCopy(digest, 0, entry, 0, digest.Length);
            Buffer.BlockCopy(payload, 0, entry, digest.Length, payload.Length);
            return entry;
        }
    }

    /// <summary>只交付通过完整性核验的协议字节，无值或意外损坏时返回空。</summary>
    /// <param name="value">Redis 返回的完整二进制值。</param>
    private static ByteString UnpackSceneData(RedisValue value)
    {
        // SHA-256 原始摘要长度是 32 字节。
        const int digestLength = 32;
        // 数据库空值转换为空数组引用。
        byte[] entry = (byte[])value;
        if (entry == null || entry.Length <= digestLength) return null;
        using (SHA256 hash = SHA256.Create())
        {
            // 校验实际载荷后，调用方直接解析协议。
            byte[] digest = hash.ComputeHash(entry, digestLength, entry.Length - digestLength);
            for (int index = 0; index < digestLength; index++)
                if (digest[index] != entry[index]) return null;
        }
        return ByteString.CopyFrom(entry, digestLength, entry.Length - digestLength);
    }

    #region 查询与判定
    /// <summary>同步读取字符串键，存在时通过 out 参数返回值，键不存在或 RedisException 返回 false；未初始化和非法键仍抛异常。</summary>
    /// <param name="key">Redis 键，非空。</param>
    /// <param name="value">输出读取到的字符串值，失败时为 null。</param>
    public bool Get(string key, out string value)
    {
        value = null;
        try
        {
            RedisValue redisValue = GetDatabase().StringGet(ValidateKey(key));
            if (!redisValue.HasValue)
            {
                return false;
            }

            value = redisValue.ToString();
            return true;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis GET failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>读取字符串键并直接返回值，键不存在或 RedisException 时返回 null；未初始化和非法键仍抛异常。</summary>
    public string GetValue(string key)
    {
        string value;
        return Get(key, out value) ? value : null;
    }

    /// <summary>异步读取字符串键，键不存在或 RedisException 时返回 null；未初始化和非法键仍抛异常。</summary>
    public async Task<string> GetValueAsync(string key)
    {
        try
        {
            RedisValue value = await GetDatabase().StringGetAsync(ValidateKey(key));
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis GET failed: " + ex.Message, LogMsgType.Error);
            return null;
        }
    }

    /// <summary>取当前数据库操作对象，未初始化时抛 InvalidOperationException。</summary>
    private IDatabase GetDatabase()
    {
        if (_database == null)
        {
            throw new InvalidOperationException("Call RedisMgr.Instance.Init() before using Redis.");
        }

        return _database;
    }
    #endregion

    #region 核心逻辑
    /// <summary>写入字符串键，可选过期时间；失败返回 false。</summary>
    public bool Set(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            return GetDatabase().StringSet(ValidateKey(key), value, expiry);
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis SET failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>列表左端压入，返回是否成功推入。</summary>
    public bool LPush(string key, string value)
    {
        try
        {
            return GetDatabase().ListLeftPush(ValidateKey(key), value) > 0;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis LPUSH failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>列表左端弹出，存在元素时经 out 返回值。</summary>
    public bool LPop(string key, out string value)
    {
        value = null;
        try
        {
            RedisValue redisValue = GetDatabase().ListLeftPop(ValidateKey(key));
            if (!redisValue.HasValue)
            {
                return false;
            }

            value = redisValue.ToString();
            return true;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis LPOP failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>列表右端压入，返回是否成功推入。</summary>
    public bool RPush(string key, string value)
    {
        try
        {
            return GetDatabase().ListRightPush(ValidateKey(key), value) > 0;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis RPUSH failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>列表右端弹出，存在元素时经 out 返回值。</summary>
    public bool RPop(string key, out string value)
    {
        value = null;
        try
        {
            RedisValue redisValue = GetDatabase().ListRightPop(ValidateKey(key));
            if (!redisValue.HasValue)
            {
                return false;
            }

            value = redisValue.ToString();
            return true;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis RPOP failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>哈希写入字段；更新已存在字段仍视为成功。</summary>
    public bool HSet(string key, string field, string value)
    {
        try
        {
            // Redis 的 HashSet 在更新已有字段时返回 false，本封装仍把正常更新视为成功。
            GetDatabase().HashSet(ValidateKey(key), ValidateKey(field), value);
            return true;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis HSET failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>哈希读取字段，存在时经 out 返回值。</summary>
    public bool HGet(string key, string field, out string value)
    {
        value = null;
        try
        {
            RedisValue redisValue = GetDatabase().HashGet(ValidateKey(key), ValidateKey(field));
            if (!redisValue.HasValue)
            {
                return false;
            }

            value = redisValue.ToString();
            return true;
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis HGET failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>删除哈希字段，返回删除是否生效。</summary>
    public bool HDel(string key, string field)
    {
        try
        {
            return GetDatabase().HashDelete(ValidateKey(key), ValidateKey(field));
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis HDEL failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>删除键，返回删除是否生效。</summary>
    public bool Del(string key)
    {
        try
        {
            return GetDatabase().KeyDelete(ValidateKey(key));
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis DEL failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>判断键是否存在。</summary>
    public bool ExistsKey(string key)
    {
        try
        {
            return GetDatabase().KeyExists(ValidateKey(key));
        }
        catch (RedisException ex)
        {
            LogMsg.Info("Redis EXISTS failed: " + ex.Message, LogMsgType.Error);
            return false;
        }
    }

    /// <summary>校验键非空，空键抛 ArgumentException。</summary>
    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Redis key cannot be empty.", "key");
        }

        return key;
    }
    #endregion
}
