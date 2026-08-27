using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class MinMapHelp : MonoBehaviour
{
   public static MinMapHelp instance;

   private void Awake()
   {
      instance = this;
   }
}
