using UnityEngine;

/**
 * Title:GameObject��չ
 * Description:
 */


/// <summary>
/// GameObject 与 Transform 的常用扩展方法集合。
/// </summary>
public static class GameObjectUtils {


    /// <summary>设置游戏对象显隐的扩展方法。</summary>
    /// <param name="go">目标游戏对象，为空时直接忽略。</param>
    /// <param name="isActive">true 激活，false 隐藏。</param>
    public static void Show(this GameObject go, bool isActive = true) {
        if (go == null) return;
        go.SetActive(isActive);

    }
    /// <summary>设置 Transform 所属游戏对象显隐的扩展方法。</summary>
    /// <param name="trans">目标 Transform，为空时直接忽略。</param>
    /// <param name="isActive">true 激活，false 隐藏。</param>
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
