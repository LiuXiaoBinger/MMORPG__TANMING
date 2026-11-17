using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


/// <summary>为过场 UI 的射线检测提供根画布相机；其他图层使用无相机的屏幕空间检测。</summary>
public class MGraphicRaycaster:GraphicRaycaster
{
    /// <summary>仅当前对象属于 CutSceneUI 层时返回 UIRoot 相机，其他层返回空。</summary>
    public override Camera eventCamera
    {
        get
        {
            if(/*MCutSceneMgr.singleton.IsPlaying &&*/ gameObject.layer != MLayer.ID_CutSceneUI)
            {
                return null;
            }
            return UIRoot.Instance.GetUICamera();
        }
    }

    /// <summary>公开基类的遮挡层掩码，供外部调整哪些物理层会挡住 UI 射线。</summary>
    public LayerMask BlockingMask
    {
        get
        {
            return m_BlockingMask;
        }
        set
        {
            m_BlockingMask = value;
        }
    }

}