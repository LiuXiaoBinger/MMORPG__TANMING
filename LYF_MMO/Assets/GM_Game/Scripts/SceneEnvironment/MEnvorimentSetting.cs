using UnityEngine;

/// <summary>
/// 魔神塔场景最小环境组件。对应 RO 源工程 MEnvorimentSetting 的精简移植：
/// 按源数据结构与调用顺序，把固定黄昏配置（主光方向、场景色、角色光、云草树环境色、
/// 静态雾与散射参数）写入 RO 场景着色器使用的全局变量，并恢复烘焙 Lightmap 与 LightProbes。
/// 仅覆盖本场景实际使用的通道：不含昼夜过渡、天气循环、雨雪雷电、后处理与相机控制。
/// 与源工程唯一的生命周期差异：源工程只靠运行时 OnEnable 生效，本组件额外标记 [ExecuteAlways]，
/// 让编辑器里打开场景、重新勾选组件、脚本重载后也自动应用同一套数据，不必先右键「应用环境配置」
/// 才看得到烘焙光图；停用或离开时仍由 OnDisable 还原应用前状态。
/// 自动应用只是一次性整批写入，不在 Update／OnGUI 里反复重写。
///
/// 与源 ResetGlobalShaderVariable 的边界说明：源工程的 ResetGlobalShaderVariable 把全局量写死成
/// 一套「引擎默认兜底值」，因为源客户端只有这一套全局环境系统，退出即回到默认。
/// 本组件是挂在单个场景上的独立组件，若同样写死兜底值，会把其他场景已经设置好的全局环境量冲掉。
/// 因此停用时改为还原「首次应用前实际捕获到的原值」：既不残留本场景数据，也不覆盖别人的数据。
/// </summary>
// 标记 [ExecuteAlways] 是本任务唯一的生命周期适配：Unity 在编辑器里也会调用它的
// OnEnable／OnDisable，因此打开场景、重新勾选组件、脚本重载后会自动应用，取消勾选或关闭场景会自动还原。
// 不加 Update／OnGUI，避免每帧重写 937 条渲染器绑定。
[ExecuteAlways]
public class MEnvorimentSetting : MonoBehaviour
{
    /// <summary>
    /// 渲染器烘焙光分组。每个元素对应一套烘焙方案，
    /// 由 MLightmapData.lightmapIndex 选中其中一组；元素内部的绑定逐条记录渲染器与光照图 UV 变换。
    /// </summary>
    public MLightmapIndex[] lightmapIndex = new MLightmapIndex[2];

    /// <summary>
    /// 默认烘焙光照数据。非空时在应用开始时恢复光照图、光照探针与逐渲染器绑定。
    /// </summary>
    public MLightmapData defaultLightmapData;

    /// <summary>
    /// 默认时间段数据（本场景为黄昏）。非空时写入主光/可视光方向、场景色偏、云草树环境色等通道。
    /// </summary>
    public MDayTimeData defaultDayTimeData;

    /// <summary>
    /// 默认天气数据。非空时写入雾色、海拔雾与散射参数、云影色、雾/云流动与风参数。
    /// </summary>
    public MWeatherData defaultWeatherData;

    /// <summary>
    /// 雾贴图的 UV 平移缩放基准（x/y 为平铺次数，z/w 为偏移）。
    /// 应用时按世界尺寸换算后写入 _ROCFogST。
    /// </summary>
    public Vector4 fogST = new Vector4(1f, 1f, 0f, 0f);

    /// <summary>
    /// 云贴图的 UV 平移缩放基准（x/y 为平铺次数，z/w 为偏移）。
    /// 应用时按世界尺寸换算后写入 _ROCCloudST。
    /// </summary>
    public Vector4 cloudST = new Vector4(1f, 1f, 0f, 0f);

    /// <summary>
    /// 环境纹理覆盖的世界尺寸，单位：世界单位。应用天气时同步为 MWeatherData.worldSize，
    /// 用于雾/云 UV 换算（换算时取倒数），值必须大于 0。
    /// </summary>
    public int worldSize = 100;

    /// <summary>角色主光颜色通道 ID（_ROCLightColor0）</summary>
    private static readonly int MainLightColorID = Shader.PropertyToID("_ROCLightColor0");

