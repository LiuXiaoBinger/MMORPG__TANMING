# 2026-10-25 角色面部灯光优化（脸黑修复）

## 背景

用户反馈：捏人场景和主城场景里角色面部都偏暗，参考其他游戏希望角色面部始终有光；且场景主光是方向光，角色一旦背光面部又变黑。

## 根因（已取证）

排查方式：Unity MCP 读取运行时渲染设置 + 场景/材质/预制体 YAML 静态分析 + 隔离场景对照渲染（用同一 `Role_JX.prefab`，只改光照条件）。

1. **捏人场景实际零灯光**：`Scene_CreateRole.unity` 场景文件里没有 Light 组件；唯一带灯的 `CreateRole_Models.prefab`（12 盏点光 + 1 盏方向光）在该场景被整体关闭——`Scene_CreateRole.unity` 第 1155-1157 行 `m_IsActive: 0`（target `3173776425578160273`，即该预制体根）。场景相机也处于未激活状态（运行时启用）。
2. **角色材质不吃环境光**：皮肤/头发/眼睛/服装等 `P03_*.mat` 使用 UTS2 卡通着色器 `Assets/GM_Game/Models/Role_JX/Shaders/LegacyToon.shader`（`_utsVersion 2.075`，走 `_SHADINGGRADEMAP` 分支），参数 `_GI_Intensity: 0`，即着色器最终色里环境光（`UCTS_ShadingGradeMap.cginc:325`）贡献为 0，只认主方向光。
3. 两者叠加：无主光时 UTS 回落到 `defaultLightColor ≈ max(0.05, ShadeSH9) ≈ 0.2`，角色整体只剩约 20% 亮度 → 面部发暗。

量化（560×560 渲染，头部区域平均亮度，满分 255）：

| 条件 | 头部平均亮度 |
|---|---|
| 捏人场景原状（无灯，GI=0） | 120.7（隔离测试无灯态 91.3，肤色像素占比 0%） |
| 正面受光（基准） | 179.8 |
| 背光 + 原材质 GI=0 | 144.3 |
| 背光 + GI=0.7 | 175.3 |
| 背光 + GI=0.7 + `_Is_BLD` 固定受光方向 | 193.0（略过曝，且需额外维护方向参数，未采用） |

## 修改内容

### 1. 角色材质：`_GI_Intensity` 0 → 0.7（7 个）

- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_skin.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_hair_A.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_eye.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Brows.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Cheek.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Cloth_A.mat`
- `LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Metal_A.mat`

作用：环境光按 0.7 系数补进最终色，背光面不再死黑。材质通过 `Role_JX_fbx_1.fbx.meta` 的材质映射（第 41 行）绑定到模型，改材质即全角色生效，无需逐个预制体改。

### 2. 捏人场景补一盏主方向光

`LYF_MMO/Assets/GM_Game/Scenes/Scene_CreateRole.unity` 新增 `CreateRole_KeyLight`（根节点，无父级）：

| 参数 | 值 |
|---|---|
| 类型 | Directional（m_Type: 1） |
| 颜色 | (1, 0.96, 0.90) 暖白 |
| 强度 | 0.85 |
| 世界朝向 | euler (17.6, 265.5, 0)，即按场景捏人相机朝向再叠加 (18°, 25°) 偏移 |
| 阴影 | 关闭（None），与主城方向光一致 |
| 位置 | (0,0,0)（方向光位置无意义） |

只新增 1 个 GameObject + 1 个 Light 组件，未改动任何既有对象或预制体实例的覆盖记录（`m_RemovedComponents` 保持 18 条不变）。

## 验证

- 材质导入后读回：7 个材质 `_GI_Intensity = 0.7`、`_Is_BLD = 0`（未开固定受光方向）。
- 捏人场景内用临时角色（渲染后立即删除，未保存进场景）做前后对照：头部区域亮度 120.7 → 170.8（强度 0.85 档），过曝像素（>235）占比 19.2%；1.05 档为 178.3 / 31.3%，偏白偏平，故取 0.85。
- 场景文件 diff 复核：+104 / −10 行，主体为新增 GameObject/Light/Transform 三段文档，两处 `stripped` Transform 仅顺序变化；`Light:` 块数 = 1。

## 未验证 / 风险

- **未在真实运行时场景截图**：`Scene_CreateRole` 的角色与相机均由运行时代码启用/生成，编辑器态下场景内无角色，故用「临时实例化同一预制体 + 场景真实相机」的方式验证；主城 `Scene_MainCity` 同样未做运行时截图，主城效果仅由「同预制体 + 隔离场景背光测试」推断（背光 144.3 → 175.3）。
- 材质改动对**所有使用该材质的角色**生效（含其他玩家/NPC 若共用该材质）；实测亮度提升约 20%，若亮场景下偏白可回调到 0.5~0.6。
- 未开启 `_Is_BLD`（固定受光方向）：实测收益有限且会让角色不再响应场景太阳方向，保留为可选项。
- NPC `QuQu` 使用另一套着色器 `Toon_DoubleShadeWithFeather_Mobile.shader`，本次未处理。

## 回滚

- 材质备份：`_temp/lighting_backup_20261025/`（7 个 .mat 改动前副本）。
- 场景回滚：`git checkout -- LYF_MMO/Assets/GM_Game/Scenes/Scene_CreateRole.unity`（新增的灯在本次提交中，剔除后即恢复原状）。

## 附带说明（过程记录）

- 排查过程中一次误操作：用 `FindObjectsOfType<Light>(true)` 清场时把场景内未激活的预置灯（预制体内部 100+ 盏）一并删除并保存，已通过 `git checkout` 完整恢复后重做，最终 diff 已复核无残留删除。
- 临时脚本与对照图：`_temp/inspect_prefab_lights.py`、`_temp/bld_*.png`、`_temp/cr_face_*.png`。
