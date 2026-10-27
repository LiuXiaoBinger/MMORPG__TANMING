using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// RO 环境设置——角色光照链（从源工程 MoonClient/Effect/Scene/MEnvorimentSetting.cs 适配迁入，源文件 1446 行）。
/// 保留的职责（与源实现同名同序）：
///   1) GetShaderID：缓存角色光/方向/场景色等 Shader 全局量 ID（源 :319-371）；
///   2) StoreDayTimeData：把本次时段数据存成"上一次"，供插值起点使用（源 :409-432）；
///   3) ApplyDayTimeDataLerp：按 v 写入两个方向 + 角色四色 + 场景色（源 :908-936）；
///   4) InitOrChangeEnviromentSetting：时段切换主流程，含 2 秒插值分支与立即应用分支（源 :454-704 的 dayTime 段）；
///   5) ApplyEnvorimentData：外部入口，先 KillLerping 再走 InitOrChangeEnviromentSetting（源 :965-1021）；
///   6) ApplyWorldAmbient：用 MAmbientData 设置 RenderSettings 环境光（源 :739-752），是角色 Shader 里 ShadeSH9 的上游。
/// 依赖：DG.Tweening（DOTween），直接引用源工程自带的 Assets/Demigiant/DOTween/DOTween.dll
/// （本 Demo 复制到 Assets/Plugins/DOTween/，程序集本体与原 .meta 均按原样保留），
/// 插值调用与源 :481 / :494 一致：DOTween.To(setter, 0, 1, 2).OnComplete(...)，Kill 用 Tweener.Kill()。
/// 裁掉的职责（天气/雾/闪电/合批/光照图/后处理/事件/框架单例）在 README「迁入边界」与
/// 项目分析/RO角色光照完整迁入与调亮流程-2026-10-28.md 里逐项列了源行号与原因。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("RO/环境设置（角色光照链）")]
public class MEnvorimentSetting : MonoBehaviour
{
    /// <summary>源实现的场景环境数据目录常量，保留作为来源标注；本 Demo 不通过 Resources 加载资产。</summary>
    public static readonly string envorimentDataPath = "Assets/Resources/SceneEnviroData/";

    /// <summary>默认时段数据：OnEnable 时按它做一次立即应用（源实现 defaultDayTimeData，由场景预制体指定）。</summary>
    public MDayTimeData defaultDayTimeData;

    /// <summary>默认世界环境数据：ApplyWorldAmbient 用它设置环境模式/环境色/强度/反射模式（源实现 defaultAmbientData）。</summary>
    public MAmbientData defaultAmbientData;

    /// <summary>是否由相机天空盒接管背景；源实现默认 true，为 true 时 ApplyCamSetting 不改相机背景色。</summary>
    public bool camSkybox = true;

    /// <summary>最近一次应用过的时段数据（源实现属性名 _cacheDayTimeData，供外部查询"当前是哪个时段配置"）。</summary>
    public MDayTimeData _cacheDayTimeData { get; private set; }

    /// <summary>上一次生效的主光颜色（插值起点），源 :123。</summary>
    private Color _mainLightColor;

    /// <summary>上一次生效的背光颜色（插值起点），源 :124。</summary>
    private Color _backLightColor;

    /// <summary>上一次生效的环境颜色（插值起点），源 :125。</summary>
    private Color _ambientColor;

    /// <summary>上一次生效的边缘光颜色（插值起点），源 :126。</summary>
    private Color _rimColor;

    /// <summary>当前时段配置的反射立方图，来自 MCreatureLightData.reflectMap，源 :127、:470。</summary>
    private Cubemap _reflection;

    /// <summary>当前时段类型（白天/夜晚/黄昏/黎明），源 :133。</summary>
    public MEnvorimentData.DaytimeType _defaultDaytimeType { get; private set; }

    /// <summary>ApplyCamSetting 用到的相机缓存，源实现取 MScene.singleton.GameCamera.UCam，这里取 Camera.main。</summary>
    private Camera _camera;

    /// <summary>上一次生效的主光世界方向（插值起点，单位向量），源 :142。</summary>
    private Vector3 _lightDir;

