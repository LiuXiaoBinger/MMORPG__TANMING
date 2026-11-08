using System;
using UnityEngine;

/// <summary>原结果点的弹道和实际命中表现，不能用于客户端扣血。</summary>
[Serializable]
public sealed class MSceneMonsterResultData
{
    /// <summary>ResultList原始零基序号。</summary>
    public int Index;
    /// <summary>FireSkill后的原结果秒数。</summary>
    public float At;
    /// <summary>普通命中的原效果；无效果时为空。</summary>
    public MSceneMonsterFxData HitFx;
    /// <summary>暴击命中的原效果；无专用效果时沿普通结果。</summary>
    public MSceneMonsterFxData CriticalFx;
    /// <summary>普通命中声音地址。</summary>
    public string HitAudio;
    /// <summary>暴击命中声音地址。</summary>
    public string CriticalAudio;
    /// <summary>原结果的弹道表现参数，无弹道时为空。</summary>
    public MSceneMonsterProjectileData Projectile;
}
