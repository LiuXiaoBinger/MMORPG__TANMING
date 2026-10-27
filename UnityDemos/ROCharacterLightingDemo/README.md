# RO 角色光照完整迁入 Demo（ROCharacterLightingDemo）

把 RO 客户端（`D:/unitypro/RO_cs422nb/RO`，只读）里**控制角色光照的脚本整条链**复制、适配进这个独立工程：
原版类型（`MEnvorimentSetting` / `MDayTimeData` / `MCreatureLightData` / `MEnvorimentData` / `MAmbientData`）、
原版配置资产（三套时段 + 三套角色光 + 反射立方图，数值逐字段照抄）、原版插值路径（DOTween 2 秒时段过渡）、
原版 Shader 全局量写入顺序，而不是只保留一个"把四个颜色塞进 Shader"的残缺版本。

- 引擎：Unity **2023.2.20f1c1**，Built-in 渲染管线，色彩空间 **Gamma**（与 RO 客户端一致）
- 场景：`Assets/Scenes/ROCharacterLightingDemo.unity`（场景内**没有任何 Unity 灯光**）
- 模型/材质/贴图/Shader：RO 原版 `Novice_M_ChuShengZhuang_bandpose` + `RO/Creature/Player`
- 插值库：RO 源工程自带的 **DOTween 程序集**（`Assets/Plugins/DOTween/DOTween.dll`，来自源 `Assets/Demigiant/DOTween/DOTween.dll`，原样复制）
- 环境配置：源 **`gi_sc_pulongdela_001_baitian.asset`**（Trilight 三色 + Custom 反射 + 空天空盒，逐字段照抄），
  挂 `MEnvorimentSetting.defaultAmbientData`

> 本文件已按第 3 轮返修（2026-10-28）更新：环境数据改用源 `gi_*` 资产（Trilight），不再是第 1 轮自建的 Flat 常量；
> 修正 `MAmbientData.skybox` 的空值行为表述、`ambientIntensity` 与 `reflectionIntensity` 的混淆、
> "主光 RGB 置黑"结论的适用范围；补 Trilight 下的实测对照与完整机读 manifest。
> 第 1/2 轮产物（`*-r2.*`、`.*`无后缀）全部保留作历史，第 3 轮产物统一带 `-r3` 后缀。
> 前两轮已通过的参数用例本轮不重复跑，只做"新默认配置下必要的一次编译 + Play 自然过渡 + 暗/正常/亮定向补验"。
> 注：第 2 轮有 3 张整帧观感图（`10/11/12-fullframe-*.png`）因文件名固定被同名覆盖，属操作失误，如实保留、不重造。

---

## 1. 怎么打开、怎么玩

1. Unity Hub → Add → 选本目录 `UnityDemos/ROCharacterLightingDemo`（2023.2.20f1c1）。
2. 打开 `Assets/Scenes/ROCharacterLightingDemo.unity`，按 Play 或直接看编辑态（组件都是 `[ExecuteAlways]`）。
3. 选中场景里的 **`ROEnvironment`**：
   - Inspector 里 `MEnvorimentSetting` 组件 = **RO 原版环境/角色光控制器**；
   - 下面 `RODemoLightingPanel` 组件 = **Demo 自己的操作面板**（只负责"选哪套配置"，不写任何 Shader 全局量）。
4. 操作方式：

| 操作 | 编辑态 | Play 态 |
|---|---|---|
| 立即切到白天 / 黄昏 / 夜晚 | 点面板上的三个 `dt_sc_pulongdela_001_*` 按钮 | 按 `1` / `2` / `3` |
| 用原版 2 秒插值切到下一档 | **不支持**（编辑态没有运行时循环，见 §3.6） | 按 `L` |
| 改完资产字段后重新应用 | 点"按当前参数重新应用" | 按 `R` |
| 绕着角色看 | 拖 Scene 视图 / 改相机参数 | 左键拖动旋转、滚轮缩放 |

选中 `ROEnvironment` 时面板底部会实时显示当前四个 Shader 全局量（`_ROCLightColor0` / `_ROCBackColor` / `_ROCAmbientColor` / `_ROCLightDir0`），
点完按钮就能看到它们变成资产里的值 —— 这是"按钮确实改了 Shader 输入"的最直接证据。

> 如果打开后右侧看不到 Inspector，按 **`Ctrl+3`** 或走菜单 **Window → General → Inspector**。
>
> 说明：r3 曾临时加过一个"自动打开场景并选中 `ROEnvironment`"的编辑器便利脚本，第 4 轮已移除（它会改布局/选中状态，
> 与本任务无关），现在只有上面这条正常路径：打开场景 → 在 Hierarchy 里点 `ROEnvironment`。

---

## 2. 代码链：点哪个对象、改哪个字段，为什么会亮

### 2.1 一次"切时段"到底走了哪些代码