    /// <summary>本次目标主光世界方向（插值终点，单位向量），源 :143。</summary>
    private Vector3 _lightDirLerp;

    /// <summary>上一次生效的可见光世界方向（插值起点），源 :144。</summary>
    private Vector3 _lightDir2;

    /// <summary>本次目标可见光世界方向（插值终点），源 :145。</summary>
    private Vector3 _lightDir2Lerp;

    /// <summary>当前生效的场景色偏（_ROCSceneColor 的值），源 :148。</summary>
    private Color _sceneColor;

    /// <summary>本时段数据里的场景色，切换天气时作为回退目标，源 :149。</summary>
    private Color _daySceneColor;

    /// <summary>上一次生效的阴影色（写 _ROCShadowColor），源 :150。</summary>
    private Color _shadowColor;

    /// <summary>上一次生效的云环境色（写 _ROCloudEnvorimentColor），源 :151。</summary>
    private Color _cloudEnvorimentColor;

    /// <summary>上一次生效的草环境色（写 _ROGrassEnvorimentColor），源 :152。</summary>
    private Color _grassEnvorimentColor;

    /// <summary>上一次生效的草暗部环境色（写 _ROGrassEnvorimentDarkColor），源 :153。</summary>
    private Color _grassEnvorimentDarkColor;

    /// <summary>上一次生效的树环境色（写 _ROTreeEnvorimentColor），源 :154。</summary>
    private Color _treeEnvorimentColor;

    /// <summary>上一次生效的暖阳色（写 _ROCSunColor），源 :155。</summary>
    private Color _sunColor;

    /// <summary>上一次生效的地形烘焙亮色（写 _ROTerrainLightColor），源 :200。</summary>
    private Color _terrainLightColor;

    /// <summary>上一次生效的地形烘焙暗色（写 _ROTerrainDarkColor），源 :201。</summary>
    private Color _terrainDarkColor;

    /// <summary>上一次生效的地形明暗阈值（写 _ROTerrainThreshold），源 :202。</summary>
    private float _terrainThreshold;

    /// <summary>时段插值句柄（与源 :115 同类型 DG.Tweening.Tweener，来自源工程 DOTween 程序集），切换时可 Kill。</summary>
    private Tweener _dayTimeLerping;

    /// <summary>场景色插值句柄（与源 :117 同类型 DG.Tweening.Tweener）。</summary>
    private Tweener _sceneColorLerping;

    /// <summary>当前主光方向（Inspector 可读，度），源 :209。</summary>
    public Vector3 mainLightDir { get; private set; }

    /// <summary>当前阴影色（Inspector 可读），源 :210。</summary>
    public Color shadowColor { get; private set; }

    /// <summary>主光颜色全局量 ID：_ROCLightColor0（rgb=直射颜色，a=主光强度系数），源 :335。</summary>
    private static int _MainLightColorID;

    /// <summary>背光颜色全局量 ID：_ROCBackColor（rgb=背光色，a=混合权重），源 :336。</summary>
    private static int _BackLightColorID;

    /// <summary>环境颜色全局量 ID：_ROCAmbientColor（rgb=替换色，a=替换 SH 的比例），源 :337。</summary>
    private static int _AmbientColorID;

    /// <summary>边缘光颜色全局量 ID：_ROCRimColor，源 :338。</summary>
    private static int _RimColorID;

    /// <summary>云环境色全局量 ID：_ROCloudEnvorimentColor，源 :339。</summary>
    private static int _CloudEnvorimentColorID;

    /// <summary>草环境色全局量 ID：_ROGrassEnvorimentColor，源 :340。</summary>
    private static int _GrassEnvorimentColorID;

    /// <summary>草暗部环境色全局量 ID：_ROGrassEnvorimentDarkColor，源 :341。</summary>
    private static int _GrassEnvorimentDarkColorID;

    /// <summary>树环境色全局量 ID：_ROTreeEnvorimentColor，源 :342。</summary>
    private static int _TreeEnvorimentColorID;

    /// <summary>主光方向全局量 ID：_ROCLightDir0（世界空间单位向量），源 :343。</summary>
    private static int _MainLightDirID;

    /// <summary>可见光方向全局量 ID：_ROCLightDir1，源 :344。</summary>
    private static int _VisLightDirID;

