using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 用代码重建本工程需要的 RO 原版配置资产（资产内容逐字段照抄源工程 YAML，不是自己编的参数）。
/// 为什么要重建而不是直接复制 .asset：源工程的 MDayTimeData/MCreatureLightData 脚本 GUID（c7d8e4bb…）指向
/// 打包好的 MoonSerializable 程序集，本工程里没有那个程序集，直接拷 .asset 会变成 Missing Script；
/// 因此这里用迁入的同名类型重新生成资产，字段值与原资产一一对应（映射表见项目分析文档）。
/// 菜单：RO/重建角色光照配置资产；也可用 -executeMethod ROConfigAssetBuilder.BuildFromCommandLine 批处理调用。
/// </summary>
public static class ROConfigAssetBuilder
{
    /// <summary>配置资产根目录（项目内相对路径）。</summary>
    private const string ConfigRoot = "Assets/RO/Config";

    /// <summary>时段数据子目录。</summary>
    private const string DayTimeFolder = ConfigRoot + "/DayTime";

    /// <summary>角色光数据子目录。</summary>
    private const string CreatureLightFolder = ConfigRoot + "/CreatureLight";

    /// <summary>世界环境数据子目录。</summary>
    private const string AmbientFolder = ConfigRoot + "/Ambient";

    /// <summary>白天角色光资产名（对应源 cl_sc_pulongdela_001_baitian）。</summary>
    private const string CreatureDayName = "cl_sc_pulongdela_001_baitian";

    /// <summary>黄昏角色光资产名（对应源 cl_sc_pulongdela_001_huanghun）。</summary>
    private const string CreatureDuskName = "cl_sc_pulongdela_001_huanghun";

    /// <summary>夜晚角色光资产名（对应源 cl_sc_pulongdela_001_yewan）。</summary>
    private const string CreatureNightName = "cl_sc_pulongdela_001_yewan";

    /// <summary>白天时段资产名（对应源 dt_sc_pulongdela_001_baitian）。</summary>
    private const string DayTimeDayName = "dt_sc_pulongdela_001_baitian";

    /// <summary>黄昏时段资产名（对应源 dt_sc_pulongdela_001_huanghun）。</summary>
    private const string DayTimeDuskName = "dt_sc_pulongdela_001_huanghun";

    /// <summary>夜晚时段资产名（对应源 dt_sc_pulongdela_001_yewan）。</summary>
    private const string DayTimeNightName = "dt_sc_pulongdela_001_yewan";

    /// <summary>演示用世界环境资产名（Demo 自己建的，值等于本场景原有 Flat 环境设置）。</summary>
    private const string AmbientAssetName = "RODemo_Ambient";

    /// <summary>菜单入口：重建全部配置资产（会覆盖同名资产）。</summary>
    [MenuItem("RO/重建角色光照配置资产")]
    public static void BuildFromMenu()
    {
        Build();
        Debug.Log("[RODemo] 配置资产重建完成：" + ConfigRoot);
    }

    /// <summary>命令行入口：重建后退出编辑器。</summary>
    public static void BuildFromCommandLine()
    {
        Build();
        EditorApplication.Exit(0);
    }