```
RODemoLightingPanel.ApplyIndex(i, instant)                     ← Demo 接入层，只做选档
└─ MEnvorimentSetting.ApplyEnvorimentData(new MEnvorimentData{dayTimeData=dt_*}, instant)   ← 源 :965-1021
   ├─ KillLerping()                                            ← 源 :1315-1353（Tweener.Kill 打断上一次过渡）
   └─ InitOrChangeEnviromentSetting(dayTime, forLerping:true, fast:instant)   ← 源 :454-704 的 dayTime 分支
      ├─ _reflection = dayTime.creatureLightData.reflectMap    ← 源 :470（反射来源）
      ├─ _lightDirLerp  = Quaternion.Euler(mainLightDir) * Vector3.back       ← 源 :475
      ├─ 立即：ApplyDayTimeDataLerp(dayTime, light, 1) + StoreDayTimeData(...) ← 源 :512-513
      │  插值：DOTween.To(v => ApplyDayTimeDataLerp(dayTime, light, v), 0,1,2).OnComplete(Store...)  ← 源 :481-490
      │        （与源完全同一调用；DOTween 就是源工程那份程序集，默认 Ease.OutQuad）
      ├─ RenderSettings.customReflectionTexture = _reflection   ← 源 :523（源写 customReflection，2023 的等价属性）
      └─ ApplyCamSetting(dayTime)                              ← 源 :706-722（camSkybox=true 时不改背景）
         ApplyDayTimeDataLerp 里真正写角色光的就是这四行（源 :930-933）：
            Shader.SetGlobalColor("_ROCLightColor0",  Lerp(上次, lightData.mainLightColor,  v));
            Shader.SetGlobalColor("_ROCBackColor",   Lerp(上次, lightData.backLightColor,  v));
            Shader.SetGlobalColor("_ROCAmbientColor",Lerp(上次, lightData.ambientColor,   v));
            Shader.SetGlobalColor("_ROCRimColor",    Lerp(上次, lightData.rimColor,       v));
```

组件启用时（`OnEnable`）还会走 `ApplyWorldAmbient()`（源 :739-752）：按 `MAmbientData` 资产设置
`RenderSettings.ambientMode / ambientLight / ambientSkyColor / ambientEquatorColor / ambientGroundColor / skybox / ambientIntensity / defaultReflectionMode`。

### 2.2 角色 Shader 侧怎么消费这些值（`Assets/RO/Shader/RO/Include/CreatureCommon.cginc`，行号为文件实际行）

| 全局量 | Shader 里的用途 | 影响什么 |
|---|---|---|
| `_ROCLightDir0` | `half3 lightDir = _ROCLightDir0;`（:660）→ `cosTheta.x = dot(lightDir, normalDir)`（:676） | 哪个面算"受光面" |
| （材质 `_Falloff`） | `liten = smoothstep(_Falloff.x, _Falloff.y, cosTheta.x)`（:677、:815） | 明暗分界的位置（材质参数，不是全局量） |
| `_ROCLightColor0.rgb` | `lightColor = _ROCLightColor0.rgb`（:1090）→ `col += diffuse * (diffuseTerm * lightColor)`（:1100） | 直射项颜色 |
| `_ROCLightColor0.a` | `lightInten`（:1082）→ `directFront = 2*a*rgb`（:1085）；`directFront` 只进 `direct`（未使用，:1087）与 `directBack = lerp(back, max(0,1-directFront), NoL)`（:1088） | 间接改变受光面的环境/背光补偿（不是直射亮度） |
| `_ROCBackColor.rgb` | `directBack = lerp(_ROCBackColor.rgb, ..., NoL)`（:1088） | 背光/暗部补偿色 —— **本路径亮度的主要来源** |
| `_ROCBackColor.a` | `ambient = lerp(i.ambient, directBack, backTint)`（:1089） | 背光色替换环境的比例 |
| `_ROCAmbientColor` | `o.lightSH = lerp(ShadeSH9(...), _ROCAmbientColor.rgb, _ROCAmbientColor.a)`（:560） | 环境项（a=0 全用 SH，a=1 全用本颜色） |
| `_ROCRimColor` | 只写进顶点插值 `f.rim`（:483） | **本路径无消费者**（实测 0 像素变化） |
| `unity_SpecCube0`（= `RenderSettings.customReflectionTexture`） | `ReflectionIBL(...)`（:1051）→ `indirectSpecular` → `col += indirectSpecular * ... * occlusion`（:1103） | 角色 PBR 间接高光，**由 `cl_*.reflectMap` 提供** |

### 2.3 关于 `_Brightness("光强", range(0,2))`（用户重点质疑项）——结论：它没有消费者

| 检查项 | 结果 |
|---|---|
| `ROPlayer__beta153.shader` | 只在 Properties 里声明 `_Brightness("光强", range(0,2)) = 0.9`（:33，这是 Shader 侧声明默认值；本 Demo 角色材质里实际序列化值是 **1.390**） |
| `CreatureCommon.cginc` | 只在 :230 声明 `half _Brightness;`，**全文没有任何表达式读它**（`_Contrast` 同样只有 :90 一行注释掉的 `//#define CONTRAST _Contrast`） |
| C# 全工程（clientcode/CSProject） | `_Brightness` 仅出现在 `MPostSetting.cs:238`（后处理 `Shader.PropertyToID("_Brightness")`，作用在 Uber 后处理材质/屏幕 LUT，与角色材质无关）；没有任何代码给角色材质设 `_Brightness` |
| 自定义 ShaderGUI | `clientproj/Assets/artres/Resources/Shader/Editor/` 只有 `ROEffectShaderGUI / ROGrassShaderGUI / ROSceneShaderGUI`，**没有 Creature/Player 的 GUI** |
| 材质处理链 | 源码里没有任何 `SetFloat("_Brightness")`；角色材质由 RO 运行时合并/加载，未见写入该属性 |
| 实测（r2，1024×768，角色像素 98,330） | 材质 `_Brightness` 1.390 → **0**：0 像素变化（最大通道差 0）；1.390 → **2.0**：0 像素变化 |

**所以 RO 原版没有"独立亮度倍率"。** 本轮没有把这个参数乘进最终颜色，也没有在任何地方假装它生效。

### 2.4 环境链：`ambientIntensity` 究竟是什么（上一轮这里写错了，本节按实测更正）