    /// <summary>角色背光颜色通道 ID（_ROCBackColor）</summary>
    private static readonly int BackLightColorID = Shader.PropertyToID("_ROCBackColor");

    /// <summary>角色环境光颜色通道 ID（_ROCAmbientColor）</summary>
    private static readonly int AmbientColorID = Shader.PropertyToID("_ROCAmbientColor");

    /// <summary>角色边缘光颜色通道 ID（_ROCRimColor）</summary>
    private static readonly int RimColorID = Shader.PropertyToID("_ROCRimColor");

    /// <summary>云环境色通道 ID（_ROCloudEnvorimentColor）</summary>
    private static readonly int CloudEnvColorID = Shader.PropertyToID("_ROCloudEnvorimentColor");

    /// <summary>草地环境色通道 ID（_ROGrassEnvorimentColor）</summary>
    private static readonly int GrassEnvColorID = Shader.PropertyToID("_ROGrassEnvorimentColor");

    /// <summary>草地暗部环境色通道 ID（_ROGrassEnvorimentDarkColor）</summary>
    private static readonly int GrassEnvDarkColorID = Shader.PropertyToID("_ROGrassEnvorimentDarkColor");

    /// <summary>树木环境色通道 ID（_ROTreeEnvorimentColor）</summary>
    private static readonly int TreeEnvColorID = Shader.PropertyToID("_ROTreeEnvorimentColor");

    /// <summary>主光方向通道 ID（_ROCLightDir0），存单位方向向量而非角度</summary>
    private static readonly int MainLightDirID = Shader.PropertyToID("_ROCLightDir0");

    /// <summary>可视光（第二路光）方向通道 ID（_ROCLightDir1），存单位方向向量而非角度</summary>
    private static readonly int VisLightDirID = Shader.PropertyToID("_ROCLightDir1");

    /// <summary>场景色偏通道 ID（_ROCSceneColor），rgb 为整体色调乘算值</summary>
    private static readonly int SceneColorID = Shader.PropertyToID("_ROCSceneColor");

    /// <summary>影色通道 ID（_ROCShadowColor），rgb 为暗部色、a 为暗部强度</summary>
    private static readonly int ShadowColorID = Shader.PropertyToID("_ROCShadowColor");

    /// <summary>日色通道 ID（_ROCSunColor），rgb 为阳光色、a 为阳光强度</summary>
    private static readonly int SunColorID = Shader.PropertyToID("_ROCSunColor");

    /// <summary>世界尺寸倒数通道 ID（_ROCWorldParams），值为 1/worldSize 的浮点</summary>
    private static readonly int WorldSizeID = Shader.PropertyToID("_ROCWorldParams");

    /// <summary>海拔雾参数通道 ID（_ROCHeightFogParams），xyzw=(起始高度倒数, 衰减指数, 相机雾是否影响海拔雾, 是否启用)</summary>
    private static readonly int HeightFogID = Shader.PropertyToID("_ROCHeightFogParams");

    /// <summary>散射雾参数通道 ID（_ROCRadiusFogParams），xyzw=(散射起始距离, Mie 系数, Rayleigh 密度, Mie 密度)</summary>
    private static readonly int CamFogID = Shader.PropertyToID("_ROCRadiusFogParams");

    /// <summary>Mie 散射三元组通道 ID（_ROCMieG），按 Mie 系数换算出的相位函数常量</summary>
    private static readonly int MieGID = Shader.PropertyToID("_ROCMieG");

    /// <summary>云影/雪色通道 ID（_ROCMaskColor）</summary>
    private static readonly int CloudMaskColorID = Shader.PropertyToID("_ROCMaskColor");

    /// <summary>雾/云流动速度通道 ID（_ROCMapFlow），xy 为雾速、zw 为云速</summary>
    private static readonly int MapFlowID = Shader.PropertyToID("_ROCMapFlow");

    /// <summary>雾 UV 变换通道 ID（_ROCFogST），xy 为缩放、zw 为偏移</summary>
    private static readonly int FogSTID = Shader.PropertyToID("_ROCFogST");

    /// <summary>云 UV 变换通道 ID（_ROCCloudST），xy 为缩放、zw 为偏移</summary>
    private static readonly int CloudSTID = Shader.PropertyToID("_ROCCloudST");

