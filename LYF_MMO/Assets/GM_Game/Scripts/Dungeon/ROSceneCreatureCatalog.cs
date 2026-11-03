using System;
using UnityEngine;

/// <summary>编辑器与正式副本共用的强类型资源目录；实体仍由 SceneWorld 管理。</summary>
[Serializable]
public sealed class ROSceneCreatureCatalog
{
    /// <summary>主工程内的模型、动作目录位置。</summary>
    public const string AssetPath = "Assets/GM_Game/Resources/Dungeons/Moshenta/MoshentaCreatures.json";
    /// <summary>已经迁移的 16 个模型和各自原版动作。</summary>
    public ROSceneCreatureModel[] models;
    /// <summary>魔神塔 EZ 实际引用的怪物表映射。</summary>
    public ROSceneCreatureEntity[] entities;

    /// <summary>读取随编辑器保存的本项目资源映射。</summary>
    public static ROSceneCreatureCatalog Load()
    {
        return JsonUtility.FromJson<ROSceneCreatureCatalog>(Resources.Load<TextAsset>("Dungeons/Moshenta/MoshentaCreatures").text);
    }

    /// <summary>按原怪物配置编号查找表现数据。</summary>
    /// <param name="id">RO EntityTable 编号。</param>
    public ROSceneCreatureEntity FindEntity(int id)
    {
        for (int i = 0; i < entities.Length; i++) if (entities[i].id == id) return entities[i];
        return null;
    }

    /// <summary>按 PresentTable 模型名称定位已经迁入的原预制体。</summary>
    /// <param name="name">RO 原模型名。</param>
    public ROSceneCreatureModel FindModel(string name)
    {
        for (int i = 0; i < models.Length; i++) if (models[i].modelName == name) return models[i];
        return null;
    }
}