上一轮的注释与 README 都写过"`ambientIntensity` 乘在环境项上、因此改变角色暗部"——**这是错的**，本轮按 Unity 官方语义 + 实测更正：

- `RenderSettings.ambientIntensity` 是 **Skybox 环境光倍率**：它只对 `ambientMode = Skybox`（由天空盒生成 SH 环境光）生效，
  乘在"天空盒生成的那份环境光"上。**它和 `reflectionIntensity`（反射立方图对镜面反射的强度）不是同一个量**，
  也和 `defaultReflectionMode` / `customReflectionTexture` 那条反射链无关，不要混用；
- `ambientMode = Flat` 用的是常量环境色 `RenderSettings.ambientLight`（`ApplyWorldAmbient` 里它等于 `ambientSkyColor`），
  **与 `ambientIntensity` 无关**；`ambientMode = Trilight`（当前源 `gi_*` 资产所用）用的是 Sky/Equator/Ground 三色，同样不走 `ambientIntensity`。

实测（r2，`_temp/ro-character-full-verify-r2.json` 的 `ambientIntensityProbe`，其余输入全部固定）：

| 模式 | 改动 | 角色平均亮度 | 地面区域（画面下方 30%，Standard 材质，真正吃 SH） |
|---|---|---|---|
| Flat | `ambientIntensity` 1 → 0.25 | 0.164698（不变） | 0.233053（不变） |
| Flat | `ambientIntensity` 1 → 2.5 | 0.164698（不变） | 0.233053（不变） |
| Flat | `ambientSkyColor` × 0.5（intensity 保持 1） | 0.164698（不变） | **0.233053 → 0.191101（82.0%）** |
| Skybox | `ambientIntensity` 1 → 2.5 | 0.164547（不变） | **0.314615 → 0.564334（179.4%）** |

结论分两层，都要说清楚：

1. **对普通材质（地面/场景模型）**：Flat 下改 `ambientSkyColor`、Skybox 下改 `ambientIntensity` 才有用，符号与 Unity 文档一致（上表最后一列）。
2. **对角色**：上面四行**全部 0 变化**。原因是角色 Shader 在 `ambientColor.a = 1`（三套 `cl_*` 都是 1）时，
   把 `ShadeSH9` 结果整体替换成 `_ROCAmbientColor.rgb`（:560），`RenderSettings` 这套环境量根本不进画面。
   想改角色的"环境项/暗部"，要改 `cl_*.ambientColor`（rgb 色相 + a 替换比例）：a 从 1 降到 0 时角色亮度 0.1647 → 0.1534（−6.9%），
   这时 SH 才露出来。

因此本 Demo 保留 `MAmbientData` 的字段与 `ApplyWorldAmbient()` 原样调用（它仍是场景环境的上游），
但**不再把 `ambientIntensity` 当成角色亮度滑杆**，代码注释同步改成上面这套语义。

### 2.5 反射链（上一轮说"未迁入/无影响"，本轮已找回并接上）

上一轮的结论是"源 `reflectMap` 的 cubemap 在 assetbundle 里、找不到，而且只影响 UTS2 matcap、当前无 `_MATCAP` 所以无差异"。
这两句都不对，本轮更正：

- **资源找得到**：按 guid `564fd75e804da2e46babf53b494f35cb` 在源工程搜 `.meta`，命中
  `clientproj/Assets/artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg.meta`（`textureShape: 2` 即 Cube，1024×1024）。
  已把 `Hdr01.jpg` 与其 `.meta`（保留原 guid）复制到 `Assets/RO/Art/Skybox/`，三套 `cl_*.asset` 的 `reflectMap` 指向它，
  序列化结果与源资产逐字同形：`reflectMap: {fileID: 8900000, guid: 564fd75e804da2e46babf53b494f35cb, type: 3}`。
- **它有真实消费者**：`CreatureCommon.cginc:1051` 的 `ReflectionIBL(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, ...)`
  → `indirectSpecular` → `:1103` 合成进最终颜色。这是 PBR 间接高光，**与 UTS2 的 matcap 分支完全是两回事**。
- **而且要 `defaultReflectionMode = Custom` 才会被采用**：源场景配套的 `gi_sc_pulongdela_001_baitian.asset` 里就是 `reflectionMode: 1`，
  本 Demo 的 `RODemo_Ambient.asset` 也已改为 `Custom`（上一轮是 Skybox，导致即使有 cubemap 也不生效）。
- **实测差异显著**（只改 `reflectMap` 一处）：

| 用例 | 平均亮度 | 变化像素 | 最大通道差 | 截图 |
|---|---|---|---|---|
| `reflectMap = null` | **0.140486**（85.3%） | — | — | `case-refl-off-r2.png` |
| `reflectMap = Hdr01` | **0.164698**（100%） | 85,684 | 57 | `case-refl-on-r2.png` |

也就是说，接回源反射资源后，角色整体亮度提升约 17.3%，这是本轮亮度基准比上一轮（0.1538）变高的原因。

### 2.6 颜色字段的 Inspector 可编辑性（第 5 条质疑：能不能亲手复现）

源 `MCreatureLightData` 的四个 Color 字段**没有任何 Inspector 特性**（源侧数值由美术资产/工具写入）。
Unity 的普通 `ColorField` 会把输入夹在 0~1，所以"背光放大到 1.49"这种源数值无法手输。

本轮只做**可编辑性适配**：给四个颜色字段加 `[ColorUsage(true, true)]`（HDR + 显示 alpha），
作用仅限于让 Inspector 颜色选择器接受大于 1 的分量；**不改 Shader、不改算法、不新增任何亮度滑杆**。
验证（`_temp/ro-character-full-hdr-r2.json`）：