    /// <summary>风参数通道 ID（_ROCWindParams），xyz 为风向量、w 为频率</summary>
    private static readonly int WindParamsID = Shader.PropertyToID("_ROCWindParams");

    /// <summary>地形亮部叠加色通道 ID（_ROTerrainLightColor）</summary>
    private static readonly int TerrainLightColorID = Shader.PropertyToID("_ROTerrainLightColor");

    /// <summary>地形暗部叠加色通道 ID（_ROTerrainDarkColor）</summary>
    private static readonly int TerrainDarkColorID = Shader.PropertyToID("_ROTerrainDarkColor");

    /// <summary>地形明暗分界阈值通道 ID（_ROTerrainThreshold），取值 0~1 的浮点</summary>
    private static readonly int TerrainThresholdID = Shader.PropertyToID("_ROTerrainThreshold");

    /// <summary>雾混合起色通道 ID（_ROCFogStartColor），rgb 为混合色、a 为开始距离</summary>
    private static readonly int FogStartColorID = Shader.PropertyToID("_ROCFogStartColor");

    /// <summary>雾混合终色通道 ID（_ROCFogEndColor），rgb 为雾色、a 为结束距离</summary>
    private static readonly int FogEndColorID = Shader.PropertyToID("_ROCFogEndColor");

    /// <summary>
    /// 本组件实际写入的 Color 类型全局通道 ID 集合（共 16 项）。
    /// 首次应用前按此集合逐项取原值，停用时逐项写回；顺序即快照数组下标，改动顺序必须与
    /// ColorChannelIDs 的读取、还原循环共用同一份列表，避免类型错位。
    /// </summary>
    private static readonly int[] ColorChannelIDs = new int[]
    {
        MainLightColorID, BackLightColorID, AmbientColorID, RimColorID,
        CloudEnvColorID, GrassEnvColorID, GrassEnvDarkColorID, TreeEnvColorID,
        SceneColorID, SunColorID, ShadowColorID,
        TerrainLightColorID, TerrainDarkColorID,
        CloudMaskColorID, FogStartColorID, FogEndColorID
    };

    /// <summary>
    /// 本组件实际写入的 Vector 类型全局通道 ID 集合（共 9 项）。
    /// 方向、雾参数、UV 变换、风参数走向量入口，不能与 Color 混用，否则 a 通道语义会错位。
    /// </summary>
    private static readonly int[] VectorChannelIDs = new int[]
    {
        MainLightDirID, VisLightDirID, HeightFogID, CamFogID, MieGID,
        WindParamsID, MapFlowID, FogSTID, CloudSTID
    };

    /// <summary>
    /// 本组件实际写入的 Float 类型全局通道 ID 集合（共 2 项）。
    /// 浮点通道若用 SetGlobalColor 写会被当成四维向量，故单独成组按浮点还原。
    /// </summary>
    private static readonly int[] FloatChannelIDs = new int[]
    {
        TerrainThresholdID, WorldSizeID
    };

    /// <summary>
    /// 是否已经取过快照并写入过环境数据。为 true 表示「应用前的原值快照已存在」，
    /// 此时再次显式应用只重写数据、不重取快照；为 false 表示当前处于未应用（或已还原）状态。
    /// </summary>
    private bool _applied;

    /// <summary>应用前的雾色（RenderSettings 为场景级设置，需按原值还原）</summary>
    private Color _prevFogColor;

    /// <summary>应用前的自定义反射贴图（角色光数据的 reflectMap 会覆盖它）</summary>
    private Texture _prevCustomReflection;

    /// <summary>应用前场景的光照图数组</summary>
    private LightmapData[] _prevLightmaps;

    /// <summary>应用前场景的光照探针</summary>
    private LightProbes _prevLightProbes;

    /// <summary>应用前被绑定渲染器的光照图下标，与 _prevScaleOffsets 一一对应</summary>
    private int[] _prevRendererIndices;

    /// <summary>应用前被绑定渲染器的光照图 UV 变换，与 _prevRendererIndices 一一对应</summary>
    private Vector4[] _prevScaleOffsets;

    /// <summary>
    /// 首次应用前的 Color 通道原值，与 ColorChannelIDs 按下标一一对应。
    /// 空数组表示尚未取过快照；原值可能本来就是全 0（这些通道在编辑器里无人写过），按实捕获不做兜底替换。
    /// </summary>
    private Color[] _prevColors = new Color[0];

