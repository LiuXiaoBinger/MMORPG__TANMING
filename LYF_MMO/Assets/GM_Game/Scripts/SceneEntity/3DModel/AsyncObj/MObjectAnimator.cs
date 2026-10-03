/// <summary>
/// 非角色对象使用的简化动画封装，对应 RO 的 MObjectAnimator。
/// 跳过 CrossFade 过渡，直接播放目标动画，适用于怪物、NPC、掉落物等简单动画对象。
/// </summary>
public sealed class MObjectAnimator : MAnimator
{
    /// <summary>
    /// 简化版 CrossFade：不做过渡混合，直接播放目标状态。
    /// </summary>
    /// <param name="stateName">目标动画状态名。</param>
    /// <param name="transitionDuration">过渡时长，简化版忽略此参数。</param>
    /// <returns>true 表示播放成功。</returns>
    public override bool CrossFade(string stateName, float transitionDuration)
    {
        return Play(stateName);
    }
}
