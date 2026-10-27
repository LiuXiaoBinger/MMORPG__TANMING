# 魔神塔最小环境组件与 Shader 依赖修复 — 正式变更日志

- 业务任务编号：`RO-MOSHENTA-MIN-ENV-20261027`
- 记录同步子任务：本次仅做记录与中英同步，**不是开发第 3 轮**（开发共 2 轮：2026-10-27 第 1 轮实现 + 第 2 轮原地返修）
- 会话：WorkBuddy 原生桌面会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`（原会话续作，未新建会话）
- 模型：`deepseek-v4.1-flash`。Codex 在两个原生轮次的实际请求里均独立观察到 `requestModelId = deepseek-v4.1-flash`；本记录不把"界面配置显示"当成模型事实的唯一证据。
- 分支：`codex/ro-first-dungeon-scene-20261026`；基线 `0b3c33806ef4637cede0667f425091d727301878`
- 日期：2026-10-27
- 性质：**最小环境适配 + 缺失依赖修复**。不含天气循环、昼夜过渡、雨雪雷电、服务端协议、后处理/相机/全局管理框架、静态合批、分时物件、UTS/角色 shader 重构。
- 源工程 `F:/UnityPro/ro/Ro_cs422nb`：全程只读（未移动/修改/删除任何源文件）。

## 1. 结论

| 项 | 结果 |
|---|---|
| 全局 Shader 报错 | `RO/Effect/AllInOne: Couldn't open include file '../Include/EffectCommon.cginc' at line 111` 已消除。实测 `supported=True`、`hasError=False`、`GetShaderMessages=0` |
| 修复方式 | 仅补齐递归依赖闭包 3 个 `.cginc`（`EffectCommon` / `SceneMap` / `ColorCorrect`）+ 目录与文件 `.meta`（全新 GUID）。**Shader 本体未改**，sha256 与源一致 |
| 环境组件 | `MEnvorimentSetting` 挂到场景根 `sc_dungeon_moshenta_001`，按源调用顺序应用固定黄昏配置：快照 → 烘焙光 → 时间段 → 天气 → 场景色偏 |
| 烘焙光绑定 | **937/937 真实解析、0 空引用**；`index` 分布 `{1:734, 0:203}` 与源一致；`scaleOffset` 逐条相同。679 条 stripped renderer 文档在目标场景**新物化**后才可落引用 |
| 烘焙资源 | 2 张 EXR（1024×1024）+ 1 个 LightProbes 逐字节同源；4 份 ScriptableObject 因类型 GUID 与字段裁剪必然重写（**不是**逐字节同源） |
| 文件规模 | 本次共 **41 个文件**：新增 **39**、修改 **2**、删除 **0**。第 2 轮返修只再改 1 个 `.cs` |
| 场景 | `sc_dungeon_moshenta_001.unity` 10,017,238 B，sha256 `92c2ce0db331ac918614e69a01c113efbf929c67aea9a42a94432e9f511422a9`（验收快照逐字节一致） |
| 视觉 | 同机位场景视图：洋红 0、近黑 0.00%；全景洋红 0、近黑 0.37%。无黑云、无丢失 FX |
| Git | **待主 Agent 精确暂存并提交推送**。本任务未 stage / commit / push |

## 2. 授权边界与任务身份

- 派工模式：主 Agent 只派工、验收、失败退回；WorkBuddy 作为原生桌面子 Agent 独立实现与自检。派工要求"主 Agent 不并行代写"，实际执行符合。
- 旧 Hub 的取消/超时轮次保持真实失败，未使用旧 claim / report / lease，未改网关或任务库，未伪造回执。
- 授权写入范围：`LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/`、`LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/`、`LYF_MMO/Assets/artres/Resources/Shader/RO/Include/`（本轮新建）、该场景文件、该目录内云 Shader 的参数入口、`_temp` 证据。其他角色/前副本/业务/Boot/配置表/协议/UI/Packages/ProjectSettings/已有全局材质与 Shader 均未改。
- 开工前工作区状态存 `_temp/ro-environment-implement-before-git.txt`（306 行：97 个 ` M` + 203 个 `??`，本任务只新增自己的文件，未吸收他人改动）。
- 本轮记录任务仅授权：新建本日志、向 4 个 `Docs/AI` 文件各追加 1 段、向 `项目分析/项目难点.md` 追加 1 条、写 `_temp` 证据。**未触碰任何 `.cs` / 场景 / 资源**，未重建映射，未跑 Unity 测试，未做 Git。

## 3. 任务 1：全局 `RO/Effect/AllInOne` 缺 include

### 3.1 根因

`LYF_MMO/Assets/artres/Resources/Shader/RO/Effect/ROEffect__beta153.shader:111` 写的是 `#include "../Include/EffectCommon.cginc"`，而 `Assets/artres/Resources/Shader/RO/Include/` 目录在本项目里**根本不存在**。上一轮只在 Moshenta 局部副本目录补过 include，全局目录仍然缺失，所以用户看到的是全局报错。

### 3.2 修复方式（只补递归闭包，不搬整套框架）

- 新建 `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/`（含目录 `.meta`），只放 **3 个** 文件：
  - `EffectCommon.cginc` 17,246 B — 直接缺失项，源同源逐字节
  - `SceneMap.cginc` 10,650 B — `EffectCommon` 递归依赖
  - `ColorCorrect.cginc` 2,012 B — `EffectCommon` 递归依赖
- 递归闭包推导：`EffectCommon` → `UnityCG.cginc`（引擎内置，不属于工程缺失）+ `../Include/SceneMap.cginc` + `../Include/ColorCorrect.cginc`；`SceneMap` → `UnityCG.cginc`；`ColorCorrect` → 无。合计 29,908 B。
- **未复制** `CautionCommon` / `CreatureCommon` / `DepthShadow` / `GpuSkinning` / `SceneCommon` / `SceneMeta` / `SceneWaterFoam`——它们不在本 Shader 的闭包内，按"不整套搬框架"的要求排除。
- `.meta` 全部使用**新 GUID**，并与全工程既有 4,347 个 guid 做过冲突检测（0 冲突）；没有复制源的 `.meta`，避免重复 GUID。
- Shader 本体未改：`ROEffect__beta153.shader` 的 sha256 与源一致（`2eb45a38…`）。

### 3.3 编译验证

重新导入后实测：`name=RO/Effect/AllInOne`、`supported=True`、`hasError=False`、`GetShaderMessages=0`、`passCount=1`、`renderQueue=3000`。未清空用户 Console，旧历史条目保留。

## 4. 任务 2：魔神塔最小环境组件

### 4.1 数据结构（8 个类型，一个顶级类型一个文件）

