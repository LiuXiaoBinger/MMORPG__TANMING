# RO 首个副本场景复制迁移 — 正式变更日志

- 业务任务编号：`RO-FIRST-DUNGEON-SCENE-20261026`
- 记录任务编号：`RO-FIRST-DUNGEON-RECORDS-20261026`
- 业务 Hub：`hub-0f37bdcc-e89f-4a17-8020-9f9c2024ade5`；记录 Hub：`hub-31657e8b-efce-401c-b501-0420b64b2b5b`
- 会话：`5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`（WorkBuddy 桌面会话，desktop_queue）
- 日期：2026-10-26
- 模型：`deepseek-v4-flash`（业务 4 轮与本次记录均未切换）
- 性质：**资源复制迁移 + 最小视觉适配**。不是可玩副本，不含副本玩法、组队、阵营或服务端实现。

## 1. 结论

| 项 | 结果 |
|---|---|
| 迁入副本 | 1 个：RO `sc_dungeon_duobiwanfa_001`（小型躲避玩法副本，172 个 Renderer 的真实几何）→ `RODungeon_First.unity` |
| 落盘规模 | **251 个文件 / 18,661,241 字节 / 17.80 MiB**（本次验收 / 拟提交范围；目录内 250 = 98 资源 + 152 `.meta`，其中 54 个目录 `.meta`；另加目录外同级 `RODungeon_First.meta` 1 个。记录期复核发现单项漂移，现已核实为一次 Unity 编辑器保存，见第 5 节末注） |
| 源工程 | 只读。**这是复制，不是移动**，源场景与全部源资源保持原样 |
| RO 脚本 | 移除 7 个组件 / 2 个类型（`FxAnimationHelper` ×6、`MoonClient.dll` 内组件 ×1）；未复制任何 `.cs` / `.dll` / `.asmdef` |
| 当前状态 | MonoBehaviour 0 组件 / 0 类型，Missing Script 0 |
| 视觉 | 4 轮迭代后几何与贴图可辨；洋红 0；R3 的整体偏绿已消除（G−R +19.7 → +0.1） |
| 未做 | 未接 Boot / 配置表 / YooAsset 运行时加载；无副本玩法、组队、阵营、服务端、打包或联调 |

## 2. 授权边界与任务身份

| 角色 | 实际承担 |
|---|---|
| WorkBuddy 桌面会话（本会话） | 场景筛选、依赖闭包、复制迁移、脚本剥离、材质适配、着色器编写、Unity 编辑器内实测与截图自检、逐轮返修 |
| Codex（主 Agent） | 派工与逐轮验收、读源码与截图判定、Git 分支建立与最终集成、本记录任务的授权范围划定 |
| 用户 | 授权迁移范围与正式日志清单，要求"移动到游戏"按复制处理 |

- WorkBuddy 实际使用工具：Unity MCP `execute_code` / `read_console`、Bash、PowerShell、Read / Write / Edit、本地 Python 脚本。**未通过 Skill 机制加载任何技能包**，也未使用子 Agent；业务 4 轮的记录由本会话直接产出，本记录任务另行独立记录。
- 本次记录任务只写文档：不改资源、不改业务代码、不改 Git。记录轮次与迁移轮次分开计数，不把记录轮算成第 5 轮迁移。

## 3. 迁移对象与选择依据

- 源场景：`F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/Resources/Scenes/sc_dungeon_duobiwanfa_001/sc_dungeon_duobiwanfa_001.unity`（932,010 字节，只读未变；剥离 7 个 RO MonoBehaviour 后目标为 904,144 字节）
- 目标场景：`LYF_MMO/Assets/GM_Game/Scenes/RODungeon_First/RODungeon_First.unity`
- 选择依据：
  1. 属客户端 `artres/Resources/Scenes` 下 `sc_dungeon_` 前缀的独立实例场景，根对象即场景名，可直接判定为副本而非城市场景。
  2. **先排除 `sc_dungeon_shenggeshilian`**：它只有 238 KB，内容是 Cube 占位和「这里放交互物件」类模板空节点，不是真副本；不能用模板冒充副本。
  3. 在确认为真几何的候选中取体积最小者：`duobiwanfa` 932 KB < `mayidong` 1.09 MB < `jifendixia` 1.31 MB < `beisenbianyi` 4.36 MB，便于首先验证。
  4. 源工程与目标工程同为 Unity 2023.2.20f1c1、Built-in 渲染管线，无管线迁移成本。

## 4. 源到目标映射（97 条复制资源）

源路径相对 `clientproj/Assets`，目标路径相对 `GM_Game/Scenes`。本表来自迁移首轮清单 `_temp/ro-first-dungeon-migration-manifest.json` 的 `manifest` 字段，共 97 条，与后续 `.meta` 无关。

