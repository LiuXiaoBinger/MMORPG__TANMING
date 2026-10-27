using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 完整角色光照链的验证运行器（2026-10-28 新增，第 2 轮返修：产物统一带 -r2 后缀，不覆盖第一轮证据）。
/// 只做测量与取证，不改光照逻辑：所有变体都是改 RO 原版字段/原版资产，再走 MEnvorimentSetting 原版调用链应用。
/// 输出统一写到仓库根 _temp/ro-character-full-*（本工程目录内不留运行产物，也不覆盖旧的 ro-character-demo-* 证据）。
/// 编辑态（RunEditMode）：三档时段 / 方向 / 材质参数 / 反射 A/B / ambientIntensity 对照 / 恢复一致性，
/// 插值在编辑态不做（无运行时循环，不手动推进也不假装自然播放），只在 Play 模式按真实经过时长采样。
/// Play 态（RunPlayMode）：按真实经过时长采样原版 2 秒插值的自然推进，再用立即应用做终点比对。
/// </summary>
public static class RODemoFullLightingRunner
{
    /// <summary>演示场景路径（与 RODemoSceneBuilder 一致）。</summary>
    private const string ScenePath = "Assets/Scenes/ROCharacterLightingDemo.unity";

    /// <summary>本轮（第 2 轮返修）所有新增产物的后缀，保证不覆盖第一轮已验收的截图与 JSON。</summary>
    private const string CaseSuffix = "-r3";

    /// <summary>截图输出目录（仓库 _temp，文件名带 -r2 后缀，不覆盖第一轮证据）。</summary>
    private const string CaptureDir = "D:/unitypro/LYFMMORGP/_temp/ro-character-full-captures";

    /// <summary>测量结果 JSON 路径（r2）。</summary>
    private const string ResultPath = "D:/unitypro/LYFMMORGP/_temp/ro-character-full-verify-r3.json";

    /// <summary>进度/状态文件，外部（MCP）据此判断流程是否跑完（r2）。</summary>
    private const string ProgressPath = "D:/unitypro/LYFMMORGP/_temp/ro-character-full-verify-r3-progress.txt";

    /// <summary>截图宽（像素），与旧批次一致便于对照。</summary>
    private const int CaptureWidth = 1024;

    /// <summary>截图高（像素）。</summary>
    private const int CaptureHeight = 768;

    /// <summary>角色遮罩阈值：任一变体里任一通道大于该值（0~255）就算角色像素。</summary>
    private const int MaskChannelThreshold = 8;

    /// <summary>已采集的变体：名字、截图贴图、说明。</summary>
    private static readonly List<CaptureRecord> Captures = new List<CaptureRecord>();

    /// <summary>当前场景里的环境组件。</summary>
    private static MEnvorimentSetting _environment;

    /// <summary>当前场景主相机。</summary>
    private static Camera _camera;

    /// <summary>角色静态渲染器（关掉它之前的启用状态，用于恢复）。</summary>
    private static Renderer _characterRenderer;

    /// <summary>角色材质（材质参数用例会临时改它，测完恢复）。</summary>
    private static Material _characterMaterial;

    /// <summary>地面渲染器（取证时关掉，只留角色）。</summary>
    private static Renderer _groundRenderer;

    /// <summary>取证前的相机清除方式，跑完要还原，避免编辑器里的场景停在纯黑背景。</summary>
    private static CameraClearFlags _originalClearFlags;

    /// <summary>取证前的相机背景色，跑完要还原。</summary>
    private static Color _originalBackground;

    /// <summary>三套时段资产，顺序 0=白天、1=黄昏、2=夜晚。</summary>
    private static MDayTimeData[] _dayTimes;

    /// <summary>本轮读到的反射立方图摘要（名字/尺寸/格式），写进结果 JSON 证明反射资源真的接上了。</summary>
    private static string _reflectionInfo = "reflectMap=null";

    /// <summary>一次采集的记录：名字 + 像素 + 说明。</summary>
    private class CaptureRecord
    {
        /// <summary>用例名，同时做截图文件名片段。</summary>
        public string name;

        /// <summary>渲染结果像素（RGB24，行优先，左下起）。</summary>
        public Color32[] pixels;

        /// <summary>给人看的说明（这条用例改了什么）。</summary>
        public string note;

        /// <summary>该用例应用后读回的 Shader 全局量摘要。</summary>
        public string globals;
    }

    /// <summary>入口：完整跑一遍验证（同步段 + 插值段），跑完写 JSON 与进度文件。</summary>
    [MenuItem("RO/运行完整光照验证（编辑态）")]
    public static void RunEditMode()
    {
        Directory.CreateDirectory(CaptureDir);
        File.WriteAllText(ProgressPath, "start\n", new UTF8Encoding(false));
        Captures.Clear();

        // 确保打开的是演示场景（本流程会重建场景，用户未保存编辑已另存快照）
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 必须用 LOD 600 的完整 SubShader（低档会丢法线/点光分支）
        Shader.globalMaximumLOD = 600;

        _environment = Object.FindFirstObjectByType<MEnvorimentSetting>();
        _camera = Camera.main;
        _characterRenderer = FindCharacterRenderer();
        _groundRenderer = FindGroundRenderer();
        if (_environment == null)
        {
            File.AppendAllText(ProgressPath, "error:no-environment\n", new UTF8Encoding(false));
            return;
        }
        if (_camera == null)
        {
            File.AppendAllText(ProgressPath, "error:no-camera\n", new UTF8Encoding(false));
            return;
        }
        if (_characterRenderer == null)
        {
            File.AppendAllText(ProgressPath, "error:no-character\n", new UTF8Encoding(false));
            return;
        }

        _characterMaterial = _characterRenderer.sharedMaterial;
        _dayTimes = LoadDayTimes();

        // 记录场景/相机/材质/渲染设置的固定条件，证明各档之间只有 RO 参数在变
        AppendFixedConditions();

        // 采集环境准备：纯黑背景 + 关地面，只留角色像素
        _originalClearFlags = _camera.clearFlags;
        _originalBackground = _camera.backgroundColor;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = false;
        }

        // ---- 第 1 组：三档时段（RO 原版资产直接切换） ----
        ApplyDayTime(0, "day-baitian", "白天配置 dt_sc_pulongdela_001_baitian（RO 原版资产）");
        ApplyDayTime(1, "dusk-huanghun", "黄昏配置 dt_sc_pulongdela_001_huanghun（RO 原版资产）");
        ApplyDayTime(2, "night-yewan", "夜晚配置 dt_sc_pulongdela_001_yewan（RO 原版资产）");

