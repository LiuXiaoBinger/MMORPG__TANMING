using UnityEngine;

/// <summary>原MFxGradeMgr的片段引用；本副本保留完整原特效，不引入原质量管理器。</summary>
public sealed class MSceneMonsterFxGrade : MonoBehaviour
{
    /// <summary>原五档特效子对象，空引用表示该档没有额外片段。</summary>
    public GameObject[] clips;

    /// <summary>新实例使用完整原片段集合。</summary>
    private void OnEnable()
    {
        SetFxLevel(clips.Length - 1);
    }

    /// <summary>保持原逐档包含规则，供本副本原特效初始化。</summary>
    /// <param name="fxLevel">需要保留的最高零基片段档位。</param>
    public void SetFxLevel(int fxLevel)
    {
        for (int index = 0; index < clips.Length; index++)
            if (clips[index] != null) clips[index].SetActive(index <= fxLevel);
    }
}
