using UnityEngine;

/// <summary>四个原达纳托斯法阵实际挂载的Rotate逻辑，每秒沿世界Y轴转100度。</summary>
public sealed class MSceneMonsterFxRotate : MonoBehaviour
{
    /// <summary>原出生旋转，逐帧累积时保持X/Z轴不变。</summary>
    private Vector3 angle;

    /// <summary>沿原Start记录预制体原旋转。</summary>
    private void Start()
    {
        angle = transform.eulerAngles;
    }

    /// <summary>保持原脚本的角速度和世界坐标语义。</summary>
    private void Update()
    {
        angle.y += Time.deltaTime * 100f;
        transform.eulerAngles = angle;
    }
}
