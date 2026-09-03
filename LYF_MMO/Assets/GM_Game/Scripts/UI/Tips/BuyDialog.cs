using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/**
* Title:
* Descrpiton:
*/

public class BuyDialog : MonoBehaviour
{
   private Action<BuyDialog> _onClosed;

   /// <summary>
   /// 由弹窗管理器登记关闭回调，用于在弹窗销毁时解除重复打开限制。
   /// </summary>
   public void Initialize(Action<BuyDialog> onClosed)
   {
      _onClosed = onClosed;
   }

   public void CloseDialog()
    {
       Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 无论是关闭按钮还是其他流程销毁弹窗，都必须通知管理器释放锁定。
        Action<BuyDialog> onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke(this);
    }

    public void OnConfirmButtonClick()
    {
        //确定数量 
        
        //判断角色钱币是否充足
        
        //服务端验证 
    }
}
