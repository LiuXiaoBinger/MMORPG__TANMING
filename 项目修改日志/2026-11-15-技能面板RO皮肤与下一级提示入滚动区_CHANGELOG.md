# 技能面板 RO 皮肤统一 +「下一级」提示收进描述滚动区

- 日期：2026-11-15
- 分支：`codex/feature-development`
- 需求来源：用户指出 `Txt_NextLevel` 控件应放入项目的滚动条中，并要求把技能面板设计成 RO 项目 UI 风格。
- 方向确认（本轮交互选择）：**保留现有全屏武学布局、所有节点坐标与业务结构，只换 RO 皮肤**；改动范围 **整个技能面板**。

## 一、问题定位

1. 第九轮（`2026-11-14-技能面板一梦江湖布局重构_CHANGELOG.md` 第 15 节）按一梦江湖实机把描述做成 `DetailPanel/DescScroll`（`ScrollRect + Viewport(RectMask2D) + Content(VerticalLayoutGroup + ContentSizeFitter)`），Content 里收了 `Txt_Sec1 / Txt_Desc / Divider_2 / Txt_Sec2 / Txt_Cond` 五个对象，右侧 `DescScrollbar`。
2. 但参考实机需求第 3 条明确写「长文本（白色叙述 / 橙色数值 / 青蓝词条 / **绿色下一级**）需要滚动查看」，实现却把绿色「下一级(角色N级可提升)」做成 `NormalUpgradeRoot` 下的固定独立控件 `Txt_NextLevel`，不随描述滚动 —— 这是本轮要纠正的错位。
3. 面板大部分视觉元素已使用 RO 图集 `Assets/artres/Resources/UI/Atlas/Common1.png`（guid `c46212db0fcbdd547a2a319e0ab42aba`）的九宫格：外框/详情卡片 `UI_Common_Bg_Normal_04`、页签与竖排导航 `UI_Common_Bg_20Fillet_01`、标题徽记 `UI_Common_Panel_Head_01` 等。仍为纯色 `Image`（`m_Sprite: {fileID: 0}`）的是第九轮新增的升级区、描述滚动条，以及列表滚动条、铜币按钮、标题分隔线等共 12 处，视觉语言不统一。

## 二、文件清单（新增 0、修改 3、删除 0）

