using UnityEngine;

/**
 * Title:GameObject��չ
 * Description:
 */


public static class GameObjectUtils {


    public static void Show(this GameObject go, bool isActive = true) {
        if (go == null) return;
        go.SetActive(isActive);

    }
    public static void Show(this Transform trans, bool isActive = true) {
        if (trans == null) return;
        trans.gameObject.SetActive(isActive);

    }
    /// <summary>
    /// GameObject 设置父组件
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="parent"></param>
    /// <param name="pos"></param>
    /// <param name="angle"></param>
    public static void SetParent(this GameObject obj, Transform parent, Vector3 pos = default, Vector3 angle = default)
    {
        if(obj == null||parent==null) return;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = angle;
        obj.transform.localScale = Vector3.one;
    }
    /// <summary>
    /// 水平朝向目标
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="lookTarget"></param>
    public static void LookAtTarget(this Transform trans, Transform lookTarget)
    {
        if (lookTarget == null||trans==null) return;
       
        Vector3 dir = (lookTarget.position - trans.position).normalized;
        dir.y = 0;
        trans.rotation = Quaternion.LookRotation(dir);
            
    }
}
