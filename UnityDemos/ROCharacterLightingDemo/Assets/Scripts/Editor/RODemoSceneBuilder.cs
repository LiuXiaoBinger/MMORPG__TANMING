using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 一次性场景构建器：生成 ROCharacterLightingDemo 演示场景与地面材质。
/// 只在开发期使用（菜单 RO/构建角色光照演示场景，或命令行 -executeMethod 调用），运行时不依赖它。
/// </summary>
public static class RODemoSceneBuilder
{
    /// <summary>场景输出路径（项目根相对），README 里指向该路径。</summary>
    private const string ScenePath = "Assets/Scenes/ROCharacterLightingDemo.unity";

    /// <summary>地面材质输出路径，避免使用 Unity 默认材质被误改。</summary>
    private const string GroundMaterialPath = "Assets/Materials/DemoGround.mat";

    /// <summary>已组装的 RO 玩家 bandpose 预制体路径（从源工程原样复制）。</summary>
    private const string CharacterPrefabPath = "Assets/RO/Art/_Creature/Player/Novice/Preview/Novice_M_ChuShengZhuang_bandpose.prefab";

    /// <summary>菜单入口：在打开的编辑器里重新生成场景与地面材质。</summary>
    [MenuItem("RO/构建角色光照演示场景")]
    public static void BuildFromMenu()
    {
        // 菜单调用后不退出编辑器
        Build();
        Debug.Log("[RODemo] 场景构建完成：" + ScenePath);
    }

    /// <summary>命令行入口：构建完成后直接退出，供 -executeMethod 使用。</summary>
    public static void BuildFromCommandLine()
    {
        // 命令行批处理必须自行退出，否则 Unity 会挂在 batchmode
        Build();
        Debug.Log("[RODemo] 命令行场景构建完成：" + ScenePath);
        EditorApplication.Exit(0);
    }

    /// <summary>构建演示场景：地面 + 角色 + 相机 + RO 环境设置组件（MEnvorimentSetting）+ 时段切换面板。场景内不放任何 Unity 灯光。</summary>
    public static void Build()
    {
        // 与已验 RO 场景保持一致使用 Gamma 色彩空间，避免 Shader 里 GammaToLinear 分支走错
        if (PlayerSettings.colorSpace != ColorSpace.Gamma)
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        // 先确保 RO 原版数值的配置资产存在（时段/角色光/世界环境），否则场景引用会为空
        ROConfigAssetBuilder.Build();

        // 保证取到 LOD 600 的完整 SubShader（源 Shader 最高一档），低档会丢法线/点光分支
        Shader.globalMaximumLOD = 600;

        // 目录不存在时先创建，AssetDatabase 才能落盘
        if (AssetDatabase.IsValidFolder("Assets/Materials") == false)
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // 读取或创建地面材质
        Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        if (groundMaterial == null)
        {
            groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.name = "DemoGround";
            // 参考地面：场景里没有任何 Unity 灯光，地面亮度只来自环境光，所以底色取得比真实地面更亮，避免整块变黑
            groundMaterial.color = new Color(0.62f, 0.64f, 0.68f, 1f);
            groundMaterial.SetFloat("_Glossiness", 0.12f);
            AssetDatabase.CreateAsset(groundMaterial, GroundMaterialPath);
        }

        // 新建空场景，避免带入默认相机/灯光造成重复
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 环境光/天空盒/反射模式全部由 MEnvorimentSetting.ApplyWorldAmbient 按 MAmbientData 资产写入（源链路）。
        // 这里只把场景初始值对齐源 gi 资产（Trilight 三色 + 空天空盒），避免打开场景时先闪一帧旧值。
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.620891f, 0.7245941f, 0.9705882f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.58959544f, 0.5631488f, 0.9117647f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.9558824f, 0.9447337f, 0.7942258f, 1f);
        RenderSettings.ambientIntensity = 1f;

        // 地面：Unity 内置 Plane，缩放到 30x30 米，只作视觉参照
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
        groundRenderer.sharedMaterial = groundMaterial;

        // 角色：直接实例化源工程已组装好的 bandpose 预制体，保留原始网格与材质引用
        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        GameObject character = PrefabUtility.InstantiatePrefab(characterPrefab) as GameObject;
        character.name = "RO_Player_Novice";
        character.transform.position = Vector3.zero;

        // 源 bandpose 网格是 Z 轴向上烘焙的，需要绕 X 轴 -90 度立起来；按包围盒判断，避免硬编码猜错
        SkinnedMeshRenderer characterRenderer = character.GetComponentInChildren<SkinnedMeshRenderer>();
        if (characterRenderer != null)
        {
            Bounds meshBounds = characterRenderer.sharedMesh.bounds;
            if (meshBounds.size.z > meshBounds.size.y)
            {
                // 再绕 Y 轴 180 度，让默认镜头看到角色正面（源网格正面朝 +Z，相机在 -Z 侧）
                character.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
            }
            else
            {
                character.transform.localRotation = Quaternion.identity;
            }
        }