| 类型 | 作用 | 与源的关系 |
|---|---|---|
| `MEnvorimentSetting` | 组件本体：生命周期、四个 Apply 阶段、27 个全局通道的写入与还原 | 最小移植源 `MoonClient/Effect/Scene/MEnvorimentSetting.cs`，剥掉 `MInterfaceMgr` / RO 事件 / `MGameContext` / RO 资源·音频·特效·天气·后处理管理器依赖，不引入 `MoonClient` / `MoonSerializable` DLL |
| `MDayTimeData` | 时间段数据：主光/可视光方向、场景色、云草树环境色、日色影色、地形色、地形阈值 | 源 `MoonSerializable/Effect/MDayTimeData`，只保留本次用到字段；未移植天空球/后处理/大气散射 |
| `MCreatureLightData` | 角色光数据：主光/背光/环境/边缘光颜色 + 反射 Cubemap | 源同名字段与默认值 |
| `MWeatherData` | 天气数据：世界尺寸、雾色/雾混合区间、黄昏雾、海拔雾、散射、云影色、风 | 源 `MWeatherData` 中本次所需字段；未移植昼夜雾/雨雪雷电/彩虹/后处理 |
| `MLightmapData` | 烘焙光数据：`lightmaps`、`lightProbes`、`lightmapIndex` | 照源结构 |
| `MRendererLightmapScale` | 单条渲染器绑定：`renderer` / `index` / `scaleOffset` | 照源 `[System.Serializable] struct` 最小移植 |
| `MLightmapIndex` | 分组容器：`MRendererLightmapScale[]` | 照源 |
| `MDaytimeType` | 时间段枚举 `Nothing=0/Day=1/Night=2/Dusk=4/Dawn=8` | 从源 `MEnvorimentData.DaytimeType` 取值，避免为一个枚举搬整个数据类（其中含未用字段与 WeatherType） |

未创建 `MAmbientData`、天气枚举等"为未来需求留的"类型——源里 `defaultAmbientData` 本来就是 None。

### 4.2 调用顺序（照源，不新增框架）

```
OnEnable  → ApplyEnvironment
              ├─ 首次：CaptureRenderState + CaptureGlobalShaderState（只做一次快照）
              ├─ ApplyLightmapData(defaultLightmapData)       // 937 条 index + scaleOffset，再写 LightmapSettings
              ├─ ApplyDayTimeData(defaultDayTimeData)         // 光向/场景色/角色光/云草树/地形
              ├─ ApplyWeatherData(defaultWeatherData)         // 雾/散射/云影/风
              └─ ApplySceneColor()                            // 场景色偏与雾色合流
OnDisable → RestoreEnvironment（27 个全局通道按类型逐项回原值 + 渲染器绑定复原 + LightmapSettings 复原）
```

- 光向换算严格用源写法：`Quaternion.Euler(dayTime.mainLightDir) * Vector3.back`，`Vector2` 自动扩展为 `Euler(x=俯角, y=方位角, z=0)`。**没有**把向量和角度混用。
  - `mainLightDir=(65,135)` → `_ROCLightDir0=(-0.2988, 0.9063, 0.2988)`（实测与期望值逐位一致）
  - `visLightDir=(30,135)` → `_ROCLightDir1=(-0.6124, 0.5000, 0.6124)`
- 颜色 alpha 语义保留：例如 `sunColor=(1,0.96,0.84,0.1)`、`shadowColor` 的 `a=0.6157`、`_ROCFogStartColor.a=50`、`_ROCFogEndColor.a=40` 都是 alpha 承载数值，没有被"规整"成 1。
- 编辑器预览靠组件右键 `[ContextMenu("应用环境配置")]`（源无 `ExecuteAlways`，所以编辑模式不会自动生效）；Play 由 `OnEnable` 自动应用、退出由 `OnDisable` 还原。
- 没有 `Update` 轮询、没有自定义 Inspector（源那份 750 行 Inspector 未移植）。

### 4.3 27 个全局通道（第 2 轮返修后的完整清单）

- Color 16：`_ROCLightColor0` / `_ROCBackColor` / `_ROCAmbientColor` / `_ROCRimColor` / `_ROCCloudEnvorimentColor` / `_ROCGrassEnvorimentColor` / `_ROCGrassEnvorimentDarkColor` / `_ROCTreeEnvorimentColor` / `_ROCSceneColor` / `_ROCSunColor` / `_ROCShadowColor` / `_ROCMaskColor` / `_ROCTerrainLightColor` / `_ROCTerrainDarkColor` / `_ROCFogStartColor` / `_ROCFogEndColor`
- Vector 9：`_ROCLightDir0` / `_ROCLightDir1` / `_ROCHeightFogParams` / `_ROCRadiusFogParams` / `_ROCMieG` / `_ROCWindParams` / `_ROCMapFlow` / `_ROCFogST` / `_ROCCloudST`
- Float 2：`_ROCTerrainThreshold` / `_ROCWorldParams`

Float 单独成组是必要的：后两者是标量，用 `SetGlobalVector` 写会被当成四维，取值语义就不对了。

### 4.4 937 条烘焙光绑定的重建方法

- 源组件 `&2104064827` 实际保有 **937 条** `MRendererLightmapScale`，但**全部是 stripped renderer 引用**（`renderer: {fileID: …}` 指向已剥离文档）。
- 目标场景当时只有 258 个同 fileID 文档可命中，其余 679 条在 YAML 里没有对应文档——因为剥掉 RO 组件后 Unity 重存把孤儿文档丢了。
- 不能用名字匹配（重名很多），也不能直接按 fileID 匹配（PrefabInstance 的 YAML fileID 与运行时 `GetInstanceID()` 完全不同，`TryGetGUIDAndLocalFileIdentifier` 对实例 handle 返回 -1）。
- 采用的匹配键：`(prefabGuid, sourceObjectFileID, 实例父路径, 实例名)`；同键多义组（95 组、涉 227 条）再用实例本地位置最近邻消歧（阈值 0.01）。
  - 父路径必须用 `m_TransformParent`（YAML 里是 **4 空格**缩进，正则要用 `^\s+m_TransformParent:`；写成 2 空格会全部退化成空串，路径签名直接失效——这是实际踩过的坑）。
- 结果：**937/937 匹配、0 未匹配、937 个不同目标 ordinal**；`index` 分布 `{1:734, 0:203}` 与源完全一致；`scaleOffset` 无一条为空；`nullRenderer=0`。
- 没有改网格、没有改 Prefab 结构、没有丢 679 条记录、没有填假引用。

### 4.5 云层参数入口（源逻辑会失效的地方）

