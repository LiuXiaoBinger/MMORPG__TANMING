using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


/// <summary>
/// 服务端通用数据工具：登录时间缓存、账号/手机号/邮箱格式校验、MD5。
/// </summary>
public class DataUtils : Singleton<DataUtils>
{
    /// <summary>登录时间缓存：键为账号用户名，值为最近一次登录的 UTC 毫秒时间戳。</summary>
    public Dictionary<string, long> _loginTimeDic = new Dictionary<string, long>();

    /// <summary>
    /// 添加登录毫秒值
    /// </summary>
    /// <param name="username">账号用户名。</param>
    /// <param name="milliseconds">登录时刻的 UTC 毫秒时间戳。</param>
    public void AddLoginMilliseconds(string username, long milliseconds)
    {
        /*if (!_loginTimeDic.ContainsKey(username))
        {
            _loginTimeDic.Add(username, milliseconds);
        }*/
        _loginTimeDic[username] = milliseconds;
    }

    /// <summary>查询指定账号最近一次登录的 UTC 毫秒时间戳；未记录时返回 0。</summary>
    public long GetLoginMilliseconds(string username)
    {
        if (_loginTimeDic.ContainsKey(username))
        {
            return _loginTimeDic[username];
        }
        return 0;
    }

    /// <summary>
    /// 验证中国大陆手机号（11位，以1开头）
    /// 支持最新号段：13/14/15/16/17/18/19
    /// </summary>
    public static bool IsValidMobile(string phone)
    {
        // 正则表达式解析：
        // ^1          以1开头
        // [3-9]       第二位为3-9
        // \d{9}$      后续9位数字
        const string pattern = @"^1[3-9]\d{9}$";
        return Regex.IsMatch(phone, pattern);
    }

    /// <summary>
    /// 验证邮箱地址是否合法。
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        email = email.Trim();
        if (email.Length > 254)
        {
            return false;
        }

        const string pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 验证账号是否合法
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public static bool IsValidUserName(string username)
    {
        // 正则表达式：以字母开头，后面可以跟任意数量的字母、数字或下划线
        string pattern = @"^[A-Za-z][A-Za-z0-9_]*$";
        return Regex.IsMatch(username, pattern);
    }

    /// <summary>
    /// MD5
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string GetMD5Hash(string input)
    {
        // 使用MD5创建哈希对象
        using (MD5 md5Hash = MD5.Create())
        {
            // 将输入字符串转换为字节数组并计算哈希
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // 创建一个新的Stringbuilder来收集字节并创建字符串
            StringBuilder sBuilder = new StringBuilder();

            // 循环字节数组并格式化每个字节为两个十六进制数字
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }

}