    /// <summary>创建/覆盖三类配置资产，并把时段资产与角色光资产连起来。</summary>
    public static void Build()
    {
        EnsureFolders();

        // 1) 三个角色光资产：数值来自源工程 cl_sc_pulongdela_001_*.asset 的逐字段抄录
        MCreatureLightData dayLight = CreateOrLoad<MCreatureLightData>(CreatureLightFolder + "/" + CreatureDayName + ".asset");
        FillCreatureLight(dayLight,
            new Color(0.8773585f, 0.864943f, 0.864943f, 0.7607843f),
            new Color(0.8784314f, 0.8235294f, 0.8784314f, 0.89411765f),
            new Color(0.81960785f, 0.81960785f, 0.81960785f, 1f),
            new Color(0.8392157f, 0.8352941f, 0.83137256f, 0.278f));

        MCreatureLightData duskLight = CreateOrLoad<MCreatureLightData>(CreatureLightFolder + "/" + CreatureDuskName + ".asset");
        FillCreatureLight(duskLight,
            new Color(0.8113208f, 0.6715233f, 0.501335f, 0.6509804f),
            new Color(0.8490566f, 0.756942f, 0.812769f, 0.7607843f),
            new Color(0.7205882f, 0.5467993f, 0.41327855f, 1f),
            new Color(0.89705884f, 0.76313686f, 0.5936419f, 0.203f));

        MCreatureLightData nightLight = CreateOrLoad<MCreatureLightData>(CreatureLightFolder + "/" + CreatureNightName + ".asset");
        FillCreatureLight(nightLight,
            new Color(0.7154798f, 0.7079922f, 0.8113208f, 0.6509804f),
            new Color(0.6051555f, 0.5585173f, 0.7264151f, 0.7607843f),
            new Color(0.6392157f, 0.6313726f, 0.77254903f, 0.48235294f),
            new Color(0.8235294f, 0.85882354f, 0.99215686f, 0.22352941f));

        // 2) 三个时段资产：方向/场景色/后处理数值同样逐字段照抄源 YAML
        MDayTimeData dayTimeDay = CreateOrLoad<MDayTimeData>(DayTimeFolder + "/" + DayTimeDayName + ".asset");
        FillDayTime(dayTimeDay, dayLight, MEnvorimentData.DaytimeType.Day, new Vector2(56.3f, -593f), new Vector2(162f, -28f),
            new Color(0.52205884f, 0.52205884f, 0.52205884f, 1f),
            new Color(0.9968635f, 0.8469279f, 0.7100302f, 0.47058824f),
            new Color(0.3065528f, 0.3677961f, 0.66176474f, 0.828f),
            Color.white, Color.white, Color.white, Color.white,
            5f, 5f, 5f, 0f, 1.22f, 4f,
            new Color(1.0592737f, 1.0426359f, 0.90953344f, 1f),
            new Color(0.31764707f, 0.39147982f, 0.7058824f, 1f));

        MDayTimeData dayTimeDusk = CreateOrLoad<MDayTimeData>(DayTimeFolder + "/" + DayTimeDuskName + ".asset");
        FillDayTime(dayTimeDusk, duskLight, MEnvorimentData.DaytimeType.Dusk, new Vector2(56.3f, -593f), new Vector2(177f, -28f),
            new Color(0.5147059f, 0.45937237f, 0.43901384f, 1f),
            new Color(0.99264705f, 0.93717563f, 0.85396844f, 0.47058824f),
            new Color(0.17227122f, 0.125534f, 0.2924528f, 0.5882353f),
            Color.white, Color.white, Color.white, Color.white,
            4f, 0f, 5f, 0f, 1.3f, 10f,
            new Color(0.9221513f, 0.83524704f, 0.7193746f, 1f),
            new Color(0.6187543f, 0.49047703f, 0.6226415f, 1f));

        MDayTimeData dayTimeNight = CreateOrLoad<MDayTimeData>(DayTimeFolder + "/" + DayTimeNightName + ".asset");
        FillDayTime(dayTimeNight, nightLight, MEnvorimentData.DaytimeType.Night, new Vector2(56.3f, -593f), new Vector2(-90f, -8.7f),
            new Color(0.41553932f, 0.43632308f, 0.5471698f, 1f),
            new Color(0.3975454f, 0.49254265f, 0.9485294f, 0.259f),
            new Color(0.2673268f, 0.22249912f, 0.3773585f, 0.72156864f),
            Color.white, Color.white, Color.white, new Color(0.14297791f, 0.22919294f, 0.5943396f, 1f),
            8f, 0f, 5f, -2f, 1.3f, -5f,
            new Color(1.4980392f, 1.2737322f, 1.0352942f, 1f),
            new Color(0.4915814f, 0.52262866f, 0.98833734f, 1f));

        // 3) 世界环境资产：Demo 自建，Flat 常量环境光 + 天空盒反射，用来替代旧工程里那个"天空盒强度"错误滑杆
        MAmbientData ambient = CreateOrLoad<MAmbientData>(AmbientFolder + "/" + AmbientAssetName + ".asset");
        ambient.ambientMode = AmbientMode.Flat;
        ambient.ambientSkyColor = new Color(0.16f, 0.19f, 0.26f, 1f);
        ambient.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f, 1f);
        ambient.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f, 1f);
        ambient.ambientIntensity = 1f;
        ambient.reflectionMode = DefaultReflectionMode.Skybox;
        ambient.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        EditorUtility.SetDirty(ambient);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RODemo] 配置资产：" + ConfigRoot + "（3 套时段 + 3 套角色光 + 1 套环境）");
    }

    /// <summary>确保输出目录存在。</summary>
    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "RO");
        EnsureFolder("Assets/RO", "Config");
        EnsureFolder(ConfigRoot, "DayTime");
        EnsureFolder(ConfigRoot, "CreatureLight");
        EnsureFolder(ConfigRoot, "Ambient");
    }

    /// <summary>确保某个父目录下存在指定子目录。</summary>
    /// <param name="parent">父目录（Assets 相对路径）。</param>
    /// <param name="child">子目录名。</param>
    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (AssetDatabase.IsValidFolder(full) == false)
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    /// <summary>读取已有资产，没有就新建并落盘。</summary>
    /// <typeparam name="T">ScriptableObject 派生类型。</typeparam>
    /// <param name="path">资产路径（Assets 相对）。</param>
    /// <returns>资产实例。</returns>
    private static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    /// <summary>写入角色光四色与反射来源（反射立方图资产在 AB 包内、工程里没有，故留空并在此说明）。</summary>
    /// <param name="target">目标资产。</param>
    /// <param name="main">主光色（rgb 直射色，a 强度系数）。</param>
    /// <param name="back">背光色（rgb 背光色，a 混合权重）。</param>
    /// <param name="ambient">环境色（rgb 替换色，a 替换比例）。</param>
    /// <param name="rim">边缘光色（当前 Player 路径无消费者）。</param>
    private static void FillCreatureLight(MCreatureLightData target, Color main, Color back, Color ambient, Color rim)
    {
        target.mainLightColor = main;
        target.backLightColor = back;
        target.ambientColor = ambient;
        target.rimColor = rim;
        // 源资产 reflectMap 指向的 cubemap（guid 564fd75e…）在 assetbundle 里，clientproj/Assets 下没有对应文件，无法复制
        target.reflectMap = null;
        EditorUtility.SetDirty(target);
    }

    /// <summary>写入时段资产的全部字段（与源 YAML 顺序一致）。</summary>
    /// <param name="target">目标资产。</param>
    /// <param name="light">引用的角色光资产。</param>
    /// <param name="daytimeType">时段类型。</param>
    /// <param name="mainDir">主光方向（x 俯角、y 方位角，度）。</param>
    /// <param name="visDir">可见光方向（度）。</param>
    /// <param name="sceneColor">场景色偏。</param>
    /// <param name="sunColor">暖阳色。</param>
    /// <param name="shadowColor">阴影色。</param>
    /// <param name="cloudColor">云环境色。</param>
    /// <param name="grassColor">草环境色。</param>
    /// <param name="grassDarkColor">草暗部色。</param>
    /// <param name="treeColor">树环境色。</param>
    /// <param name="bloom">泛光强度（后处理未迁入，仅保留数据）。</param>
    /// <param name="temperature">色温（后处理未迁入）。</param>
    /// <param name="tint">色调（后处理未迁入）。</param>
    /// <param name="saturation">饱和度（后处理未迁入）。</param>
    /// <param name="exposure">曝光（后处理未迁入）。</param>
    /// <param name="contrast">对比度（后处理未迁入）。</param>
    /// <param name="terrainLight">地形亮色。</param>
    /// <param name="terrainDark">地形暗色。</param>
    private static void FillDayTime(MDayTimeData target, MCreatureLightData light, MEnvorimentData.DaytimeType daytimeType,
        Vector2 mainDir, Vector2 visDir, Color sceneColor, Color sunColor, Color shadowColor,
        Color cloudColor, Color grassColor, Color grassDarkColor, Color treeColor,
        float bloom, float temperature, float tint, float saturation, float exposure, float contrast,
        Color terrainLight, Color terrainDark)
    {
        target.daytimeType = daytimeType;
        target.mainLightDir = mainDir;
        target.visLightDir = visDir;
        target.sceneColor = sceneColor;
        target.creatureLightData = light;
        target.sunColor = sunColor;
        target.shadowColor = shadowColor;
        target.cloudEnvorimentColor = cloudColor;
        target.grassEnvorimentColor = grassColor;
        target.grassEnvorimentDarkColor = grassDarkColor;
        target.treeEnvorimentColor = treeColor;
        target.bloomIntensity = bloom;
        target.temperature = temperature;
        target.tint = tint;
        target.saturation = saturation;
        target.exposure = exposure;
        target.contrast = contrast;
        target.terrainLightColor = terrainLight;
        target.terrainDarkColor = terrainDark;
        target.terrainThreshold = 0.25f;
        EditorUtility.SetDirty(target);
    }
}
