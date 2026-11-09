using System.IO;
using System.Web.Script.Serialization;

/// <summary>与客户端共用的场景登记；只读取服务端战斗配置入口。</summary>
internal sealed class DungeonSceneRegistration
{
    /// <summary>相对服务端发布目录的战斗配置路径；空值表示仅运行现有场景流程。</summary>
    public string battleConfig { get; set; }

    /// <summary>按实际SceneID读取同一份客户端登记，不默认选择其他副本。</summary>
    /// <param name="sceneId">DungeonsTable选定的场景编号。</param>
    internal static DungeonSceneRegistration Load(int sceneId)
    {
        // 每个登记由项目资源直接嵌入，发布包不访问开发机资源路径。
        using (Stream source = typeof(DungeonSceneRegistration).Assembly.GetManifestResourceStream("Dungeons.Scenes." + sceneId + ".json"))
        using (StreamReader reader = new StreamReader(source))
            return new JavaScriptSerializer().Deserialize<DungeonSceneRegistration>(reader.ReadToEnd());
    }
}
