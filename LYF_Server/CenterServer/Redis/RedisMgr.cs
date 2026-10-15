using StackExchange.Redis;
using System;
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

        ConfigurationOptions options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            Password = password
        };
        options.EndPoints.Add(host.Trim(), port);

        _connection = ConnectionMultiplexer.Connect(options);
        _database = _connection.GetDatabase();
    }

    /// <summary>同步读取字符串键，存在时通过 out 参数返回值，键不存在或异常返回 false。</summary>
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

    /// <summary>读取字符串键并直接返回值，键不存在或异常时返回 null。</summary>
    public string GetValue(string key)
    {
        string value;
        return Get(key, out value) ? value : null;
    }

    /// <summary>异步读取字符串键，键不存在或异常时返回 null。</summary>
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
            // HashSet returns false when an existing field is updated, which is still success.
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

    /// <summary>取当前数据库操作对象，未初始化时抛 InvalidOperationException。</summary>
    private IDatabase GetDatabase()
    {
        if (_database == null)
        {
            throw new InvalidOperationException("Call RedisMgr.Instance.Init() before using Redis.");
        }

        return _database;
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
}
