using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title: 单列基类
* Descrpiton:
*/

/// <summary>
/// 线程安全的懒加载泛型单例基类，要求具体类型具备公开无参构造函数。
/// </summary>
/// <typeparam name="T">继承本类的具体单例类型。</typeparam>
public class Singleton<T> where T:new ()
{
   /// <summary>唯一实例，首次访问时按双检锁创建。</summary>
   private static T instance;
   /// <summary>创建实例时的同步锁对象，保证多线程下只初始化一次。</summary>
   private static object locker = new object();
   /// <summary>全局唯一实例，不存在时在锁内双重检查后新建。</summary>
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
