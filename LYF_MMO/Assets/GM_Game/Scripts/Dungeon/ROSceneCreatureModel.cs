using System;

/// <summary>一个已迁移 RO 模型及其原版动作列表。</summary>
[Serializable]
public sealed class ROSceneCreatureModel
{
    /// <summary>PresentTable 使用的模型名称。</summary>
    public string modelName;
    /// <summary>原展示工程中的中文模型名称。</summary>
    public string displayName;
    /// <summary>保留骨骼、材质和原 GUID 的主工程预制体路径。</summary>
    public string prefabPath;
    /// <summary>该模型可预览的原版动作，顺序沿用测试工程。</summary>
    public ROSceneCreatureAnimation[] animations;
}