- 上一轮为修黑云，在 Moshenta 局部 `ROCloud-SoftAlphaBlend.shader` 的 `Properties` 里临时加过 `_ROCloudEnvorimentColor`。**材质属性优先于 `Shader.SetGlobalColor`**（Unity 2023.2 官方文档明确），所以只写 global 云色不会跟随。
- 处理方式：删掉那行 Properties，Shader 退回**纯 global 入口**；改后该文件 sha256 与源**逐字节一致**。初值由挂载组件在应用时写入 global。
- 验证不是看字段赋值：临时把 `cloudEnvorimentColor` 改红 → 云层均值 `129.81/67.38/80.93`（G/B 大幅下降，R 不变）；恢复白色 → `129.81/129.62/129.42`。随后恢复源默认 `(1,1,1,1)` 并 `SaveAssets`。

### 4.6 场景挂载

- 根 GameObject `sc_dungeon_moshenta_001`（fileID `2104064826`）新增 1 个 `MEnvorimentSetting`（fileID `2104064829`，`m_Script` guid `e41588fda1cc65048bfd90ea6c312684`，指向本项目新类型，**不依赖原 DLL 的 m_Script GUID**）。
- 组件上回填：`lightmapIndex[0]` 937 条、`lightmapIndex[1]` 空组、三份数据引用、`worldSize=665`、`fogST/cloudST=(1,1,0,0)`。
- 场景内 Missing Script 0、0 Missing；组件所属程序集 `GM_Game`。

## 5. 失败与返修（两轮，失败全部保留）

### 第 1 轮（实现）

交付后主 Agent 验收指出两处**代码缺陷**，不是测试环境问题：

1. `RestoreEnvironment` 只把 9 个通道硬写成源 `ResetGlobalShaderVariable` 的兜底值，另外 18 个组件实际写入的通道既不快照也不还原 → 停用/离场后 18 个全局量残留本场景值并污染后续场景；同时 9 个硬写值会冲掉其他场景已有的环境状态。
   - 边界认识：源工程写死兜底是因为它是**全局唯一**环境系统；本组件是**场景级独立组件**，边界不同，必须按实际原值还原。
2. `ApplyEnvironment` 开头 `if (_applied) { return; }` → 首次应用后改完配置再点「应用环境配置」完全无响应，编辑器调参不可用。

### 第 2 轮（原地返修，唯一改动文件 `MEnvorimentSetting.cs`）

- 缺陷 1 修法：按类型分三张表 `ColorChannelIDs`(16) / `VectorChannelIDs`(9) / `FloatChannelIDs`(2) = 27 个；首次应用前按 `GetGlobalColor/Vector/Float` 逐项取原值（原值为 0 也按实捕获），停用时按同类型 Setter 逐项写回。**没有**增加管理器、代次、队列、重试或所有权框架。
- 缺陷 2 修法：门禁改成"只拦首次快照"——`if (!_applied) { CaptureRenderState(); CaptureGlobalShaderState(); _applied = true; }`，之后始终按当前数据重写。
- 同时按项目规则补齐了局部变量/循环变量的具体中文用途注释（`group` / `scales` / `count` / `i` / `renderer` / `groupIndex` / `maps` / `datas` / `data` / `mainLightDir` / `visLightDir` / `lightData` / `useDuskFog` / `fogColor` / `blendStart` 等），不是只重复名字。
- 过程性事实（如实记录）：应用→还原往返中，逐渲染器写 `lightmapIndex` 会让 Unity 在**下一帧延迟脏检查**里把场景标脏（测试内即时读是 `False`，稍后变 `True`）。因内存值已全部还原、从未 `SaveScene`、磁盘哈希与验收快照一致，用 Unity 内部 `EditorSceneManager.ClearSceneDirtiness(scene)` 撤掉这枚测试产生的脏标记，最终 `dirty=False`，**未改任何文件**。

## 6. 场景序列化差异（上一轮报告的漏报，此处更正）

上一轮报告写"保存只新增 1 条 m_StitchLightmapSeams 覆盖"，**这个说法不完整，已作废**。本轮以 HEAD 版本场景（`git show HEAD:…`，只读取，未做任何 Git 写操作）作语义 diff，实测：

| 项 | 数值 |
|---|---|
| 文件字节 | 9,678,409 → 10,017,238（**+338,829**） |
| YAML 文档数 | 3,091 → 3,771（+680） |
| 新增文档 | **679 个 `!u!23` MeshRenderer（全部 stripped 形式）+ 1 个 `!u!114` MonoBehaviour** |
| 删除文档 | 0 |
| PrefabInstance 总数 / 被修改数 | 972 / **189**（只增覆盖项，无删除项） |
| 新增覆盖项 | **359 条 `m_Materials.Array.size`**、**1 条 `m_StitchLightmapSeams`** |
| RenderSettings | 仅 `m_FogColor` 变化：`(0.9056604, 0.5819715, 0.40583837)` → `(0.31461376, 0.40945742, 0.6603774)`（黄昏雾色） |
| RenderSettings 其他 | `m_AmbientMode=1(Trilight)`、`m_CustomReflection=Hdr01`、`m_Fog=0` 在 HEAD 与当前**相同**，未变 |
| LightmapSettings 文档 | **完全未变**（0 行差异） |
| 根对象 | `sc_dungeon_moshenta_001` 的 `m_Component` 从 1 项变 2 项（新增环境组件） |

- 679 个 stripped MeshRenderer 是**必要产物**：937 条绑定里有 679 条要指向这些渲染器，Unity 在保存时必须把它们物化成 stripped 文档，否则引用无处落地。
- 359 条 `m_Materials.Array.size` 与 1 条 `m_StitchLightmapSeams` 是 Unity 在实例化/序列化这些渲染器时补写的预制体覆盖项，属正常序列化行为。
- 一个重要事实，避免误读：**场景 YAML 的 `LightmapSettings` 里没有 lightmap 条目**（HEAD 与当前都是空的），2 张光照图与 LightProbes 是组件 `defaultLightmapData` 在应用时写进 `LightmapSettings` 的——这与源 RO 的 `ApplyLightmapData` 设计一致。所以"烘焙数据已落盘到 LightmapSettings"的说法不成立，正确表述是"烘焙资源随组件引用落盘，运行时/应用时注入 LightmapSettings"。
- 以上差异可解释为正常 Unity 序列化，**不擅自修改已验收场景**；本记录任务也未再保存场景。

## 7. 验证证据

### 7.1 编辑器（右键「应用环境配置」预览态）通道逐项

实测值与源数据/源公式一致（节选，全表见 `_temp/ro-environment-implement-apply-report.txt`）：