    /// <summary>场景色偏全局量 ID：_ROCSceneColor，源 :345。</summary>
    private static int _SceneColorID;

    /// <summary>阴影色全局量 ID：_ROCShadowColor，源 :346。</summary>
    private static int _SceneShadowID;

    /// <summary>暖阳色全局量 ID：_ROCSunColor，源 :347。</summary>
    private static int _SunColorID;

    /// <summary>地形烘焙亮色全局量 ID：_ROTerrainLightColor，源 :363。</summary>
    private static int _TerrainLightColorID;

    /// <summary>地形烘焙暗色全局量 ID：_ROTerrainDarkColor，源 :364。</summary>
    private static int _TerrainDarkColorID;

    /// <summary>地形明暗阈值全局量 ID：_ROTerrainThreshold，源 :365。</summary>
    private static int _TerrainThresholdID;

    /// <summary>组件启用：缓存 ID → 应用默认环境 → 应用默认时段（源 OnEnable :275-282，去掉了框架单例与事件注册）。</summary>
    private void OnEnable()
    {
        GetShaderID();
        SetEnviromentSettingDataToDefault();
    }

    /// <summary>应用默认数据（源 :284-288 去掉光照图一段）。</summary>
    private void SetEnviromentSettingDataToDefault()
    {
        // 环境光/反射模式必须先于时段应用，否则角色 ShadeSH9 项会用到场景残留设置
        ApplyWorldAmbient();
        SetDayTimeAndWeatherToDefault();
    }

    /// <summary>按默认时段数据立即应用一次（源 :297-300，源签名带 weather 参数，天气链未迁入故去掉）。</summary>
    private void SetDayTimeAndWeatherToDefault()
    {
        InitOrChangeEnviromentSetting(defaultDayTimeData, false, true);
    }

    /// <summary>组件销毁：停插值并把全局量复位，避免下一个场景读到本场景的残留值（源 OnDestroy :309-313）。</summary>
    private void OnDestroy()
    {
        KillLerping();
        ResetGlobalShaderVariable();
    }

    /// <summary>缓存 Shader 全局量 ID（源 GetShaderID :319-371，只保留角色光照链会写的那些）。</summary>
    private void GetShaderID()
    {
        _MainLightColorID = Shader.PropertyToID("_ROCLightColor0");
        _BackLightColorID = Shader.PropertyToID("_ROCBackColor");
        _AmbientColorID = Shader.PropertyToID("_ROCAmbientColor");
        _RimColorID = Shader.PropertyToID("_ROCRimColor");
        _CloudEnvorimentColorID = Shader.PropertyToID("_ROCloudEnvorimentColor");
        _GrassEnvorimentColorID = Shader.PropertyToID("_ROGrassEnvorimentColor");
        _GrassEnvorimentDarkColorID = Shader.PropertyToID("_ROGrassEnvorimentDarkColor");
        _TreeEnvorimentColorID = Shader.PropertyToID("_ROTreeEnvorimentColor");
        _MainLightDirID = Shader.PropertyToID("_ROCLightDir0");
        _VisLightDirID = Shader.PropertyToID("_ROCLightDir1");
        _SceneColorID = Shader.PropertyToID("_ROCSceneColor");
        _SceneShadowID = Shader.PropertyToID("_ROCShadowColor");
        _SunColorID = Shader.PropertyToID("_ROCSunColor");
        _TerrainLightColorID = Shader.PropertyToID("_ROTerrainLightColor");
        _TerrainDarkColorID = Shader.PropertyToID("_ROTerrainDarkColor");
        _TerrainThresholdID = Shader.PropertyToID("_ROTerrainThreshold");
    }

