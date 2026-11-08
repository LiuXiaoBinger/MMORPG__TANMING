/// <summary>原FaceToCameraObj按帧更新同一朝向公式，替换原MEffectUpdater依赖。</summary>
public sealed class MSceneMonsterFaceToCameraObj : MSceneMonsterFaceToCamera
{
    /// <summary>相机完成本帧移动后刷新原特效面向。</summary>
    private void LateUpdate()
    {
        FaceToAction();
    }
}