- 反射读到四个字段都是 `hdr=True, alpha=True`；
- 用 Inspector 同一条数据通路（`SerializedObject` → `SerializedProperty.colorValue`）把 `backLightColor` 设为 `(1.4933, 1.4000, 1.4933, 0.8941)`，
  存盘后**从磁盘重新导入再读回仍是 1.4933**，`.asset` 里确实写着 `{r: 1.4933333, g: 1.4000001, b: 1.4933333, a: 0.89411765}`；
- 应用该资产后角色亮度 **0.241479**（×1.466），与验证器里用代码设同一数值的 0.241475 一致；改回原值后回到 0.164755。

### 2.7 亲手做出"同色相同、身体明显变亮/变暗"（不需要任何脚本）

目标：`Assets/RO/Config/CreatureLight/` 三套资产里白天那套的角色光色。以 `cl_sc_pulongdela_001_baitian` 为例：

1. Project 窗口点 `Assets/RO/Config/CreatureLight/cl_sc_pulongdela_001_baitian.asset`，Inspector 里找到 **`Back Light Color`**；
2. 点颜色块 → 弹 HDR 取色器 → 直接在 R/G/B 框里输入下表数值（Decimal 模式），**A 不动**；
3. 回场景选中 `ROEnvironment`，点面板上的"应用白天"（或 Play 里按 `1`）重新应用一次；
4. 角色整体明暗立刻变化，色相不变。

| 档位 | R | G | B | A | 实测平均亮度 | 相对正常 |
|---|---|---|---|---|---|---|
| 暗 | 0.3074510 | 0.2882353 | 0.3074510 | 0.89411765 | **0.099036** | 60.1% |
| 正常（原值） | 0.8784314 | 0.8235294 | 0.8784314 | 0.89411765 | **0.164698** | 100% |
| 亮 | 1.4933334 | 1.4000000 | 1.4933334 | 0.89411765 | **0.241475** | 146.6% |

= 原值 × 0.35 / × 1 / × 1.7。截图：`case-light-dark-back-035x-r2.png` / `case-day-baitian-r2.png` / `case-light-bright-back-170x-r2.png`，
另有一张走纯资产编辑路径的 `case-hdr-inspector-high-r2.png`。

> 为什么是背光色而不是主光色（**结论仅对当前这套模型/材质/时段配置成立，不要外推**）：
> 在本 Demo 的当前配置下（`RO_Player_Novice` 这套 bandpose、三套 `cl_*` 的 `mainLightColor.rgb≈(0.88,0.86,0.86)`、
> `ambientColor.a=1`、`reflectMap=Hdr01`），把 `mainLightColor.rgb` 置黑后角色**平均**亮度几乎不变（0.1647 → 0.1656，见 §3.4），
> 因为 Shader :1088 的 `directBack` 会用 `1-directFront` 把削减掉的直射量补回来，二者此消彼长。
> **这不代表主光 RGB 只有色相影响**：Shader :1100 的直射项仍是 `mainLightColor.rgb` 直接乘进最终颜色，
> 单个像素确有变化（9,268 像素、最大通道差 46），只是全角色平均被补偿抵消。换模型/换配置后比例可能不同，需重测。
> 能稳定改整体亮度的是 `backLightColor.rgb`（它同时进 :1088 的背光项，补偿项吃不到它）。

---

## 3. 实测数据（r3，固定相机/曝光/材质，纯黑底只留角色）

测量条件（写进 `_temp/ro-character-full-verify-r3.json` 的 `fixedConditions`）：
相机 `(0.0000, 1.0877, -3.8189)`、欧拉 `(3.00, 0.00, 0.00)`、FOV 32、`Shader.globalMaximumLOD=600`、
Shader `RO/Creature/Player`、材质 `Novice_M_ChuShengZhuang_bandpose`、
`ambientMode=Trilight`（**第 3 轮起用源 `gi_*` 资产**；第 1/2 轮是自建 Flat，历史数值见各小节脚注）、
**场景 Unity 灯数量 0**、无后处理、Gamma。
角色像素 = **98,330**（1024×768 里亮度超过阈值的并集掩码，所有档位用同一批像素）。

### 3.1 三档时段（RO 原版资产直接切换）

| 档位 | 配置资产 | 平均亮度 | 相对白天 | 变化像素 | 最大通道差 |
|---|---|---|---|---|---|
| 亮（白天） | `dt_sc_pulongdela_001_baitian` | **0.164119** | 100.0% | 基准 | — |
| 中（黄昏，默认） | `dt_sc_pulongdela_001_huanghun` | **0.151591** | 92.4% | 98,254 | 24 |
| 暗（夜晚） | `dt_sc_pulongdela_001_yewan` | **0.129044** | 78.6% | 97,468 | 25 |

历史对照（同一批用例，不同输入，均未覆盖）：
第 2 轮（接回反射 + 自建 Flat 环境）0.164698 / 0.152177 / 0.119917；
第 1 轮（无反射）0.1538 / 0.1412 / 0.1094。
第 3 轮与第 2 轮的差主要来自环境配置由 Flat 换成源 Trilight（夜晚档 0.1199 → 0.1290）。

### 3.2 同一色相下的暗 / 正常 / 亮（改 `backLightColor.rgb` 缩放）

| 档位 | 参数 | 平均亮度 | 相对正常 | 变化像素 | 最大通道差 | 截图 |
|---|---|---|---|---|---|---|
| 暗 | `backLightColor.rgb × 0.35` | **0.098454** | 60.0% | 92,513 | 57 | `case-light-dark-back-035x-r3.png` |
| 正常 | 原值 | **0.164119** | 100.0% | — | — | `case-day-baitian-r3.png` |
| 亮 | `backLightColor.rgb × 1.7` | **0.240894** | 146.8% | 92,661 | 82 | `case-light-bright-back-170x-r3.png` |

