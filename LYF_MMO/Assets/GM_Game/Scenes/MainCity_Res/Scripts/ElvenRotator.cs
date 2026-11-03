using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 Inspector 配置持续旋转物体，旋转角度每帧累加到物体自身的本地坐标轴上。
/// </summary>
public class ElvenRotator : MonoBehaviour
{
    // 三个本地轴各自的基础角速度，单位为度/游戏秒；这些分量会直接参与计算，不会归一化为单位方向。
    [SerializeField] private Vector3 _rotationAxis;
    // 统一乘到三个轴上的无单位倍数；0 表示不转，负值让各轴反向旋转。
    [SerializeField] private float _speedMultiplier;


    /// <summary>
    /// 将每秒角速度换算为本帧旋转角度，并按 Transform.Rotate 的默认本地坐标方式累加。
    /// </summary>
    void Update()
    {
        // deltaTime 是受 timeScale 影响的本帧秒数，使转速按游戏时间计算，不随每秒帧数直接变化。
        transform.Rotate(_rotationAxis * _speedMultiplier * Time.deltaTime);
    }
}