    /// <summary>首次应用前的 Vector 通道原值，与 VectorChannelIDs 按下标一一对应</summary>
    private Vector4[] _prevVectors = new Vector4[0];

    /// <summary>首次应用前的 Float 通道原值，与 FloatChannelIDs 按下标一一对应</summary>
    private float[] _prevFloats = new float[0];

    /// <summary>
    /// 启用时按源组件顺序应用默认配置：先恢复烘焙光，再写天色与天气。
    /// 运行时进入场景即生效；由于类上标了 [ExecuteAlways]，编辑器里打开场景、重新勾选组件、
    /// 脚本重载后同样会走到这里，因此编辑态不再需要手动点右键菜单。
    /// </summary>
    private void OnEnable()
    {
        ApplyEnvironment();
    }

    /// <summary>
    /// 停用时还原应用前状态，避免把本场景的全局变量与场景级设置泄漏给其他场景。
    /// 编辑器里取消勾选组件或关闭场景也走这里，是「停用即还原」的验证入口。
    /// </summary>
    private void OnDisable()
    {
        RestoreEnvironment();
    }

    /// <summary>
    /// 应用默认环境配置。编辑器里由 OnEnable 自动调用；也可在 Inspector 右键菜单手动触发，
    /// 用于改完配置后立刻重写一遍（例如只调了雾色或云速）。
    /// 顺序与源 MEnvorimentSetting 一致：烘焙光 → 时间段 → 天气 → 场景色偏。
    /// 首次调用时取一次「应用前原值」快照；之后再次调用只按当前数据重新写入，
    /// 不再覆盖首次快照，因此改完配置再次点菜单能立即看到新结果，且停用时仍能还原到最初状态。
    /// </summary>
    [ContextMenu("应用环境配置")]
    public void ApplyEnvironment()
    {
        if (!_applied)
        {
            // 只有首次应用（或还原后再次应用）才记录原值，保证快照始终是「未被本组件改过」的状态
            CaptureRenderState();
            CaptureGlobalShaderState();
            _applied = true;
        }

        ApplyLightmapData(defaultLightmapData);
        ApplyDayTimeData(defaultDayTimeData);
        ApplyWeatherData(defaultWeatherData);
        ApplySceneColor();
    }

    /// <summary>
    /// 还原应用前状态。全局 Shader 变量按类型逐项写回首次应用前捕获的原值，
    /// 场景级设置（雾色、自定义反射、光照图、探针）与逐渲染器绑定按应用前快照还原。
    /// </summary>
    [ContextMenu("恢复原全局状态")]
    public void RestoreEnvironment()
    {
        if (!_applied)
        {
            return;
        }

        RestoreGlobalShaderState();

        RenderSettings.fogColor = _prevFogColor;
        RenderSettings.customReflectionTexture = _prevCustomReflection;
        LightmapSettings.lightmaps = _prevLightmaps;
        LightmapSettings.lightProbes = _prevLightProbes;

        RestoreRendererLightmapBindings();
        _applied = false;
    }

    /// <summary>
    /// 记录应用前的场景级设置与被绑定渲染器的光照图状态，供停用时还原。
    /// 只在首次应用时调用一次。
    /// </summary>
    private void CaptureRenderState()
    {
        _prevFogColor = RenderSettings.fogColor;
        _prevCustomReflection = RenderSettings.customReflectionTexture;
        _prevLightmaps = LightmapSettings.lightmaps;
        _prevLightProbes = LightmapSettings.lightProbes;

        // 当前光照数据选中的那一组渲染器绑定；为空说明没有可回填的烘焙数据
        MLightmapIndex group = GetLightmapGroup();
        if (group == null)
        {
            _prevRendererIndices = null;
            _prevScaleOffsets = null;
            return;
        }

        // 该组的逐渲染器烘焙绑定数组，长度即需要记录原值的渲染器数量（本场景 937 条）
        MRendererLightmapScale[] scales = group.lightmapScales;
        // 实际参与记录的渲染器条数，为 0 时下面两个数组留空
        int count = 0;
        if (scales != null)
        {
            count = scales.Length;
        }
        _prevRendererIndices = new int[count];
        _prevScaleOffsets = new Vector4[count];
        for (int i = 0; i < count; i++)
        {
            // 第 i 条绑定指向的渲染器；为空表示该条引用已丢失，跳过不记录
            Renderer renderer = scales[i].renderer;
            if (renderer == null)
            {
                continue;
            }
            _prevRendererIndices[i] = renderer.lightmapIndex;
            _prevScaleOffsets[i] = renderer.lightmapScaleOffset;
        }
    }