    /// <summary>
    /// 保存"上一次"的时段数据，作为下一次插值的起点（源 StoreDayTimeData :409-432，逐行对应）。
    /// </summary>
    /// <param name="dayTime">本次要生效的时段数据，非空。</param>
    /// <param name="lightData">本次要生效的角色光数据，允许为空（为空时四色缓存保持不变）。</param>
    private void StoreDayTimeData(MDayTimeData dayTime, MCreatureLightData lightData)
    {
        // 方向：把本次目标方向存成下一次的起点
        _lightDir = _lightDirLerp;
        _lightDir2 = _lightDir2Lerp;

        // 场景侧颜色（给场景 Shader 用，角色 Shader 不读）
        _shadowColor = dayTime.shadowColor;
        _cloudEnvorimentColor = dayTime.cloudEnvorimentColor;
        _grassEnvorimentColor = dayTime.grassEnvorimentColor;
        _grassEnvorimentDarkColor = dayTime.grassEnvorimentDarkColor;
        _treeEnvorimentColor = dayTime.treeEnvorimentColor;
        _sunColor = dayTime.sunColor;

        // 地形烘焙叠加参数
        _terrainLightColor = dayTime.terrainLightColor;
        _terrainDarkColor = dayTime.terrainDarkColor;
        _terrainThreshold = dayTime.terrainThreshold;

        // 角色四色是插值起点，缺角色光数据时保持旧值（与源实现一致）
        if (lightData)
        {
            _mainLightColor = lightData.mainLightColor;
            _backLightColor = lightData.backLightColor;
            _ambientColor = lightData.ambientColor;
            _rimColor = lightData.rimColor;
        }
    }

    /// <summary>
    /// 时段切换主流程（源 InitOrChangeEnviromentSetting :454-704 的 dayTime 分支，逐段对应）。
    /// </summary>
    /// <param name="dayTime">目标时段数据；为空时只把地形叠加色复位成白色（源实现的 editor 分支）。</param>
    /// <param name="forLerping">true 且 fast=false 时走 2 秒插值，否则立即以 v=1 应用。</param>
    /// <param name="fast">true 表示立即应用（源实现切场景时用 fast=true）。</param>
    public void InitOrChangeEnviromentSetting(MDayTimeData dayTime, bool forLerping = false, bool fast = true)
    {
        // 编辑器态也保证 ID 已缓存（源实现用 MGameContext.singleton.IsGameEditorMode 判断，这里恒执行且幂等）
        GetShaderID();

        if (dayTime)
        {
            _defaultDaytimeType = dayTime.daytimeType;

            // 反射来源：角色光的反射立方图（可能为空）
            if (dayTime.creatureLightData)
            {
                _reflection = dayTime.creatureLightData.reflectMap;
            }
            else
            {
                _reflection = null;
            }

            mainLightDir = dayTime.mainLightDir;
            shadowColor = dayTime.shadowColor;

            // 方向换算：绕 X 取俯角、绕 Y 取方位角，默认朝 -Z，与源实现完全一致
            _lightDirLerp = Quaternion.Euler(dayTime.mainLightDir) * Vector3.back;
            _lightDir2Lerp = Quaternion.Euler(dayTime.visLightDir) * Vector3.back;

            if (forLerping && !fast)
            {
                // 过渡的时段变化：2 秒内把 v 从 0 推到 1，每帧重写全局量（源 :481 原样调用）
                _dayTimeLerping = DOTween.To(delegate (float v)
                {
                    if (dayTime)
                    {
                        ApplyDayTimeDataLerp(dayTime, dayTime.creatureLightData, v);
                    }
                }, 0f, 1f, 2f)
                .OnComplete(delegate
                {
                    if (dayTime)
                    {
                        StoreDayTimeData(dayTime, dayTime.creatureLightData);
                    }
                });
                // 源实现此处包在 if (!_keepSceneColor) 里，而 _keepSceneColor 唯一写入点是天气分支（源 :461），天气链未迁入 → 恒成立
                _sceneColorLerping = DOTween.To(delegate (float v)
                {
                    if (dayTime)
                    {
                        Shader.SetGlobalColor(_SceneColorID, Color.Lerp(_sceneColor, dayTime.sceneColor, v));
                    }
                }, 0f, 1f, 2f)
                .OnComplete(delegate
                {
                    if (dayTime)
                    {
                        _sceneColor = dayTime.sceneColor;
                    }
                });
            }
            else
            {
                // 立即应用：v=1 等价于直接写目标值，同时把目标存成下一次的起点
                ApplyDayTimeDataLerp(dayTime, dayTime.creatureLightData, 1f);
                StoreDayTimeData(dayTime, dayTime.creatureLightData);
                // 同上：源实现的 !_keepSceneColor 条件恒成立
                _sceneColor = dayTime.sceneColor;
                Shader.SetGlobalColor(_SceneColorID, _sceneColor);
            }
            _daySceneColor = dayTime.sceneColor;

            // 反射来源写入场景：源实现是 IsQualitySupport ? _reflection : null（画质分级属 RO 全局设置，Demo 无分级，视为达标）。
            // 源写法 RenderSettings.customReflection 在 2023 已废弃，等价属性是 customReflectionTexture（类型同为 Cubemap）。
            RenderSettings.customReflectionTexture = _reflection;

            ApplyCamSetting(dayTime);

            // 源实现此处还有 ApplyPostProcess(dayTime) 与 FireUpdateEvent()：后处理链与事件框架未迁入
        }
        else
        {
            // 源实现的分支条件是 MGameContext.singleton.IsUnityEditorOrPC，Demo 恒为编辑器/PC，等价于条件成立
            Shader.SetGlobalColor(_TerrainLightColorID, Color.white);
            Shader.SetGlobalColor(_TerrainDarkColorID, Color.white);
            Shader.SetGlobalFloat(_TerrainThresholdID, 0.25f);
        }

        // 源实现后面还有 weather 分支（世界尺寸/雾/云/闪电/风/天空球）与 ObjectVisibility()，均属天气链，未迁入
    }