| 通道 | 实测 |
|---|---|
| `_ROCLightDir0` | `(-0.2988, 0.9063, 0.2988, 0)` = `Euler(65,135)*back` |
| `_ROCLightDir1` | `(-0.6124, 0.5000, 0.6124, 0)` = `Euler(30,135)*back` |
| `_ROCSceneColor` | `RGBA(0.5020, 0.5020, 0.5020, 1)` |
| `_ROCloudEnvorimentColor` | `RGBA(1,1,1,1)` |
| `_ROCSunColor` / `_ROCShadowColor` | `(1, 0.96, 0.84, 0.1)` / `(0.1829, 0.2178, 0.2830, 0.6157)` |
| 角色光四路 | 主光 `(0.8868,0.7887,0.6651,0.6510)`、背光 `(0.7547,0.7298,0.7496,0.7686)`、环境 `(0.4857,0.4639,0.7075,0.6157)`、边缘 `(0.4738,0.7620,0.8302,0.6000)` |
| 雾 | `_ROCHeightFogParams=(0.2,15,0,1)`、`_ROCRadiusFogParams=(30,0.85,0.0015,0.5)`、`_ROCMieG=(0.10193,1.7225,1.7)`、`fogST=cloudST=(0.001504,0.001504,0,0)`、`worldParams=0.001504`(=1/665) |
| 黄昏雾 | `fogStart=(0.3172,0.3714,0.6792, a=50)`、`fogEnd=(0.3146,0.4095,0.6604, a=40)`、`RenderSettings.fogColor=fogEnd` |
| 烘焙 | `lightmaps=2`（两张 1024×1024）、`lightProbes=lm_lp_sc_dungeon_moshenta_001_00`、`customReflection=Hdr01`、`renderersWithLightmap=937/937 mismatch=0` |

### 7.2 像素级证明（不是"Inspector 里有引用"）

- 烘焙光真实参与渲染：同机位渲染"开/关 lightmap"差分，按建筑屏幕矩形（640×360 下 x 96–544 / y 90–306，96,768 px）统计：`meanSumDiff=10.01`、`diff>12 = 25.25%`、`diff>30 = 10.57%`。整帧差分曾只有 8.59%，因为画面大部分是天空/云，所以必须按建筑区测量。
- 云层跟随 global：见 4.5（`129.81/67.38/80.93` vs `129.81/129.62/129.42`），扰动后已恢复源默认白。

### 7.3 Play 与退出（第 1 轮，一次性）

| 阶段 | 实测 |
|---|---|
| 进入 Play | `components=1` 在 `sc_dungeon_moshenta_001`；`playScales=937 lit=937 null=0`；三份引用有效；`lightmaps=2`；`probes=ok`；`_ROCLightDir0=(-0.2988,0.9063,0.2988)`；`cloudEnv=(1,1,1,1)`；`sceneColor=(0.502,…)`；`fogColor=(0.3146,0.4095,0.6604,1)` |
| Play 截图 | 洋红 0、近黑 0.00%、均值 `129,130,130` |
| 退出 Play | `dirty=False`、场景数 1；`lightmaps=0`、`probes=NULL`、`_ROCLightDir0=(0,1,0)`、`editorScales=937 lit=0` —— 即 `OnDisable → RestoreEnvironment` 已把全局量与场景级设置还原为"应用前快照"，这是预期行为 |

未播放、未执行任何会停止用户 Play 的操作；一次必要 Play 验证后已恢复原模式。

### 7.4 重载后仍有效

关闭/重开场景与脚本域重载后，937 条绑定与三份引用仍在，组件程序集仍是 `GM_Game`，`AllInOne` 仍 `supported=True msgs=0`。

### 7.5 第 2 轮增量验证（27 通道三阶段）

方法：预置可区别于源默认值的哨兵值（Color 逐通道 `0.11+0.01i`、Vector `0.13+0.01i`、Float `101+i`）→ 首次 Apply → 改数据副本再 Apply → Restore，逐通道记录；用 Python 按**源资产值 + 源公式**独立复算期望值，不拿被测代码的中间结果自证。

| 阶段 | 结果 |
|---|---|
| 首次 Apply | 27/27 写入数据值；与独立复算 **27/27 一致**（最大误差 5.4e-7，属浮点舍入） |
| 改配置再 Apply | `_ROCloudEnvorimentColor` 由 `(1,1,1,1)` → `(1.0000,0.2000,0.2000,1.0000)`，证明门禁缺陷已消除；数据副本用完 `DestroyImmediate`、引用还原、**未落盘** |
| Restore | **27/27 全部 EQ 回到哨兵原值**（逐项精确，不是近似） |
| 收尾 | 写回测试前真实状态后，最终 27 通道与 S0 对比 **0 处差异** |

第 2 轮**没有**重复跑：lightmap 像素差分（光路未改）、937 映射重建（场景与 Prefab 结构未改）、Play 大测试（`OnEnable/OnDisable` 调用的正是本轮已验证的两个方法，调用点未改）。

### 7.6 截图（整幅，非裁剪避让）

| 文件 | 结论 |
|---|---|
| `_temp/ro-environment-implement-after-apply.png`（1600×900，用户场景视机位） | 洋红 0、近黑 0.00%、均值 `130,130,129` |
| `_temp/ro-environment-implement-overview.png`（1280×720） | 洋红 0、近黑 0.37%、均值 `80,96,121` |
| `_temp/ro-environment-implement-closeup.png`（1600×900） | 洋红 0、近黑 0.00%、均值 `119,120,124` |
| `_temp/ro-environment-implement-play.png` | 洋红 0、近黑 0.00%、均值 `129,130,130` |

目视确认：浮空石塔与城堡清晰、黄昏蓝白积云、火焰特效完好，无洋红、无黑云、无贴图丢失。

### 7.7 源码侧的已知边界（不是接线遗漏）

`RO/Scene/AllInOne`（`ROScene.shader`）的 `SCENE_MAP_ON` 在 `ForwardBase` 段被注释掉，本场景材质也没有启用相关关键字（与源材质逐字节一致）。因此扰动 `_ROCLightDir0` / `_ROCSceneColor` 时静态建筑画面几乎不变——实测光向扰动 `diff>12=0%`、场景色扰动 `diff>30=0%`。这是**源设计**导致的：静态建筑不消费动态场景色调/雾通道，只吃烘焙光与云层响应。因此本任务**不声称**"已还原 RO 完整渲染效果"，也没有改材质关键字或加灯来"制造"变化。

另有一个未处理但已核实的现象：源里处于非激活状态的两个对象 `sc_fuben_002_002`、`sc_moshenta_shan_001` 各有 1 个空材质槽（`slot=0 of 1`、`activeInHierarchy=False`）——源本如此，不是本次丢失。

## 8. 完整逐项清单（41 条：新增 39 + 修改 2 + 删除 0）

路径相对项目根；字节与 sha256 为 2026-10-27 记录同步时的实测值，与 `_temp/ro-environment-implement-final-manifest.json` 一致。

