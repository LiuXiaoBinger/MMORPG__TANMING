using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title: 单例基类
* Descrpiton:
*/

/// <summary>
/// 为每一种 T 保存一个共享实例，第一次访问 Instance 时创建；T 必须有公开无参构造函数。
/// </summary>
/// <typeparam name="T">继承本类的具体单例类型。</typeparam>
public class Singleton<T> where T:new ()
{
   /// <summary>该类型的共享实例；尚未访问 Instance 时为 null。</summary>
   private static T instance;
   /// <summary>创建实例时的同步锁对象，保证多线程下只初始化一次。</summary>
   private static object locker = new object();
   /// <summary>返回共享实例；创建前加锁并再次检查，避免等待锁的线程重复创建。</summary>
   public static T Instance
   {
      get
      {
         if(instance == null)
         {
            lock (locker)
            {
               if(instance == null)
                  instance = new T();
            }
         }
         return instance;
      }
   }
}