    /// <summary>
    /// 相机背景色设置（源 ApplyCamSetting :706-722）。camSkybox 为 true 时不改背景（默认场景用天空盒）。
    /// </summary>
    /// <param name="dayTime">提供 camSkyboxColor 的时段数据。</param>
    public void ApplyCamSetting(MDayTimeData dayTime)
    {
        // 源实现取 MScene.singleton.GameCamera.UCam，Demo 没有该单例，取主相机；取不到时保持不做事
        if (!_camera)
        {
            _camera = Camera.main;
        }

        if (_camera && !camSkybox)
        {
            _camera.backgroundColor = dayTime.camSkyboxColor;
        }
    }

    /// <summary>
    /// 用 MAmbientData 设置世界环境光与反射模式（源 ApplyWorldAmbient :739-752，逐行对应）。
    /// 这是角色 Shader 里 SH 环境项（CreatureCommon.cginc:552、:560）的上游来源。
    /// </summary>
    public void ApplyWorldAmbient()
    {
        if (defaultAmbientData)
        {
            RenderSettings.ambientMode = defaultAmbientData.ambientMode;
            RenderSettings.ambientLight = defaultAmbientData.ambientSkyColor;
            RenderSettings.ambientSkyColor = defaultAmbientData.ambientSkyColor;
            RenderSettings.ambientEquatorColor = defaultAmbientData.ambientEquatorColor;
            RenderSettings.ambientGroundColor = defaultAmbientData.ambientGroundColor;
            RenderSettings.skybox = defaultAmbientData.skybox;
            RenderSettings.ambientIntensity = defaultAmbientData.ambientIntensity;
            RenderSettings.defaultReflectionMode = defaultAmbientData.reflectionMode;
        }
    }

