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
