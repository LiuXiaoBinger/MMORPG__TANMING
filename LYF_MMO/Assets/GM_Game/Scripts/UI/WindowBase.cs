using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class WindowBase : MonoBehaviour
{
   public virtual void InitWindow()
   {
      
   }
   public virtual void Show(bool  isShow = true,object obj = null)
   {
      gameObject.Show(isShow);
      if (obj != null)
      {
         ReFreshUI(obj);
      }
   }

   public virtual void ReFreshUI(object obj)
   {
       
   }

   public virtual void CloseWindow()
   {
      gameObject.Show(false);
   }
}