| # | 文件（项目根相对） | 类别 | 用途 / 变化 | 字节 | sha256 |
|---|---|---|---|---|---|
| 1 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment.meta` | 目录 meta | 新建代码目录 meta，保证 Unity 追踪新增目录 | 172 | `eedf407eca2d435b7b71bf62a3b4210f18d4a409d3bd66b73c2d705ae599c5dd` |
| 2 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MDaytimeType.cs` | 代码 | 时间段枚举，取值对齐源 MEnvorimentData.DaytimeType（Nothing/Day/Night/Dusk/Dawn） | 595 | `2a6ea1992a9105ef7080b41c2e692ccc7c43f278e9d5497c70bffd9e83f26a07` |
| 3 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MDaytimeType.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `ae9e44b49e79c80b4bb9859023a1e157f0dc9ca68df61dd17ca997c129233cf3` |
| 4 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MCreatureLightData.cs` | 代码 | 角色光数据（主光/背光/环境/边缘光颜色 + 反射 Cubemap） | 1571 | `1345678813840299e39b261786df5653d76cb3bededa2493b1b57105555dd7b5` |
| 5 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MCreatureLightData.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `591528a21b85f1c22c466c58ac0bef1f3eb8f6b9d6ca2c9bde0a96a52c401442` |
| 6 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MDayTimeData.cs` | 代码 | 黄昏时间段数据（主光/可视光方向、场景色、云草树环境色、日色影色、地形色） | 3635 | `119eb1403465e843330884f407db7934f284e9c516a75a60dfe97c2102f223a0` |
| 7 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MDayTimeData.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `8db7b38527b25d5a13a09268cada306bd7488cd85568d847cc46e28ff638fd1f` |
| 8 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MWeatherData.cs` | 代码 | 天气数据（世界尺寸、雾色/雾混合/海拔雾、散射参数、云影色、风参数），按本次所需字段移植 | 4994 | `bbe3d752664be4385441650b9038fcef94ee177cb40a4cfcc7eae15e92668455` |
| 9 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MWeatherData.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `f3b22f9ea1021466fe717cec714fd889b2dcdd4b7acf97051fa8645df65fb6dd` |
| 10 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MLightmapData.cs` | 代码 | 烘焙光数据（lightmaps 贴图数组 / lightProbes / lightmapIndex） | 1197 | `656ba6d68c48b3fb0a66025a68d9018bd9cc8573d7a71608da173fb45eb3ea43` |
| 11 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MLightmapData.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `d9bda6760227822208ca98dc5b8dfee8ce93e2359397d5ed32a3bf190d8af9f6` |
| 12 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MRendererLightmapScale.cs` | 代码 | 单条渲染器烘焙光绑定（renderer / index / scaleOffset），照源结构最小移植 | 992 | `a2719c8362069e76f57300077c54aeee5985a3fc8e13a78c3964072f815cac41` |
| 13 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MRendererLightmapScale.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `4ce96d09aa7369deb04abafabba965ab459a6082156c8698200fee4c9a11ee30` |
| 14 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MLightmapIndex.cs` | 代码 | 烘焙光分组容器（MRendererLightmapScale 数组） | 614 | `1ec8301f362047a0875552ff9a1671e574c17d170a49a5996a684ccaf47a4665` |
| 15 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MLightmapIndex.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `71e873924177949dfbf23548b61467ea3002ade53dcdaa18df70cf4f458c3d01` |
| 16 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MEnvorimentSetting.cs` | 代码 | 场景环境组件本体：OnEnable 应用、OnDisable 还原；按源顺序 烘焙光 -> 时间段 -> 天气 -> 场景色偏 | 31168 | `5d038da5437e0ca8840e5c26e4e916c096ed260c674121a8666ee2c7801998eb` |
| 17 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEnvironment/MEnvorimentSetting.cs.meta` | 文件 meta | 脚本 meta（新 GUID，不依赖源 DLL 的 m_Script） | 59 | `5f8ad3a716f1a92a5d017f7a13ceb8ed183fb9eb2f6ae89f8ebc8ff0018fc913` |
| 18 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment.meta` | 目录 meta | 新建环境资源目录 meta | 172 | `60e490343206e8bf4d6c53ad033f19fd5d5c84669db25e7c6d6f7ab389c7fd46` |
| 19 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/cl_sc_dungeon_moshenta_001_huanghun.asset` | 资源 | 角色光数据实例（四路颜色逐位取自源，reflectMap=Hdr01.jpg） | 794 | `5d0d124e54bf9f303811d3a9e938258b3e323ceb6f37436dd793ef39f61492d2` |
| 20 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/cl_sc_dungeon_moshenta_001_huanghun.asset.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 189 | `46a083d54fe54ca96c418f0563b6e2347bac84a4aa7c5bf774f920d584111ad1` |
| 21 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/dt_sc_dungeon_moshenta_001_huanghun.asset` | 资源 | 黄昏时间段实例（mainLightDir=(65,135)、visLightDir=(30,135)、sceneColor=(0.5,0.5,0.5,1)、cloudEnvorimentColor=(1,1,1,1)） | 1121 | `d66c36b00b276f6869df6c2da3e7e315e11f454964d83c7a882ce6e3639e4cd8` |
| 22 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/dt_sc_dungeon_moshenta_001_huanghun.asset.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 189 | `e68e677cbf986ad4f31463ca60e7be36192afccb34b93e98c63cc238ef6f9cd3` |
| 23 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/wt_sc_dungeon_moshenta_001_huanghun.asset` | 资源 | 天气实例（worldSize=665、雾色与黄昏雾、散射参数、云影色、风参数全部取自源 YAML） | 1222 | `6df02b603dd955f2bae5c0d0d00a9e010ce08f09986687d4c74e261766440bbb` |
| 24 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/wt_sc_dungeon_moshenta_001_huanghun.asset.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 189 | `4c2f7d5c022a56e23c35544cebe3d51f1c110ef83ec655cd0bc68adfbf0d7bc6` |
| 25 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_default.asset` | 资源 | 烘焙光数据实例（引用 2 张 EXR + LightProbes，lightmapIndex=0） | 680 | `95adb34cd2d7733a6f201d72b740216f4587c64618275a9a424a69ea67bd80f1` |
| 26 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_default.asset.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 189 | `0fe8b0292653a177b7695e16094526bccebbf6b325777036f3436b5a2ed4929e` |
| 27 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_lp_sc_dungeon_moshenta_001_00.asset` | 资源 | 源 LightProbes 烘焙数据 7,111,102 B，逐字节同源 | 7111102 | `13e0ac2f6f1a63c6f52c7803ce73c1c456b7d53282d45278a9d1f3aca1c2c5c2` |
| 28 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_lp_sc_dungeon_moshenta_001_00.asset.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 189 | `27b8effc5e2dca2b1762e6dabb38f5c4ce801b1f05bf12c0da4c95fea72fba20` |
| 29 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_00_00.exr` | 资源 | 烘焙光照图 0，3,266,628 B，1024x1024，逐字节同源 | 3266628 | `0e116285781645151e6cd918e26c263259fe231d63cfb8bf82bd5690341a2fd1` |
| 30 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_00_00.exr.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 1927 | `240d94470c8c5470503934cfcc6190aacaf4bbf3f6ce2b72341746b5f10c4ab2` |
| 31 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_00_01.exr` | 资源 | 烘焙光照图 1，3,725,031 B，1024x1024，逐字节同源 | 3725031 | `699e5278498e387d8d3e15f8255d641e83e81861d80c366033eae0a99a31baf0` |
| 32 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/Environment/lm_sc_dungeon_moshenta_001_00_01.exr.meta` | 文件 meta | 资源 meta（保留源导入设置，仅换新 GUID） | 1927 | `038ab06c26c0fcc1de81811784692bd155eb6135f9f31517d826875dc7e35c1b` |
| 33 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include.meta` | 目录 meta | 全局 RO Shader include 目录 meta（本轮新建） | 172 | `9a686b621bedb49ff0edf18f88ff7a48f2c0bbf8fed9d5de891400b64a5a0a28` |
| 34 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/EffectCommon.cginc` | Shader include | 全局 AllInOne 直接缺失的 include，与源逐字节一致 | 17246 | `3566973c619d32eabfb93684873ebbc7351712bfa893cbe0f5541d9c1adaee7f` |
| 35 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/EffectCommon.cginc.meta` | 文件 meta | include meta（新 GUID，与全工程已有 guid 无冲突） | 176 | `b28890f31f88beaa3585a74f786b870e94cfd7ba6ad699b2a17c30a9eb5d9632` |
| 36 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/SceneMap.cginc` | Shader include | EffectCommon 递归依赖，与源逐字节一致 | 10650 | `a44bee5b12759a83ed92d9ddf9df802fbf06d44fcf8189f82d83cce0c7385ffa` |
| 37 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/SceneMap.cginc.meta` | 文件 meta | include meta（新 GUID，与全工程已有 guid 无冲突） | 176 | `59f661373947afac7b3d30e38a5d7153fe215ffa3267afd454b7ae489ad8364f` |
| 38 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/ColorCorrect.cginc` | Shader include | EffectCommon 递归依赖，与源逐字节一致 | 2012 | `1af89abfd8537f3488ee3a8847faac28d2950ec5eb93d4701fb195c137cc6baf` |
| 39 | `LYF_MMO/Assets/artres/Resources/Shader/RO/Include/ColorCorrect.cginc.meta` | 文件 meta | include meta（新 GUID，与全工程已有 guid 无冲突） | 176 | `3543342ba0f24a1af247e8cbbecd67fae7226272fe7794018d780d4bf104c5ee` |
| 40 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/RO/artres/Resources/Shader/RO/Scene/ROCloud-SoftAlphaBlend.shader` | 修改 | 删除上轮为修黑云临时加进 Properties 的 _ROCloudEnvorimentColor，退回纯 global 入口；改后与源逐字节一致 | 3540 | `599e2965386975eca214ae2edcfa1eefd2c1368f7e40378de5339de7885524b2` |
| 41 | `LYF_MMO/Assets/GM_Game/Scenes/RODungeon_Moshenta/sc_dungeon_moshenta_001.unity` | 修改 | 根节点挂 MEnvorimentSetting 并回填 937 条烘焙光绑定、三份数据引用、worldSize=665、fogST/cloudST；保存后新增 1 条 m_StitchLightmapSeams=0 预制体覆盖（烘焙期属性，运行期无影响） | 10017238 | `92c2ce0db331ac918614e69a01c113efbf929c67aea9a42a94432e9f511422a9` |