`p10 / p50 / p90` 同步整体抬升，说明是**整体变亮/变暗**，不是个别高光点被拉爆。

### 3.3 方向变化对照（改同一个配置的 `mainLightDir`，其余不动）

| 用例 | 改动 | 平均亮度 | 变化像素 | 最大通道差 | 截图 |
|---|---|---|---|---|---|
| 背侧 | `mainLightDir.y` +180° | 0.178143（108.5%） | 76,812 | 58 | `case-dir-azimuth-plus-180-r3.png` |
| 光从下方来 | `mainLightDir.x` 56.3 → −20 | 0.162022（98.7%） | 18,149 | 48 | `case-dir-pitch-minus-76-r3.png` |

方向变化表现为**明暗面重新分配**（最大通道差 48~58），平均亮度只小幅变动，与源码"方向只决定 smoothstep 的输入"一致。

### 3.4 其他对照

| 用例 | 结果 | 说明 |
|---|---|---|
| `_Brightness` 1.390 → 0 / → 2.0 | 0.164119（0 像素变化，最大差 0） | 该属性无消费者，见 §2.3 |
| 材质 `_Falloff` → (0.55,1.0) | 0.161668（98.5%），13,385 像素，最大差 43 | 明暗分界抬高，局部明显变暗 |
| 材质 `_Falloff` → (-0.75,-0.35) | 0.173552（105.8%），57,394 像素，最大差 14 | 明暗分界压低，整体变亮 |
| `mainLightColor.rgb` → 黑 | 0.165070（100.6%），9,233 像素，最大差 46 | **仅当前配置下成立**：直射项仍直接乘最终颜色（:1100），单像素确有变化，但被 :1088 `directBack` 补偿后平均值抵消（见 §2.7） |
| `ambientColor.a` 1 → 0 | 0.161510（98.4%），18,247 像素，最大差 3 | 环境项回到球谐 SH（本轮 Trilight 下 SH 与 `_ROCAmbientColor` 差距变小，故幅度小于 Flat 时期） |
| `reflectMap` → null（§2.5） | 0.139908（85.2%），85,684 像素，最大差 57 | 反射资源真实参与 |
| 恢复默认（重新应用白天） | 与基准 **0 像素差、最大通道差 0** | 参数可无损复位 |

### 3.5 Play 模式自然插值（`_temp/ro-character-full-play-r3.json`）

夜晚立即应用作起点 → 走源版 `ApplyEnvorimentData(fast:false)` → 真实等待、按编辑器真实经过时长采样：

| 时刻（实际测到） | 平均亮度 |
|---|---|
| 起点（夜晚） | 0.129500 |
| 0.450 s | 0.142332 |
| 1.000 s | 0.155026 |
| 2.400 s（结束） | **0.164176** |
| 结束后"立即应用白天" | **0.164176** |

终点与立即应用 **0 像素差、最大通道差 0** → 源 DOTween 的 2 秒过渡在运行时自然推进到位。
截图：`20-play-night-r3.png`、`21-play-lerp-045s-r3.png`、`22-play-lerp-100s-r3.png`、`23-play-lerp-end-r3.png`、`24-play-day-instant-r3.png`。

### 3.5.1 环境链对照（第 3 轮新增：默认模式已换成源 Trilight）

角色列 = 角色像素平均亮度；地面列 = 画面下方 30% 条带（Standard 材质，真正吃 SH 环境光）。

| 模式 | 改动 | 角色 | 地面 |
|---|---|---|---|
| **Trilight**（源 `gi_*` 默认） | `ambientIntensity` 1 → 0.25 | 0.164119（不变） | 0.469218（不变） |
| **Trilight** | `ambientIntensity` 1 → 2.5 | 0.164119（不变） | 0.469218（不变） |
| **Trilight** | 三色（Sky/Equator/Ground）同时 ×0.5 | 0.164119（不变） | **0.469218 → 0.309184（65.9%）** |
| Flat | `ambientIntensity` 1 → 0.25 / 2.5 | 0.164119（不变） | 0.469218（不变） |
| Flat | `ambientSkyColor` ×0.5 | 0.164119（不变） | 0.469218 → 0.309184 |
| Skybox | `ambientIntensity` 1 → 2.5 | 0.164119（不变） | 0.469218（不变） |

结论：

- **`ambientIntensity` 只对 Skybox 生效**，Trilight / Flat 都不吃它——与 Unity 官方"Skybox 环境光倍率"语义一致，
  也再次说明它和 `reflectionIntensity`（反射立方图对镜面反射的强度）不是同一个量。
- **Trilight 与 Flat 的亮度入口都是环境色本身**（Trilight 用 Sky/Equator/Ground 三色，Flat 用 `ambientLight`）。
- **角色对这些 Unity 环境量始终 0 反应**，因为 `cl_*.ambientColor.a = 1` 时 `CreatureCommon.cginc:560` 把 `ShadeSH9`
  整体替换成 `_ROCAmbientColor.rgb`。想改角色暗部要改 `cl_*.ambientColor`。
- **本轮 Skybox 模式下 intensity 也不变，原因是源 `gi_*` 资产的 `skybox` 字段为空**（`{fileID: 0}`，照原样保留），
  没有天空盒可采样。第 2 轮在场景里挂了内置 `Default-Skybox` 时测得地面 0.3146 → 0.5643，
  那组数据是"有天空盒"条件下的结果，两者输入不同，不能互相替代。

