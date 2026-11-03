using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* Title:
* Descrpiton:
*/
/// <summary>保存一次震屏的延迟、时长和强度配置。</summary>
[Serializable]
public class ShakeScreenInfo
{
    // 从触发到开始震屏的等待秒数。
    public float _delay;
    // 震屏持续秒数。
    public float _duration;
    // 震屏强度参数，具体效果由播放方解释。
    public float _force;
    
}
/// <summary>描述一次攻击特效使用的粒子资源、挂点和变换。</summary>
[Serializable]
public class EffectInfo
{
    // 要实例化的粒子特效预制体。
    public ParticleSystem _fx ;
    // 从角色根节点查找的挂点路径；为空或找不到时，粒子保持无父节点。
    public string  _parentName ;
    
    // 写入粒子的 localPosition；有挂点时是局部位置，无父节点时等同世界位置。
    public Vector3 _position ;
    
    // 特效的欧拉角，单位为度。
    public Vector3 _eulerAngles ;
    
    // 配置中的特效缩放；当前 RoleAnimaBehaviour 实际固定使用 Vector3.one。
    public Vector3 _scale ;
}
/// <summary>攻击配置中的一条表现记录，组合粒子、音频和震屏配置。</summary>
[Serializable]
public class atkConfigEntity
{
    // 本条攻击表现要播放的粒子及其摆放方式。
    public EffectInfo _effectInfo ;
    // 本条攻击表现可用的音频资源。
    public AudioClip [] _audioClips ;
    // 本条攻击表现配置的震屏参数。
    public ShakeScreenInfo  _shakeScreenInfo ;
}
/// <summary>供角色动画事件按下标读取的攻击表现配置资源。</summary>
[CreateAssetMenu(fileName = "AtkConfig", menuName = "GameData/AtkConfig")]
public class RoleAtkConfig : ScriptableObject
{
    /// <summary>攻击表现列表；动画事件传入的 index 对应此列表的下标。</summary>
    public List<atkConfigEntity> atkConfigEntities;
}