| # | 源（相对 clientproj/Assets） | 目标（相对 GM_Game/Scenes） | 字节 |
|---|---|---|---|
| 1 | `artres/Resources/Scenes/sc_dungeon_duobiwanfa_001/sc_dungeon_duobiwanfa_001.unity` | `RODungeon_First/RODungeon_First.unity` | 904144 |
| 2 | `artres/Resources/Shader/RO/Include/CautionCommon.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CautionCommon.cginc` | 7742 |
| 3 | `artres/Resources/Shader/RO/Include/ColorCorrect.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/ColorCorrect.cginc` | 2012 |
| 4 | `artres/Resources/Shader/RO/Include/CreatureCommon.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CreatureCommon.cginc` | 32073 |
| 5 | `artres/Resources/Shader/RO/Include/DepthShadow.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/DepthShadow.cginc` | 2421 |
| 6 | `artres/Resources/Shader/RO/Include/EffectCommon.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/EffectCommon.cginc` | 17246 |
| 7 | `artres/Resources/Shader/RO/Include/GpuSkinning.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/GpuSkinning.cginc` | 4940 |
| 8 | `artres/Resources/Shader/RO/Include/SceneCommon.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneCommon.cginc` | 44935 |
| 9 | `artres/Resources/Shader/RO/Include/SceneMap.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMap.cginc` | 10650 |
| 10 | `artres/Resources/Shader/RO/Include/SceneMeta.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMeta.cginc` | 2060 |
| 11 | `artres/Resources/Shader/RO/Include/SceneWaterFoam.cginc` | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneWaterFoam.cginc` | 8695 |
| 12 | `artres/Resources/Shader/RO/Scene/ROScene.shader` | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROScene.shader` | 8614 |
| 13 | `artres/Resources/Shader/RO/Scene/ROSceneUnderWater.shader` | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROSceneUnderWater.shader` | 9943 |
| 14 | `artres/Resources/Shader/RO/Scene/ROTerrain.shader` | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROTerrain.shader` | 21320 |
| 15 | `artres/Resources/Shader/RO/Scene/ROWater.shader` | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROWater.shader` | 7719 |
| 16 | `artres/_GameRes/DemoTest/sc_blackbox.prefab` | `RODungeon_First/RO/artres/_GameRes/DemoTest/sc_blackbox.prefab` | 2274 |
| 17 | `artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/Item_Scene_QiQiu.controller` | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/Item_Scene_QiQiu.controller` | 2030 |
| 18 | `artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/sc_qiuqian_001.mat` | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/sc_qiuqian_001.mat` | 1454 |
| 19 | `artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/sc_aierbeita_002_01.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/sc_aierbeita_002_01.mat` | 1757 |
| 20 | `artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/sc_aierbeita_002_01.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/sc_aierbeita_002_01.tga` | 786476 |
| 21 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/sc_liwu_001.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/sc_liwu_001.mat` | 1452 |
| 22 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_liwu_001b.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_liwu_001b.FBX` | 35328 |
| 23 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001a.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001a.FBX` | 73152 |
| 24 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001c.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001c.FBX` | 29216 |
| 25 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001d.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001d.FBX` | 29008 |
| 26 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001f.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001f.FBX` | 29424 |
| 27 | `artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/sc_liwu_001.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/sc_liwu_001.tga` | 1048620 |
| 28 | `artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/sc_qiuqian_001b.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/sc_qiuqian_001b.FBX` | 278064 |
| 29 | `artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/sc_qiuqian_001.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/sc_qiuqian_001.tga` | 1048620 |
| 30 | `artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001c.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001c.FBX` | 61552 |
| 31 | `artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001d.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001d.FBX` | 80000 |
| 32 | `artres/_GameRes/Scenes/Common/Doodad/sc_dungeon_duobiwanfa_guanzi_001.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_dungeon_duobiwanfa_guanzi_001.prefab` | 2270 |
| 33 | `artres/_GameRes/Scenes/Common/Doodad/sc_liuliuhoulouti_001.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liuliuhoulouti_001.prefab` | 2310 |
| 34 | `artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001a.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001a.prefab` | 2262 |
| 35 | `artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001b.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001b.prefab` | 2290 |
| 36 | `artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001c.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001c.prefab` | 2271 |
| 37 | `artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001d.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001d.prefab` | 2264 |
| 38 | `artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001f.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001f.prefab` | 2271 |
| 39 | `artres/_GameRes/Scenes/Common/Doodad/sc_qiuqian_001b.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_qiuqian_001b.prefab` | 3919 |
| 40 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_004.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_004.FBX` | 27184 |
| 41 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_005.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_005.FBX` | 30336 |
| 42 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_006.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_006.FBX` | 26768 |
| 43 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_007.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_007.FBX` | 24912 |
| 44 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_008.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_008.FBX` | 25136 |
| 45 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_009.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_009.FBX` | 25152 |
| 46 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_001.FBX` | 38080 |
| 47 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_002.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_002.FBX` | 22416 |
| 48 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_003.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_003.FBX` | 22448 |
| 49 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_004.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_004.FBX` | 22416 |
| 50 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_006.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_006.FBX` | 22448 |
| 51 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_007.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_007.FBX` | 22416 |
| 52 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_009.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_009.FBX` | 21904 |
| 53 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_010.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_010.FBX` | 21904 |
| 54 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_011.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_011.FBX` | 23696 |
| 55 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_003_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_003_001.FBX` | 160544 |
| 56 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_004_002.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_004_002.FBX` | 26064 |
| 57 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_001.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_001.tga` | 3145772 |
| 58 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_002.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_002.tga` | 786476 |
| 59 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_003.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_003.tga` | 3145772 |
| 60 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_004.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_004.tga` | 786476 |
| 61 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_taijie_001.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_taijie_001.tga` | 196652 |
| 62 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_001.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_001.mat` | 2153 |
| 63 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_002.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_002.mat` | 1426 |
| 64 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_003.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_003.mat` | 1424 |
| 65 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_004.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_004.mat` | 1424 |
| 66 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_taijie_001.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_taijie_001.mat` | 1427 |
| 67 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_004.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_004.prefab` | 2266 |
| 68 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_005.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_005.prefab` | 2270 |
| 69 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_006.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_006.prefab` | 2258 |
| 70 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_007.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_007.prefab` | 2264 |
| 71 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_008.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_008.prefab` | 2277 |
| 72 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_009.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_009.prefab` | 2265 |
| 73 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_001.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_001.prefab` | 2276 |
| 74 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_006.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_006.prefab` | 2277 |
| 75 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_007.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_007.prefab` | 2277 |
| 76 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_009.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_009.prefab` | 2275 |
| 77 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_010.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_010.prefab` | 2276 |
| 78 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_011.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_011.prefab` | 2275 |
| 79 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_003_001.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_003_001.prefab` | 5531 |
| 80 | `artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_004_002.prefab` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_004_002.prefab` | 2266 |
| 81 | `artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg` | 1277312 |
| 82 | `artres/_GameRes/Scenes/Common/Skybox/ReflectionProbe-0.exr` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/ReflectionProbe-0.exr` | 632852 |
| 83 | `artres/_GameRes/Scenes/Common/Water/Textures/sc_water_map_006.psd` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_map_006.psd` | 39636 |
| 84 | `artres/_GameRes/Scenes/Common/Water/Textures/sc_water_nrm_005.jpg` | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_nrm_005.jpg` | 138555 |
| 85 | `artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_lp_sc_dungeon_duobiwanfa_001_00.asset` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_lp_sc_dungeon_duobiwanfa_001_00.asset` | 1919543 |
| 86 | `artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_sc_dungeon_duobiwanfa_001_00_00.exr` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_sc_dungeon_duobiwanfa_001_00_00.exr` | 834270 |
| 87 | `artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/ReflectionProbe-0.exr` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/ReflectionProbe-0.exr` | 198109 |
| 88 | `artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001c.controller` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001c.controller` | 2038 |
| 89 | `artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001d.controller` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001d.controller` | 2038 |
| 90 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/sc_dungeon_duobiwanfa_water_001.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/sc_dungeon_duobiwanfa_water_001.mat` | 1817 |
| 91 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001.FBX` | 79824 |
| 92 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001_b.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001_b.FBX` | 27152 |
| 93 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_guanzi_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_guanzi_001.FBX` | 34400 |
| 94 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_water_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_water_001.FBX` | 25552 |
| 95 | `artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/sc_liuliuhoulouti_001.FBX` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/sc_liuliuhoulouti_001.FBX` | 24368 |
| 96 | `artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/sc_zhuanzhidating_box.mat` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/sc_zhuanzhidating_box.mat` | 1423 |
| 97 | `artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/sc_zhuanzhidating_back_001.tga` | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/sc_zhuanzhidating_back_001.tga` | 56 |

**未复制项（有明确理由，不是遗漏）**：

| 源路径 | 字节 | 理由 |
|---|---|---|
| `Plugins/GameLibs/MoonClient.dll` | 18,740,224 | RO 脚本 / DLL 不移植 |
| `Plugins/GameLibs/MoonSerializable.dll` | 42,496 | RO 脚本 / DLL 不移植 |
| `Scripts/Effects/FxAnimation/FxAnimationHelper.cs` | 59,895 | RO 脚本 / DLL 不移植 |
| `artres/Resources/SceneEnviroData/cl_..._default.asset` | 799 | 仅被被剥离的 RO MonoBehaviour 引用 |
| `artres/Resources/SceneEnviroData/dt_..._default.asset` | 1,588 | 同上 |
| `artres/Resources/SceneEnviroData/lm_..._default.asset` | 613 | 同上 |
| `artres/Resources/SceneEnviroData/wt_..._default.asset` | 2,262 | 同上 |

## 5. 完整逐项清单（251 条）

相对路径基准为 `GM_Game/Scenes`。序号 1–250 在 `RODungeon_First/` 目录内部，第 251 条为目录外同级目录 `.meta`。本表为持久清单，**不依赖 `_temp` 下的临时文件**；逐字节可复算脚本为 `_temp/ro-first-dungeon-manifest-r4.py`。

