using DG.Tweening;
using UnityEngine;

/// <summary>持有单次权威开盾的模型和资源，直到服务器确认破盾或自然到期。</summary>
internal sealed class MjsBarrierVisual
{
    /// <summary>壳面从开裂到消散的秒数。</summary>
    private const float BurstSeconds = 0.28f;
    /// <summary>球罩材质的开裂进度属性，零表示完好，一表示完全消散。</summary>
    private static readonly int BurstProperty = Shader.PropertyToID("_Burst");
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
        // 从本次持有者移交给短暂的世界空间爆裂动画，后续开盾不会覆盖它。
        GameObject shell = _shield.gameObject;
        _shield = null;
        if (!burst)
        {
            shell.SetActive(false);
            Object.Destroy(shell);
            return;
        }
        shell.transform.SetParent(null, true);
        // 壳面从当前实际大小扩展；沿用头顶护盾条使用的可见球罩网格。
        Vector3 startScale = shell.transform.localScale;
        MeshRenderer rim = shell.GetComponentInChildren<MeshRenderer>();
        // 每个角色只改变自己这一层罩体的材质参数。
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        rim.GetPropertyBlock(properties);
        // progress 为 0 到 1 的爆裂进度，壳面向外冲出后化成碎光。
        DOVirtual.Float(0f, 1f, BurstSeconds, progress =>
        {
            shell.transform.localScale = startScale * (1f + 1.25f * progress);
            properties.SetFloat(BurstProperty, progress);
            rim.SetPropertyBlock(properties);
        }).SetEase(Ease.OutQuad).SetLink(shell).OnComplete(() => Object.Destroy(shell));
    }
}