    /// <summary>
    /// 把被绑定渲染器的光照图下标与 UV 变换还原为记录值。
    /// </summary>
    private void RestoreRendererLightmapBindings()
    {
        // 当前光照数据选中的那一组渲染器绑定
        MLightmapIndex group = GetLightmapGroup();
        if (group == null)
        {
            return;
        }
        // 该组的逐渲染器烘焙绑定数组
        MRendererLightmapScale[] scales = group.lightmapScales;
        if (scales == null)
        {
            return;
        }
        if (_prevRendererIndices == null)
        {
            return;
        }
        if (_prevRendererIndices.Length != scales.Length)
        {
            return;
        }
        for (int i = 0; i < scales.Length; i++)
        {
            // 第 i 条绑定指向的渲染器；为空表示引用已丢失，跳过
            Renderer renderer = scales[i].renderer;
            if (renderer == null)
            {
                continue;
            }
            renderer.lightmapIndex = _prevRendererIndices[i];
            renderer.lightmapScaleOffset = _prevScaleOffsets[i];
        }
    }

    /// <summary>
    /// 取当前光照数据指向的那一组渲染器绑定；下标无效或数据缺失时返回空。
    /// </summary>
    private MLightmapIndex GetLightmapGroup()
    {
        if (defaultLightmapData == null)
        {
            return null;
        }
        if (lightmapIndex == null)
        {
            return null;
        }
        // 光照数据指定的分组下标，索引 lightmapIndex 数组；越界即视为无有效烘焙方案
        int groupIndex = defaultLightmapData.lightmapIndex;
        if (groupIndex < 0)
        {
            return null;
        }
        if (groupIndex >= lightmapIndex.Length)
        {
            return null;
        }
        return lightmapIndex[groupIndex];
    }

    /// <summary>
    /// 恢复烘焙光照：先逐渲染器回填光照图下标与 UV 变换，再把光照图与探针写入场景设置。
    /// 只写贴图不写 scaleOffset 会导致烘焙贴图错位，故两者必须同批处理。
    /// </summary>
    /// <param name="lightmap">烘焙光照数据，为空时整个步骤跳过</param>
    private void ApplyLightmapData(MLightmapData lightmap)
    {
        if (lightmap == null)
        {
            return;
        }

        // 当前光照数据选中的那一组渲染器绑定；为空则只写场景级光照图与探针
        MLightmapIndex group = GetLightmapGroup();
        if (group != null)
        {
            // 该组的逐渲染器烘焙绑定数组，每条含目标下标与 UV 变换
            MRendererLightmapScale[] scales = group.lightmapScales;
            if (scales != null)
            {
                for (int i = 0; i < scales.Length; i++)
                {
                    // 第 i 条绑定指向的渲染器；为空表示引用已丢失，跳过
                    Renderer renderer = scales[i].renderer;
                    if (renderer == null)
                    {
                        continue;
                    }
                    renderer.lightmapIndex = scales[i].index;
                    renderer.lightmapScaleOffset = scales[i].scaleOffset;
                }
            }
        }

        // 数据里的烘焙光照贴图数组，本场景为 2 张 1024x1024 EXR 解出的贴图
        Texture2D[] maps = lightmap.lightmaps;
        if (maps != null)
        {
            // 组装给 LightmapSettings 用的场景光照图条目数组，长度与贴图数一致
            LightmapData[] datas = new LightmapData[maps.Length];
            for (int i = 0; i < maps.Length; i++)
            {
                // 第 i 张光照图对应的场景设置条目；只填 lightmapColor，其余通道本场景烘焙未使用
                LightmapData data = new LightmapData();
                data.lightmapColor = maps[i];
                datas[i] = data;
            }
            LightmapSettings.lightmaps = datas;
        }

        LightmapSettings.lightProbes = lightmap.lightProbes;
    }

