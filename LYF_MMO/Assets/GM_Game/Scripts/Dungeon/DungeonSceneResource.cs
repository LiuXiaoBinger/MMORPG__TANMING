using System;
using UnityEngine;

/// <summary>按 SceneID 登记已经迁入客户端的场景、流程和模型资源，不保存副本目标定义。</summary>
[Serializable]
public sealed class DungeonSceneResource
{
    /// <summary>原 SceneTable 编号，同场景的不同副本难度共用这份资源定位。</summary>
    public int sceneId;
    /// <summary>YooAsset 场景地址，不含 .unity；空值表示该地图尚未迁入。</summary>
    public string sceneLocation;
    /// <summary>原流程 XML 的 Resources 路径，不含 .txt。</summary>
    public string flowResource;
    /// <summary>当前场景模型目录的 Resources 路径，不含 .json。</summary>
    public string creatureResource;
    /// <summary>服务端战斗配置路径；非空表示服务器为本场景提供所有 Actor 的完整出生快照。</summary>
    public string battleConfig;
    /// <summary>本场景技能、Buff 和通知目录的 Resources 根路径；空值表示尚未迁入战斗表现。</summary>
    public string battleResourceRoot;
    /// <summary>普攻伤害跳字预制体的 Resources 路径；空值不显示跳字。</summary>
    public string hitWordResource;
    /// <summary>传送交互预制体的 Resources 路径，不从其他副本借用默认资源。</summary>
    public string portalResource;
    /// <summary>包含碰撞及导航障碍的空气墙预制体 Resources 路径。</summary>
    public string airWallResource;
    /// <summary>采集物效果预制体的 Resources 路径；空值仅保留配置交互。</summary>
    public string collectionResource;
    /// <summary>当前场景采集交互按钮的文案，和采集物表现一同登记。</summary>
    public string collectionTitle;

    /// <summary>读取指定场景的明确登记，未迁入时返回空，不回退到其他副本。</summary>
    /// <param name="sceneId">DungeonsTable.SceneID 或服务器入场通知的场景编号。</param>
    public static DungeonSceneResource Load(int sceneId)
    {
        // 登记文件名直接使用 SceneID，业务目标仍由 Luban 表提供。
        TextAsset asset = Resources.Load<TextAsset>("Dungeons/Scenes/" + sceneId);
        if (asset == null) return null;
        return JsonUtility.FromJson<DungeonSceneResource>(asset.text);
    }
}
