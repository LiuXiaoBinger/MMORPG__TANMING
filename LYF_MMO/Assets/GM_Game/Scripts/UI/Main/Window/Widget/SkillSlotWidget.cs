using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/**
* Title:
* Descrpiton:
*/

public class SkillSlotWidget : MonoBehaviour
{
   [SerializeField,Header("技能图标")]private Image _imgIcon;
   [SerializeField,Header("技能绑定按键")]private TMP_Text _texkKey;
   [SerializeField,Header("技能cd mask")]private Image _imgMask;
   [SerializeField,Header("技能cd")]private TMP_Text _texCD;
   
   public  void ReFreshUI(string key)
   {
      //todo 
      _texkKey.SetText(key);
      _imgMask.gameObject.Show(false);
   }
}