    /// <summary>
    /// 写入时间段相关通道。方向按源算法由角度转向量：
    /// Quaternion.Euler(角度) * Vector3.back，不能把 Vector2 角度直接当方向使用。
    /// </summary>
    /// <param name="dayTime">时间段数据，为空时跳过</param>
    private void ApplyDayTimeData(MDayTimeData dayTime)
    {
        if (dayTime == null)
        {
            return;
        }

        // 主光方向：把 (俯角, 方位角) 角度转成指向场景的单位向量，本场景为 Euler(65,135)*back
        Vector3 mainLightDir = Quaternion.Euler(dayTime.mainLightDir) * Vector3.back;
        // 可视光（第二路补光）方向：同一换算规则，本场景为 Euler(30,135)*back
        Vector3 visLightDir = Quaternion.Euler(dayTime.visLightDir) * Vector3.back;

        Shader.SetGlobalVector(MainLightDirID, mainLightDir);
        Shader.SetGlobalVector(VisLightDirID, visLightDir);
        Shader.SetGlobalColor(ShadowColorID, dayTime.shadowColor);
        Shader.SetGlobalColor(SunColorID, dayTime.sunColor);
        Shader.SetGlobalColor(CloudEnvColorID, dayTime.cloudEnvorimentColor);
        Shader.SetGlobalColor(GrassEnvColorID, dayTime.grassEnvorimentColor);
        Shader.SetGlobalColor(GrassEnvDarkColorID, dayTime.grassEnvorimentDarkColor);
        Shader.SetGlobalColor(TreeEnvColorID, dayTime.treeEnvorimentColor);
        Shader.SetGlobalColor(TerrainLightColorID, dayTime.terrainLightColor);
        Shader.SetGlobalColor(TerrainDarkColorID, dayTime.terrainDarkColor);
        Shader.SetGlobalFloat(TerrainThresholdID, dayTime.terrainThreshold);

        // 角色光照数据（主光/背光/环境/边缘光四色 + 反射 Cubemap），时间段数据未配置时跳过角色光部分
        MCreatureLightData lightData = dayTime.creatureLightData;
        if (lightData == null)
        {
            return;
        }
        Shader.SetGlobalColor(MainLightColorID, lightData.mainLightColor);
        Shader.SetGlobalColor(BackLightColorID, lightData.backLightColor);
        Shader.SetGlobalColor(AmbientColorID, lightData.ambientColor);
        Shader.SetGlobalColor(RimColorID, lightData.rimColor);

        if (lightData.reflectMap != null)
        {
            RenderSettings.customReflectionTexture = lightData.reflectMap;
        }
    }

