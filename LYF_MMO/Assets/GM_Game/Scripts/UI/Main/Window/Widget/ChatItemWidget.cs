using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

/// <summary>一条聊天消息的纯展示控件，分别显示频道、发言人和正文。</summary>
public class ChatItemWidget : MonoBehaviour
{
   /// <summary>消息所属频道的文字。</summary>
   [SerializeField, Header("聊天频道")] private TMP_Text _texChannel;
   /// <summary>发言人的昵称文字。</summary>
   [SerializeField, Header("昵称")] private TMP_Text _texNickName;
   /// <summary>消息正文，不在这里解析频道或发送网络请求。</summary>
   [SerializeField, Header("聊天消息")] private TMP_Text _texMsg;
   
   /// <summary>按 channel 频道、nickname 昵称和 msg 正文覆盖当前消息显示。</summary>
   public void RefreshUI(string channel, string nickname, string msg)
   {
      _texChannel.SetText(channel);
      _texNickName.SetText(nickname);
      _texMsg.SetText(msg);
   }
}
