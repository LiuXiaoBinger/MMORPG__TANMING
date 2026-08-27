using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;


/**
* Title:
* Descrpiton:
*/

public class ChatWindow : WindowBase
{
    [SerializeField,Header(("消息展示框"))] private Transform _content;
    [SerializeField,Header(("发送消息频道选择"))] private TMP_Dropdown _dropdown;
    [SerializeField,Header(("发送消息的内容"))] private TMP_InputField _iptChat;

    private void Start()
    {
        _iptChat.onSelect.AddListener(arg0 =>
        {
            PlayerInputCtr.Instance.OnDisable();
        });
        _iptChat.onDeselect.AddListener(arg0 =>
        {
            PlayerInputCtr.Instance.OnEnable(); 
        });
    }
    
    public void OnSendBrnClicked()
    {
        //1.校验输入框是否为空
        if (string.IsNullOrEmpty(_iptChat.text))
        {
            TipsMgr.Instance.ShowSystemTips("请输入聊天内容.. ");
            return;
        }
        
        //2.获取聊天频道
        TMP_Dropdown.OptionData optionData = _dropdown.options[_dropdown.value];
        if (optionData == null)
        {
            return;
        }
        
        //发送数据到服务器 todo
        
        Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/ChatItemWidget").Completed +=
            (AssetOperationHandle handle) =>
            {
                GameObject obj = handle.InstantiateSync();
                if (obj != null)
                {
                    // 将技能槽放入技能栏父节点下。
                    obj.SetParent(_content);
                }
                ChatItemWidget slot = obj.GetComponent<ChatItemWidget>();
                if (slot != null)
                {
                    slot.RefreshUI(optionData.text,"昵称:小米",_iptChat.text);
                }

                _iptChat.text = "";
            };
    }
    
}
