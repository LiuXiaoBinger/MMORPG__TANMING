using StackExchange.Redis;
using System;
using System.Threading.Tasks;

/// <summary>
/// Redis manager. ConnectionMultiplexer is thread-safe and reused process-wide.
/// </summary>
public class RedisMgr : Singleton<RedisMgr>
{
    private ConnectionMultiplexer _connection;
    private IDatabase _database;

    public bool IsInitialized
    {
        get { return _connection != null && _connection.IsConnected; }
    }

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

    public string GetValue(string key)
    {
        string value;
        return Get(key, out value) ? value : null;
    }

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

    private IDatabase GetDatabase()
    {
        if (_database == null)
        {
            throw new InvalidOperationException("Call RedisMgr.Instance.Init() before using Redis.");
        }

        return _database;
    }

    private static string ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Redis key cannot be empty.", "key");
        }

        return key;
    }
}