## 9. 未验证边界与限制

- **未做**正式游戏场景进出流程（Boot / 配置表 / YooAsset 运行时加载）验证；本组件的应用依赖"场景被打开并激活根对象"，没有在真实进出流程里跑过。
- **未做**角色光照接入验证：角色光四路颜色已写入 global 通道并逐位核对，但角色 shader 是否消费、消费后观感如何，未验证。
- **未做**客户端完整编译与打包验证（只做了 Editor 编译 + Console 0 error）。
- 未做天气循环、昼夜过渡、雨雪雷电、服务端协议、后处理/相机/全局管理框架。
- 未重建烘焙光、未改网格/Prefab 结构；679 条 stripped 文档是 Unity 保存时物化的，不是重新烘焙。
- `cl_` 资产的 `reflectMap` 引用 `Hdr01.jpg`（Cubemap，两侧 meta 一致）；源中未定位的反射 GUID 边界保留，未新造反射资源。
- 工程内若还有其它 RO Shader 引用 `Include/` 下未复制的 `.cginc`，属另外的全局问题，本轮只按授权补 `EffectOnly` 闭包，**未扩大范围**。
- 编辑器预览会写逐渲染器 `lightmapIndex`，下一帧可能把场景标脏：**预览后不要保存**；要保存请先右键「恢复原全局状态」。
- 若进入场景时这 27 个全局通道原本就是 0（全新会话、无人写过），停用后如实还原成 0，此时云色 `(0,0,0,0)` 会让云发黑——这是"恢复前态"的真实结果，**未用硬编码伪装**。
- 该场景 `LightmapSettings` 在 YAML 里不带 lightmap 条目，光图靠组件注入；若有别的代码在组件应用前读取 `LightmapSettings.lightmaps`，会读到空。

## 10. 用量说明

- 证据：`_temp/ro-environment-implement-token-collect.txt`、`_temp/ro-environment-implement-token-report.json`（由主 Agent 的 `token-monitor` collect/report 产出，本子会话未运行该技能）。
- 摘要（**仅代码块/数值摘要，不含源码、提示词或全会话内容**）：
  - `collect`：`codex 19 files 3390 turns 58 new`，页脚自注 `codex adapter is experimental — verify totals against 'codex' usage screens`。
  - `report`：`days=1`、`events=760`、`sessions=4`、`inputTokens=1,976,285`、`outputTokens=373,838`、`cacheReadTokens=158,091,136`、`thinkingTokens=166,334`、`spendTokens=2,350,123`、`costUsd≈37.15`（`costEstimated=True`）、`cacheHitRatio=0.9877`。
  - `coverage`：`source=codex`、`activeDays=2`（2026-10-26 ~ 2026-10-27）、`windowDays=1`。