| 文件 | 用途与具体变化 |
|---|---|
| `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | ① `DetailPanel/NormalUpgradeRoot/Txt_NextLevel` 整体移到 `DetailPanel/DescScroll/Viewport/Content` 末尾，成为文档流第 6 个子对象；RectTransform 改 `anchorMin=(0,1)`、`anchorMax=(1,1)`、`pivot=(0.5,1)`、`anchoredPosition=(0,0)`、`sizeDelta=(0,30)`，`m_Father` 指向 Content。② 12 处纯色 Image 换成 RO 图集九宫格并设 `m_Type=1(Sliced)`，同时统一蓝银/青蓝配色（清单见第三节）。③ **未增删任何 GameObject、组件或文档**，文档数 7606 保持不变。 |
| `LYF_MMO/Assets/GM_Game/Editor/SkillUpgradeUIBuilder.cs` | ① 新增 `RoAtlasPath` 与 5 个 RO 配色常量、`RoSprites` 缓存。② `Build()` 增加 `LoadRoSprites / RescueNextLevel / ApplyRoSkin` 三步，并把原有的三目取值改为 if/else。③ `BuildScroll()` 的 `saved[]` 增加 `Txt_NextLevel`，并在 Content 文档流末尾 `MoveInto` 该控件。④ `BuildNormalUpgrade()` 删除 `Txt_NextLevel` 的创建（改由滚动区持有）。⑤ 新增 RO 皮肤 region：`LoadRoSprites`、`RescueNextLevel`、`ApplyRoSkin`、`FindImage`、`ApplyRoSprite`、`TintImage`。 |
| `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SkillInfoWindow.cs` | ① `_normalUpgradeRoot` / `_nextLevelText` 字段注释改为滚动区语义。② `EnsureUpgradeRefs()` 中 `_nextLevelText` 的查找路径由 `ChildText(normal, "Txt_NextLevel")` 改为 `ChildText(detail, "DescScroll/Viewport/Content/Txt_NextLevel")`。③ 新增私有方法 `SetNextLevelActive(bool)`；`RefreshUpgradeBlock()` 在 `skill == null` 时隐藏该行、在绝学页隐藏该行、普通技能页显示该行 —— 因为控件父节点已不再是随之显隐的 `NormalUpgradeRoot`。 |

另外本轮在 `_temp/ui-ro-skin-20261115/` 生成只读分析、落盘与校验脚本及证据（`inspect_panel.py`、`apply_skin.py`、`verify_skin.py`、`panel_structure.json`、`apply_skin_report.json`、`verify_result.json`、`sprite_ids.json`、`UIRoot.prefab.before_ro_skin.bak`），按项目规则属临时产物，不入库。

## 三、RO 皮肤逐项清单（12 处）

| 控件路径（相对 `SkillInfoWindow`） | RO 九宫格 | 配色 |
|---|---|---|
| `DetailPanel/DescScrollbar` | `UI_Common_Bg_progress_01` | (0.22, 0.34, 0.50, 0.55) |
| `DetailPanel/DescScrollbar/Sliding Area/Handle` | `UI_Common_Btn_Scrollbar_01` | (0.72, 0.86, 1, 0.95) |
| `DetailPanel/NormalUpgradeRoot/Btn_Upgrade` | `UI_Common_Btn_Normal_01` | (0.42, 0.62, 0.88, 1) |
| `DetailPanel/UltimateUpgradeRoot/Btn_ExpItem` | `UI_Common_Btn_Medium_01` | (0.30, 0.48, 0.72, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpBarBg` | `UI_Common_Bg_progress_01` | (0.18, 0.26, 0.38, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpBarBg/ExpBarFill` | 保持无 sprite（宽度由 `anchorMax` 控制，避免九宫格压缩变形） | 改 (0.42, 0.72, 0.95, 1) |
| `DetailPanel/UltimateUpgradeRoot/Btn_UltimateUpgrade` | `UI_Common_Btn_Normal_01` | (0.42, 0.62, 0.88, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpItemHint` | `UI_Common_Bg_Tips_01` | (0.09, 0.13, 0.20, 0.97) |
| `CurrencyBar/Btn_UpgradeAll` | `UI_Common_Btn_Medium_01` | (0.36, 0.56, 0.82, 1) |
| `ListRoot/ScrollView/Scrollbar` | `UI_Common_Bg_progress_01` | (0.22, 0.34, 0.50, 0.35) |
| `ListRoot/ScrollView/Scrollbar/Sliding Area/Handle` | `UI_Common_Btn_Scrollbar_01` | (0.72, 0.86, 1, 0.85) |
| `TitleLine` | `UI_Common_Bg_qujianjianbian_01` | (0.62, 0.72, 0.92, 0.55) |

保持不变：`BG_Inner` 与 `RoTopBar` 仍为纯色不透明底（前者用于盖住 3D 场景，后者是第八轮为避免 360×78 云纹条被拉到 1920 宽失真而刻意去掉 sprite 的结论）；标题、页签、竖排导航、环形节点、详情卡片、列表条目等已使用 RO 图集的元素不动；所有坐标、锚点、层级与业务代码结构不动。

## 四、设计思路

- 控件迁移复用项目既有的 `DescScroll/Viewport/Content` 文档流（`VerticalLayoutGroup` + `ContentSizeFitter`），不需要新建滚动容器；Content 由 5 个子对象变 6 个，内容高度随 TMP 首选高度自动增加，仍超过 460 视口高度，因此保持可滚动。「下一级」作为文档流最后一行，符合参考实机。
- 编辑器构建器与落盘 prefab 保持严格一致：构建器可重复执行，执行前 `RescueNextLevel` 会把控件从 `NormalUpgradeRoot`（旧位置）或 `DescScroll/Viewport/Content`（新位置）救回 `DetailPanel`，避免随旧滚动区或升级区一起被销毁；`BuildScroll` 重新收进 Content；`BuildNormalUpgrade` 不再创建该控件。
- 皮肤统一走 `Common1.png` 按 sprite 名加载，构建器运行时用 `AssetDatabase.LoadAllAssetsAtPath` 缓存，避免手改 YAML 的 fileID；落盘则用按对象名/路径定位的精确行替换，只改 `m_Sprite` / `m_Color` / `m_Type` 与一对父子引用。

## 五、验证

| 项目 | 结果 |
|---|---|
| prefab 结构 | 文档数 `7606 → 7606`、GameObject `1872 → 1872`、技能面板节点 `210 → 210`，无增删；`git diff --numstat` 仅 `41 / 41` 行 |
| 引用完整性 | fileID 无重复；本地 `{fileID: X}` 引用 0 处断链；`Txt_NextLevel` 的 `m_Father` 指向 Content，`Content.m_Children` 6 项、`NormalUpgradeRoot.m_Children` 3 项 |
| 字段核对 | 12 处皮肤的 `m_Sprite` / `m_Color` / `m_Type` 逐项比对全部命中；迁移后 anchor/pivot/sizeDelta 符合文档流要求 |
| 自动校验 | `_temp/ui-ro-skin-20261115/verify_skin.py`：`checks=20 failed=0` |
| C# 静态检查 | 两个 .cs 花括号/圆括号均配平；新增代码无 `foreach`、无命名空间、无三目运算符、无 `interface`、无超过 3 项的布尔表达式；`SkillUpgradeUIBuilder.cs` 原有三目 `font` 取值已改 if/else |
| 编辑器编译 | Unity 编辑器当前在运行，`Editor.log` 近期无 `error CS`；**本轮未主动触发 Unity 重新编译，也未在 Play 模式实拍** |

## 六、未验证 / 遗留

- **未做 Play 模式真实渲染截图**：滚动到底部时「下一级」行是否随文档流出现、12 处九宫格在窄滚动条（6px / 4px）上的 Sliced 观感、列表页与轻功页/秘笈页皮肤，均需 Unity 内实测。
- Unity 编辑器当前开着：磁盘 prefab 已更新，需让编辑器刷新资源后查看；若正处于 Play 模式，磁盘资产不会同步到当前场景实例，需退出 Play 再进。也可在 Unity 菜单 `Tools/Skill/Build Upgrade UI` 重建一次，验证构建器与落盘结果一致。
- 历史遗留不变：`Assets/GM_Game/Dlls/GM_Game.dll.bytes` 未重建（打包真机前必须跑 `Build/BuildDll`）；多分辨率按 1920×1080 绝对像素写死；升级/心得仍是本地 UI 模拟未接协议；「背景小青点」未消除。
- 本轮未改动脚本之外的 `.meta`，未新增或删除任何文件。

# 返修（2026-11-15 14:3x）：整体换成 RO 淡蓝不透明底

## R1 问题与根因

用户反馈「风格没换啊 你还是这种深蓝背景嘛」。上一轮只把 12 处纯色控件换成 RO 图集九宫格，**没有改主背景**：`BG_Inner` 仍为 `(0.045, 0.06, 0.09, 1)` 深蓝近黑、`RoTopBar` `(0.055, 0.075, 0.11, 0.94)`、`DetailPanel` `(0.09, 0.12, 0.17, 0.97)`，全屏观感与改前一致。换贴图不等于换风格，主色板才决定观感。

## R2 采用的 RO 基准（项目内已批准的既有约定，非新发明）

| 来源 | 结论 |
|---|---|
| `项目修改日志/2026-11-09-装备详情不透明布局与基因面板归位.md` | 装备详情「背景指定 RO 风格淡蓝不透明」「不透明浅米白底与深色正文，重点用金色」；prefab 实测 `EquipmentDetailWindow` `(0.90,0.96,1)`、`DetailBackground` `(0.965,0.982,1)`、按钮 `(0.80,0.89,1)` + 深蓝字 `(0.08,0.21,0.35)` |
| `项目修改日志/2026-11-12-角色属性页裁切与蓝白配色修复.md` | 「选中为蓝底白字，未选中为浅蓝底深蓝字。基础和高级正文使用浅蓝白底与深蓝文字」 |

因此 RO 风格 = **淡蓝白不透明底 + 深蓝正文 + 蓝银按钮/描边 + 深金/深橙重点**，而不是技能面板原先的「深蓝底 + 白字 + 亮金」。

## R3 本轮改动（仍为 0 新增 / 3 修改 / 0 删除）

| 文件 | 变化 |
|---|---|
| `UIRoot.prefab` | 精确替换 **87 处 Image `m_Color` + 59 处 TMP `m_fontColor`**：全屏底 `BG_Inner` → `(0.90,0.95,1,1)`、顶栏 `RoTopBar` → `(0.36,0.56,0.82,1)`、详情卡片 → `(0.965,0.982,1,1)`、外框 → 蓝银；页签/导航改为「选中蓝底白字 `(0.28,0.50,0.78)`、未选中浅蓝底深蓝字 `(0.80,0.89,1)`」；节点内圆/遮罩/列表条目/空壳槽位提亮；所有正文改深蓝 `(0.12,0.28,0.46)`、次要 `(0.35,0.47,0.60)`、章节蓝、数值深金、`下一级` 深绿。**未增删任何 GameObject/组件/文档**，文档数仍 7606；上轮 12 处 RO 九宫格 sprite 引用全部保留，只把颜色同步到浅色系。3 处 `LightRoot/LightNode_*/Tag` 无此节点，属正常跳过。 |
| `SkillInfoWindow.cs` | 14 个颜色常量改为 RO 淡蓝体系；`RefreshNav` 里 5 处硬编码深蓝/金字改为复用常量；节点标签与富文本色改浅底可读（`#FFB46B`→`#B26A18`、`#7FD2F0`→`#1B6FA8`、`#6BE39A`→`#1E8A4C`）；新增 `NavDisabledTextColor`、`NodeTagNormal`、`NodeTagUltimate` 三个具名常量。 |
| `SkillUpgradeUIBuilder.cs` | 新增 `ApplyRoPalette(window, detail)` 与 `SetImageColor` / `SetTextColor`，让 `Tools/Skill/Build Upgrade UI` 重建后仍保持同一套淡蓝配色；`ApplyRoSkin` 的 12 处颜色同步为浅色系；删除上轮 4 个已不再使用的常量；既有 `FindText` 的三目改 if/else。 |

`_temp/ui-ro-skin-20261115/` 新增 `apply_light_palette.py`、`verify_light_palette.py`、`light_palette_report.json`、`verify_light_result.json`、`UIRoot.prefab.before_light_palette.bak`，均为临时产物，不入库。上轮的 `verify_skin.py` 期望的是旧深蓝配色，已被 `verify_light_palette.py` 取代。

## R4 验证

| 项目 | 结果 |
|---|---|
| 自动校验 | `_temp/ui-ro-skin-20261115/verify_light_palette.py`：`checks=162 failed=0`；逐项比对 87 处底色与 59 处字色，另含 GameObject 集合不变、fileID 无重复、本地引用 0 断链、`Txt_NextLevel` 仍在滚动 Content、RO 图集 sprite 引用仍 ≥30、代码无旧亮色常量 |
| 关键事实 | `BG_Inner` 由 `(0.045,0.06,0.09)` → `(0.90,0.95,1)`；`DetailPanel` → `(0.965,0.982,1)`；`Txt_Name` 字色 → `(0.12,0.28,0.46)` |
| C# 静态 | 两个 .cs 花括号/圆括号配平；构建器无 `foreach`、无命名空间、无三目；未新增 `interface` |
| 未验证 | **未做 Play 模式实拍**：深色立绘 `MJS_Portrait` 在淡蓝底上的融合、`PortraitHalo` 深蓝圆斑是否足够、全屏浅色底与 HUD/其它窗口的对比、页签/导航运行时 `RefreshTabs`/`RefreshNav` 覆盖后的最终观感，均需 Unity 内实测 |

## R5 遗留

- 若要改成 RO 参考工程的米白/羊皮纸色（`RoleAttriibuteWindow/ActiveGenesPanel` 为 `(0.97,0.95,0.9)`），只需替换 `BG_Inner` / `DetailPanel` / `RoCardBg` 三个色值，结构与代码不用动。
- 历史遗留不变：`GM_Game.dll.bytes` 打包前需 `Build/BuildDll`；多分辨率坐标写死；升级/心得仍为本地模拟。

## 七、Git 回执

- 分支 `codex/feature-development`，主提交 **`1a6b036`** `style: 武学面板统一 RO 皮肤并把「下一级」收进描述滚动区`（4 files changed, 293 insertions(+), 50 deletions(-)）。暂存清单只含本轮 3 个业务文件与本 CHANGELOG，未纳入工作区中其它任务的既有改动。
- **推送成功**：`776ab0f..1a6b036 codex/feature-development -> codex/feature-development`（`github.com:LiuXiaoBinger/MMORPG__TANMING.git`），`git status -sb` 无 ahead/behind。
- 本段回执为推送后补记，单独以 `docs` 提交入库（`b777b74`）；不改动任何业务文件。

### 返修回执（R1~R5）

- 主提交 **`17d59dd`** `fix: 武学面板整体换成 RO 淡蓝不透明底（返修深蓝背景）`（4 files changed, 459 insertions(+), 199 deletions(-)）。暂存清单只含本轮 3 个业务文件与本 CHANGELOG。
- **推送成功**：`b777b74..17d59dd codex/feature-development -> codex/feature-development`，`git status -sb` 无 ahead/behind。
- 本段回执同样为推送后补记，单独以 `docs` 提交入库，提交号见 `git log`；不改动任何业务文件。
