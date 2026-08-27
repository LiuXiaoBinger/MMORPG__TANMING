using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title: 单列基类
* Descrpiton:
*/

public class Singleton<T> where T:new ()
{
   private static T instance;
   private static object locker = new object();
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
