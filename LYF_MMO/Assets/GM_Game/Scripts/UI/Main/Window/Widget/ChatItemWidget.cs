using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class ChatItemWidget : MonoBehaviour
{
   [SerializeField, Header("聊天频道")] private TMP_Text _texChannel;
   [SerializeField, Header("昵称")] private TMP_Text _texNickName;
   [SerializeField, Header("聊天消息")] private TMP_Text _texMsg;
   
   public void RefreshUI(string channel, string nickname, string msg)
   {
      _texChannel.SetText(channel);
      _texNickName.SetText(nickname);
      _texMsg.SetText(msg);
   }
}
