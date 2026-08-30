using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


public class MGraphicRaycaster:GraphicRaycaster
{
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