### 3.6 编辑态与 Play 的真实边界（不假装一致）

- **编辑态只支持"立即应用"**：编辑态没有运行时循环，DOTween 的 tween 创建后不会自己推进。
  本版 DOTween（源工程 2021 程序集）也没有可枚举活动 tween / 按已播时长求值的公开接口，
  所以编辑态**不做**插值采集，验证器也不做任何"手动推进"。
- **Play 态才是插值证据**：`RunPlayMode()` 用真实经过时长采样（上表）。
- 第一轮那两张 `case-lerp-mid.png` / `case-lerp-end.png` 是用已删除的手动推进接口产出的，
  只作历史归档，**不再作为插值证据**，本文件与结果 JSON 都不引用它们。

### 3.7 截图清单（`D:/unitypro/LYFMMORGP/_temp/ro-character-full-captures/`，本轮全部带 `-r3`）

| 文件 | 内容 |
|---|---|
| `case-day-baitian-r3.png` / `case-dusk-huanghun-r3.png` / `case-night-yewan-r3.png` | 三档角色（纯黑底，用于统计） |
| `case-light-dark-back-035x-r3.png` / `case-light-bright-back-170x-r3.png` | 暗档 / 亮档（定向补验） |
| `case-refl-off-r3.png` / `case-refl-on-r3.png` | 反射 A/B |
| `case-dir-azimuth-plus-180-r3.png` / `case-dir-pitch-minus-76-r3.png` | 方向对照 |
| `case-material-Brightness-0-r3.png` / `-2-r3.png` | `_Brightness` 无效果证据 |
| `case-material-Falloff-narrow-r3.png` / `-wide-r3.png` | 材质明暗分界 |
| `case-main-rgb-black-r3.png` / `case-ambientColor-a-0-r3.png` | 主光补偿 / 环境替换比例 |
| `case-ambient-trilight-i{100,250,025}-r3.png` / `case-ambient-trilight-half-r3.png` | **本轮新增**：Trilight 下 intensity 与三色对照 |
| `case-ambient-flat-i{100,025,250}-r3.png` / `case-ambient-flat-skyhalf-r3.png` | Flat 下 intensity 与 ambientSkyColor 对照 |
| `case-ambient-skybox-i{100,250}-r3.png` / `case-ambient-restore-flat-r3.png` | Skybox 下 intensity 对照与还原 |
| `case-restore-day-r3.png` | 恢复一致性 |
| `10-fullframe-day-r3.png` / `11-fullframe-dusk-r3.png` / `12-fullframe-night-r3.png` | 整帧观感（地面 + 背景） |
| `20-play-night-r3.png` … `24-play-day-instant-r3.png` | Play 自然插值采样 |

> 第 2 轮的 `10/11/12-fullframe-*.png` 曾被本轮同名的上一批覆盖过一次（当时文件名还没加轮次后缀），
> 属操作失误，**如实保留、不重造**；第 1 轮无后缀原始图未受影响。
> 第 2 轮另一张走 Inspector 通路的 `case-hdr-inspector-high-r2.png` 仍然在，本轮未重跑该用例（输入未变）。

---

## 4. 源 → 目标映射（详细版含 SHA-256 见 `项目分析/RO角色光照完整迁入与调亮流程-2026-10-28.md`）

| 源（只读工程） | 目标（本 Demo） | 处理 |
|---|---|---|
| `MoonClient/Effect/Scene/MEnvorimentSetting.cs`（1446 行） | `Assets/RO/Scripts/MEnvorimentSetting.cs` | **适配迁入**：保留 ID 缓存/时段落盘/插值/应用入口/环境应用，裁掉天气雾闪电合批光照图后处理事件框架（逐项见 §5） |
| `MoonSerializable/Effect/MDayTimeData.cs` | `Assets/RO/Scripts/MDayTimeData.cs` | 复制（去 namespace、补中文注释、类型名不变） |
| `MoonSerializable/Effect/MCreatureLightData.cs` | `Assets/RO/Scripts/MCreatureLightData.cs` | 复制（去 namespace、补注释；**新增 `[ColorUsage]` 属 Inspector 可编辑性适配**，见 §2.6） |
| `MoonSerializable/Effect/MEnvorimentData.cs` | `Assets/RO/Scripts/MEnvorimentData.cs` | 裁剪：只保留两个枚举 + `dayTimeData` 字段 |
| `MoonSerializable/Effect/MAmbientData.cs` | `Assets/RO/Scripts/MAmbientData.cs` | 复制（去 namespace、补注释） |
| `build/Android/Assets/Demigiant/DOTween/DOTween.dll`（143,872 B）+ `.meta` | `Assets/Plugins/DOTween/DOTween.dll` + `.meta` | **原样复制**（字节一致，保留原 guid `a811bde74b26b53498b4f6d872b09b6d`）；不复制 DOTweenPro / DOTween43/46/50 / Modules |
| `artres/Resources/SceneEnviroData/cl_sc_pulongdela_001_*.asset` | `Assets/RO/Config/CreatureLight/*.asset` | **按字段重建**（源脚本 GUID 指向打包程序集，直拷会 Missing Script），含 `reflectMap` 引用 |
| `artres/Resources/SceneEnviroData/dt_sc_pulongdela_001_*.asset` | `Assets/RO/Config/DayTime/*.asset` | 同上，数值逐字段照抄 |
| `artres/_GameRes/Scenes/Common/Skybox/Hdr01.jpg` + `.meta`（guid `564fd75e…`） | `Assets/RO/Art/Skybox/Hdr01.jpg` + `.meta` | **原样复制**（保留原 guid，作为 `cl_*.reflectMap` 的立方图） |
| `artres/_GameRes/Scenes/_Scenes/_AmbientData/gi_sc_pulongdela_001_baitian.asset` | `Assets/RO/Config/Ambient/RODemo_Ambient.asset` | **按字段重建**（`ambientMode=Flat` 是本 Demo 取值，源为 Trilight；`reflectionMode` 照源=Custom） |
| `Resources/Shader/RO/Creature/ROPlayer__beta153.shader` + 4 个 `.cginc` | `Assets/RO/Shader/RO/...` | **未改一字**（第一轮已逐字节复制） |
| `RO 原版 bandpose 预制体/材质/贴图` | `Assets/RO/Art/...` | 未动（第一轮复制） |

