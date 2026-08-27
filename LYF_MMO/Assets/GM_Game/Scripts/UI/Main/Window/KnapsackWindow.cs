using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using YooAsset;

/**
* Title:
* Descrpiton:
*/

public class KnapsackWindow : WindowBase,IDragHandler
{
    [SerializeField, Header("背包Slot父组件")] private Transform _content;

    [SerializeField, Header("金币")] private TMP_Text _texGold;
    [SerializeField, Header("灵石")] private TMP_Text _texLs;
    
    private RectTransform _rectTransform;
    
    //模拟物品图片
    private string[] _strings = { "Item_2001","Item_2002","item_2003",
        "item_2005","Item_2201","Item_2202","Item_2301","Item_2401",
        "Item_2501",
        "Item_2601",
        "Item_2701",
        "Item_2801",
    };
    /// <summary>
    /// 刷新UI
    /// </summary>
    /// <param name="obj"></param>
    public  override void ReFreshUI(object obj)
    {
        
    }

    public void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    private void Start()
    {
        AddItemSlot();
    }

    private void AddItemSlot()
    {
        //test 
        Global.Instance.YooPackage.LoadAssetAsync($"{ConstDefine.PrefabPath}UIPrefabs/KnapsackSlotWidget").Completed +=
            (AssetOperationHandle handle) =>
            {
                for (int i = 0; i < 100; i++)
                {
                    GameObject obj = handle.InstantiateSync();
                    if (obj != null)
                    {
                        // 将技能槽放入技能栏父节点下。
                        obj.SetParent(_content);
                    }
                    KnapsackSlotWidget slot = obj.GetComponent<KnapsackSlotWidget>();
                    if (slot != null)
                    {
                        if (i < 10)
                        {
                            slot.RefreshUI(i+1,_strings[i]);
                        }
                        else
                        {
                            slot.RefreshUI(0,"");
                        }
                        
                    }
                }
            };
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (_rectTransform == null || _rectTransform.parent is not RectTransform parentRect)
        {
            return;
        }

        // 将鼠标屏幕坐标转换为窗口父节点下的局部 UI 坐标。
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }
}