        // 源预制体的 SkinnedMeshRenderer 骨骼数组为空（骨骼由 RO 运行时注入），
        // 独立工程里无骨骼的 SMR 会把顶点蒙皮到退化位置导致完全不可见。
        // 最小适配：在同一网格与材质上补一个 MeshFilter + MeshRenderer 做静态渲染，原 SMR 保留但禁用，来源仍可查。
        MeshRenderer staticRenderer = null;
        if (characterRenderer != null)
        {
            if (characterRenderer.bones.Length == 0)
            {
                GameObject staticRendererObject = new GameObject("StaticBandposeRenderer");
                staticRendererObject.transform.SetParent(character.transform, false);
                MeshFilter meshFilter = staticRendererObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = characterRenderer.sharedMesh;
                staticRenderer = staticRendererObject.AddComponent<MeshRenderer>();
                staticRenderer.sharedMaterial = characterRenderer.sharedMaterial;
                staticRenderer.shadowCastingMode = ShadowCastingMode.Off;
                staticRenderer.receiveShadows = false;
                staticRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                staticRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                characterRenderer.enabled = false;
                Debug.Log("[RODemo] 源 SMR 无骨骼，已用 MeshRenderer 静态渲染同一网格：" +
                    meshFilter.sharedMesh.name + " / 材质 " + staticRenderer.sharedMaterial.name);
            }
        }

        // 让脚底正好落在地面 y=0：按当前世界包围盒下沿整体抬高角色
        Renderer framingRenderer = staticRenderer;
        if (framingRenderer == null)
        {
            framingRenderer = characterRenderer;
        }
        if (framingRenderer != null)
        {
            Bounds worldBounds = framingRenderer.bounds;
            character.transform.position = new Vector3(0f, -worldBounds.min.y, 0f);
            Debug.Log("[RODemo] 角色世界包围盒 size=" + framingRenderer.bounds.size +
                " center=" + framingRenderer.bounds.center);
        }

        // 相机注视点取角色包围盒中心，距离按竖直视角与角色高度反算，保证默认视角完整看到全身
        GameObject cameraTarget = new GameObject("CameraTarget");
        Vector3 lookPoint = new Vector3(0f, 0.9f, 0f);
        float framedDistance = 3.2f;
        if (framingRenderer != null)
        {
            Bounds worldBounds = framingRenderer.bounds;
            lookPoint = worldBounds.center;
            float halfFovRadians = 32f * 0.5f * Mathf.Deg2Rad;
            framedDistance = (worldBounds.size.y * 1.25f) / (2f * Mathf.Tan(halfFovRadians));
        }
        cameraTarget.transform.position = lookPoint;

        // 主相机：Skybox 背景 + 轨道相机组件
        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 32f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        RODemoOrbitCamera orbit = cameraObject.AddComponent<RODemoOrbitCamera>();
        orbit.target = cameraTarget.transform;
        orbit.distance = framedDistance;
        orbit.yaw = 0f;
        orbit.pitch = 3f;
        orbit.UpdateTransform();
        Debug.Log("[RODemo] 相机距离=" + framedDistance.ToString("F3") + " 注视点=" + lookPoint);


        // 环境组件宿主：RO 原版 MEnvorimentSetting（迁入版），由它把时段/角色光写进 Shader 全局量
        GameObject environmentObject = new GameObject("ROEnvironment");
        MEnvorimentSetting environment = environmentObject.AddComponent<MEnvorimentSetting>();
        environment.defaultDayTimeData = AssetDatabase.LoadAssetAtPath<MDayTimeData>(
            "Assets/RO/Config/DayTime/dt_sc_pulongdela_001_huanghun.asset");
        // 环境配置用源工程对应资产（普隆德拉白天 gi_sc_pulongdela_001_baitian）：Trilight 三色 + Custom 反射 + 空天空盒，
        // 字段逐项照源 .asset，不自建一套 Flat 常量环境光顶替
        environment.defaultAmbientData = AssetDatabase.LoadAssetAtPath<MAmbientData>(
            "Assets/RO/Config/Ambient/gi_sc_pulongdela_001_baitian.asset");
        environment.ApplyWorldAmbient();

        // 演示操作面板：只负责选配置/选立即或插值，真正的写入仍由 MEnvorimentSetting 完成
        RODemoLightingPanel panel = environmentObject.AddComponent<RODemoLightingPanel>();
        panel.environment = environment;
        panel.dayTimes = new MDayTimeData[3];
        panel.dayTimes[0] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_baitian.asset");
        panel.dayTimes[1] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_huanghun.asset");
        panel.dayTimes[2] = AssetDatabase.LoadAssetAtPath<MDayTimeData>("Assets/RO/Config/DayTime/dt_sc_pulongdela_001_yewan.asset");
        panel.currentIndex = 1;
        panel.instantApply = true;

        // 目录不存在时创建 Assets/Scenes，否则保存会失败
        if (AssetDatabase.IsValidFolder("Assets/Scenes") == false)
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // 保存场景并刷新资源数据库
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RODemo] 场景已保存：" + ScenePath + " 角色立起旋转=" + character.transform.localRotation.eulerAngles);
    }
}
