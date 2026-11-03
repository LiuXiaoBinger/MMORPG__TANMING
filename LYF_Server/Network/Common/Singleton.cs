using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 每个泛型类型保存一份懒创建实例；创建时在锁内再次检查，避免多个调用同时构造。
/// </summary>
public class Singleton<T> where T : new()
{
    /// <summary>单例实例，首次访问时创建。</summary>
    private static T instance;

    /// <summary>创建实例时的锁对象。</summary>
    private static object instanceLock = new object();

    /// <summary>获取单例实例，不存在时在锁内创建。</summary>
    public static T Instance
    {


        get
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }
}