| # | 相对路径 | 类型 | 变化 | 用途 | 字节 |
|---|---|---|---|---|---|
| 1 | `RODungeon_First/RO/Shaders/DungeonROVertexColorSafe.shader` | shader | 新增（新建）：R3 建立、R4 重写为最简实现 | 本任务新建最简贴图着色器（无 RO 源文件，非复制） | 2873 |
| 2 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CautionCommon.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 7742 |
| 3 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/ColorCorrect.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 2012 |
| 4 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CreatureCommon.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 32073 |
| 5 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/DepthShadow.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 2421 |
| 6 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/EffectCommon.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 17246 |
| 7 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/GpuSkinning.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 4940 |
| 8 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneCommon.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 44935 |
| 9 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMap.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 10650 |
| 10 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMeta.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 2060 |
| 11 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneWaterFoam.cginc` | cginc | 新增（复制） | RO 着色器 include（按原相对路径保留，避免 #include 断链） | 8695 |
| 12 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROScene.shader` | shader | 新增（复制） | RO 场景着色器（原样复制保留供追溯；本场景材质已不再引用） | 8614 |
| 13 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROSceneUnderWater.shader` | shader | 新增（复制） | RO 场景着色器（原样复制保留供追溯；本场景材质已不再引用） | 9943 |
| 14 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROTerrain.shader` | shader | 新增（复制） | RO 场景着色器（原样复制保留供追溯；本场景材质已不再引用） | 21320 |
| 15 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROWater.shader` | shader | 新增（复制） | RO 场景着色器（原样复制保留供追溯；本场景材质已不再引用） | 7719 |
| 16 | `RODungeon_First/RO/artres/_GameRes/DemoTest/sc_blackbox.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2274 |
| 17 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/Item_Scene_QiQiu.controller` | controller | 新增（复制） | Animator 控制器（装饰动画，不含 RO 脚本） | 2030 |
| 18 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/sc_qiuqian_001.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1619 |
| 19 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/sc_aierbeita_002_01.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1910 |
| 20 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/sc_aierbeita_002_01.tga` | tga | 新增（复制） | 贴图（TGA） | 786476 |
| 21 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/sc_liwu_001.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1617 |
| 22 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_liwu_001b.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 35328 |
| 23 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001a.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 73152 |
| 24 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001c.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 29216 |
| 25 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001d.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 29008 |
| 26 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001f.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 29424 |
| 27 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/sc_liwu_001.tga` | tga | 新增（复制） | 贴图（TGA） | 1048620 |
| 28 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/sc_qiuqian_001b.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 278064 |
| 29 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/sc_qiuqian_001.tga` | tga | 新增（复制） | 贴图（TGA） | 1048620 |
| 30 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001c.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 61552 |
| 31 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001d.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 80000 |
| 32 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_dungeon_duobiwanfa_guanzi_001.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2270 |
| 33 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liuliuhoulouti_001.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2310 |
| 34 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001a.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2262 |
| 35 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001b.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2290 |
| 36 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001c.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2271 |
| 37 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001d.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2264 |
| 38 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001f.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2271 |
| 39 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_qiuqian_001b.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 3919 |
| 40 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_004.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 27184 |
| 41 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_005.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 30336 |
| 42 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_006.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 26768 |
| 43 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_007.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 24912 |
| 44 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_008.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 25136 |
| 45 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_009.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 25152 |
| 46 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 38080 |
| 47 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_002.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 22416 |
| 48 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_003.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 22448 |
| 49 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_004.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 22416 |
| 50 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_006.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 22448 |
| 51 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_007.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 22416 |
| 52 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_009.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 21904 |
| 53 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_010.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 21904 |
| 54 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_011.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 23696 |
| 55 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_003_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 160544 |
| 56 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_004_002.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 26064 |
| 57 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_001.tga` | tga | 新增（复制） | 贴图（TGA） | 3145772 |
| 58 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_002.tga` | tga | 新增（复制） | 贴图（TGA） | 786476 |
| 59 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_003.tga` | tga | 新增（复制） | 贴图（TGA） | 3145772 |
| 60 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_004.tga` | tga | 新增（复制） | 贴图（TGA） | 786476 |
| 61 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_taijie_001.tga` | tga | 新增（复制） | 贴图（TGA） | 196652 |
| 62 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_001.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 2295 |
| 63 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_002.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1579 |
| 64 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_003.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1577 |
| 65 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_004.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1577 |
| 66 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_taijie_001.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1580 |
| 67 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_004.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2266 |
| 68 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_005.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2270 |
| 69 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_006.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2258 |
| 70 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_007.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2264 |
| 71 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_008.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2277 |
| 72 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_009.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2265 |
| 73 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_001.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2276 |
| 74 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_006.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2277 |
| 75 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_007.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2277 |
| 76 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_009.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2275 |
| 77 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_010.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2276 |
| 78 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_011.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2275 |
| 79 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_003_001.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 5531 |
| 80 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_004_002.prefab` | prefab | 新增（复制） | 静态装饰预制体（仅 Transform/MeshFilter/MeshRenderer/LODGroup，无脚本） | 2266 |
| 81 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg` | jpg | 新增（复制） | 贴图 / 天空盒（JPG） | 1277312 |
| 82 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/ReflectionProbe-0.exr` | exr | 新增（复制） | 光照贴图 / 反射探针数据（EXR） | 632852 |
| 83 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_map_006.psd` | psd | 新增（复制） | 水面贴图源文件（PSD） | 39636 |
| 84 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_nrm_005.jpg` | jpg | 新增（复制） | 贴图 / 天空盒（JPG） | 138555 |
| 85 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_lp_sc_dungeon_duobiwanfa_001_00.asset` | asset | 新增（复制） | 光照数据资产（LightingData 对应项） | 1919543 |
| 86 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_sc_dungeon_duobiwanfa_001_00_00.exr` | exr | 新增（复制） | 光照贴图 / 反射探针数据（EXR） | 834270 |
| 87 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/ReflectionProbe-0.exr` | exr | 新增（复制） | 光照贴图 / 反射探针数据（EXR） | 198109 |
| 88 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001c.controller` | controller | 新增（复制） | Animator 控制器（装饰动画，不含 RO 脚本） | 2038 |
| 89 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001d.controller` | controller | 新增（复制） | Animator 控制器（装饰动画，不含 RO 脚本） | 2038 |
| 90 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/sc_dungeon_duobiwanfa_water_001.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1960 |
| 91 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 79824 |
| 92 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001_b.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 27152 |
| 93 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_guanzi_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 34400 |
| 94 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_water_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 25552 |
| 95 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/sc_liuliuhoulouti_001.FBX` | fbx | 新增（复制） | 场景几何网格（FBX） | 24368 |
| 96 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/sc_zhuanzhidating_box.mat` | mat | 新增（复制）+ 内容修改：R2–R4 改指本目录 `LYF/Dungeon/ROVertexColorSafe` | 材质（保留原贴图 / UV 平铺 / _Color=0.5） | 1604 |
| 97 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/sc_zhuanzhidating_back_001.tga` | tga | 新增（复制） | 贴图（TGA） | 56 |
| 98 | `RODungeon_First/RODungeon_First.unity` | unity | 新增（复制）+ 内容修改：剥离 7 个 RO MonoBehaviour | 副本场景主文件；根对象 1 个、172 个 Renderer、10 个材质槽位 | 904144 |
| 99 | `RODungeon_First/RO.meta` | meta | 新增（复制） | 目录 `RO/` 的导入元数据（Unity 自动生成） | 172 |
| 100 | `RODungeon_First/RO/Shaders.meta` | meta | 新增（复制） | 目录 `RO/Shaders/` 的导入元数据（Unity 自动生成） | 172 |
| 101 | `RODungeon_First/RO/Shaders/DungeonROVertexColorSafe.shader.meta` | meta | 新增（复制） | `RO/Shaders/DungeonROVertexColorSafe.shader` 的导入元数据（Unity 自动生成，保持原 GUID） | 204 |
| 102 | `RODungeon_First/RO/artres.meta` | meta | 新增（复制） | 目录 `RO/artres/` 的导入元数据（Unity 自动生成） | 172 |
| 103 | `RODungeon_First/RO/artres/Resources.meta` | meta | 新增（复制） | 目录 `RO/artres/Resources/` 的导入元数据（Unity 自动生成） | 172 |
| 104 | `RODungeon_First/RO/artres/Resources/Shader.meta` | meta | 新增（复制） | 目录 `RO/artres/Resources/Shader/` 的导入元数据（Unity 自动生成） | 172 |
| 105 | `RODungeon_First/RO/artres/Resources/Shader/RO.meta` | meta | 新增（复制） | 目录 `RO/artres/Resources/Shader/RO/` 的导入元数据（Unity 自动生成） | 172 |
| 106 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include.meta` | meta | 新增（复制） | 目录 `RO/artres/Resources/Shader/RO/Include/` 的导入元数据（Unity 自动生成） | 172 |
| 107 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CautionCommon.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/CautionCommon.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 217 |
| 108 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/ColorCorrect.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/ColorCorrect.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 217 |
| 109 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/CreatureCommon.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/CreatureCommon.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 195 |
| 110 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/DepthShadow.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/DepthShadow.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 196 |
| 111 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/EffectCommon.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/EffectCommon.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 176 |
| 112 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/GpuSkinning.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/GpuSkinning.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 217 |
| 113 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneCommon.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/SceneCommon.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 176 |
| 114 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMap.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/SceneMap.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 195 |
| 115 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneMeta.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/SceneMeta.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 176 |
| 116 | `RODungeon_First/RO/artres/Resources/Shader/RO/Include/SceneWaterFoam.cginc.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Include/SceneWaterFoam.cginc` 的导入元数据（Unity 自动生成，保持原 GUID） | 176 |
| 117 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene.meta` | meta | 新增（复制） | 目录 `RO/artres/Resources/Shader/RO/Scene/` 的导入元数据（Unity 自动生成） | 172 |
| 118 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROScene.shader.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Scene/ROScene.shader` 的导入元数据（Unity 自动生成，保持原 GUID） | 217 |
| 119 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROSceneUnderWater.shader.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Scene/ROSceneUnderWater.shader` 的导入元数据（Unity 自动生成，保持原 GUID） | 204 |
| 120 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROTerrain.shader.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Scene/ROTerrain.shader` 的导入元数据（Unity 自动生成，保持原 GUID） | 217 |
| 121 | `RODungeon_First/RO/artres/Resources/Shader/RO/Scene/ROWater.shader.meta` | meta | 新增（复制） | `RO/artres/Resources/Shader/RO/Scene/ROWater.shader` 的导入元数据（Unity 自动生成，保持原 GUID） | 218 |
| 122 | `RODungeon_First/RO/artres/_GameRes.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/` 的导入元数据（Unity 自动生成） | 172 |
| 123 | `RODungeon_First/RO/artres/_GameRes/DemoTest.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/DemoTest/` 的导入元数据（Unity 自动生成） | 172 |
| 124 | `RODungeon_First/RO/artres/_GameRes/DemoTest/sc_blackbox.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/DemoTest/sc_blackbox.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 125 | `RODungeon_First/RO/artres/_GameRes/Item.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Item/` 的导入元数据（Unity 自动生成） | 172 |
| 126 | `RODungeon_First/RO/artres/_GameRes/Item/Scene.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Item/Scene/` 的导入元数据（Unity 自动生成） | 172 |
| 127 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/` 的导入元数据（Unity 自动生成） | 172 |
| 128 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/` 的导入元数据（Unity 自动生成） | 172 |
| 129 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/Item_Scene_QiQiu.controller.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Anims/Item_Scene_QiQiu.controller` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 130 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/` 的导入元数据（Unity 自动生成） | 172 |
| 131 | `RODungeon_First/RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/sc_qiuqian_001.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Item/Scene/Item_Scene_QiuQian/Materials/sc_qiuqian_001.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 229 |
| 132 | `RODungeon_First/RO/artres/_GameRes/Scenes.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/` 的导入元数据（Unity 自动生成） | 172 |
| 133 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/` 的导入元数据（Unity 自动生成） | 172 |
| 134 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Building/` 的导入元数据（Unity 自动生成） | 172 |
| 135 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/` 的导入元数据（Unity 自动生成） | 172 |
| 136 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/` 的导入元数据（Unity 自动生成） | 172 |
| 137 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/sc_aierbeita_002_01.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Materials/sc_aierbeita_002_01.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 138 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/` 的导入元数据（Unity 自动生成） | 172 |
| 139 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/sc_aierbeita_002_01.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Building/sc_aierbeita_001/Texture/sc_aierbeita_002_01.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 140 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/` 的导入元数据（Unity 自动生成） | 172 |
| 141 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/` 的导入元数据（Unity 自动生成） | 172 |
| 142 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 143 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/` 的导入元数据（Unity 自动生成） | 172 |
| 144 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/sc_liwu_001.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/Materials/sc_liwu_001.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 229 |
| 145 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_liwu_001b.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_liwu_001b.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2511 |
| 146 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001a.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001a.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2511 |
| 147 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001c.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001c.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2511 |
| 148 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001d.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001d.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2511 |
| 149 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001f.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/mesh/sc_sc_liwu_001f.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2511 |
| 150 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/` 的导入元数据（Unity 自动生成） | 172 |
| 151 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/sc_liwu_001.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_liwu_001/texture/sc_liwu_001.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2775 |
| 152 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/` 的导入元数据（Unity 自动生成） | 172 |
| 153 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 154 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/sc_qiuqian_001b.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/mesh/sc_qiuqian_001b.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 3930 |
| 155 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/` 的导入元数据（Unity 自动生成） | 172 |
| 156 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/sc_qiuqian_001.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/RO_qiuqian_001/texture/sc_qiuqian_001.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2775 |
| 157 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/` 的导入元数据（Unity 自动生成） | 172 |
| 158 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 159 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001c.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001c.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 3482 |
| 160 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001d.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_aierbeita_001/Mesh/sc_aierbeita_yongchi_001d.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 3696 |
| 161 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_dungeon_duobiwanfa_guanzi_001.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_dungeon_duobiwanfa_guanzi_001.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 162 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liuliuhoulouti_001.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liuliuhoulouti_001.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 163 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001a.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001a.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 164 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001b.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001b.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 165 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001c.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001c.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 166 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001d.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001d.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 167 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001f.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_liwu_001f.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 168 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_qiuqian_001b.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_qiuqian_001b.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 231 |
| 169 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/` 的导入元数据（Unity 自动生成） | 172 |
| 170 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 171 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_004.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_004.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2451 |
| 172 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_005.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_005.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 173 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_006.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_006.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 174 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_007.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_007.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 175 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_008.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_008.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 176 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_009.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_001_009.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 177 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 178 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_002.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_002.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 179 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_003.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_003.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 180 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_004.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_004.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 181 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_006.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_006.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 182 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_007.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_007.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 183 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_009.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_009.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2405 |
| 184 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_010.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_010.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2405 |
| 185 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_011.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_002_011.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2429 |
| 186 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_003_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_003_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2955 |
| 187 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_004_002.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Mesh/sc_wjgc_004_002.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2405 |
| 188 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/` 的导入元数据（Unity 自动生成） | 172 |
| 189 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_001.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_001.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 190 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_002.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_002.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 191 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_003.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_003.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 192 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_004.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_001_004.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 193 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_taijie_001.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/Texture/sc_wjgc_taijie_001.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2496 |
| 194 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/` 的导入元数据（Unity 自动生成） | 172 |
| 195 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_001.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_001.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 196 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_002.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_002.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 197 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_003.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_003.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 198 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_004.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_001_004.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 199 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_taijie_001.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001/materials/sc_wjgc_taijie_001.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 200 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_004.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_004.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 201 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_005.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_005.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 202 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_006.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_006.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 203 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_007.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_007.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 204 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_008.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_008.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 205 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_009.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_001_009.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 206 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_001.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_001.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 207 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_006.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_006.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 208 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_007.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_007.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 209 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_009.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_009.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 210 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_010.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_010.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 211 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_011.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_002_011.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 212 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_003_001.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_003_001.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 213 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_004_002.prefab.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Doodad/sc_wjgc_004_002.prefab` 的导入元数据（Unity 自动生成，保持原 GUID） | 190 |
| 214 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Skybox/` 的导入元数据（Unity 自动生成） | 172 |
| 215 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg` 的导入元数据（Unity 自动生成，保持原 GUID） | 2807 |
| 216 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Skybox/ReflectionProbe-0.exr.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Skybox/ReflectionProbe-0.exr` 的导入元数据（Unity 自动生成，保持原 GUID） | 2803 |
| 217 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Water/` 的导入元数据（Unity 自动生成） | 172 |
| 218 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/Common/Water/Textures/` 的导入元数据（Unity 自动生成） | 172 |
| 219 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_map_006.psd.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_map_006.psd` 的导入元数据（Unity 自动生成，保持原 GUID） | 2768 |
| 220 | `RODungeon_First/RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_nrm_005.jpg.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/Common/Water/Textures/sc_water_nrm_005.jpg` 的导入元数据（Unity 自动生成，保持原 GUID） | 2774 |
| 221 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Scenes/` 的导入元数据（Unity 自动生成） | 172 |
| 222 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/` 的导入元数据（Unity 自动生成） | 172 |
| 223 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/` 的导入元数据（Unity 自动生成） | 172 |
| 224 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_lp_sc_dungeon_duobiwanfa_001_00.asset.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_lp_sc_dungeon_duobiwanfa_001_00.asset` 的导入元数据（Unity 自动生成，保持原 GUID） | 189 |
| 225 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_sc_dungeon_duobiwanfa_001_00_00.exr.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_duobiwanfa_001/lm_sc_dungeon_duobiwanfa_001_00_00.exr` 的导入元数据（Unity 自动生成，保持原 GUID） | 2488 |
| 226 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/` 的导入元数据（Unity 自动生成） | 172 |
| 227 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/` 的导入元数据（Unity 自动生成） | 172 |
| 228 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/ReflectionProbe-0.exr.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Scenes/sc_shamochadao_001/sc_shamochadao_001/ReflectionProbe-0.exr` 的导入元数据（Unity 自动生成，保持原 GUID） | 2518 |
| 229 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/` 的导入元数据（Unity 自动生成） | 172 |
| 230 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/` 的导入元数据（Unity 自动生成） | 172 |
| 231 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/` 的导入元数据（Unity 自动生成） | 172 |
| 232 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001c.controller.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001c.controller` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 233 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001d.controller.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_aierbeita_001/anim/sc_aierbeita_yongchi_001d.controller` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 234 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/` 的导入元数据（Unity 自动生成） | 172 |
| 235 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/` 的导入元数据（Unity 自动生成） | 172 |
| 236 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/sc_dungeon_duobiwanfa_water_001.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Materials/sc_dungeon_duobiwanfa_water_001.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 188 |
| 237 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 238 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2569 |
| 239 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001_b.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_001_b.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2417 |
| 240 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_guanzi_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_guanzi_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2422 |
| 241 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_water_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_duobiwanfa_001/Mesh/sc_dungeon_duobiwanfa_water_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2421 |
| 242 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/` 的导入元数据（Unity 自动生成） | 172 |
| 243 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/` 的导入元数据（Unity 自动生成） | 172 |
| 244 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/sc_liuliuhoulouti_001.FBX.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_dungeon_liuliuhou_001/mesh/sc_liuliuhoulouti_001.FBX` 的导入元数据（Unity 自动生成，保持原 GUID） | 2435 |
| 245 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/` 的导入元数据（Unity 自动生成） | 172 |
| 246 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/` 的导入元数据（Unity 自动生成） | 172 |
| 247 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/sc_zhuanzhidating_box.mat.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Materials/sc_zhuanzhidating_box.mat` 的导入元数据（Unity 自动生成，保持原 GUID） | 229 |
| 248 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture.meta` | meta | 新增（复制） | 目录 `RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/` 的导入元数据（Unity 自动生成） | 172 |
| 249 | `RODungeon_First/RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/sc_zhuanzhidating_back_001.tga.meta` | meta | 新增（复制） | `RO/artres/_GameRes/Scenes/_Shared/sc_zhuanzhidating_001/Texture/sc_zhuanzhidating_back_001.tga` 的导入元数据（Unity 自动生成，保持原 GUID） | 2776 |
| 250 | `RODungeon_First/RODungeon_First.unity.meta` | meta | 新增（复制） | `RODungeon_First.unity` 的导入元数据（Unity 自动生成，保持原 GUID） | 155 |
| 251 | `RODungeon_First.meta` | meta | 新增（Unity 自动生成）：R2 清单遗漏、R3 补入 | 目录 `RODungeon_First/` 的导入元数据（目录外同级） | 172 |

**分类合计**：`.meta` 152（其中目录 `.meta` 54）｜`.fbx` 30｜`.prefab` 23｜`.cginc` 10｜`.mat` 10｜`.tga` 9｜`.shader` 5（4 个 RO 复制 + 1 个本任务新建）｜`.controller` 3｜`.exr` 3｜`.jpg` 2｜`.psd` 1｜`.asset` 1｜`.unity` 1；另加目录外同级 `.meta` 1。

**记录时复核（2026-10-26 22:39 之后）**：上表字节是 R4 验收时（`manifest-r4.json` 生成时）的落盘值，合计 18,661,241 字节，与主 Agent 的逐数核对一致。本次记录任务开工前复核，发现**只有 `RODungeon_First.unity` 一项漂移**：904,144 → 946,305 字节（+42,161），文件 mtime 为 2026-10-26 22:39:09；其余 250 项的文件数与字节均未变，当前磁盘总计 18,703,402 字节（该合计只加了场景增量，漏了保存时新生成的两个 lighting 文件；正确值 18,705,374，见下段更正）。该文件在本次记录任务中未被打开或修改（本任务未启动 Unity、未写任何资源），漂移发生在记录任务开工之前，**具体触发原因未逐行核实**。复核同时确认结构仍正常：884 个对象块、`MonoBehaviour` 0、`PrefabInstance` 171。上表保留验收时数值以便与 `manifest-r4.json` 对齐，不按当前磁盘回改。

**漂移已核实（只读核验，不计入迁移轮次；时间以平台实际事件为准）**：上一段的"具体触发原因未逐行核实"是记录任务开工时的**首次发现状态**，保留不改；其后按业务任务补做了一轮**只读**核验（平台第 5 轮，attempt `run-77bf2883-af7b-49c1-aba9-fefa6099f8b9`——这是核验轮，不是第 5 轮迁移开发，迁移仍为 4 轮）。核验方法与结论：

1. **先拿到逐字节等价的旧基线**：R1 的迁移脚本是确定性的（读源场景 → 删全部 `!u!114` → 删保留集外的 `!u!1001` → 从 GameObject 组件列表移除被删条目 → 其余逐字节保留）。用同一规则在 `_temp` 重跑（只写 `_temp`，未执行会覆盖正式目标的旧脚本），重建结果**精确 904,144 字节**，sha256 `fa99118e6d66eac3bdd5d4ce7e9210c8e333ad5051b178578d021a479688d8e9`。所以下面是逐字节差分，不是按体积推测。
2. **当前文件**：946,305 字节，sha256 `55f9981e887f0a06a82c4588d85b020de9e3d87408e029ee34b3949d92f6eea5`；只读快照 `_temp/ro-first-dungeon-drift-current.unity`（与目标逐字节相同）。结构计数自洽：171 个 `PrefabInstance` = 170 既有 + 1 新增，`MonoBehaviour` 仍 0。
3. **字节归因（合计精确等于 +42,161）**：

| 项 | 字节 |
|---|---|
| 新增 `PrefabInstance &71181203`（`Role_MJS` 角色预制体实例，根级，位置 80.512 / 9.772 / 24.403） | +2,408 |
| 新增 `SceneRoots &9223372036854775807`（`m_Roots = [2104064828, 71181203]`） | +133 |
| 170 个既有 `PrefabInstance`（`m_Modification` 加 `serializedVersion: 3`、新增空的 `m_AddedGameObjects` / `m_RemovedGameObjects`、显式记录既有 `m_AddedComponents`、修改项重排） | +40,907（231~424 / 个） |
| YAML 折行合并（`MeshRenderer` ×170 −680、`GameObject` ×172 −688、`Transform` ×181 −251、`SkinnedMeshRenderer` ×2 −8） | −1,627 |
| `LightmapSettings` +286 / `NavMeshSettings` +53 / `RenderSettings` +1（`serializedVersion` 升级） | +340 |

4. **语义等价（不是"文件被改坏"）**：170 个既有实例的 `m_Modifications` 展开折行、去掉 `propertyPath` 单引号后逐键对比，**缺失 0 / 多出 0 / 值变化 0**；`m_AddedComponents` 指向的 172 个 `addedObject` 全是基线中已存在的 `LODGroup`（class 205），不是新增组件；`m_RemovedComponents` / `m_RemovedGameObjects` 全为 `[]`；场景内 `m_Script:` 引用 0，`!u!114` / `!u!20` / `!u!108` 文档均为 0；GUID 引用集合 39 → 41，**新增仅 2 个**（`Role_MJS.prefab`、`RODungeon_FirstSettings.lighting`），39 个 RO 资源引用**一个未丢**。
5. **目录口径更正**：Unity 保存时同时生成 `RODungeon_FirstSettings.lighting`（1,772 字节）与其 `.meta`（200 字节），当前目录为 **253 文件 / 18,705,374 字节**（= 251 + 2 文件；= 18,661,241 + 42,161 + 1,972）。上一段写的 18,703,402 只加了场景增量、**漏了这两个 lighting 文件**，此处更正；上方 251 条表仍保留验收时字节，不按当前磁盘回改。
6. **性质与归因边界**：由**一次 Unity 编辑器保存**产生（`Editor.log` 顺序：多次附加加载 → 一次非附加打开 → `.unity` 与 `.lighting` 同时被写入并重新导入 → `Reloading assemblies for play mode.`）。**执行保存的是谁无法从文件证据判定，未推定**：既不写成"用户亲手添加"，也不归到本任务的迁移脚本——`_temp/ro-first-dungeon-*.py` 与本会话历史中都没有 `SaveScene` / `Role_MJS` 调用。

## 6. 脚本剥离与 GUID 口径

- 移除的 7 个组件分属 2 个类型：`FxAnimationHelper`（脚本 GUID `54b058c848d670544abc9ee8d4bf40be`，6 个组件，含 fileID `114453742084257020`）与 `MoonClient.dll` 内组件（DLL GUID `0a06b724a7ecc1948b8fd5525e4102e6`，1 个组件，fileID `2104064827`）。
- 保留的内建组件：Transform / MeshFilter / MeshRenderer / LODGroup / Animator / Collider 等。递归统计当前 `MonoBehaviour = 0`、`Missing Script = 0`。
- **GUID 冲突口径**：迁移前与项目既有资源的真实冲突为 **0**。迁移后对全 `Assets` 自扫描会命中 150 条同名 GUID，全部指向本任务目录自身（自引用），不是与既有资源的冲突。目录之外既有资源一个字节未动。
- 本任务新建着色器 GUID `ed5e99bf8913e8a4d8a927f63af9fb1a`，全 `Assets` 唯一。

## 7. 四轮过程：真实失败与修正

| 轮次 | attempt | 做法 | 结果 |
|---|---|---|---|
| R1 | `run-733d99cb-e82a-40fc-a2fd-df7b99c363e9` | 复制 97 个资源 + `.meta`，附加加载后渲染核对 | **未通过**。相机落在 414 m 的 `sc_blackbox` 包围盒**外侧**，只拍到黑盒外壳；且场景 `lightmaps.Length = 0`，画面黑、主体极小 |
| R2 | `run-c454c5e0-b572-42ac-b4a8-c92bf4e27611` | 10 个复制材质改 `Shader.Find("Standard")`，相机移入盒内 | 主体可见，但**命中项目同名替身** `Assets/artres/Resources/Shader/RO/Replace/Standard.shader`，不是内置 Standard；该 shader 把顶点色乘进 albedo，RO 礼盒网格含 (255,0,255) 标记 → 单像素洋红；R2 报告"改指内建 Standard"是**事实错误**，已在 R3 更正 |
| R3 | `run-5bd3b53b-0ac1-49f1-9f7e-d5106c0fbd41` | 本目录新建 `DungeonROVertexColorSafe.shader`，用 `isMagenta / step / lerp` 阈值抹掉洋红，并保留 `normalize(normal)*0.5+0.5` | **被退回**。阈值特判是像素级补丁而非忠实适配；法线 RGB 调色是假光照，实测截图平均 `31.6/51.3/30.7`，G 比 R/B 高 19.7 / 20.6 个色阶，**整体偏绿** |
| R4 | `run-b574c91b-5930-438a-837f-386f3e37afa7` | 删掉阈值特判、顶点 COLOR 通路与法线调色，只留 `tex2D(_MainTex, uv) * _Color` | **通过**（completed / review accepted）。色偏 `63.0/63.2/63.6`，G−R +0.1、G−B −0.5；洋红 0 |

失败与错误一律保留在证据里，不回填、不粉饰：R1 的黑图截图、R2 的洋红截图、R3 的偏绿截图都还在 `_temp`，R2 的错误表述也写在上面这一行。

## 8. 最终着色器取舍

先查内置 shader，结论是**没有可用的**：

| 候选 | 实际属性 | 不满足的原因 |
|---|---|---|
| `Unlit/Texture` | `[_MainTex]` | 没有 `_Color`，丢失原 `_Diffuse` 颜色语义 |
| `Unlit/Color` | `[_Color]` | 没有贴图 |
| `Legacy Shaders/Diffuse` | `[_Color,_MainTex]` | 需要实时光源，本场景 0 盏灯 |
| `Mobile/Diffuse` | `[_MainTex]` | 需要实时光源 |
| `Legacy Shaders/VertexLit` | 含 `_Color` / `_MainTex` | 需要实时光源 |
| `Standard` | `[_Color,_MainTex]` | `Shader.Find` 命中项目同名替身，就是 R2 出洋红的那个 |

最终实现（`RODungeon_First/RO/Shaders/DungeonROVertexColorSafe.shader`，2,873 字节）：

- `appdata` 只声明 `POSITION` + `TEXCOORD0`，**刻意不声明 COLOR / NORMAL**，从结构上切断顶点色与法线调色两条路径；
- 片元只有一句 `fixed4 col = tex2D(_MainTex, i.uv) * _Color;`；
- 保留 `_Color`（取原 RO `_Diffuse` 的 0.5）、`_MainTex`、UV / 平铺参数、Opaque 队列；
- 只在本目录内新建这一个极小 shader，**没有另起渲染框架**，也没有改项目已有的 `Replace/Standard.shader`、RO 源 shader 或全局管线。

**边界表述**：这是"无烘焙光环境下的明暗近似适配"，**不是还原原 RO 烘焙效果**。原场景 `lightmaps` 为空、本身 0 盏灯，验证时用临时方向光 + SolidColor 背景，不能宣称已还原原画面。

## 9. 验证证据

Unity 实例 `LYF_MMO@47ea91c229a904f3`，2023.2.20f1c1，Built-in。全部通过**附加加载**与临时相机完成，未覆盖用户当时打开的 `Scene_Boot`，编辑器非 Play、非编译中。

| 检查 | 结果 |
|---|---|
| 着色器编译 | `ShaderUtil.ShaderHasError = False`，`isSupported = True` |
| 材质绑定 | 10 个 `.mat` 全部指向最终 shader，`nullShader = 0`、`wrongShader = 0` |
| 场景 | 根对象 1；Renderer 172；`nullMat / noShader / errorShader / nullMainTex / multiSlot` 全 0 |
| RO 残留 | MonoBehaviour 0 组件 / 0 类型；Missing Script 0；目录内无 `.cs` / `.dll` / `.asmdef` |
| 灯光 / 碰撞 | 0 / 0（源场景本身如此，非迁移丢失） |
| 洋红 | 全帧 0；原问题像素 `(271,383)` 由 `255,17,197` 变为 `71,66,65` |
| 色偏 | 平均 RGB `63.0/63.2/63.6`，G−R = +0.1、G−B = −0.5（R3 为 +19.7 / +20.6） |
| 与 R3 差异 | 509,758 / 786,432 像素（64.8%），最大通道差 127，属亮度与色偏整体变化，符合预期 |
| Console | 仅两条历史 `sc_dungeon_duobiwanfa_water_001 ... Shader 'RO/Scene/Water' doesn't have _LightFactor`，是 R2 诊断阶段临时读写该属性的副作用；本轮新增错误 0，Console 未清空。`Could not load the file 'NiceIO'.` 为历史无关条目，未修 |

