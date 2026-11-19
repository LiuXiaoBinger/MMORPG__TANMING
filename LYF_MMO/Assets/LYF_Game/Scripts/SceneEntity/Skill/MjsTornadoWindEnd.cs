using UnityEngine;

/// <summary>在龙卷斩风体的前进动画抵达最远点时，结束整组特效。</summary>
public sealed class MjsTornadoWindEnd : MonoBehaviour
{
    /// <summary>本次播放的最外层风体对象，包含前进动画和所有尾迹粒子。</summary>
    [SerializeField] private GameObject _windRoot;

    /// <summary>由专用前进剪辑的最远帧事件调用，销毁当前风体及其子粒子。</summary>
    public void FinishTravel()
    {
        Destroy(_windRoot);
    }
}
