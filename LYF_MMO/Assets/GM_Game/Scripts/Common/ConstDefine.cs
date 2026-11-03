using System.Collections.Generic;
using UnityEngine;

// 供资源加载器和界面读取的固定路径、职业名称。


/// <summary>
/// 保存客户端常用资源目录和职业名称；路径相对于 Unity 工程。
/// </summary>
public class ConstDefine {


    /// <summary>GM_Game 资源根目录的相对路径，YooAsset 下各子目录路径均以此为前缀。</summary>
    public const string BasePath = "Assets/GM_Game/";
    /// <summary>常用预制体的路径前缀，调用方在后面拼接具体资源路径。</summary>
    public const string PrefabPath = BasePath + "Prefabs/";
    /// <summary>特效资源目录。</summary>
    public const string EffectPath = BasePath + "Effects/";
    /// <summary>UI 图集与精灵资源目录。</summary>
    public const string SpritePath = BasePath + "UISprites/";
    /// <summary>场景资源目录。</summary>
    public const string ScenePath = BasePath + "Scenes/";

    /// <summary>职业名称表，下标与职业ID对应，第0项为空占位，第1项为魔剑士。</summary>
    public  readonly static List<string> RoleJobName = new List<string>() { "","魔剑士" };
}