主 Agent 已独立读最终 shader 源码并直接查看 `_temp/ro-first-dungeon-final-r4.png`，确认几何 / 贴图可辨、偏绿消除。

### 9.1 记录期漂移核验（只读，不是迁移轮次）

字节算式见第 5 节末注，这里只记**独立于文件字节的实机核对**。全部只读：不打开 / 不切换 / 不保存 / 不退出当时正在进行的 Play，不清 Console，不插入临时相机或灯光，只用 `GetComponentsInChildren` 遍历现有层级与材质。

| 检查 | 结果 |
|---|---|
| 副本子树 `sc_dungeon_duobiwanfa_001` | 208 GameObject / **172 renderer**（170 MeshRenderer + 2 SkinnedMeshRenderer）/ **0 MonoBehaviour** / **0 MissingScript** / **0 相机** / **0 灯** / **0 碰撞体** —— 与迁移验收边界一致 |
| 新增 `Role_MJS` 子树 | 194 GameObject / 30 renderer（2 + 28）/ 5 个本项目 MonoBehaviour（`PlayerInputCtr`、`RoleAnimaBehaviour`、`HighlightPlus.HighlightEffect`、`GhostEffect`、`HighlightPlus.HighlightManager`）/ 1 盏预制体自带 `Point Light`（Point，range 10，intensity 1，无阴影）/ 1 个预制体自带 `CharacterController` —— 是后加的角色子树，不是 RO 脚本残留，也不是测试残留 |
| 场景合计 | 402 GameObject / **202 renderer** |
| 材质 / shader | `nullMatSlots` / `noShader` / `errorShader` / `nullMainTex` / `multiSlotRenderers` 全 0；在用 shader 为 `LYF/Dungeon/ROVertexColorSafe`、`Standard`、`Standard Double Sided`、`Shader Graphs/IDA_{Clothes,Skin,Base,Hair}` |
| 烘焙光 | `LightmapSettings.lightmaps.Length = 0`；新建的 `RODungeon_FirstSettings.lighting` 是默认参数、未做过烘焙 → 边界未变 |
| Console | 错误条目 **0**（未清空） |