- 口径必须说清：
  - 这是**近 1 日、Codex 侧、项目 `LYFMMORGP` 的聚合**，adapter 处于 experimental，**不是本任务的用量，也不是账户余额**。
  - 五类活动拆分（thinking / coding / exploration / testing / shipping / conversation）里 exploration / testing / shipping 为 0，属**未记录/不可测**，不据此推断本任务没做探索或测试。
  - **WorkBuddy 侧独立额度不可测**：本机没有可用的 WorkBuddy 额度接口。本子会话只能声明开发两轮未切换模型（`deepseek-v4.1-flash`）。

## 11. 技能使用

- 主 Agent 侧（按实际派工/验收记录，本子会话不冒认）：
  - `project-subagent-coordination` — 派工、逐轮验收、失败退回
  - `workbuddy-executor` — 原生会话恢复与"原会话返修"
  - `unity-mcp-orchestrator` — 独立实测 Shader 支持/错误、引用与 Console
  - `task-log-policy` — 按批准范围列单写记录（即本日志与同步规则）
  - `token-monitor` — 上面的 `collect` / `report` 额度证据
- WorkBuddy 子会话侧（如实）：**开发两轮均未使用任何技能**，未通过 Skill 工具加载 Skill；全部工作由 Unity MCP（`execute_code` / `read_console` 等）与本地 Python/Bash 完成。MCP 与 CLI 属于工具调用，不计为技能。记录同步这一轮同样未使用技能。

## 12. Git 状态

- 本任务**未执行** add / commit / stash / reset / 切分支 / push；源 RO 工程只读。
- 集成动作：**待主 Agent 精确暂存并提交推送**。工作区另有大量前序任务的未提交改动（开工前 97 个 ` M` + 203 个 `??`），本任务只应集成上表 41 个文件，不得吸收其他改动。
- 场景文件按已验收版本（sha256 `92c2ce0d…`，10,017,238 B）提交；若用户后续保存导致漂移，以漂移后文件为准并披露，不回滚用户修改。

## 13. 证据索引

| 内容 | 路径（项目根相对） |
|---|---|
| 实施与验证报告（含两轮失败根因） | `_temp/ro-environment-implement-review.md` |
| 逐项清单（机器可读） | `_temp/ro-environment-implement-final-manifest.json` |
| 第 1 轮结果 | `_temp/ro-environment-implement-final.json` |
| 第 2 轮结果 | `_temp/ro-environment-implement-r2-final.json` |
| 场景验收快照 | `_temp/ro-environment-implement-accepted-scene.unity` |
| 27 通道三阶段明细 | `_temp/ro-environment-implement-r2-channels.txt` |
| 独立期望值复算 | `_temp/ro-environment-implement-r2-expected-check.py` / `-expected-check.txt` |
| 937 绑定计划 | `_temp/ro-environment-implement-binding-plan.tsv` / `-binding-plan.json` |
| 源组件解析 | `_temp/ro-environment-implement-source-bindings.json` |
| 场景语义 diff（本轮新增） | `_temp/ro-environment-record-scene-diff.json` |
| HEAD 版场景（只读取用） | `_temp/ro-environment-record-scene-head.unity` |
| 记录前原始字节快照 | `_temp/ro-environment-record-before/`（含 `manifest-before.json`） |
| 记录同步核查 | `_temp/ro-environment-record-sync-check.json` |
| 本记录任务结果 | `_temp/ro-environment-record-final.json` |
| 截图（全景/近景/Play/lightmap 开关/云色扰动） | `_temp/ro-environment-implement-*.png` |

## 14. 主 Agent Git 集成实际回执

- 集成分支：`codex/ro-first-dungeon-scene-20261026`，基线 `0b3c33806ef4637cede0667f425091d727301878`。
- 实现与记录提交：`c03a0f50cf677839db1c01fa02da8e221a69c315`，中文说明 `feat: 挂载魔神塔最小环境并补齐RO特效Shader依赖`。
- 精确暂存 48 项：本次清单 41 个程序/资源文件及 `.meta`，新增本日志，四份双语入口的本次追加段、难点本次追加段与由已暂存英文事实生成的索引；原工作区已有记录修改未混入提交。五个既有文件的原字节前缀保留，提交内容按基线加本次追加构造；实际核对差异为 0。
- 已成功推送 `origin/codex/ro-first-dungeon-scene-20261026`，远端回执 `0b3c338..c03a0f5`；未强制推送、未合并或推送主分支。
- 本节仅补充真实 Git 回执，随后单独提交并推送回执文件；不改变实现或双语事实，不重复已通过测试。最终回执提交号见本文件 Git 历史及交付回复。
- 目录机械重建已执行；工作区索引对应工作区全部既有源记录，提交索引对应本次提交的源记录，保留两者差异以免夹带其他任务。

- 最终口径校正：LightProbes 源资产为 `!u!258` Unity YAML 文本；三份烘焙资产逐字节同源仍成立，英文/中文 CURRENT_TASK 本次段落已由 WorkBuddy 定向改为“两张 EXR 与一份 YAML LightProbes”。单行可逆字节核查通过，历史不改；证据 `_temp/ro-environment-record-format-fix.json`。实现与验收输入均未改变。
- Git 回执提交 `d5522434cfbfdb6ef8bb6454209d03f3f4d679de` 已成功推送（远端回执 `c03a0f5..d552243`）；上述格式文字校正随后独立提交推送，最终提交号见 Git 历史。

## 15. 只读查询记录：柱子编辑态看不到烘焙光图（`RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027`）

### 15.1 身份与轮次口径