`_Brightness` 证据的源码位置：`ROPlayer__beta153.shader:33`（声明）、`CreatureCommon.cginc:230`（声明）、
`MPostSetting.cs:238`（唯一 C# 引用，属后处理）。

### 旧文件替换关系

| 旧文件 | 旧用途 | 现在由谁取代 |
|---|---|---|
| `Assets/Scripts/ROCharacterLightingEnvironment.cs`（第一轮已删） | 自写的"只写四个全局量"组件 | `Assets/RO/Scripts/MEnvorimentSetting.cs` |
| `Assets/Scripts/ROCharacterLightingData.cs`（第一轮已删） | 自写数据容器 | `Assets/RO/Scripts/MDayTimeData.cs` + `MCreatureLightData.cs` |
| `Assets/Scripts/Editor/RODemoVerificationRunner.cs`（第一轮已删） | 旧验证器：只做参数变体 + 4 个全局量对比，没有原版控制链、没有用户可复现的调亮入口 | `Assets/Scripts/Editor/RODemoFullLightingRunner.cs` |
| `Assets/RO/Scripts/ROTween.cs`（**第二轮删除**） | 第一轮自造的通用插值器（200+ 行 + 驱动对象 + 活动列表 + `TickManually`），顶替 DOTween | 源 **DOTween 程序集**，`MEnvorimentSetting` 恢复 `DOTween.To/.Kill/.OnComplete` 原调用 |

备份位置：第一轮三个旧脚本在 `_temp/ro-character-full-20261028/removed-old-scripts/`；
第二轮的 `ROTween.cs` 在 `_temp/ro-character-full-r2-20261028/removed-r2/`。

---

## 5. 迁入边界：裁掉了什么、为什么（逐项对应源行号）

| 源位置 | 内容 | 处理与原因 |
|---|---|---|
| :16-35, :45-59, :275-282 | `Instance` / `MInterfaceMgr` / `MQualityGradeSetting` 单例与画质分级 | 裁：RO 框架接口注册与全局画质档；`IsQualitySupport ? _reflection : null` 视为恒成立 |
| :62-73 | `lightmapIndex` / `visibilityObjects` / `batchClusters` / `defaultLightmapData` / `defaultWeatherData` | 裁：光照图切换、物件隔离、静态合批、天气 |
| :373-404, :724-737 | `CoroutineSettingPost` / `CoroutineSettingSky` | 裁：后处理与天气天空球协程 |
| :437-448, :537-699, :941-960 | `StoreWeatherData` / 天气分支（雾/云/闪电/风/世界尺寸）/ `ApplyWeatherDataLerp` | 裁：天气链，不写任何角色光全局量 |
| :756-803 | `ObjectVisibility` | 裁：物件显隐（依赖 `MScene`/`MCamera` 状态机） |
| :850-903 | `ApplyLightmapData` | 裁：光照图/探针切换 + 合批 |
| :1027-1049 | `ApplyFogST` / `ApplyCloudST` | 裁：雾/云 UV 变换 |
| :1052-1161 | 闪电（`LightningAtPoint` / `VisionLightning` / FX / 音效） | 裁：特效与音频 |
| :1163-1215 | 合批（`AllowStaticBatch` / `BatchObject`） | 裁：`StaticBatchingUtility` + 资源卸载 |
| :1219-1229, :1241-1284 | `ApplyPostProcess` / 光照图过渡里的曝光色温 | 裁：后处理（`MPostEffectMgr`）。**这同时保证本轮亮度对比不含后处理干扰** |
| :1305-1313, :1370-1431 | 事件监听（`MEventMgr`）、画质变化回调 | 裁：事件框架 |
| :1286-1299 | `ResetGlobalShaderVariable` | **保留**，只复位角色四色/两方向/场景色 |
| — | `_keepSceneColor`（源 :147、:461） | 唯一写入点是天气分支，裁掉后条件恒成立，故删字段并把两处 `if` 展开 |
| — | `scColorReady`（源 :464） | 唯一消费者是天气分支，随之一并删除 |

**没有掏空任何角色光照方法**：`StoreDayTimeData`、`ApplyDayTimeDataLerp`（含全部 11 个全局量写入）、
`InitOrChangeEnviromentSetting` 的 dayTime 分支（立即 + 2 秒 DOTween 两路）、`ApplyEnvorimentData`、`ApplyWorldAmbient`、
`GetShaderID`、`KillLerping`、`ResetGlobalShaderVariable` 都按源结构逐段保留。

---

## 6. 文件清单（两轮合并，含 `.meta`；机读版见 `_temp/ro-character-full-manifest.json`）