**本轮没有新截图**：场景内 0 相机、Game 视图无渲染，插入临时相机会改动用户运行态；因此以"运行态层级 + 材质 / shader + 静态 YAML"三项只读检查替代，覆盖范围比单张截图更全。

## 10. 未验证边界

1. **未接游戏入口**：没有接 Boot、配置表、YooAsset 运行时加载，场景目前只能手动打开。
2. **不是可玩副本**：没有副本玩法、进入流程、组队、阵营、服务端或打包联调，不得表述为"可玩副本已实现"。
3. **无碰撞**：源场景本身 0 个 Collider，未补造。
4. **无灯光 / 未重建烘焙**：源场景 0 盏 Light，`lightmaps.Length = 0`；未重建原烘焙光，验证用的是临时方向光。
5. **视觉为近似**：局部最简贴图着色不等于原 RO 完整渲染效果。
6. **未迁其余副本**：只迁 1 个场景，其余 `sc_dungeon_*` 与 RO 客户端脚本未动。
7. **未做完整编译**：没有客户端或服务端完整编译，更没有游戏副本联调。
8. **漂移保存者未确认**：能证明发生了一次编辑器保存，不能判定触发者是人还是工具；本轮明确不推定，也未回滚、未删除该角色实例与 lighting 资产。

## 11. 用量说明

