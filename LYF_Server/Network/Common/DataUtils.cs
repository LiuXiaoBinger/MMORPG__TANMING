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
    /// 仅按字符串规则检查第二位是否为 3 至 9，不查询真实号段或手机号是否存在。
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
    /// 检查邮箱的基本文本形状：非空、最长 254 字符、包含 @ 与域名中的点；不验证地址是否真实可用。
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
    /// 检查账号以英文字母开头，其余字符只能是英文字母、数字或下划线。
    /// </summary>
    /// <param name="username">待检查的账号文本；此函数不限制最大长度。</param>
    /// <returns></returns>
    public static bool IsValidUserName(string username)
    {
        // 正则表达式：以字母开头，后面可以跟任意数量的字母、数字或下划线
        string pattern = @"^[A-Za-z][A-Za-z0-9_]*$";
        return Regex.IsMatch(username, pattern);
    }

    /// <summary>
    /// 对输入文本的 UTF-8 字节计算 MD5，返回 32 个小写十六进制字符。
    /// </summary>
    /// <param name="input">待计算摘要的原始文本。</param>
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