    /// <summary>
    /// 写入天气相关通道：静态雾（黄昏段优先）、海拔雾、散射、云影、雾云流动与风。
    /// 源实现在初始化阶段先生成目标值再按 v=1 插值，等价于此处直接赋值。
    /// </summary>
    /// <param name="weather">天气数据，为空时跳过</param>
    private void ApplyWeatherData(MWeatherData weather)
    {
        if (weather == null)
        {
            return;
        }

        worldSize = weather.worldSize;

        // 是否改用黄昏分支的雾参数：只有时间段是黄昏且天气数据开启了昼夜雾才为真
        bool useDuskFog = false;
        if (defaultDayTimeData != null && defaultDayTimeData.daytimeType == MDaytimeType.Dusk && weather.enableDayTimeFog)
        {
            useDuskFog = true;
        }

        // 雾色，rgb 为远景雾色；默认取白天段，黄昏分支会换成 fogColorDusk
        Color fogColor = weather.fogColor;
        // 雾混合起始色，rgb 为近景混合色；黄昏分支会换成 fogBlendColorDusk
        Color fogBlendColor = weather.fogBlendColor;
        // 雾混合开始距离，单位：米，小于该距离的像素按 fogBlendColor 混合
        float blendStart = weather.fogBlendStart;
        // 雾混合结束距离，单位：米，超过该距离的像素完全等于 fogColor
        float blendEnd = weather.fogBlendEnd;
        if (useDuskFog)
        {
            fogColor = weather.fogColorDusk;
            fogBlendColor = weather.fogBlendColorDusk;
            blendStart = weather.fogBlendStartDusk;
            blendEnd = weather.fogBlendEndDusk;
        }

        RenderSettings.fogColor = fogColor;

        // 写入 _ROCFogStartColor 的合成值：rgb 保留混合色、a 塞入开始距离
        Color fogStartColor = fogBlendColor;
        fogStartColor.a = blendStart;
        // 写入 _ROCFogEndColor 的合成值：rgb 保留雾色、a 塞入结束距离
        Color fogEndColor = fogColor;
        fogEndColor.a = blendEnd;
        Shader.SetGlobalColor(FogStartColorID, fogStartColor);
        Shader.SetGlobalColor(FogEndColorID, fogEndColor);

        // 海拔雾起始高度（米）小于等于 0 表示不启用海拔雾，此时衰减系数按 1 处理避免除零
        bool muteHeightFog = weather.hightFogStart <= 0f;
        // 海拔雾起始高度的倒数，作为 shader 内的海拔衰减系数；不使用海拔雾时为 1
        float rcpHeightFog = 1f;
        if (!muteHeightFog)
        {
            rcpHeightFog = 1f / weather.hightFogStart;
        }
        // 相机雾是否叠加到海拔雾上，shader 用 0/1 开关量表示
        float camFogAffect = 0f;
        if (weather.camFogAffectHightFog)
        {
            camFogAffect = 1f;
        }
        // 海拔雾启用开关量：不启用时为 0，shader 据此整体屏蔽海拔雾
        float heightFogEnable = 1f;
        if (muteHeightFog)
        {
            heightFogEnable = 0f;
        }
        Shader.SetGlobalVector(HeightFogID, new Vector4(rcpHeightFog, weather.hightFogExp, camFogAffect, heightFogEnable));

        Shader.SetGlobalVector(CamFogID, new Vector4(weather.scatterStart, weather.mieG, weather.rayleighDensity, weather.mieDensity));

        // Mie 系数的平方，用于下面换算相位函数常量（避免 shader 内重复乘）
        float mieG2 = weather.mieG * weather.mieG;
        Shader.SetGlobalVector(MieGID, new Vector3((1f - mieG2) / (2f + mieG2), 1f + mieG2, weather.mieG * 2f));

        Shader.SetGlobalColor(CloudMaskColorID, weather.cloudMaskColor);

        // 雾与云的流动速度合成值：xy 为雾速、zw 为云速，单位：UV/秒
        Vector4 mapFlow = new Vector4(weather.fogSpeed.x, weather.fogSpeed.y, weather.cloudSpeed.x, weather.cloudSpeed.y);
        // 风向量：把 (俯角, 方位角) 转成朝向向量后乘以强度，强度为 0 时得到零向量
        Vector3 windDir = Quaternion.Euler(weather.windDirection) * Vector3.forward;
        windDir = windDir * weather.windIntensity;
        Shader.SetGlobalVector(WindParamsID, new Vector4(windDir.x, windDir.y, windDir.z, weather.windFrequency));

        Shader.SetGlobalFloat(WorldSizeID, 1f / worldSize);
        Shader.SetGlobalVector(MapFlowID, mapFlow);

        ApplyFogST();
        ApplyCloudST();
    }

    /// <summary>
    /// 写入场景色偏。源实现按天气的 keepSceneColor 决定取天气色还是时间段色，此处等价合并处理。
    /// </summary>
    private void ApplySceneColor()
    {
        // 最终写入 _ROCSceneColor 的色偏值，默认中灰表示不改变原色
        Color sceneColor = Color.gray;
        if (defaultDayTimeData != null)
        {
            sceneColor = defaultDayTimeData.sceneColor;
        }
        if (defaultWeatherData != null && defaultWeatherData.keepSceneColor)
        {
            sceneColor = defaultWeatherData.sceneColor;
        }
        Shader.SetGlobalColor(SceneColorID, sceneColor);
    }

    /// <summary>
    /// 把雾的 UV 基准按世界尺寸换算后写入全局变量：
    /// x/y 取倒数，偏移乘缩放，最后整体除以世界尺寸。
    /// </summary>
    public void ApplyFogST()
    {
        // 由序列化基准值派生出的换算结果，改写它不影响 Inspector 里保存的 fogST
        Vector4 fogSTBase = fogST;
        fogSTBase.x = 1f / fogSTBase.x;
        fogSTBase.y = 1f / fogSTBase.y;
        fogSTBase.z *= fogSTBase.x;
        fogSTBase.w *= fogSTBase.y;
        fogSTBase = fogSTBase / worldSize;
        Shader.SetGlobalVector(FogSTID, fogSTBase);
    }

