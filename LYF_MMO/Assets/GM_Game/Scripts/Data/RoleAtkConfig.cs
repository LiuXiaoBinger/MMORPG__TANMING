using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* Title:
* Descrpiton:
*/
[Serializable]
public class ShakeScreenInfo
{
    //延迟时间
    public float _delay;
    //持续时间
    public float _duration;
    //力度
    public float _force;
    
}
[Serializable]
public class EffectInfo
{
    //特效资源
    public ParticleSystem _fx ;
    //特效父组件
    public string  _parentName ;
    
    //特效位置
    public Vector3 _position ;
    
    //特效旋转信息 
    public Vector3 _eulerAngles ;
    
    //缩放
    public Vector3 _scale ;
}
[Serializable]
public class atkConfigEntity
{
    //特效信息
    public EffectInfo _effectInfo ;
    //音频信息
    public AudioClip [] _audioClips ;
    //震动屏幕
    public ShakeScreenInfo  _shakeScreenInfo ;
}
[CreateAssetMenu(fileName = "AtkConfig", menuName = "GameData/AtkConfig")]
public class RoleAtkConfig : ScriptableObject
{
    public List<atkConfigEntity> atkConfigEntities;
}