- 任务编号：`RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027`，性质为**只读核验**，无业务改动。
- 提供方与执行：WorkBuddy 原生桌面会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`，工作目录 `D:/unitypro/LYFMMORGP`；基线 `d994157079a22d28552a2cf68365f58c41a3a4be`，分支 `codex/ro-first-dungeon-scene-20261026`。分支与基线只引用主 Agent 提供的事实，本子任务不执行 Git 核对。
- 实际模型：`deepseek-v4.1-flash`，由主 Agent 从原生会话侧独立读到的 `requestModelId` 观察确认；本子会话未切换模型。
- 轮次口径：本查询是**纯调查 1 轮**，随后这一轮是**记录／中英同步子任务**，两者都**不算开发轮次**。上文第 1、2 节那样的旧环境组件开发**仍然是 2 轮**（第 1 轮实现 + 第 2 轮原地返修），不因本次记录改成 3 轮。
- 调用链路事实：原生传输在本轮产物文件写完之后断开，没有成功的最终 RPC 回执；核心结论由主 Agent 从实际产物文件加独立核验接受，未重复派发调查。

### 15.2 问题与「同一根柱子」的证据

- 用户问题：RO 工程里柱子／道路的 Mesh Renderer 能看到 Lightmap 与阴影（截图 `C:/Users/lyf/AppData/Local/Temp/codex-clipboard-d9c208d6-e659-43ee-8598-bff1191b7307.png`：Lightmap Static、Scale In Lightmap `0.1`、Baked Lightmap Index `1`、Tiling `(0.04050754, 0.04050754)`、Offset `(0.3158843, 0.7874021)`、材质 `sc_moshenta_001_c`），本项目用同样材质却设不了。
- 同对象定位（按坐标加预制体，不按名字猜）：`sc_dungeon_moshenta_building_002 (1)`，本地坐标 `(72.987, 79.1624, 589.11035)`，预制体实例 `150537529`，源预制体 GUID `e12c4a666ffc9f54dbb6f2ebc6fba78d`；坐标加预制体在源与目标各只唯一命中 1 个实例，无第二候选。
- 绑定逐值对比如下，index 一致，差异只在 scaleOffset 第 7 位十进制的 float 序列化舍入（约 4e-9）。截图上的三个数值与源、目标两侧都对得上；该 scaleOffset 在源 937 条绑定里只出现 1 次，不存在把别的物体当成本柱子的情况。

| 位置 | 渲染器 fileID | index | scaleOffset |
|---|---|---|---|
| 源 937 条绑定 | `117451335` | `1` | `(0.040507544, 0.040507544, 0.31588426, 0.7874021)` |
| 本项目副本 | `150537531` | `1` | `(0.04050754, 0.04050754, 0.31588426, 0.7874021)` |

### 15.3 当时的编辑器观察（历史观测，不是本子任务重新验证）

- 主 Agent 同时对比 YAML 记录与实际编辑器，结论为：`scaleInLightmap=0.1`、`Receive GI=Lightmaps`、`LightmapSettings.lightmaps=0`、`renderer.lightmapIndex=-1`、环境组件 `_applied=False`、`Cast Shadows=Off`、`Receive Shadows=False`、场景 `dirty=True`。
- 由此成立的判断只有一句：**旧烘焙绑定确实存在组件数据里，只是当前没有注入到编辑态的 `LightmapSettings`**。可编辑的烘焙设置（Contribute GI／Scale In Lightmap／Receive GI／Stitch Seams／Lightmap Parameters）与「Baked Lightmap 结果显示」是两件不同的事，后者是烘焙产物，不能像材质槽那样直接拖。
- 本子任务**没有**重跑 Unity，上述数值只是记录转述，明确标注为「当时观察」。

### 15.4 源美术链路与发布链路的差别

- 源美术烘焙场景 `Assets/artres/_GameRes/Scenes/_Scenes/sc_dungeon_moshenta_001/sc_dungeon_moshenta_001.unity`：带 `LightingDataAsset`，同实例 `Cast Shadows=1`、`Receive Shadows=1`。
- 源发布场景 `Assets/artres/Resources/Scenes/sc_dungeon_moshenta_001/sc_dungeon_moshenta_001.unity`：`m_LightingDataAsset={fileID: 0}`，同实例 `Cast Shadows=0`、`Receive Shadows=0`；本项目副本与发布场景一致（`0/0`、无 `LightingDataAsset`）。
- 因此截图那套设置与**美术烘焙场景**一致；但仅凭图片**不能证明**当时打开的是哪条路径，记录里不写「已确证来自美术场景」。
- 烘焙产物落在 `_GameRes/Scenes/_Scenes/_Lightmaps/sc_dungeon_moshenta_001/`，导出时由 `MSceneBakePanel` 复制成 `Resources/SceneEnviroData/` 下的 `lm_` 资产并清空发布场景光图；运行时由 `MEnvorimentSetting.ApplyLightmapData` 写回 `renderer.lightmapIndex`、`renderer.lightmapScaleOffset`、`LightmapSettings.lightmaps`、`LightmapSettings.lightProbes`。本项目是**直接消费这套已烘好的产物**，没有搬烘焙面板与导出链路，所以能「接收旧烘焙结果」，不能「改完再重新算光」。

### 15.5 被否决的结论与仍属未验证的项

原始只读报告里有几处不成立，按主 Agent 验收记录作废，只采信验收事实：

- 「当前场景 0 盏 Light」→ 作废：主 Agent 实测为 **1 盏**。
- 「看 Lightmap 时取消勾选组件 Enabled 就能还原」→ 不采纳：本组件没有 `ExecuteAlways`，停用不等于编辑态的还原回调；组件只有显式右键菜单「应用环境配置」这一条入口。
- 「持久预览必须落盘序列化」→ 不作为结论：没有实现、没有授权，属推测。
- 「`_Lightmaps` 里多出来的 EXR 是历史烘焙残留」→ 未证明，不写。

仍未验证（本轮同样没有补）：

- 没有 Apply 前后的 Inspector 截图；「Baked Lightmap 区不显示」仍是由 `lightmaps=0` 加 `lightmapIndex=-1` 推断，非眼见。
- 没有重新烘焙，也没有验证重烘后画面与 937 条绑定是否要重算。
- 没有打开 RO 编辑器实例，无法核对截图当时的 Unity 版本与打开路径。
- 场景 `dirty=True`（用户未保存改动）按约束保留，未清除、未回滚旧快照、未保存。

### 15.6 本次记录范围与额度证据

- 本次只动 3 个正式文件、各追加独立段，既有字节不改：
  - `项目修改日志/2026-10-27-魔神塔最小环境组件与Shader依赖修复_CHANGELOG.md`（本节）
  - `Docs/AI/QUERY_TASKS.en.md`（新增 `## [QUERY][场景][RO-MOSHENTA-LIGHTMAP-INSPECTOR-QUERY-20261027]`）
  - `Docs/AI/QUERY_TASKS.md`（同编号中文段）
- `Docs/AI/RECORD_INDEX.md` 由主 Agent 最后机械生成，本子任务不改；未改 cs／场景／资源／其它正式记录／`项目分析/项目难点.md`；未跑 Unity、未编译、未测试、未操作 Git／网关／模型配置、未重跑旧映射。
- 额度证据路径：`_temp/ro-lightmap-inspector-query-token-collect.txt`、`_temp/ro-lightmap-inspector-query-token-report.json`。口径是**近 1 日、项目 `LYFMMORGP`、`days=1`、覆盖 2026-10-26 至 2026-10-27** 的聚合，adapter 自注 experimental；**不是本任务用量，不是账单，也不是账户余额**。本任务数字由主 Agent 稍后另行 collect／report，此处不重复。
- 未验证：WorkBuddy 侧独立额度不可测，本子会话只能声明未切换模型。
- 验收与证据文件：`_temp/ro-lightmap-inspector-query-codex-accept.json`、`_temp/ro-lightmap-inspector-query-review.md`、`_temp/ro-lightmap-inspector-query-final.json`；原始报告中被否决的断言以验收记录为准。
- Git：本子任务**未执行**任何 Git 操作；只集成这一次增量由主 Agent 负责。
