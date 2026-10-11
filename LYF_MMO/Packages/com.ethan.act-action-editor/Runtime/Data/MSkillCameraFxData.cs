using System;

/// <summary>RO镜头特效制作数据，保留镜头特效配置ID和接收对象筛选，不替换现有Y镜头轨道。</summary>
[Serializable]
public sealed class MSkillCameraFxData
{
    /// <summary>镜头特效的可见接收对象，保留RO枚举值用于后续筛选。</summary>
    public ESkillFxShowToType FxShowTo;

    /// <summary>RO镜头特效配置ID，完成配置后必须为正整数。</summary>
    public int CameraFxId;

    /// <summary>从技能开始计时的镜头特效触发时刻，单位秒，非负。</summary>
    public float At;
}