    /// <summary>
    /// 把云的 UV 基准按世界尺寸换算后写入全局变量，换算规则与雾一致。
    /// </summary>
    public void ApplyCloudST()
    {
        // 由序列化基准值派生出的换算结果，改写它不影响 Inspector 里保存的 cloudST
        Vector4 cloudSTBase = cloudST;
        cloudSTBase.x = 1f / cloudSTBase.x;
        cloudSTBase.y = 1f / cloudSTBase.y;
        cloudSTBase.z *= cloudSTBase.x;
        cloudSTBase.w *= cloudSTBase.y;
        cloudSTBase = cloudSTBase / worldSize;
        Shader.SetGlobalVector(CloudSTID, cloudSTBase);
    }

    /// <summary>
    /// 记录本组件写入的 27 个全局通道原值（16 个 Color、9 个 Vector、2 个 Float）。
    /// 只在首次应用时调用一次：若每次应用都重取，快照会被上一次写入值污染，停用就还原不到真正的前一状态。
    /// 原值本来就可能是 0（这些通道在编辑器里无人设置过），按实捕获，不做兜底值替换。
    /// </summary>
    private void CaptureGlobalShaderState()
    {
        // Color 通道数量，决定 _prevColors 长度（当前 16）
        int colorCount = ColorChannelIDs.Length;
        _prevColors = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            // 逐个 Color 通道取当前全局原值，下标与 ColorChannelIDs 严格对应
            _prevColors[i] = Shader.GetGlobalColor(ColorChannelIDs[i]);
        }

        // Vector 通道数量，决定 _prevVectors 长度（当前 9）
        int vectorCount = VectorChannelIDs.Length;
        _prevVectors = new Vector4[vectorCount];
        for (int i = 0; i < vectorCount; i++)
        {
            // 逐个 Vector 通道取当前全局原值，下标与 VectorChannelIDs 严格对应
            _prevVectors[i] = Shader.GetGlobalVector(VectorChannelIDs[i]);
        }

        // Float 通道数量，决定 _prevFloats 长度（当前 2）
        int floatCount = FloatChannelIDs.Length;
        _prevFloats = new float[floatCount];
        for (int i = 0; i < floatCount; i++)
        {
            // 逐个 Float 通道取当前全局原值，下标与 FloatChannelIDs 严格对应
            _prevFloats[i] = Shader.GetGlobalFloat(FloatChannelIDs[i]);
        }
    }

    /// <summary>
    /// 把 27 个全局通道逐项写回首次应用前的值。按 Color/Vector/Float 三种类型分别还原，
    /// 不能统一用 SetGlobalColor 代写，否则 Float 通道会被写成四维向量、a 通道语义错位。
    /// 快照未建立（长度为 0）时整体跳过，避免把通道写成默认零值。
    /// </summary>
    private void RestoreGlobalShaderState()
    {
        if (_prevColors.Length == ColorChannelIDs.Length)
        {
            for (int i = 0; i < ColorChannelIDs.Length; i++)
            {
                // 把第 i 个 Color 通道写回捕获时的原值
                Shader.SetGlobalColor(ColorChannelIDs[i], _prevColors[i]);
            }
        }

        if (_prevVectors.Length == VectorChannelIDs.Length)
        {
            for (int i = 0; i < VectorChannelIDs.Length; i++)
            {
                // 把第 i 个 Vector 通道写回捕获时的原值（含 w 分量，避免方向向量的 w 残留）
                Shader.SetGlobalVector(VectorChannelIDs[i], _prevVectors[i]);
            }
        }

        if (_prevFloats.Length == FloatChannelIDs.Length)
        {
            for (int i = 0; i < FloatChannelIDs.Length; i++)
            {
                // 把第 i 个 Float 通道写回捕获时的原值
                Shader.SetGlobalFloat(FloatChannelIDs[i], _prevFloats[i]);
            }
        }
    }
}