        // ---- 第 2 组：方向对照（改主光方向字段，其余不变） ----
        ApplyDayTimeInstant(0);
        Vector2 originalDir = _dayTimes[0].mainLightDir;
        _dayTimes[0].mainLightDir = new Vector2(originalDir.x, originalDir.y + 180f);
        ApplyDayTimeInstant(0);
        CaptureCase("dir-azimuth-plus-180", "同一白天配置，mainLightDir.y（方位角）+180 度：主光转到背侧");
        _dayTimes[0].mainLightDir = new Vector2(-20f, originalDir.y);
        ApplyDayTimeInstant(0);
        CaptureCase("dir-pitch-minus-76", "同一白天配置，mainLightDir.x（俯角）56.3 改 -20 度：光从下方来");
        _dayTimes[0].mainLightDir = originalDir;
        ApplyDayTimeInstant(0);

        // ---- 第 3 组：材质参数（验证 _Brightness 到底有没有消费者） ----
        float originalBrightness = ReadMaterialFloat("_Brightness", 0.9f);
        Vector4 originalFalloff = ReadMaterialVector("_Falloff", new Vector4(-0.1f, 0.35f, 0f, 0f));

        _characterMaterial.SetFloat("_Brightness", 0f);
        CaptureCase("material-Brightness-0", "材质 _Brightness 从运行时默认值 " + originalBrightness.ToString("F3") + " 改 0（源 Shader 声明了该属性，检查是否真有消费者）");
        _characterMaterial.SetFloat("_Brightness", 2f);
        CaptureCase("material-Brightness-2", "材质 _Brightness 从运行时默认值 " + originalBrightness.ToString("F3") + " 改 2（同上，反向）");
        _characterMaterial.SetFloat("_Brightness", originalBrightness);

        _characterMaterial.SetVector("_Falloff", new Vector4(0.55f, 1.0f, 0f, 0f));
        CaptureCase("material-Falloff-narrow", "材质 _Falloff 改为 (0.55,1.0)：明暗分界抬高，只有最正对光的部分发亮");
        _characterMaterial.SetVector("_Falloff", new Vector4(-0.75f, -0.35f, 0f, 0f));
        CaptureCase("material-Falloff-wide", "材质 _Falloff 放宽到 (-0.75,-0.35)：明暗分界压低，角色整体变亮");
        _characterMaterial.SetVector("_Falloff", new Vector4(originalFalloff.x, originalFalloff.y, 0f, 0f));

        // ---- 第 3.5 组：同一色相下的整体明暗（实测：这条路径的亮度由背光色主导，见项目分析文档） ----
        ApplyDayTimeInstant(0);
        MCreatureLightData scaleLight = _dayTimes[0].creatureLightData;
        Color originalBack = scaleLight.backLightColor;

        scaleLight.backLightColor = new Color(originalBack.r * 0.35f, originalBack.g * 0.35f, originalBack.b * 0.35f, originalBack.a);
        ApplyDayTimeInstant(0);
        CaptureCase("light-dark-back-035x", "白天配置 backLightColor rgb 乘 0.35（色相不变，只降亮度）：暗档");

        scaleLight.backLightColor = new Color(originalBack.r * 1.7f, originalBack.g * 1.7f, originalBack.b * 1.7f, originalBack.a);
        ApplyDayTimeInstant(0);
        CaptureCase("light-bright-back-170x", "白天配置 backLightColor rgb 乘 1.7（色相不变，只升亮度）：亮档");

        scaleLight.backLightColor = originalBack;
        ApplyDayTimeInstant(0);

        // 反例：把主光 rgb 直接置黑，整体亮度几乎不变（直射项被 directBack 补偿抵消），说明"调亮"不能靠主光 rgb
        Color originalMainForProbe = scaleLight.mainLightColor;
        scaleLight.mainLightColor = new Color(0f, 0f, 0f, originalMainForProbe.a);
        ApplyDayTimeInstant(0);
        CaptureCase("main-rgb-black", "白天配置 mainLightColor rgb 置黑：验证直射项对整体亮度被 directBack 补偿抵消");
        scaleLight.mainLightColor = originalMainForProbe;
        ApplyDayTimeInstant(0);

        // ---- 第 4 组：环境色替换比例（RO 原版四色之一） ----
        ApplyDayTimeInstant(0);
        MCreatureLightData dayLight = _dayTimes[0].creatureLightData;
        Color originalAmbient = dayLight.ambientColor;
        dayLight.ambientColor = new Color(originalAmbient.r, originalAmbient.g, originalAmbient.b, 0f);
        ApplyDayTimeInstant(0);
        CaptureCase("ambientColor-a-0", "白天配置 ambientColor.a 1 改 0（环境项全部回到球谐 SH）");
        dayLight.ambientColor = originalAmbient;
        ApplyDayTimeInstant(0);

        // ---- 第 4.5 组：反射来源 A/B（源 cl_*.reflectMap 指向 Hdr01 立方图，已按源 guid 复制进本工程） ----
        ApplyDayTimeInstant(0);
        MCreatureLightData reflectionLight = _dayTimes[0].creatureLightData;
        Cubemap sourceReflection = reflectionLight.reflectMap;
        _reflectionInfo = "reflectMap=";
        if (sourceReflection == null)
        {
            _reflectionInfo = _reflectionInfo + "null";
        }
        else
        {
            _reflectionInfo = _reflectionInfo + sourceReflection.name + "(" + sourceReflection.width + "x" + sourceReflection.height + "," + sourceReflection.format + ")";
        }

        reflectionLight.reflectMap = null;
        ApplyDayTimeInstant(0);
        CaptureCase("refl-off", "白天配置把 reflectMap 置空：关闭源反射立方图后的对照");

        reflectionLight.reflectMap = sourceReflection;
        ApplyDayTimeInstant(0);
        CaptureCase("refl-on", "白天配置接回源 cl_*.reflectMap（Hdr01 立方图），经 unity_SpecCube0 → indirectSpecular 进入最终颜色");

        // ---- 第 5 组：恢复默认应与第一张白天图逐像素一致 ----
        ApplyDayTimeInstant(0);
        CaptureCase("restore-day", "恢复白天配置（应与 day-baitian 逐像素一致）");

        // ---- 第 5.5 组：ambientIntensity / ambientSkyColor 语义对照（放在最后，避免影响上面恢复一致性的判定） ----
        MAmbientData ambientData = _environment.defaultAmbientData;
        AmbientMode originalAmbientMode = ambientData.ambientMode;
        Color originalSkyColor = ambientData.ambientSkyColor;
        Color originalEquatorColor = ambientData.ambientEquatorColor;
        Color originalGroundColor = ambientData.ambientGroundColor;
        float originalAmbientIntensity = ambientData.ambientIntensity;

        // r3：默认模式已从自建 Flat 换成源 gi 资产的 Trilight，先按当前真实默认测一组
        ambientData.ambientMode = AmbientMode.Trilight;
        ambientData.ambientIntensity = 1f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-trilight-i100", "Trilight（源 gi 资产默认）+ ambientIntensity=1：本轮真实默认基准");

        ambientData.ambientIntensity = 2.5f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-trilight-i250", "Trilight + ambientIntensity 1→2.5：Trilight 是否也吃这个倍率");

        ambientData.ambientIntensity = 0.25f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-trilight-i025", "Trilight + ambientIntensity 1→0.25：同上，反向");

