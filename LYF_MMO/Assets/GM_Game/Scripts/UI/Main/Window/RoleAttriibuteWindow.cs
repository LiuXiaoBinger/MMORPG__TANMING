using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using YooAsset;

/**
* Title:
* Descrpiton:
*/

public class RoleAttriibuteWindow : WindowBase ,IDragHandler
{
    [SerializeField, Header("穿戴装备父组件")] private Transform _content;

    private RectTransform _rectTransform;
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        AddItemSlot();
    }
    
    private void AddItemSlot()
    {
        //test 
        Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/EquipSlotWidget").Completed +=
            (AssetOperationHandle handle) =>
            {
                for (int i = 0; i < 12; i++)
                {
                    GameObject obj = handle.InstantiateSync();
                    if (obj != null)
                    {
                        // 将技能槽放入技能栏父节点下。
                        obj.SetParent(_content);
                    }
                    EquipSlotWidget slot = obj.GetComponent<EquipSlotWidget>();
                    
                    
                }
            };
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rectTransform.position += new Vector3(eventData.delta.x, eventData.delta.y, 0);
    }
}
