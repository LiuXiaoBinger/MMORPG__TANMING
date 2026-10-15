using UnityEngine;

/// <summary>粒子克隆体独立持有源资产，技能结束或回池后仍保证有限粒子尾迹的资源有效。</summary>
public sealed class SkillRuntimeFxLifetime : MonoBehaviour
{
    /// <summary>该粒子源自的表现行主键，零表示尚未绑定资源租约。</summary>
    private int _presentationId;

    /// <summary>本克隆体的根粒子；由真实存活状态决定回收，不估算寿命曲线的最大常量。</summary>
    private ParticleSystem _effect;

    /// <summary>所有子粒子的延迟发射和存活尾粒均结束后销毁克隆体，随后 OnDestroy 解除租约。</summary>
    private void LateUpdate()
    {
        if (_effect == null || !_effect.IsAlive(true))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>对象销毁时释放源资产及其粒子依赖的最后一个实例引用。</summary>
    private void OnDestroy()
    {
        ReleaseLease();
    }

    /// <summary>独立持有编辑器表现资产；调用方无须另外 Retain。</summary>
    /// <param name="presentationId">导出表现资产的正数主键。</param>
    public void Bind(int presentationId)
    {
        ReleaseLease();
        SkillRuntimePresentationCache.Retain(presentationId);
        _presentationId = presentationId;
        PrepareFinitePlayback();
    }

    /// <summary>将当前无引导技能的根/子粒子限定为单轮发射，保留全部已发射粒子的真实尾迹。</summary>
    private void PrepareFinitePlayback()
    {
        _effect = GetComponent<ParticleSystem>();
        // 同属当前特效克隆体的根及子粒子，生命周期由此组件统一观察。
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        // index 为克隆体内粒子数组的零起始下标，只修改克隆体而不修改原预制体。
        for (int index = 0; index < particles.Length; index++)
        {
            // 主模块是指向当前粒子系统的结构体接口，写入会作用于此克隆体。
            ParticleSystem.MainModule main = particles[index].main;
            main.loop = false;
        }
    }

    /// <summary>解绑时恰好解除当前来源的一份持有，组件重复解绑不会再扣减引用。</summary>
    private void ReleaseLease()
    {
        if (_presentationId > 0)
        {
            SkillRuntimePresentationCache.Release(_presentationId);
            _presentationId = 0;
        }
    }
}
