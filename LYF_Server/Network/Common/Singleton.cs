using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 泛型懒加载单例基类，使用双检锁保证线程安全。
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