    /// <summary>
    /// 按时段插值比例 v 写入全局量（源 ApplyDayTimeDataLerp :908-936，逐行对应）。
    /// v=1 时等同于直接写目标值；四路角色色（直射/背光/环境/边缘）就在这个函数里生效。
    /// </summary>
    /// <param name="dayTime">目标时段数据。</param>
    /// <param name="lightData">目标角色光数据，允许为空（为空时跳过角色四色，与源实现一致）。</param>
    /// <param name="v">插值比例 0~1。</param>
    private void ApplyDayTimeDataLerp(MDayTimeData dayTime, MCreatureLightData lightData, float v)
    {
        // 方向与场景侧颜色
        Shader.SetGlobalVector(_MainLightDirID, Vector3.Slerp(_lightDir, _lightDirLerp, v));
        Shader.SetGlobalVector(_VisLightDirID, Vector3.Slerp(_lightDir2, _lightDir2Lerp, v));
        Shader.SetGlobalColor(_SceneShadowID, Color.Lerp(_shadowColor, dayTime.shadowColor, v));
        Shader.SetGlobalColor(_SunColorID, Color.Lerp(_sunColor, dayTime.sunColor, v));
        Shader.SetGlobalColor(_CloudEnvorimentColorID, Color.Lerp(_cloudEnvorimentColor, dayTime.cloudEnvorimentColor, v));
        Shader.SetGlobalColor(_GrassEnvorimentColorID, Color.Lerp(_grassEnvorimentColor, dayTime.grassEnvorimentColor, v));
        Shader.SetGlobalColor(_GrassEnvorimentDarkColorID, Color.Lerp(_grassEnvorimentDarkColor, dayTime.grassEnvorimentDarkColor, v));
        Shader.SetGlobalColor(_TreeEnvorimentColorID, Color.Lerp(_treeEnvorimentColor, dayTime.treeEnvorimentColor, v));

        // 地形烘焙叠加
        Shader.SetGlobalColor(_TerrainLightColorID, Color.Lerp(_terrainLightColor, dayTime.terrainLightColor, v));
        Shader.SetGlobalColor(_TerrainDarkColorID, Color.Lerp(_terrainDarkColor, dayTime.terrainDarkColor, v));
        Shader.SetGlobalFloat(_TerrainThresholdID, Mathf.Lerp(_terrainThreshold, dayTime.terrainThreshold, v));

        // 角色光变化：四路颜色，这是角色模型表面亮暗/色调的真正来源
        if (lightData)
        {
            Shader.SetGlobalColor(_MainLightColorID, Color.Lerp(_mainLightColor, lightData.mainLightColor, v));
            Shader.SetGlobalColor(_BackLightColorID, Color.Lerp(_backLightColor, lightData.backLightColor, v));
            Shader.SetGlobalColor(_AmbientColorID, Color.Lerp(_ambientColor, lightData.ambientColor, v));
            Shader.SetGlobalColor(_RimColorID, Color.Lerp(_rimColor, lightData.rimColor, v));
        }
    }

    /// <summary>
    /// 外部切换环境数据入口（源 ApplyEnvorimentData :965-1021，去掉光照图过渡一段）。
    /// 调用顺序与源一致：先 KillLerping 打断上一次插值，再按 fast 决定立即应用还是 2 秒插值。
    /// </summary>
    /// <param name="data">环境数据组，只需 dayTimeData 字段。</param>
    /// <param name="fast">true 立即应用；false 走 2 秒插值。</param>
    public void ApplyEnvorimentData(MEnvorimentData data, bool fast = false)
    {
        KillLerping();

        GetShaderID();

        MDayTimeData dayTime = data.dayTimeData;
        _cacheDayTimeData = data.dayTimeData;

        // 源实现此处还有 lightmap 过渡（ApplyLightmapData / LightmapLerpToDay*），光照图链未迁入

        InitOrChangeEnviromentSetting(dayTime, true, fast);
    }

    /// <summary>停掉所有插值（源 KillLerping :1315-1353，只保留时段/场景色两个句柄）。</summary>
    public void KillLerping()
    {
        if (_dayTimeLerping != null)
        {
            _dayTimeLerping.Kill();
            _dayTimeLerping = null;
        }

        if (_sceneColorLerping != null)
        {
            _sceneColorLerping.Kill();
            _sceneColorLerping = null;
        }
    }

    /// <summary>
    /// 全局量复位（源 ResetGlobalShaderVariable :1286-1299，保留角色四色/方向/场景色，去掉雾与场景图那几项）。
    /// 数值沿用源码里的常量，便于对照。
    /// </summary>
    private void ResetGlobalShaderVariable()
    {
        GetShaderID();
        Shader.SetGlobalVector(_MainLightDirID, new Vector3(0f, 1f, 0f));
        Shader.SetGlobalVector(_VisLightDirID, new Vector3(0f, 1f, 0f));
        Shader.SetGlobalColor(_MainLightColorID, new Color32(163, 155, 140, 255));
        Shader.SetGlobalColor(_BackLightColorID, new Color32(218, 197, 232, 255));
        Shader.SetGlobalColor(_AmbientColorID, new Color32(120, 120, 120, 255));
        Shader.SetGlobalColor(_SceneColorID, Color.gray);
    }
}
