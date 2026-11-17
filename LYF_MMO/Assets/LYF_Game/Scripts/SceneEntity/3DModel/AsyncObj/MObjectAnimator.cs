/// <summary>
/// 立即切换动画的 MAnimator 子类。
/// 它只重写 CrossFade，把渐变切换请求改为直接调用 Play；其他逻辑沿用基类。
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