**RO 迁入脚本（一类型一文件）**
- `Assets/RO/Scripts/MEnvorimentSetting.cs` + `.meta` —— 环境/角色光控制器（适配迁入，`using DG.Tweening`）
- `Assets/RO/Scripts/MDayTimeData.cs` + `.meta`、`MCreatureLightData.cs` + `.meta`、`MAmbientData.cs` + `.meta` —— 数据类
- `Assets/RO/Scripts/MEnvorimentData.cs` + `.meta` —— 环境数据组（裁剪版）
- `Assets/RO/Config/DayTime/*.asset(+.meta) ×3`、`CreatureLight/*.asset(+.meta) ×3`、`Ambient/RODemo_Ambient.asset(+.meta)` —— 配置资产
- `Assets/RO/Art/Skybox/Hdr01.jpg` + `.meta` —— **第二轮新增：源反射立方图**
- `Assets/Plugins/DOTween/DOTween.dll` + `.meta`、`DOTween.XML` + `.meta` —— **第二轮新增：源 DOTween 程序集**

**Demo 接入与工具**
- `Assets/Scripts/RODemoLightingPanel.cs` + `.meta` —— 操作面板
- `Assets/Scripts/Editor/RODemoLightingPanelEditor.cs` + `.meta` —— 面板按钮 + 全局量只读显示
- `Assets/Scripts/Editor/ROConfigAssetBuilder.cs` + `.meta` —— 按源码数值重建配置资产
- `Assets/Scripts/Editor/RODemoFullLightingRunner.cs` + `.meta` —— 验证/截图/统计（编辑态 + Play 自然插值）

**修改**
- `Assets/RO/Scripts/MEnvorimentSetting.cs` —— 第二轮：`ROTween` → `DOTween.To`，字段类型 `Tweener`
- `Assets/RO/Scripts/MCreatureLightData.cs` —— 第二轮：四个颜色字段加 `[ColorUsage(true,true)]`，`reflectMap` 注释更正为 `unity_SpecCube0/indirectSpecular`
- `Assets/RO/Scripts/MAmbientData.cs` —— 第二轮：`ambientIntensity` 注释改为官方语义（只对 Skybox 生效）
- `Assets/RO/Config/CreatureLight/*.asset ×3` —— 第二轮：`reflectMap` 指向 `Hdr01`
- `Assets/RO/Config/Ambient/RODemo_Ambient.asset` —— 第二轮：`reflectionMode` Skybox → Custom
- `Assets/Scripts/Editor/RODemoSceneBuilder.cs`、`Assets/Scenes/ROCharacterLightingDemo.unity`、`Assets/Materials/DemoGround.mat` —— 第一轮改动（删对照灯、提亮地面）
- `README.md` —— 本文件

**删除**
- 第一轮：`ROCharacterLightingEnvironment.cs`(+meta)、`ROCharacterLightingData.cs`(+meta)、`Editor/RODemoVerificationRunner.cs`(+meta)
- 第二轮：`Assets/RO/Scripts/ROTween.cs`(+meta) → `_temp/ro-character-full-r2-20261028/removed-r2/`

**用户编辑快照（未覆盖原场景）**：`Assets/Scenes/ReservedUserEditSnapshot/ROCharacterLightingDemo_user-edit-20261028-0028.unity`

---

## 7. 重新生成 / 复跑

菜单：
- `RO/重建角色光照配置资产` —— 按源码数值重建 7 个配置资产
- `RO/构建角色光照演示场景` —— 重建场景（会覆盖 `Assets/Scenes/ROCharacterLightingDemo.unity`）
- `RO/运行完整光照验证（编辑态）` —— 出 §3.1~3.4 的截图与 `_temp/ro-character-full-verify-r2.json`

代码入口（编辑器或 MCP 里调用）：

```csharp
ROConfigAssetBuilder.Build();              // 配置资产
RODemoSceneBuilder.Build();                // 场景
RODemoFullLightingRunner.RunEditMode();    // 编辑态取证（立即应用类；不含插值）
RODemoFullLightingRunner.RunPlayMode();    // Play 态：自然 2 秒插值采样 + 终点比对
```

---

## 8. 未验证 / 边界（如实保留）

- **只覆盖 `RO/Creature/Player` 的 LOD 600 SubShader**（低档 SubShader、`RO/Creature/Monster`、UI 试衣的 `_ROUIC*` 路径未跑）。
- **反射链已接回但有两点现状**：①`reflectMap` 只接入了 pulongdela 白天/黄昏/夜晚三套 `cl_*`；源里另外 140 套 `cl_*` 的 `reflectMap` 未逐一核对是否都指向 `Hdr01`；
  ②立方图按源 meta 导入为 **DXT1、`cubemapConvolution: 0`**，与源导入设置一致，但未验证在高光极强材质上的差异。
- **`RODemo_Ambient` 的 `ambientMode` 用 Flat（源 `gi_*` 是 Trilight）**：这是 Demo 取景简化，已在 §2.4 用实测标明它只影响普通材质、不影响角色。
- **模型是静态 bandpose**：源 SMR 没有骨骼（骨骼由 RO 运行时注入），仍用同一 Mesh + 同一材质渲染，未做动画。
- **未跑 RO 的材质合并链**（`MModelCombineMesh` 等），本工程直接使用源 bandpose 材质。
- **天气/后处理/光照图/合批/物件隔离**未迁（见 §5），因此"时段切换"在本 Demo 只体现角色光与场景色，不体现雾、天空球、曝光变化。
- **编辑态不支持插值**（§3.6）；Play 态的自然推进已实测到位。
- **第一轮的三张整帧图（`10/11/12-fullframe-*.png`）在本轮重跑时被同名覆盖**，已改名为 `-r2`；
  第一轮那三张的原始字节不再可得（它们是观感图，不是测量依据；测量数据仍在 `_temp/ro-character-full-verify.json`）。
