# 2026-10-25 角色面部/材质"发白、没层次"修复

## 用户反馈
> 人物面部在场景中很怪，在其他场景/工程中人物面部和材质和在我游戏中不一样，我游戏中很丑。

## 排查过程与结论

### 1. 先排除"不是同一套材质"
- Unity 实测 `Role_JX.prefab` 的 4 个 UTS 渲染器（P03_Body / Cloth_A_Legs / Cloth_A_Spine / Hair_A）挂的就是 `materials/P03_*.mat` 这 7 个文件，shader 名 `Toon (Built-in)`（`LegacyToon.shader`）。
- 工程内没有运行时替换材质的代码（`GhostEffect` 只管残影），也没走 AssetBundle 加载角色。→ **编辑器看到的就是游戏里运行的**。

### 2. 根因 A：法线贴图被当成普通贴图导入（确定的 bug）
- `P03_Skin_Normal.png.meta`：`textureType: 0`（Default）、`sRGBTexture: 1` → 运行时 **DXT1**。
- 对照同目录 `P03_cloth_A_normal.png`：`textureType: 1`（NormalMap）、`sRGBTexture: 0` → **DXT5**，是正确的。
- 该贴图内容确认是标准法线图（均值 RGB ≈ 128,128,255）。
- 后果：桌面端 `UnpackNormal` 走 DXT5nm 通道（A/G），DXT1 无 alpha → 法线数据错误。实测开关法线影响约 11 级灰度，说明它确实参与运算。
- **修复**：`textureType` 改为 NormalMap、关闭 sRGB → 重新导入后为 DXT5。

### 3. 根因 B：高光加色项（HighColor）在没有遮罩的情况下全局生效（发白的直接原因）
- 着色器 `UCTS_ShadingGradeMap.cginc:190/192`：
  - `_HighColor_var = _HighColor_Tex.rgb * _HighColor.rgb * _Set_HighColorMask.g`
  - `Set_HighColor = Set_FinalBaseColor + _HighColor_var`
- 7 个材质的 `_Set_HighColorMask` **全部为 null**（默认白），且 `_HighColor_Tex` 就是各自的颜色贴图。
- 于是每个材质都被叠加了"自身颜色 × `_HighColor`"的加色：
  | 材质 | 原 `_HighColor` | 实际效果 |
  |---|---|---|
  | P03_skin | 0.151 | 皮肤整体 +15% 亮度 → 冲白 |
  | P03_hair_A | 0.274 | 头发 +27% |
  | P03_Cloth_A | 0.349 | 衣服 +35% |
  | P03_Metal_A | 0.198 | — |
  | P03_eye | 0.689 | 眼白/虹膜被冲淡 |
  | P03_Cheek | 0.283 | 腮红被冲淡 |
  | P03_Brows | 0.528 | 眉毛被冲淡 |
- **修复**：7 个材质 `_HighColor` → (0,0,0)，即关闭全局加色，恢复"底色 + 明暗两段"的正常输出。

### 4. 根因 C：皮肤的明暗两段几乎等于白色（"很怪/平板"）
- 原 `_1st_ShadeColor (0.943,0.903,0.903)`、`_2nd_ShadeColor (0.906,0.833,0.838)`，等于没有阴影。
- **修复**：改为 `_1st_ShadeColor (0.886,0.784,0.780)`、`_2nd_ShadeColor (0.749,0.596,0.612)`，让脸上有真实的明暗分层与血色。

### 5. 顺带确认（未修改）
- 项目色彩空间是 **Gamma**（`m_ActiveColorSpace: 0`）。UTS2 原生按 Linear 设计，改 Linear 影响全项目（UI/粒子/后处理），风险大，未动。
- `_MatCap_Sampler` 引用的贴图 guid `7421613529468b84090f1a364c929ea8` **在工程内已丢失**；但 `P03_Skin_MatMask` 的脸部遮罩为 0，实测开关 matcap 像素差 ≤1，无实际影响。
- 这份 `LegacyToon.shader` 是 **裁剪版 UTS2**：只有 `_SHADINGGRADEMAP / _IS_CLIPPING_* / _IS_ANGELRING_* / _EMISSIVE_* / _OUTLINE_*` 有 shader_feature；`_MATCAP`、`_RIMLIGHT`、`_NORMALMAP` 没有关键字，改材质里对应 float 不会切变体。

## 改动文件（8 个）
```
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_skin.mat        HighColor→0；1st/2nd Shade 加深
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_hair_A.mat      HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_eye.mat         HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Brows.mat       HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Cheek.mat       HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Cloth_A.mat     HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/P03_Metal_A.mat     HighColor→0
LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/textures/P03_Skin_Normal.png.meta  textureType→NormalMap, sRGB→0
```

## 验证（隔离场景渲染，600×600，暖白主光 0.85）
| 状态 | 脸颊 RGB | 观感 |
|---|---|---|
| 修改前 | (221,167,161) | 整体冲白、无明暗层次 |
| 修改后 | 肤色偏暖、脸颊/下颌/颈部可见明暗分层 | 见 `_temp/final_face.png` |

## 备份与回滚
- 材质备份：`_temp/mat_backup_before_highcolor_fix_20261025/`（7 个 .mat）
- 一键回滚：`git checkout -- LYF_MMO/Assets/GM_Game/Models/Role_JX/materials/`

## 未验证 / 遗留
- 未在运行时（Play 模式）截图验证，采的是"场景真实相机 + 临时实例化同一预制体"的隔离渲染。
- 主城 `Scene_MainCity` 的方向光是黄绿色 (0.985,1,0.764)、强度 1、无阴影；角色在城里肤色是否偏绿未单独取色验证。
- 若觉得皮肤"太暖/太深"，把 `P03_skin.mat` 的 `_1st_ShadeColor/_2nd_ShadeColor` 调回 (0.94,0.90,0.90)/(0.91,0.83,0.84) 即可回到偏白版。
- NPC（QuQu）走另一套 `Toon_DoubleShadeWithFeather_Mobile.shader`，本次未处理。