`_temp/ro-first-dungeon-token-collect.txt` 与 `-token-report.json` 记录：2026-10-26 项目日累计 input 985,975 / output 239,091 / cacheRead 100,490,752。该统计为 Codex adapter 实验性口径、覆盖整个项目日，**不是本任务独占**；上下文规则、需求、工具 IO、协作、返工验证五类无法准确拆分。WorkBuddy 侧独立用量未采集，余额不可测，不推算节省与精确账单。本次记录任务**未重新采集**该统计。

## 12. Git 状态

- 分支 `codex/ro-first-dungeon-scene-20261026`，基线 `c571a6975b1b548604c7a3a9e0cbc5be0e7770bb`。
- 原 `codex/context-log-cleanup` 有大量既有脏改动；切 `codex/feature-development` 被 Git 保护拒绝，主 Agent 从当前 HEAD 新建上述 codex 分支，原有改动保留（`_temp/ro-first-dungeon-before-status.txt`）。
- 工作区脏条目：记录任务第 1 轮测得 297 项（其中 295 项属此前其他任务），第 2 轮实测 **300 项**；差额 3 项正是本记录任务新增的 `RODungeon_First/`、`RODungeon_First.meta` 与本日志文件。**不把工作区其他脏改动归到本任务。**
- WorkBuddy 全程未 `stage` / `commit` / `push`，未切分支、未 stash、未 reset。**主 Agent 待本次记录验收后仅集成本任务，提交号与推送回执待追加**；本日志不声称已上传。

