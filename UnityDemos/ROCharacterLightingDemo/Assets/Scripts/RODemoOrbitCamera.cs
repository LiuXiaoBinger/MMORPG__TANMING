using UnityEngine;

/// <summary>
/// 演示用轨道相机：围绕角色旋转/缩放，只在 Play 模式响应鼠标，编辑态只按当前参数摆放。
/// 目的仅是让人能绕着角色观察光照，不参与任何光照计算。
/// </summary>
[ExecuteAlways]
[AddComponentMenu("RO/演示轨道相机")]
public class RODemoOrbitCamera : MonoBehaviour
{
    /// <summary>注视目标（角色根节点），为空时组件不改变相机变换。</summary>
    public Transform target;

    /// <summary>相机与目标的距离，单位：米，取值受 minDistance/maxDistance 限制。</summary>
    public float distance = 2.4f;

    /// <summary>水平角（度），绕目标 Y 轴旋转。</summary>
    public float yaw = 0f;

    /// <summary>俯仰角（度），正值表示相机在目标上方俯视。</summary>
    public float pitch = 5f;

    /// <summary>允许的最小距离（米），防止穿进模型。</summary>
    public float minDistance = 1.2f;

    /// <summary>允许的最大距离（米），防止把角色缩到看不见。</summary>
    public float maxDistance = 6f;

    /// <summary>鼠标拖动的旋转速度（度/帧轴值），仅 Play 模式使用。</summary>
    public float rotateSpeed = 4f;

    /// <summary>滚轮缩放系数（米/轴值），仅 Play 模式使用。</summary>
    public float zoomSpeed = 0.6f;

    /// <summary>启用时按当前参数摆一次位置，保证编辑态打开场景就能看到角色。</summary>
    private void OnEnable()
    {
        // 编辑态与运行时进入启用都摆一次，避免相机停在原点
        UpdateTransform();
    }

    /// <summary>Inspector 改参数后立刻重新摆位，方便对照不同角度。</summary>
    private void OnValidate()
    {
        // 组件未激活时不改动相机，避免影响其它对象
        if (!isActiveAndEnabled)
        {
            return;
        }
        UpdateTransform();
    }

    /// <summary>仅在 Play 模式下处理鼠标拖动旋转与滚轮缩放。</summary>
    private void Update()
    {
        // 编辑态不处理输入，防止选中对象时误改相机
        if (Application.isPlaying == false)
        {
            return;
        }

        // 按住鼠标左键拖动改变水平角与俯仰角
        if (Input.GetMouseButton(0))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            pitch = Mathf.Clamp(pitch, -60f, 80f);
        }

        // 滚轮改变观察距离，并夹在允许区间内
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        UpdateTransform();
    }

    /// <summary>按 yaw/pitch/distance 计算相机世界位置与朝向，始终朝向目标。</summary>
    public void UpdateTransform()
    {
        // 没有目标时不做任何事
        if (target == null)
        {
            return;
        }

        // 先算朝向，再沿该朝向反推位置，保证相机正对目标
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        transform.position = target.position + offset;
        transform.rotation = rotation;
    }
}
