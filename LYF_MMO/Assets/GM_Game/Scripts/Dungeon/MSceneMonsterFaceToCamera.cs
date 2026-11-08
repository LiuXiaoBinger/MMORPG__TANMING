using UnityEngine;

/// <summary>原FaceToCamera只替换项目相机入口，保留原mode与lockYUp序列化字段。</summary>
public class MSceneMonsterFaceToCamera : MonoBehaviour
{
    /// <summary>0平行于相机，1面向相机位置，保持原枚举序列化数值。</summary>
    public byte mode;
    /// <summary>原特效是否仅在水平面转向。</summary>
    public bool lockYUp;

    /// <summary>原Renderer绘制前按当前游戏相机更新朝向。</summary>
    private void OnWillRenderObject()
    {
        if (Camera.current == Camera.main) FaceToAction();
    }

    /// <summary>用现有游戏主相机实现原朝向公式。</summary>
    protected void FaceToAction()
    {
        // 本项目相机仍由既有场景相机组件持有。
        Camera camera = Camera.main;
        if (camera == null) return;
        // 原Align使用相机forward，FaceTo使用相机与特效位置之差。
        Vector3 forward = camera.transform.forward;
        if (mode == 1) forward = camera.transform.position - transform.position;
        if (lockYUp) forward.y = 0f;
        if (forward.sqrMagnitude > 0f) transform.forward = forward.normalized;
    }
}
