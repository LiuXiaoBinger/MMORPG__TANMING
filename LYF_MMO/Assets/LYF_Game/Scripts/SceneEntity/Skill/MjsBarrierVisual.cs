using UnityEngine;

/// <summary>持有单次权威开盾的模型和资源，直到服务器确认破盾或自然到期。</summary>
internal sealed class MjsBarrierVisual
{
    /// <summary>球面材质使用的开盾时刻，单位为当前关卡开始后的秒数。</summary>
    private static readonly int OpenTime = Shader.PropertyToID("_OpenTime");
    /// <summary>本次施法锁定的表现行，持有期间允许异步等待角色模型。</summary>
    private readonly int _presentationId;
    /// <summary>开盾角色的模型，罩体随该模型移动。</summary>
    private readonly MModel _model;
    /// <summary>模型就绪回调的待处理票据，热模型同步返回零。</summary>
    private int _modelToken;
    /// <summary>已附着于角色的实际罩体，等待表现资产或模型期间为空。</summary>
    private ParticleSystem _shield;
    /// <summary>服务器本次开盾的 UUID，旧结束包只能关闭与其对应的罩体。</summary>
    public ulong SkillUuid { get; }

    /// <summary>在服务器确认开盾后从技能正式表现取得罩体与挂点参数。</summary>
    /// <param name="skillUuid">原始施法 UUID。</param>
    /// <param name="presentationId">施放时选定的表现行主键。</param>
    /// <param name="model">本人或附近施法者的场景模型。</param>
    public MjsBarrierVisual(ulong skillUuid, int presentationId, MModel model)
    {
        SkillUuid = skillUuid;
        _presentationId = presentationId;
        _model = model;
        SkillRuntimePresentationCache.Retain(_presentationId);
        SkillRuntimePresentationCache.AddReadyListener(_presentationId, OnPresentationReady);
    }

    /// <summary>正式罩体来源是护盾时间轴首轨，角色模型就绪时再按配置放置。</summary>
    /// <param name="presentation">已加载的技能表现，加载失败时为空。</param>
    private void OnPresentationReady(SkillRuntimePresentation presentation)
    {
        if (presentation == null || _model == null || presentation.FxEvents.Count == 0) return;
        // 首轨保存罩体预制体、模型空间中心和缩放。
        SkillRuntimeFxEvent shell = presentation.FxEvents[0];
        // roleObject 是已经载入的角色原生模型，角色离场会一并销毁罩体。
        _modelToken = _model.AddLoadGoCallback(roleObject =>
        {
            _shield = Object.Instantiate(shell.Particle, roleObject.transform, false);
            _shield.transform.localPosition = shell.LocalPosition;
            _shield.transform.localEulerAngles = shell.LocalEulerAngles;
            _shield.transform.localScale = shell.LocalScale;
            // 克隆体在爆裂动画结束前也独立持有材质和网格，回收由权威结束通知接管。
            SkillRuntimeFxLifetime lifetime = _shield.gameObject.AddComponent<SkillRuntimeFxLifetime>();
            lifetime.Bind(_presentationId);
            lifetime.enabled = false;
            // 正式罩面按本次开盾的时间逐渐显现，角色之间独立计时。
            MeshRenderer rim = _shield.GetComponentInChildren<MeshRenderer>();
            // 只写入这个罩体实例的起点，不修改共享材质。
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            rim.GetPropertyBlock(properties);
            properties.SetFloat(OpenTime, Time.timeSinceLevelLoad);
            rim.SetPropertyBlock(properties);
            _shield.Play(true);
        });
    }

    /// <summary>结束后解除尚未完成的加载。破盾当帧隐藏，正常到期在原位置扩散并消失。</summary>
    /// <param name="burst">服务器确认自然到期时为真，破盾、离场和替换时为假。</param>
    public void Finish(bool burst)
    {
        SkillRuntimePresentationCache.RemoveReadyListener(_presentationId, OnPresentationReady);
        if (_modelToken != 0) _model.RemoveLoadGoCallback(_modelToken);
        SkillRuntimePresentationCache.Release(_presentationId);
        if (_shield == null) return;
        // 罩体在本次施法结束后交给粒子生命周期组件回收。
        GameObject shell = _shield.gameObject;
        _shield.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _shield = null;
        if (!burst)
        {
            shell.SetActive(false);
            Object.Destroy(shell);
            return;
        }
        shell.transform.SetParent(null, true);
        shell.GetComponentInChildren<MeshRenderer>().enabled = false;
        // 白闪、横向冲击波及飞散碎片是正式预制体中的同一组，只有自然到期才播放。
        ParticleSystem expiryLight = shell.transform.Find("MjsBarrierLights/ExpiryBurst").GetComponent<ParticleSystem>();
        expiryLight.gameObject.SetActive(true);
        expiryLight.Play(true);
        shell.GetComponent<SkillRuntimeFxLifetime>().enabled = true;
    }
}