### 12.1 主 Agent 的集成计划（待执行，本任务不做）

- **保留当前文件与 Play 状态**：不回滚、不覆盖当前工作区场景——里面已有后加的 `Role_MJS` 角色实例；也不把该实例或两个 lighting 文件混进本次提交。
- **提交范围**：仅 R4 已验收的 **251 文件 / 18,661,241 字节**。
- **场景的暂存方式**：经 Git index 暂存 `_temp/ro-first-dungeon-baseline-904144/RODungeon_First.unity`（904,144 字节，sha256 `fa99118e…`，用同一确定性剥离规则在 `_temp` 重建），**不覆盖工作区实际场景**；排除 `RODungeon_FirstSettings.lighting` 及其 `.meta`。
- **能力限制（如实）**：R1–R4 未保存场景文件快照、也未记当时 sha256，因此"旧文件"是**同规则重建**而非同一份字节流；可信度依据是重建字节数与 170 个既有实例修改键逐键一致。该限制不隐藏。
- 提交号与推送回执待主 Agent 执行后追加；**本日志不声称已上传**，本任务也未 stage / commit / push、未切分支。

## 13. 本次记录任务自身的文件

本记录任务共 2 轮：第 1 轮 attempt `run-b2c05b01-facc-4b46-b778-e89ff01bd9b7`（建正式日志、四入口追加、难点条目、机械重建索引）；第 2 轮 attempt `run-083d8e20-f2e4-4405-9498-744a039a6c5d`（补漂移核实结论、Git 集成边界与中英增量复核）。**记录轮次不计入迁移轮次**。