        ambientData.ambientIntensity = 1f;
        ambientData.ambientSkyColor = new Color(originalSkyColor.r * 0.5f, originalSkyColor.g * 0.5f, originalSkyColor.b * 0.5f, originalSkyColor.a);
        ambientData.ambientEquatorColor = new Color(originalEquatorColor.r * 0.5f, originalEquatorColor.g * 0.5f, originalEquatorColor.b * 0.5f, originalEquatorColor.a);
        ambientData.ambientGroundColor = new Color(originalGroundColor.r * 0.5f, originalGroundColor.g * 0.5f, originalGroundColor.b * 0.5f, originalGroundColor.a);
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-trilight-half", "Trilight 三色同时减半（intensity 保持 1）：Trilight 的亮度入口是三色本身");

        ambientData.ambientSkyColor = originalSkyColor;
        ambientData.ambientEquatorColor = originalEquatorColor;
        ambientData.ambientGroundColor = originalGroundColor;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);

        ambientData.ambientMode = AmbientMode.Flat;
        ambientData.ambientSkyColor = originalSkyColor;
        ambientData.ambientIntensity = 1f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-flat-i100", "Flat 模式 + ambientIntensity=1（基准）");

        ambientData.ambientIntensity = 0.25f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-flat-i025", "Flat 模式 + ambientIntensity 1→0.25：只改强度，验证 Flat 下是否真的生效");

        ambientData.ambientIntensity = 2.5f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-flat-i250", "Flat 模式 + ambientIntensity 1→2.5：同上，反向");

        // Flat 下真正起作用的入口：ambientSkyColor（ApplyWorldAmbient 里它就是 RenderSettings.ambientLight）
        ambientData.ambientIntensity = 1f;
        ambientData.ambientSkyColor = new Color(originalSkyColor.r * 0.5f, originalSkyColor.g * 0.5f, originalSkyColor.b * 0.5f, originalSkyColor.a);
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-flat-skyhalf", "Flat 模式 + ambientSkyColor 减半（intensity 保持 1）：验证 Flat 的亮度入口是环境色");

        ambientData.ambientSkyColor = originalSkyColor;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);

        ambientData.ambientMode = AmbientMode.Skybox;
        ambientData.ambientIntensity = 1f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-skybox-i100", "Skybox 模式 + ambientIntensity=1（基准）");

        ambientData.ambientIntensity = 2.5f;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-skybox-i250", "Skybox 模式 + ambientIntensity 1→2.5：验证强度是否乘在天空盒环境光上");

        // 还原环境数据字段与 RenderSettings，并再采一张证明回到 Flat 基准
        ambientData.ambientMode = originalAmbientMode;
        ambientData.ambientSkyColor = originalSkyColor;
        ambientData.ambientIntensity = originalAmbientIntensity;
        _environment.ApplyWorldAmbient();
        DynamicGI.UpdateEnvironment();
        ApplyDayTimeInstant(0);
        CaptureCase("ambient-restore-flat", "环境实验结束后还原 Flat/idensity=1/original 环境色，与 day-baitian 比较（验证能否无损回到基准）");

        // ---- 第 5.6 组：同一组环境参数对"真正吃球谐的材质"的效果（地面用 Standard，会走 SH 环境项） ----
        // 角色 Shader 在 ambientColor.a=1 时把环境项整体替换成 _ROCAmbientColor，所以上面几组对角色 0 变化；
        // 这一组把地面打开，用画面下方 30% 区域（纯地面）统计，验证 Unity 的 ambientIntensity/Skybox 语义在普通材质上确实成立。
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = true;
            ambientData.ambientMode = AmbientMode.Flat;
            ambientData.ambientIntensity = 1f;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundFlatI100 = MeanLumaBottomBand(RenderCameraPixels());

            ambientData.ambientIntensity = 2.5f;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundFlatI250 = MeanLumaBottomBand(RenderCameraPixels());

            // Flat 下真正的亮度入口：ambientSkyColor（= RenderSettings.ambientLight）
            ambientData.ambientIntensity = 1f;
            ambientData.ambientSkyColor = originalSkyColor;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundFlatSkyOriginal = MeanLumaBottomBand(RenderCameraPixels());

            ambientData.ambientSkyColor = new Color(originalSkyColor.r * 0.5f, originalSkyColor.g * 0.5f, originalSkyColor.b * 0.5f, originalSkyColor.a);
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundFlatSkyHalf = MeanLumaBottomBand(RenderCameraPixels());

            ambientData.ambientSkyColor = originalSkyColor;
            ambientData.ambientMode = AmbientMode.Skybox;
            ambientData.ambientIntensity = 1f;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundSkyboxI100 = MeanLumaBottomBand(RenderCameraPixels());

            ambientData.ambientIntensity = 2.5f;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundSkyboxI250 = MeanLumaBottomBand(RenderCameraPixels());

            ambientData.ambientMode = originalAmbientMode;
            ambientData.ambientIntensity = originalAmbientIntensity;
            _environment.ApplyWorldAmbient();
            DynamicGI.UpdateEnvironment();
            ApplyDayTimeInstant(0);
            _groundRenderer.enabled = false;
        }


        // 存整帧图（天空盒 + 地面），给人看真实观感；角色像素统计只认上面的纯黑底图
        _camera.clearFlags = _originalClearFlags;
        _camera.backgroundColor = _originalBackground;
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = true;
        }
        ApplyDayTimeInstant(0);
        SaveFullFrame("10-fullframe-day" + CaseSuffix + ".png");
        ApplyDayTimeInstant(1);
        SaveFullFrame("11-fullframe-dusk" + CaseSuffix + ".png");
        ApplyDayTimeInstant(2);
        SaveFullFrame("12-fullframe-night" + CaseSuffix + ".png");

        // 回到纯黑底，准备插值段
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = false;
        }

        // ---- 第 6 组：编辑态不做插值采集 ----
        // 真实边界：编辑态没有运行时循环，DOTween 的 tween 不会自然推进；本版 DOTween（源工程 2021 程序集）
        // 没有可用的"按已播时长求值"公开接口，也没有可枚举的活动 tween 列表，因此编辑态只支持"立即应用"。
        // 2 秒插值的验证只在 Play 模式做（RunPlayMode：按真实经过时长采样自然推进），不再用手动推进冒充自然播放。
        // 本轮起不再产出 lerp-mid / lerp-end 这类"手推时间步"的截图；第一轮那两张仅作历史归档，不作为插值证据。
        File.AppendAllText(ProgressPath, "lerp-skipped-in-editor\n", new UTF8Encoding(false));

        FinishAndWriteFile();
    }

    /// <summary>Play 模式验证（r2）：夜晚立即应用作起点 → 走源版 2 秒插值自然推进并按真实时长采样 → 立即应用白天做终点比对 → 退 Play 写结果。</summary>
    public static void RunPlayMode()
    {
        Directory.CreateDirectory(CaptureDir);
        File.WriteAllText(PlayProgressPath, "start\n", new UTF8Encoding(false));

        // 进 Play 会触发脚本域重载，本次流程挂在静态回调上，关掉域重载保证连续
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PlayState = 0;
        PlayFrame = 0;
        EditorApplication.update -= OnPlayUpdate;
        EditorApplication.update += OnPlayUpdate;
    }

    /// <summary>Play 模式进度文件路径（外部轮询用，r2 独立文件不覆盖第一轮证据）。</summary>
    private const string PlayProgressPath = "D:/unitypro/LYFMMORGP/_temp/ro-character-full-play-r3-progress.txt";

    /// <summary>Play 模式结果 JSON 路径（r2）。</summary>
    private const string PlayResultPath = "D:/unitypro/LYFMMORGP/_temp/ro-character-full-play-r3.json";

    /// <summary>Play 段状态机步号：0 进 Play、1 等进 Play、2 采夜晚、3 触发插值、4 采 0.45 秒、5 采 1.0 秒、6 采 2.4 秒并对比、7 采立即应用白天做终点比对、8 退 Play、9 写结果。</summary>
    private static int PlayState;

    /// <summary>当前步内已等待的帧数，用来等渲染稳定。</summary>
    private static int PlayFrame;

    /// <summary>Play 模式下的面板组件。</summary>
    private static RODemoLightingPanel _playPanel;

    /// <summary>Play 模式夜晚档（插值起点）的平均亮度。</summary>
    private static float _playNightLuma;

    /// <summary>Play 模式插值 0.45 秒时的平均亮度（自然推进，不是手动定位）。</summary>
    private static float _playLerpLuma045;

    /// <summary>Play 模式插值 1.0 秒时的平均亮度。</summary>
    private static float _playLerpLuma100;

    /// <summary>Play 模式插值 2.4 秒时的平均亮度（应已到终点）。</summary>
    private static float _playLerpLumaEnd;

    /// <summary>Play 模式立即应用白天的平均亮度，作为插值终点的比对基准。</summary>
    private static float _playDayLuma;

    /// <summary>Play 模式插值终点与"立即应用白天"的变化像素数。</summary>
    private static int _playEndVsInstantDayPixels;

    /// <summary>Play 模式插值终点与"立即应用白天"的最大单通道差。</summary>
    private static int _playEndVsInstantDayMaxDiff;

    /// <summary>Play 模式触发插值时的编辑器时间（秒），后续采样按它与当前时间之差取真实经过时长。</summary>
    private static double _playLerpStartTime;

    /// <summary>Play 模式插值过程中实际测到的 0.45 秒采样时刻（真实经过时长，秒）。</summary>
    private static double _playLerpElapsed045;

    /// <summary>Play 模式插值过程中实际测到的 1.0 秒采样时刻（真实经过时长，秒）。</summary>
    private static double _playLerpElapsed100;

    /// <summary>Play 模式插值结束时实际经过时长（秒）。</summary>
    private static double _playLerpElapsedEnd;

    /// <summary>Play 模式夜晚档像素，用于和插值终点比对。</summary>
    private static Color32[] _playNightPixels;

    /// <summary>Play 模式插值终点像素。</summary>
    private static Color32[] _playLerpEndPixels;

    /// <summary>Play 模式立即应用白天的像素。</summary>
    private static Color32[] _playDayPixels;

    /// <summary>
    /// Play 段逐帧推进（r2）：先立即应用夜晚作为起点，再走源版 2 秒插值自然推进，按真实经过时长采样三次，
    /// 最后再用"立即应用白天"做终点比对。全程不手动定位 tween，也不向运行时脚本加任何推进接口。
    /// </summary>
    private static void OnPlayUpdate()
    {
        if (PlayState == 0)
        {
            EditorApplication.isPlaying = true;
            PlayState = 1;
            PlayFrame = 0;
            File.AppendAllText(PlayProgressPath, "enter-play\n", new UTF8Encoding(false));
            return;
        }

        if (PlayState == 1)
        {
            if (EditorApplication.isPlaying == false)
            {
                return;
            }
            PlayFrame = PlayFrame + 1;
            if (PlayFrame < 30)
            {
                return;
            }
            _playPanel = Object.FindFirstObjectByType<RODemoLightingPanel>();
            _camera = Camera.main;
            _groundRenderer = FindGroundRenderer();
            _characterRenderer = FindCharacterRenderer();
            if (_playPanel == null)
            {
                File.AppendAllText(PlayProgressPath, "error:no-panel\n", new UTF8Encoding(false));
                PlayState = 7;
                return;
            }
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            if (_groundRenderer != null)
            {
                _groundRenderer.enabled = false;
            }
            _playPanel.ApplyIndex(2, true);
            PlayState = 2;
            PlayFrame = 0;
            return;
        }

        if (PlayState == 2)
        {
            PlayFrame = PlayFrame + 1;
            if (PlayFrame < 8)
            {
                return;
            }
            _playNightPixels = RenderCameraPixels();
            SavePng(_playNightPixels, "20-play-night" + CaseSuffix + ".png");
            _playNightLuma = MeanLuma(_playNightPixels, BuildMaskFromPixel(_playNightPixels));
            // 触发源版 2 秒插值：instant=false → ApplyEnvorimentData(fast:false) → DOTween.To
            _playPanel.ApplyIndex(0, false);
            _playLerpStartTime = EditorApplication.timeSinceStartup;
            File.AppendAllText(PlayProgressPath, "lerp-natural-start\n", new UTF8Encoding(false));
            PlayState = 3;
            return;
        }

        if (PlayState == 3)
        {
            double elapsed = EditorApplication.timeSinceStartup - _playLerpStartTime;
            if (elapsed < 0.45)
            {
                return;
            }
            _playLerpElapsed045 = elapsed;
            Color32[] pixels045 = RenderCameraPixels();
            SavePng(pixels045, "21-play-lerp-045s" + CaseSuffix + ".png");
            _playLerpLuma045 = MeanLuma(pixels045, BuildMaskFromPixel(pixels045));
            File.AppendAllText(PlayProgressPath, "sample-045\n", new UTF8Encoding(false));
            PlayState = 4;
            return;
        }

        if (PlayState == 4)
        {
            double elapsed = EditorApplication.timeSinceStartup - _playLerpStartTime;
            if (elapsed < 1.0)
            {
                return;
            }
            _playLerpElapsed100 = elapsed;
            Color32[] pixels100 = RenderCameraPixels();
            SavePng(pixels100, "22-play-lerp-100s" + CaseSuffix + ".png");
            _playLerpLuma100 = MeanLuma(pixels100, BuildMaskFromPixel(pixels100));
            File.AppendAllText(PlayProgressPath, "sample-100\n", new UTF8Encoding(false));
            PlayState = 5;
            return;
        }

        if (PlayState == 5)
        {
            double elapsed = EditorApplication.timeSinceStartup - _playLerpStartTime;
            if (elapsed < 2.4)
            {
                return;
            }
            _playLerpElapsedEnd = elapsed;
            _playLerpEndPixels = RenderCameraPixels();
            SavePng(_playLerpEndPixels, "23-play-lerp-end" + CaseSuffix + ".png");
            _playLerpLumaEnd = MeanLuma(_playLerpEndPixels, BuildMaskFromPixel(_playLerpEndPixels));
            File.AppendAllText(PlayProgressPath, "sample-end\n", new UTF8Encoding(false));
            PlayState = 6;
            return;
        }

        if (PlayState == 6)
        {
            // 插值结束后再立即应用白天：正常实现下两者应逐像素一致
            _playPanel.ApplyIndex(0, true);
            PlayState = 61;
            PlayFrame = 0;
            return;
        }

        if (PlayState == 61)
        {
            PlayFrame = PlayFrame + 1;
            if (PlayFrame < 8)
            {
                return;
            }
            _playDayPixels = RenderCameraPixels();
            SavePng(_playDayPixels, "24-play-day-instant" + CaseSuffix + ".png");
            _playDayLuma = MeanLuma(_playDayPixels, BuildMaskFromPixel(_playDayPixels));
            ComparePixels(_playDayPixels, _playLerpEndPixels, out _playEndVsInstantDayPixels, out _playEndVsInstantDayMaxDiff);
            PlayState = 7;
            return;
        }

        if (PlayState == 7)
        {
            EditorApplication.isPlaying = false;
            PlayState = 8;
            return;
        }

        if (PlayState == 8)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("  \"playMode\": true,\n");
            builder.Append("  \"scene\": \"").Append(ScenePath).Append("\",\n");
            builder.Append("  \"note\": \"Play 模式自然推进（不手动定位 tween）\",\n");
            builder.Append("  \"nightMeanLuma\": ").Append(_playNightLuma.ToString("F6")).Append(",\n");
            builder.Append("  \"lerpSample045\": {\"elapsedSeconds\": ").Append(_playLerpElapsed045.ToString("F3")).Append(", \"meanLuma\": ").Append(_playLerpLuma045.ToString("F6")).Append("},\n");
            builder.Append("  \"lerpSample100\": {\"elapsedSeconds\": ").Append(_playLerpElapsed100.ToString("F3")).Append(", \"meanLuma\": ").Append(_playLerpLuma100.ToString("F6")).Append("},\n");
            builder.Append("  \"lerpEnd\": {\"elapsedSeconds\": ").Append(_playLerpElapsedEnd.ToString("F3")).Append(", \"meanLuma\": ").Append(_playLerpLumaEnd.ToString("F6")).Append("},\n");
            builder.Append("  \"instantDayMeanLuma\": ").Append(_playDayLuma.ToString("F6")).Append(",\n");
            builder.Append("  \"endVsInstantDayChangedPixels\": ").Append(_playEndVsInstantDayPixels).Append(",\n");
            builder.Append("  \"endVsInstantDayMaxChannelDiff\": ").Append(_playEndVsInstantDayMaxDiff).Append(",\n");
            builder.Append("  \"endEqualsInstantDay\": ").Append(BoolText(_playEndVsInstantDayMaxDiff <= 1)).Append(",\n");
            builder.Append("  \"captures\": [\"20-play-night").Append(CaseSuffix).Append(".png\", \"21-play-lerp-045s").Append(CaseSuffix).Append(".png\", \"22-play-lerp-100s").Append(CaseSuffix).Append(".png\", \"23-play-lerp-end").Append(CaseSuffix).Append(".png\", \"24-play-day-instant").Append(CaseSuffix).Append(".png\"]\n");
            builder.Append("}\n");
            File.WriteAllText(PlayResultPath, builder.ToString(), new UTF8Encoding(false));

            // 还原进 Play 前改动的工程设置，避免影响用户后续手动进 Play
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            EditorApplication.update -= OnPlayUpdate;
            File.AppendAllText(PlayProgressPath, "done\n", new UTF8Encoding(false));
            PlayState = 99;

            // 批处理模式（无人工值守）下跑完即退出编辑器，否则 Unity 会一直空转
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }

    /// <summary>结束：算统计、写 JSON、还原场景状态。</summary>
    private static void FinishAndWriteFile()
    {
        // 掩码取所有采集变体的并集，保证三档用的是同一批角色像素
        List<int> mask = BuildMask();
        int maskCount = mask.Count;

        // 基准：白天立即应用那条，用于算亮度差与"恢复默认"一致性
        int baselineIndex = FindCaptureIndex("day-baitian");
        int restoreIndex = FindCaptureIndex("restore-day");
        float baselineLuma = MeanLuma(Captures[baselineIndex].pixels, mask);

        // 逐条用例算平均亮度/亮度分位/变化像素
        StringBuilder json = new StringBuilder();
        json.Append("{\n");
        json.Append("  \"scene\": \"").Append(ScenePath).Append("\",\n");
        json.Append("  \"captureWidth\": ").Append(CaptureWidth).Append(",\n");
        json.Append("  \"captureHeight\": ").Append(CaptureHeight).Append(",\n");
        json.Append("  \"characterMaskPixels\": ").Append(maskCount).Append(",\n");
        json.Append("  \"baselineName\": \"day-baitian\",\n");
        json.Append("  \"baselineMeanLuma\": ").Append(baselineLuma.ToString("F6")).Append(",\n");
        json.Append("  \"cases\": [\n");

        for (int i = 0; i < Captures.Count; i = i + 1)
        {
            CaptureRecord record = Captures[i];
            float luma = MeanLuma(record.pixels, mask);
            float p10 = PercentileLuma(record.pixels, mask, 0.1f);
            float p50 = PercentileLuma(record.pixels, mask, 0.5f);
            float p90 = PercentileLuma(record.pixels, mask, 0.9f);
            int changed = 0;
            int maxDiff = 0;
            if (i != baselineIndex)
            {
                ComparePixels(Captures[baselineIndex].pixels, record.pixels, out changed, out maxDiff);
            }

            json.Append("    {\"name\": \"").Append(record.name).Append("\", ");
            json.Append("\"note\": \"").Append(record.note).Append("\", ");
            json.Append("\"meanLuma\": ").Append(luma.ToString("F6")).Append(", ");
            json.Append("\"lumaDeltaVsDay\": ").Append((luma - baselineLuma).ToString("F6")).Append(", ");
            json.Append("\"lumaRelativeToDay\": ").Append((luma / baselineLuma).ToString("F6")).Append(", ");
            json.Append("\"p10\": ").Append(p10.ToString("F6")).Append(", ");
            json.Append("\"p50\": ").Append(p50.ToString("F6")).Append(", ");
            json.Append("\"p90\": ").Append(p90.ToString("F6")).Append(", ");
            json.Append("\"changedPixelsVsDay\": ").Append(changed).Append(", ");
            json.Append("\"maxChannelDiffVsDay\": ").Append(maxDiff).Append(", ");
            json.Append("\"globals\": \"").Append(record.globals).Append("\", ");
            json.Append("\"png\": \"case-").Append(record.name).Append(CaseSuffix).Append(".png\"}");

            if (i < Captures.Count - 1)
            {
                json.Append(",");
            }
            json.Append("\n");
        }
        json.Append("  ],\n");

        // 恢复默认的一致性：restore-day 与 day-baitian 的逐像素差
        int restoreChanged = 0;
        int restoreMaxDiff = 0;
        ComparePixels(Captures[baselineIndex].pixels, Captures[restoreIndex].pixels, out restoreChanged, out restoreMaxDiff);
        json.Append("  \"restoreDiffPixels\": ").Append(restoreChanged).Append(",\n");
        json.Append("  \"restoreMaxChannelDiff\": ").Append(restoreMaxDiff).Append(",\n");

        json.Append("  \"lerpInEditor\": {\"evaluated\": false, \"reason\": \"editor has no runtime loop; this DOTween build has no public seek or enumerate API\", \"naturalPlaybackEvidence\": \"_temp/ro-character-full-play-r2.json\"},\n");

        // 反射 A/B：只改 reflectMap 一处，其余固定
        int reflOffIndex = FindCaptureIndex("refl-off");
        int reflOnIndex = FindCaptureIndex("refl-on");
        float reflOffLuma = MeanLuma(Captures[reflOffIndex].pixels, mask);
        float reflOnLuma = MeanLuma(Captures[reflOnIndex].pixels, mask);
        int reflChanged = 0;
        int reflMaxDiff = 0;
        ComparePixels(Captures[reflOffIndex].pixels, Captures[reflOnIndex].pixels, out reflChanged, out reflMaxDiff);
        json.Append("  \"reflection\": {\n");
        json.Append("    \"reflectMap\": \"").Append(_reflectionInfo).Append("\",\n");
        json.Append("    \"offMeanLuma\": ").Append(reflOffLuma.ToString("F6")).Append(",\n");
        json.Append("    \"onMeanLuma\": ").Append(reflOnLuma.ToString("F6")).Append(",\n");
        json.Append("    \"changedPixelsOnVsOff\": ").Append(reflChanged).Append(",\n");
        json.Append("    \"maxChannelDiffOnVsOff\": ").Append(reflMaxDiff).Append("\n");
        json.Append("  },\n");

        // ambientIntensity 语义对照
        json.Append("  \"ambientIntensityProbe\": {\n");
        json.Append("    \"flatIntensity100\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-flat-i100")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"flatIntensity025\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-flat-i025")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"flatIntensity250\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-flat-i250")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"skyboxIntensity100\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-skybox-i100")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"skyboxIntensity250\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-skybox-i250")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"flatSkyColorHalf\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-flat-skyhalf")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"restoreFlat\": ").Append(MeanLuma(Captures[FindCaptureIndex("ambient-restore-flat")].pixels, mask).ToString("F6")).Append(",\n");
        json.Append("    \"groundBandFlatIntensity100\": ").Append(_groundFlatI100.ToString("F6")).Append(",\n");
        json.Append("    \"groundBandFlatIntensity250\": ").Append(_groundFlatI250.ToString("F6")).Append(",\n");
        json.Append("    \"groundBandSkyboxIntensity100\": ").Append(_groundSkyboxI100.ToString("F6")).Append(",\n");
        json.Append("    \"groundBandSkyboxIntensity250\": ").Append(_groundSkyboxI250.ToString("F6")).Append(",\n");
        json.Append("    \"groundBandFlatSkyOriginal\": ").Append(_groundFlatSkyOriginal.ToString("F6")).Append(",\n");
        json.Append("    \"groundBandFlatSkyHalf\": ").Append(_groundFlatSkyHalf.ToString("F6")).Append("\n");
        json.Append("  },\n");

        json.Append("  \"fixedConditions\": ").Append(FixedConditionsJson()).Append(",\n");
        json.Append("  \"captureDir\": \"").Append(CaptureDir).Append("\"\n");
        json.Append("}\n");

        File.WriteAllText(ResultPath, json.ToString(), new UTF8Encoding(false));

        // 收尾还原：相机恢复天空盒背景、地面重新显示、回到默认黄昏配置并保存场景，
        // 让编辑器窗口停在可直接查看的默认状态，而不是取证用的纯黑底
        _camera.clearFlags = _originalClearFlags;
        _camera.backgroundColor = _originalBackground;
        if (_groundRenderer != null)
        {
            _groundRenderer.enabled = true;
        }
        ApplyDayTimeInstant(1);
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        File.AppendAllText(ProgressPath, "done\n", new UTF8Encoding(false));
        Debug.Log("[RODemoFull] 验证完成，结果：" + ResultPath);
    }

    /// <summary>按当前默认配置立即应用一次并采集。</summary>
    /// <param name="index">_dayTimes 下标。</param>
    /// <param name="name">用例名。</param>
    /// <param name="note">用例说明。</param>
    private static void ApplyDayTime(int index, string name, string note)
    {
        ApplyDayTimeInstant(index);
        CaptureCase(name, note);
    }

    /// <summary>以 fast=true 立即应用指定时段（走源版 ApplyEnvorimentData 入口）。</summary>
    /// <param name="index">_dayTimes 下标。</param>
    private static void ApplyDayTimeInstant(int index)
    {
        MEnvorimentData data = new MEnvorimentData();
        data.dayTimeData = _dayTimes[index];
        _environment.ApplyEnvorimentData(data, true);
    }

    /// <summary>采集一条用例并落 PNG。</summary>
    /// <param name="name">用例名。</param>
    /// <param name="note">用例说明。</param>
    private static void CaptureCase(string name, string note)
    {
        CaptureRecord record = Capture(name, note);
        SavePng(record.pixels, "case-" + name + CaseSuffix + ".png");
    }

    /// <summary>渲染当前画面并登记为一条采集记录。</summary>
    /// <param name="name">用例名。</param>
    /// <param name="note">用例说明。</param>
    /// <returns>采集记录（含像素与当时读回的全局量）。</returns>
    private static CaptureRecord Capture(string name, string note)
    {
        CaptureRecord record = new CaptureRecord();
        record.name = name;
        record.note = note;
        record.pixels = RenderCameraPixels();
        record.globals = ReadGlobalsSummary();
        Captures.Add(record);
        Debug.Log("[RODemoFull] 采集 " + name + " 平均亮度=" + MeanLuma(record.pixels, BuildMaskFromPixel(record.pixels)).ToString("F4"));
        return record;
    }

    /// <summary>把主相机渲染到离屏 RT 并读回像素（不依赖 Game 视图是否可见）。</summary>
    /// <returns>RGB24 像素数组。</returns>
    private static Color32[] RenderCameraPixels()
    {
        RenderTexture renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        _camera.targetTexture = renderTexture;
        _camera.Render();
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
        texture.Apply();
        Color32[] pixels = texture.GetPixels32();
        RenderTexture.active = null;
        _camera.targetTexture = null;
        renderTexture.Release();
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(texture);
        return pixels;
    }

    /// <summary>存一张整帧截图（天空盒/地面都在），用于人工看观感。</summary>
    /// <param name="fileName">文件名。</param>
    private static void SaveFullFrame(string fileName)
    {
        Color32[] pixels = RenderCameraPixels();
        SavePng(pixels, fileName);
    }

    /// <summary>把像素数组编码成 PNG 落到采集目录。</summary>
    /// <param name="pixels">RGB24 像素。</param>
    /// <param name="fileName">文件名。</param>
    private static void SavePng(Color32[] pixels, string fileName)
    {
        Texture2D texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(CaptureDir, fileName), bytes);
        Object.DestroyImmediate(texture);
    }

    /// <summary>Flat 模式 + ambientIntensity=1 时，画面下方 30% 地面区域的平均亮度（证明普通材质吃 SH 环境光）。</summary>
    private static float _groundFlatI100;

    /// <summary>Flat 模式 + ambientIntensity=2.5 时，同一地面区域的平均亮度。</summary>
    private static float _groundFlatI250;

    /// <summary>Skybox 模式 + ambientIntensity=1 时，地面区域的平均亮度。</summary>
    private static float _groundSkyboxI100;

    /// <summary>Skybox 模式 + ambientIntensity=2.5 时，地面区域的平均亮度。</summary>
    private static float _groundSkyboxI250;

    /// <summary>Flat 模式 + ambientSkyColor 原值时，地面区域的平均亮度。</summary>
    private static float _groundFlatSkyOriginal;

    /// <summary>Flat 模式 + ambientSkyColor 减半时，地面区域的平均亮度。</summary>
    private static float _groundFlatSkyHalf;

    /// <summary>
    /// 统计画面下方 30% 条带（本 Demo 里只有地面）的平均亮度，跳过全黑像素。
    /// 用于单独观察"真正消费球谐环境光"的普通材质，不受角色遮罩影响。
    /// </summary>
    /// <param name="pixels">整帧像素（RGB24，行优先，左下起）。</param>
    /// <returns>条带平均亮度（0~1）。</returns>
    private static float MeanLumaBottomBand(Color32[] pixels)
    {
        int bandHeight = (int)(CaptureHeight * 0.3f);
        double sum = 0d;
        int count = 0;
        for (int y = 0; y < bandHeight; y = y + 1)
        {
            for (int x = 0; x < CaptureWidth; x = x + 1)
            {
                Color32 pixel = pixels[y * CaptureWidth + x];
                if (pixel.r == 0 && pixel.g == 0 && pixel.b == 0)
                {
                    continue;
                }
                sum = sum + (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255d;
                count = count + 1;
            }
        }
        if (count == 0)
        {
            return 0f;
        }
        return (float)(sum / count);
    }

    /// <summary>所有采集变体的像素并集构成角色遮罩下标列表。</summary>
    /// <returns>角色像素在 1024×768 缓存里的下标。</returns>
    private static List<int> BuildMask()
    {
        List<int> mask = new List<int>();
        for (int i = 0; i < Captures[0].pixels.Length; i = i + 1)
        {
            bool isCharacter = false;
            for (int c = 0; c < Captures.Count; c = c + 1)
            {
                Color32 pixel = Captures[c].pixels[i];
                if (pixel.r > MaskChannelThreshold || pixel.g > MaskChannelThreshold || pixel.b > MaskChannelThreshold)
                {
                    isCharacter = true;
                    break;
                }
            }
            if (isCharacter)
            {
                mask.Add(i);
            }
        }
        return mask;
    }

    /// <summary>单张图自身的非黑像素下标（仅用于日志）。</summary>
    /// <param name="pixels">像素数组。</param>
    /// <returns>非黑像素下标。</returns>
    private static List<int> BuildMaskFromPixel(Color32[] pixels)
    {
        List<int> mask = new List<int>();
        for (int i = 0; i < pixels.Length; i = i + 1)
        {
            if (pixels[i].r > MaskChannelThreshold || pixels[i].g > MaskChannelThreshold || pixels[i].b > MaskChannelThreshold)
            {
                mask.Add(i);
            }
        }
        return mask;
    }

    /// <summary>掩码区域的平均亮度（Rec.601 luma / 255，0~1）。</summary>
    /// <param name="pixels">像素数组。</param>
    /// <param name="mask">角色像素下标。</param>
    /// <returns>平均亮度；掩码为空时返回 0。</returns>
    private static float MeanLuma(Color32[] pixels, List<int> mask)
    {
        if (mask.Count == 0)
        {
            return 0f;
        }
        double sum = 0d;
        for (int i = 0; i < mask.Count; i = i + 1)
        {
            Color32 pixel = pixels[mask[i]];
            sum = sum + (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255d;
        }
        return (float)(sum / mask.Count);
    }

    /// <summary>掩码区域亮度的分位值，用于说明"变亮"是整体抬升还是只有少数像素。</summary>
    /// <param name="pixels">像素数组。</param>
    /// <param name="mask">角色像素下标。</param>
    /// <param name="percentile">分位（0~1），如 0.5 表示中位数。</param>
    /// <returns>对应分位的亮度。</returns>
    private static float PercentileLuma(Color32[] pixels, List<int> mask, float percentile)
    {
        if (mask.Count == 0)
        {
            return 0f;
        }
        List<float> values = new List<float>(mask.Count);
        for (int i = 0; i < mask.Count; i = i + 1)
        {
            Color32 pixel = pixels[mask[i]];
            values.Add((0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f);
        }
        values.Sort();
        int index = (int)(percentile * (values.Count - 1));
        return values[index];
    }

    /// <summary>逐像素比较，返回变化像素数（任一道差 &gt; 1）与最大单通道差。</summary>
    /// <param name="a">基准像素。</param>
    /// <param name="b">对比像素。</param>
    /// <param name="changedPixels">输出：变化像素数。</param>
    /// <param name="maxChannelDiff">输出：最大单通道差（0~255）。</param>
    private static void ComparePixels(Color32[] a, Color32[] b, out int changedPixels, out int maxChannelDiff)
    {
        changedPixels = 0;
        maxChannelDiff = 0;
        for (int i = 0; i < a.Length; i = i + 1)
        {
            int dr = Mathf.Abs(b[i].r - a[i].r);
            int dg = Mathf.Abs(b[i].g - a[i].g);
            int db = Mathf.Abs(b[i].b - a[i].b);
            int max = dr;
            if (dg > max)
            {
                max = dg;
            }
            if (db > max)
            {
                max = db;
            }
            if (max > 1)
            {
                changedPixels = changedPixels + 1;
            }
            if (max > maxChannelDiff)
            {
                maxChannelDiff = max;
            }
        }
    }

    /// <summary>按名字找采集下标，找不到返回 0。</summary>
    /// <param name="name">用例名。</param>
    /// <returns>下标。</returns>
    private static int FindCaptureIndex(string name)
    {
        for (int i = 0; i < Captures.Count; i = i + 1)
        {
            if (Captures[i].name == name)
            {
                return i;
            }
        }
        return 0;
    }

    /// <summary>读回当前生效的 Shader 全局量，作为"这一档到底用了哪些参数"的实证。</summary>
    /// <returns>形如 _ROCLightColor0=(r,g,b,a); _ROCLightDir0=(x,y,z) 的单行文本。</returns>
    private static string ReadGlobalsSummary()
    {
        Color mainColor = Shader.GetGlobalColor("_ROCLightColor0");
        Color backColor = Shader.GetGlobalColor("_ROCBackColor");
        Color ambientColor = Shader.GetGlobalColor("_ROCAmbientColor");
        Vector4 lightDir = Shader.GetGlobalVector("_ROCLightDir0");
        return "_ROCLightColor0=(" + mainColor.r.ToString("F4") + "," + mainColor.g.ToString("F4") + "," + mainColor.b.ToString("F4") + "," + mainColor.a.ToString("F4") + ")"
            + "; _ROCBackColor=(" + backColor.r.ToString("F4") + "," + backColor.g.ToString("F4") + "," + backColor.b.ToString("F4") + "," + backColor.a.ToString("F4") + ")"
            + "; _ROCAmbientColor=(" + ambientColor.r.ToString("F4") + "," + ambientColor.g.ToString("F4") + "," + ambientColor.b.ToString("F4") + "," + ambientColor.a.ToString("F4") + ")"
            + "; _ROCLightDir0=(" + lightDir.x.ToString("F4") + "," + lightDir.y.ToString("F4") + "," + lightDir.z.ToString("F4") + ")";
    }

    /// <summary>读材质 float 属性；不存在时返回调用方给的默认值。</summary>
    /// <param name="property">属性名。</param>
    /// <param name="fallback">属性不存在时的回退值。</param>
    /// <returns>属性值。</returns>
    private static float ReadMaterialFloat(string property, float fallback)
    {
        if (_characterMaterial.HasProperty(property) == false)
        {
            return fallback;
        }
        return _characterMaterial.GetFloat(property);
    }

    /// <summary>读材质 vector 属性；不存在时返回调用方给的默认值。</summary>
    /// <param name="property">属性名。</param>
    /// <param name="fallback">属性不存在时的回退值。</param>
    /// <returns>属性值。</returns>
    private static Vector4 ReadMaterialVector(string property, Vector4 fallback)
    {
        if (_characterMaterial.HasProperty(property) == false)
        {
            return fallback;
        }
        return _characterMaterial.GetVector(property);
    }

    /// <summary>找角色渲染器：优先场景里那个静态渲染对象（MeshRenderer），否则退回 SkinnedMeshRenderer。</summary>
    /// <returns>角色渲染器，找不到返回 null。</returns>
    private static Renderer FindCharacterRenderer()
    {
        GameObject staticObject = GameObject.Find("StaticBandposeRenderer");
        if (staticObject != null)
        {
            MeshRenderer meshRenderer = staticObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                return meshRenderer;
            }
        }
        GameObject character = GameObject.Find("RO_Player_Novice");
        if (character != null)
        {
            return character.GetComponentInChildren<Renderer>();
        }
        return null;
    }

    /// <summary>找地面渲染器（取证时关掉它）。</summary>
    /// <returns>地面渲染器，找不到返回 null。</returns>
    private static Renderer FindGroundRenderer()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            return null;
        }
        return ground.GetComponent<Renderer>();
    }

    /// <summary>按约定顺序加载三套时段资产。</summary>
    /// <returns>长度 3 的数组：0=白天、1=黄昏、2=夜晚。</returns>
    private static MDayTimeData[] LoadDayTimes()
    {
        MDayTimeData[] result = new MDayTimeData[3];
        result[0] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_baitian.asset");
        result[1] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_huanghun.asset");
        result[2] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_yewan.asset");
        return result;
    }

    /// <summary>固定条件 JSON 文本（相机/色彩空间/环境模式/材质/灯光数量/后处理），写进结果供复核。</summary>
    private static string _fixedConditions = "{}";

    /// <summary>收集并打印固定条件（同一批截图之间只有 RO 参数不同）。</summary>
    private static void AppendFixedConditions()
    {
        string shaderName = "null";
        if (_characterMaterial != null)
        {
            shaderName = _characterMaterial.shader.name;
        }
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        builder.Append("\"colorSpace\": \"").Append(PlayerSettings.colorSpace.ToString()).Append("\", ");
        builder.Append("\"graphicsShaderLevel\": ").Append(SystemInfo.graphicsShaderLevel).Append(", ");
        builder.Append("\"cameraPosition\": \"").Append(_camera.transform.position.ToString("F4")).Append("\", ");
        builder.Append("\"cameraRotation\": \"").Append(_camera.transform.rotation.eulerAngles.ToString("F2")).Append("\", ");
        builder.Append("\"cameraFieldOfView\": ").Append(_camera.fieldOfView.ToString("F2")).Append(", ");
        builder.Append("\"cameraClearFlags\": \"SolidColor(取证)\", ");
        builder.Append("\"ambientMode\": \"").Append(RenderSettings.ambientMode.ToString()).Append("\", ");
        builder.Append("\"ambientLight\": \"").Append(RenderSettings.ambientLight.ToString("F4")).Append("\", ");
        builder.Append("\"ambientIntensity\": ").Append(RenderSettings.ambientIntensity.ToString("F3")).Append(", ");
        builder.Append("\"unityLightCount\": ").Append(lights.Length).Append(", ");
        builder.Append("\"characterShader\": \"").Append(shaderName).Append("\", ");
        builder.Append("\"characterMaterial\": \"").Append(_characterMaterial.name).Append("\", ");
        builder.Append("\"brightnessDefault\": ").Append(ReadMaterialFloat("_Brightness", -1f).ToString("F3")).Append(", ");
        builder.Append("\"falloffDefault\": \"").Append(ReadMaterialVector("_Falloff", Vector4.zero).ToString("F3")).Append("\", ");
        builder.Append("\"shaderMaximumLOD\": ").Append(Shader.globalMaximumLOD).Append(", ");
        builder.Append("\"postProcessing\": \"none\"");
        builder.Append("}");
        _fixedConditions = builder.ToString();
        Debug.Log("[RODemoFull] 固定条件 " + _fixedConditions);
    }

    /// <summary>把布尔值转成 JSON 的 true/false 文本（C# 的 True 不是合法 JSON）。</summary>
    /// <param name="value">布尔值。</param>
    /// <returns>"true" 或 "false"。</returns>
    private static string BoolText(bool value)
    {
        if (value)
        {
            return "true";
        }
        return "false";
    }

    /// <summary>返回固定条件 JSON 文本。</summary>
    /// <returns>JSON 片段。</returns>
    private static string FixedConditionsJson()
    {
        return _fixedConditions;
    }
}
