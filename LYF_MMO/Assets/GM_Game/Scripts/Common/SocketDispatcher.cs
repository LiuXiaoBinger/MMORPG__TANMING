using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
/**
* Title: 将收到服务器的消息派发到其他模块 采用观察者模式
* Descrpiton:
*/
public delegate void OnActionHandler(ByteString data);

public class SocketDispatcher : Singleton<SocketDispatcher>
{
   private Dictionary<int,OnActionHandler> _actionDis=new Dictionary<int, OnActionHandler>();

   /// <summary>
   /// 注册事件
   /// </summary>
   /// <param name="protocode"></param>
   /// <param name="handler"></param>
   public void AddEventHandler(int protocode ,OnActionHandler handler)
   {
      if (!_actionDis.ContainsKey(protocode)&&handler!=null)
      {
         _actionDis.Add(protocode, handler);
      }
   }
   /// <summary>
   /// 删除事件
   /// </summary>
   /// <param name="protocode"></param>
   public void RemoveEventHandler(int protocode)
   {
      _actionDis.Remove(protocode);
   }
   /// <summary>
   /// 派发事件
   /// </summary>
   /// <param name="protocode"></param>
   /// <param name="data"></param>
   public void DispatchEvent(int protocode, ByteString data)
   {
      if (_actionDis.ContainsKey(protocode))
      {
         _actionDis[protocode]?.Invoke(data);
      }
   }
}