| 文件 | 用途 / 变化 |
|---|---|
| `项目修改日志/2026-10-26-RO首个副本场景复制迁移_CHANGELOG.md` | 本文件。新增；唯一完整持久日志（范围、映射、251 条清单、四轮过程、验证与边界） |
| `Docs/AI/CURRENT_TASK.en.md` | 仅尾部追加 `[FILES][Scene][RO-FIRST-DUNGEON-SCENE-20261026]` 英文段；旧字节前缀不变 |
| `Docs/AI/CURRENT_TASK.md` | 仅尾部追加同编号中文段；旧字节前缀不变 |
| `Docs/AI/TEST_REPORT.en.md` | 仅尾部追加 `[VERIFY][Scene][RO-FIRST-DUNGEON-SCENE-20261026]` 英文段；旧字节前缀不变。第 2 轮在该段内修订「记录期复核」条目（补漂移核实结论与 253 文件口径更正），未触及历史段落 |
| `Docs/AI/TEST_REPORT.md` | 仅尾部追加同编号中文段；旧字节前缀不变。第 2 轮在该段内修订「记录期复核」条目（同上），未触及历史段落 |
| `项目分析/项目难点.md` | 仅尾部追加本次编号独立条目（背景 / 难处 / 失败证据 / 根因 / 取舍 / 验证 / 面试讲述与追问） |
| `Docs/AI/RECORD_INDEX.md` | 由 `build_record_index.py` 机械重建（只含主题与行段，不含正文） |
| `_temp/ro-first-dungeon-records-baseline.json` | 写入前目标文件字节 / SHA256 / 行尾基线 |
| `_temp/ro-first-dungeon-records-consistency.md` | 本次新增段的中英增量一致性检查（范围、差异、修正、证据） |
| `_temp/ro-first-dungeon-records-gen.py` 等 | 生成与校验脚本、原始命令输出 |

## 14. 证据索引

| 文件 | 内容 |
|---|---|
| `_temp/ro-first-dungeon-final-review.md` | 最终证据（替代首轮 `migration.md` 的黑图描述），含 R3 洋红调查与 R4 推翻重写两节 |
| `_temp/ro-first-dungeon-migration-manifest-r4.json` | 251 条逐项清单与 R4 改动记录（本日志第 5 节的数据源） |
| `_temp/ro-first-dungeon-migration-manifest.json` | 首轮 97 条 src/dst 映射与跳过项（本日志第 4 节的数据源） |
| `_temp/ro-first-dungeon-final-r4.png` | R4 最终截图（与 R2 / R3 近景同机位、同光照） |
| `_temp/ro-first-dungeon-closeup-r2-fixed.png` / `-panorama-r2.png` | R2 近景与全景（已通过，保留未重拍） |
| `_temp/ro-first-dungeon-magenta-fixed.png` | R3 洋红修复对照截图 |
| `_temp/ro-first-dungeon-scene.png` | R1 黑图（失败证据，保留） |
| `_temp/ro-first-dungeon-accept.json` / `-accept-result.json` | Codex 验收结论与逐条依据 |
| `_temp/ro-first-dungeon-token-collect.txt` / `-token-report.json` | 用量统计（项目日累计，非本任务独占） |
| `_temp/ro-first-dungeon-before-status.txt` | 建分支前的工作区状态 |
| `_temp/ro-first-dungeon-drift-review.md` | 漂移核验结论（字节归因、语义等价、Unity 只读检查、归因边界与处置建议） |
| `_temp/ro-first-dungeon-migration-manifest-r5.json` | 当前磁盘 **253 项**真实字节 + sha256，含漂移块与 Unity 检查块（**不是**验收基线） |
| `_temp/ro-first-dungeon-drift-current.unity` | 当前目标场景只读快照（946,305 字节，sha256 `55f9981e…`） |
| `_temp/ro-first-dungeon-baseline-904144/RODungeon_First.unity` | 用同一剥离规则重建的旧基线（904,144 字节，sha256 `fa99118e…`），Git index 暂存用 |
| `_temp/ro-first-dungeon-drift-{rebuild,diff,byclass,mods,guids,dir}.*` | 重建、文档级 / 行级差分、按类统计、修改项语义对比、GUID 集合、目录统计的脚本与结果 |
| `_temp/ro-first-dungeon-records-consistency.md` | 本次记录任务的中英增量一致性检查与命令原始结果（含第 2 轮补记） |


## 十四、主 Agent Git 集成核对

- 仅集成本次已验收的 251 个场景/资源/元文件及批准的记录；未加入其他任务的脏改动。
- 工作区场景、后加 Role_MJS 实例、两份 LightingSettings 文件与当前 Play 状态保持不动；Git 索引中的场景来自已核验的 904,144 字节确定性重建基线。
- 四个中英记录及难点文件仅暂存本任务新增段，原有未提交段落留在工作区；索引暂存版本按暂存的英文源重新机械生成。
- 提交与推送真实回执在执行后追加于本日志，不据计划冒称成功。

- 暂存复核：258 个文件 = 251 个验收资源/元文件 + 7 个批准记录文件。工作区场景在期间继续有后续保存变化，未被本集成写入；暂存场景固定为 904,144 字节原验收重建基线。
- 格式检查边界：全量 `git diff --cached --check` 检出复制的 Unity YAML/.meta/原 Shader 固有空白；原始输出 `_temp/ro-first-dungeon-cached-check.txt`（分类：{'trailing whitespace.': 953, 'space before tab in indent.': 18}）。为保持源资源与用户工作区不变，未批量改写；新增局部 Shader 与 7 个正式记录的定向空白检查作为本次新增代码/文档检查。
