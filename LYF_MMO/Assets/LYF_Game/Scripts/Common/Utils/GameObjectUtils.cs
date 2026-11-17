using UnityEngine;

/**
 * Title: GameObject 扩展方法
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
    /// 将对象放到指定父节点下，设置局部位置、局部角度，并把局部缩放恢复为 1。
    /// </summary>
    /// <param name="obj">要移动到父节点下的对象。</param>
    /// <param name="parent">新的父节点；对象或父节点为空时不处理。</param>
    /// <param name="pos">相对父节点的位置，默认位于父节点原点。</param>
    /// <param name="angle">相对父节点的欧拉角，单位为度。</param>
    public static void SetParent(this GameObject obj, Transform parent, Vector3 pos = default, Vector3 angle = default)
    {
        if(obj == null||parent==null) return;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = angle;
        obj.transform.localScale = Vector3.one;
    }
    /// <summary>
    /// 只改变水平朝向，让对象面向目标，不向上或向下倾斜。
    /// </summary>
    /// <param name="trans">需要转向的对象节点。</param>
    /// <param name="lookTarget">面向的目标节点；任一节点为空时不处理。</param>
    public static void LookAtTarget(this Transform trans, Transform lookTarget)
    {
        if (lookTarget == null||trans==null) return;
       
        // 从当前对象指向目标的方向；下面去掉高度差，仅保留水平分量。
        Vector3 dir = (lookTarget.position - trans.position).normalized;
        dir.y = 0;
        trans.rotation = Quaternion.LookRotation(dir);
            
    }
}
